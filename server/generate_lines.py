"""
정화봇 고정 멘트 일괄 생성 스크립트

게임 중 반복되는 멘트 54개를 미리 wav로 뽑아 Unity에 넣기 위한 도구.
사전 생성이므로 게임 실행 중에는 API 호출이 없어 지연이 0이다.

실행:
    python generate_lines.py

결과:
    lines_output/ 폴더에 [카테고리]_[번호].wav 파일들이 생성됨
    함께 만들어지는 lines_manifest.json에 "파일명 ↔ 대사" 매핑이 기록되므로
    Unity에서 어떤 파일이 어떤 상황용인지 바로 확인할 수 있다.

⚠️ 공모전 제출 관련:
    무료 플랜으로 생성한 음성은 출처 표기가 필수다.
    유료(라이트) 플랜으로 전환한 뒤 이 스크립트를 다시 돌리면
    출처 표기 없이 사용할 수 있는 파일로 전부 교체된다.
"""

import os
import json
import time
from dotenv import load_dotenv

load_dotenv()

API_KEY = os.getenv("TYPECAST_API_KEY", "")
VOICE_ID = os.getenv("TYPECAST_VOICE_ID", "")
MODEL = os.getenv("TYPECAST_MODEL", "ssfm-v30")

OUTPUT_DIR = "lines_output"
MANIFEST_PATH = os.path.join(OUTPUT_DIR, "lines_manifest.json")

# 요청 사이 간격(초). 무료 플랜은 동시 호출 한도가 2라 여유를 둔다.
REQUEST_DELAY = 0.5

# 음량 정규화 기준(LUFS). 모든 파일을 같은 크기로 맞춰서 어떤 대사는 크고
# 어떤 대사는 작게 들리는 문제를 막는다. -14는 일반적인 게임/영상 기준값.
TARGET_LUFS = -14.0

# 모든 대사에 같은 감정 프리셋을 쓴다.
#
# 프리셋마다 음높이가 달라서(happy는 톤이 올라가고 toneup/tonedown은 이름 그대로
# 음높이를 바꾼다) 섞어 쓰면 파일마다 목소리가 높았다 낮았다 한다.
# 같은 캐릭터가 다른 사람처럼 들리면 몰입이 깨지므로 하나로 통일했다.
#
# 톤을 바꿔보고 싶으면 이 값만 고치면 전체가 한 번에 바뀐다.
#   normal   — 기본. 가장 차분하고 톤이 낮다 (현재 설정)
#   happy    — 밝지만 음높이가 올라간다
#   tonedown — 더 낮게. normal도 높게 느껴지면 시도해볼 것
EMOTION = "normal"


# ─────────────────────────────────────────────
# 닉네임 목록 (UI에서 아이가 골라서 선택)
#
# 12개뿐이라 닉네임이 들어가는 대사도 전부 사전 생성이 가능하다.
# 실시간 TTS로 처리하면 매번 3~5초 대기가 생기고 구독이 끝나면 아예 안 되는데,
# 미리 뽑아두면 지연 0이고 구독 해지 후에도 계속 쓸 수 있다.
#
# 호격조사(야/아) 문제를 피하려고 모든 대사를 "{닉네임}, ~" 형태(쉼표)로 썼다.
# ("슈퍼정화러야" / "똥대장아" 처럼 받침에 따라 달라지는 걸 신경 쓸 필요가 없음)
# ─────────────────────────────────────────────
NICKNAMES = [
    "똥대장", "물방울", "하수맨", "건강똥맨",
    "똥글똥글", "버블버블", "슈퍼정화러", "뿌지직맨",
    "일급수요정", "뿡뿡이", "똥박사", "슈퍼똥",
]

# 닉네임이 {name} 자리에 들어가는 대사들.
# 닉네임 12개 × 3개 대사 = 36개 파일이 추가로 생성된다.
NICKNAME_LINE_SETS = [
    {"key": "intro_greeting", "emotion": EMOTION, "template":
        "안녕! 나는 정화봇이야. {name}, 반가워! "
        "너 지금 똥이지? 걱정 마. 하수처리장을 거치면 깨끗한 물이 될 수 있어. 내가 옆에서 다 알려줄게!"},

    {"key": "ending_transform", "emotion": EMOTION, "template":
        "해냈다! {name}, 네 몸을 봐봐. 완전 맑아졌어! "
        "처음엔 똥이었는데, 이제 진짜 깨끗한 물이 된 거야. 이제 한강으로 나갈 시간이야!"},

    {"key": "ending_farewell", "emotion": EMOTION, "template":
        "이제 넌 한강으로 흘러가. 그러다 구름이 되고, 비가 되고, 또 어딘가에서 다시 만날 거야. "
        "물은 계속 돌고 도니까! 그때 또 보자, {name}!"},
]


# ─────────────────────────────────────────────
# 대사 목록
#   key = 파일명 접두사 (Unity에서 이 이름으로 찾는다)
#   emotion = ssfm-v30 지원 프리셋
#             normal / happy / sad / angry / whisper / toneup / tonedown
#   lines = 그 상황에서 랜덤으로 재생할 대사들
# ─────────────────────────────────────────────
LINE_SETS = [
    # ── 게임 시작 ──
    {"key": "intro_calibration_start", "emotion": EMOTION, "lines": [
        "출발 전에 널 잠깐 스캔할게. 똑바로 서서 기다려줄래?",
    ]},
    {"key": "intro_calibration_done", "emotion": EMOTION, "lines": [
        "스캔 완료! 나는 여기 있을게. 자, 모험을 떠나볼까?",
    ]},

    # ── 엔딩씬 2: 재등장 직후, 피드백 보드 뜨기 전 ──
    {"key": "ending_reveal_intro", "emotion": EMOTION, "lines": [
        "이제 진짜 깨끗한 물이 되었네. 얼마나 깨끗해졌는지 볼까?",
    ]},

    # ── 지도 씬: 교육 멘트 ──
    {"key": "map_stage1", "emotion": EMOTION, "lines": [
        "우리는 지금 하수관으로 들어왔어! 하수관은 도시 밑에 거미줄처럼 깔려 있는 길이야. "
        "여기를 타고 처리장까지 가보자!",
    ]},
    {"key": "map_stage2", "emotion": EMOTION, "lines": [
        "잘했어! 이제 유입 펌프장에 도착했어. 여기는 하수처리장의 첫 관문이야. "
        "큰 쓰레기랑 모래를 촘촘한 거름망으로 걸러내는 곳이지.",
    ]},
    {"key": "map_stage3", "emotion": EMOTION, "lines": [
        "여기서부터가 진짜야. 생물반응조에는 눈에 안 보이는 작은 미생물들이 살고 있어. "
        "얘네가 네 몸에 붙은 더러운 걸 먹어치워 줄 거야. 진짜 작은 청소부들이지!",
    ]},
    {"key": "map_stage4", "emotion": EMOTION, "lines": [
        "거의 다 왔어! 마지막은 여과 설비야. 여기서 남은 아주 작은 찌꺼기까지 걸러내고, "
        "자외선으로 세균을 없애. 이 단계만 지나면 넌 진짜 깨끗한 물이 되는 거야!",
    ]},

    # ── 조작 설명 ──
    {"key": "howto_stage1", "emotion": EMOTION, "lines": [
        "핸들을 좌우로 돌려서 움직여봐. 앞에 나오는 쓰레기를 피해야 해. 부딪히면 더 더러워지니까 조심!",
    ]},
    {"key": "howto_stage2", "emotion": EMOTION, "lines": [
        "앞에서 거름망이 다가올 거야. 거름망에 뚫린 모양이랑 똑같이 몸을 만들면 통과할 수 있어. 몸으로 따라해봐!",
    ]},
    {"key": "howto_stage3", "emotion": EMOTION, "lines": [
        "미생물들이 지금 자고 있어. 산소 방울을 쏴서 깨워주자! 미생물을 조준해서 맞히면 돼.",
    ]},
    {"key": "howto_stage4", "emotion": EMOTION, "lines": [
        "위에서 자외선 링이 내려올 거야. 컨트롤러를 기울여서 링이 내려오는 칸으로 얼른 이동해! 링 안으로 들어가면 소독 완료야.",
    ]},

    # ── Stage 1 ──
    {"key": "s1_hit", "emotion": EMOTION, "lines": [
        "어이쿠! 더 더러워졌잖아.",
        "앗, 부딪혔다! 괜찮아.",
        "물티슈다! 저건 물에 안 녹아.",
        "쿵! 조금만 더 일찍 돌려볼까?",
        "괜찮아, 다시 가보자!",
    ]},
    {"key": "s1_clean", "emotion": EMOTION, "lines": [
        "우와, 하나도 안 부딪혔어!",
        "완전 부드러운데!",
        "깨끗하게 통과 중!",
    ]},
    {"key": "s1_bubble", "emotion": EMOTION, "lines": [
        "좋았어! 맑은 버블이야!",
        "우와, 속도가 빨라진다!",
        "깨끗한 버블! 잘 먹었어!",
        "그대로 쭉 가자!",
        "신난다, 더 빨라졌어!",
    ]},

    # ── Stage 2 ──
    {"key": "s2_pass", "emotion": EMOTION, "lines": [
        "좋았어! 딱 맞췄다.",
        "완벽해!",
        "오, 자세 좋은데!",
        "통과! 찌꺼기가 떨어져 나갔어.",
        "잘한다! 네 몸이 조금 맑아졌어.",
    ]},
    {"key": "s2_fail_tpose", "emotion": EMOTION, "lines": [
        "아깝다! 팔을 양옆으로 더 쫙 펴봐.",
    ]},
    {"key": "s2_fail_bothup", "emotion": EMOTION, "lines": [
        "조금만 더! 양팔을 머리 위로 번쩍!",
    ]},
    {"key": "s2_fail_normal", "emotion": EMOTION, "lines": [
        "이번엔 팔을 편하게 내리면 돼!",
    ]},
    {"key": "s2_fail_common", "emotion": EMOTION, "lines": [
        "아깝다! 거름망 모양을 잘 봐.",
        "조금만 더! 다음엔 될 거야.",
        "괜찮아, 천천히 따라해도 돼.",
        "놓쳤네! 다음 거름망 온다.",
    ]},
    {"key": "s2_streak", "emotion": EMOTION, "lines": [
        "3연속 성공! 정말 잘한다!",
        "계속 맞추고 있어! 대단해!",
        "연속 통과 중! 멋진데!",
    ]},

    # ── Stage 3 ──
    {"key": "s3_hit", "emotion": EMOTION, "lines": [
        "명중! 미생물이 깨어났어.",
        "좋아, 잘하고 있어!",
        "잘 맞혔어! 널 청소해줄 거야.",
        "하나 추가! 계속 가자.",
    ]},
    {"key": "s3_miss", "emotion": EMOTION, "lines": [
        "아깝다! 조금만 더 조준해봐.",
        "빗나갔어. 천천히 겨눠도 돼.",
        "괜찮아, 미생물은 도망 안 가.",
        "다시 한번! 할 수 있어.",
    ]},
    {"key": "s3_perfect", "emotion": EMOTION, "lines": [
        "하나도 안 놓쳤어! 다 깨어났어.",
    ]},
    {"key": "s3_encourage", "emotion": EMOTION, "lines": [
        "천천히 집중해서 겨눠봐. 급할 거 없어.",
    ]},

    # ── Stage 4 ──
    {"key": "s4_pass", "emotion": EMOTION, "lines": [
        "통과! 세균이 사라졌어.",
        "좋아, 딱 맞췄다!",
        "소독 완료! 점점 맑아지고 있어.",
        "나이스! 잘 통과했어.",
    ]},
    {"key": "s4_miss", "emotion": EMOTION, "lines": [
        "아깝다! 조금만 더 빨리!",
        "놓쳤네. 링 내려오는 칸을 미리 봐둬!",
        "괜찮아, 다음 링 온다.",
    ]},
    {"key": "s4_fast", "emotion": EMOTION, "lines": [
        "우와, 재빠른데!",
        "반응 속도 장난 아닌데?",
    ]},
    {"key": "s4_close", "emotion": EMOTION, "lines": [
        "아슬아슬했어! 쫄깃했지?",
        "휴, 간신히 통과!",
    ]},
]


def main():
    if not API_KEY:
        print("[중단] TYPECAST_API_KEY가 없습니다. .env를 확인하세요.")
        return
    if not VOICE_ID:
        print("[중단] TYPECAST_VOICE_ID가 없습니다. 보이스 목록에서 골라 .env에 넣어주세요.")
        return

    from typecast import Typecast
    from typecast.models import TTSRequest, Prompt, Output

    client = Typecast(api_key=API_KEY)
    os.makedirs(OUTPUT_DIR, exist_ok=True)

    manifest = {}
    total = sum(len(s["lines"]) for s in LINE_SETS)
    nickname_total = len(NICKNAMES) * len(NICKNAME_LINE_SETS)
    done = 0
    failed = []

    print(f"총 {total + nickname_total}개 대사 생성 시작 (고정 {total} + 닉네임 {nickname_total})")
    print(f"voice={VOICE_ID}, model={MODEL}\n")

    for line_set in LINE_SETS:
        key = line_set["key"]
        emotion = line_set["emotion"]

        for idx, text in enumerate(line_set["lines"], start=1):
            filename = f"{key}_{idx:02d}.wav" if len(line_set["lines"]) > 1 else f"{key}.wav"
            path = os.path.join(OUTPUT_DIR, filename)
            done += 1

            try:
                response = client.text_to_speech(TTSRequest(
                    text=text,
                    model=MODEL,
                    voice_id=VOICE_ID,
                    prompt=Prompt(emotion_preset=emotion),
                    output=Output(audio_format="wav", target_lufs=TARGET_LUFS),
                ))
                with open(path, "wb") as f:
                    f.write(response.audio_data)

                manifest[filename] = {"text": text, "emotion": emotion, "key": key}
                print(f"[{done}/{total}] {filename}  ← {text[:30]}...")

            except Exception as e:
                failed.append((filename, str(e)))
                print(f"[{done}/{total}] 실패: {filename} — {e}")

            time.sleep(REQUEST_DELAY)

    # ── 닉네임이 들어가는 대사 (12개 닉네임 × 3종) ──
    print(f"\n닉네임 대사 생성 ({len(NICKNAMES)} × {len(NICKNAME_LINE_SETS)}개)\n")

    for line_set in NICKNAME_LINE_SETS:
        key = line_set["key"]
        emotion = line_set["emotion"]

        for nickname in NICKNAMES:
            text = line_set["template"].format(name=nickname)
            filename = f"{key}_{nickname}.wav"
            path = os.path.join(OUTPUT_DIR, filename)

            try:
                response = client.text_to_speech(TTSRequest(
                    text=text,
                    model=MODEL,
                    voice_id=VOICE_ID,
                    prompt=Prompt(emotion_preset=emotion),
                    output=Output(audio_format="wav", target_lufs=TARGET_LUFS),
                ))
                with open(path, "wb") as f:
                    f.write(response.audio_data)

                manifest[filename] = {
                    "text": text, "emotion": emotion, "key": key, "nickname": nickname,
                }
                print(f"  {filename}")

            except Exception as e:
                failed.append((filename, str(e)))
                print(f"  실패: {filename} — {e}")

            time.sleep(REQUEST_DELAY)

    with open(MANIFEST_PATH, "w", encoding="utf-8") as f:
        json.dump(manifest, f, ensure_ascii=False, indent=2)

    print(f"\n완료: {len(manifest)}개 생성 → {OUTPUT_DIR}/")
    print(f"매핑 정보: {MANIFEST_PATH}")

    if failed:
        print(f"\n실패 {len(failed)}건:")
        for name, err in failed:
            print(f"  - {name}: {err}")


if __name__ == "__main__":
    main()