"""
출똥! 하수처리장 대탐험 — 정화봇 FastAPI 서버
실행: uvicorn main:app --host 0.0.0.0 --port 8000
필요: pip install fastapi uvicorn openai python-dotenv requests
API 키: .env 파일에 UPSTAGE_API_KEY, TYPECAST_API_KEY 넣기 (코드에 하드코딩 금지)
"""

import os
import re
import json
import uuid
import random
import hashlib
import requests
from collections import OrderedDict
from fastapi import FastAPI
from fastapi.staticfiles import StaticFiles
from pydantic import BaseModel
from typing import Optional
from openai import OpenAI
from dotenv import load_dotenv

load_dotenv()

UPSTAGE_API_KEY = os.getenv("UPSTAGE_API_KEY", "")
TYPECAST_API_KEY = os.getenv("TYPECAST_API_KEY", "")
TYPECAST_VOICE_ID = os.getenv("TYPECAST_VOICE_ID", "")   # 보이스 목록에서 고른 Voice ID
TYPECAST_MODEL = os.getenv("TYPECAST_MODEL", "ssfm-v30")

# 감정 프리셋. 사전 생성한 고정 멘트(generate_lines.py의 EMOTION)와 반드시 같아야
# 실시간 대사와 고정 대사가 같은 톤으로 들린다.
TYPECAST_EMOTION = os.getenv("TYPECAST_EMOTION", "normal")

# Unity(Quest 실기기)에서 접근할 주소. 부스/실기기에서는 서버 PC의 IP로 바꿔야 함.
PUBLIC_BASE_URL = os.getenv("PUBLIC_BASE_URL", "http://localhost:8000")

# 생성된 음성 파일을 저장/서빙할 폴더
AUDIO_DIR = os.path.join(os.path.dirname(os.path.abspath(__file__)), "audio")
os.makedirs(AUDIO_DIR, exist_ok=True)

app = FastAPI(title="PooshOut 정화봇 서버")
app.mount("/audio", StaticFiles(directory=AUDIO_DIR), name="audio")

client = None
if UPSTAGE_API_KEY:
    client = OpenAI(
        api_key=UPSTAGE_API_KEY,
        base_url="https://api.upstage.ai/v1",
    )
else:
    print("[경고] UPSTAGE_API_KEY가 없습니다. 모든 응답이 폴백 멘트로 나갑니다. (.env 확인)")
if not TYPECAST_API_KEY:
    print("[경고] TYPECAST_API_KEY가 없습니다. 음성 없이 텍스트만 나갑니다. (.env 확인)")
elif not TYPECAST_VOICE_ID:
    print("[경고] TYPECAST_VOICE_ID가 없습니다. 보이스 목록에서 골라 .env에 넣어주세요.")


# ─────────────────────────────────────────────
# 시스템 프롬프트
# ─────────────────────────────────────────────
BASE_PERSONA_PROMPT = """너는 하수처리장 VR 교육 게임의 가이드 로봇 '정화봇'이야. 이 게임을 하는 친구들은
7~13세 초등학생이야. 어려운 단어 대신 쉬운 말과 재밌는 비유를 써서 설명해줘.
말투는 친근하고 다정한 반말 캐릭터 톤이야(예: '~했어!', '~해볼까?').
절대 아이를 혼내거나 겁주지 말고, 상황에 맞게 간결하게 말해줘.

음성으로 읽히는 대사이므로 반드시 지킬 것:
- 이모지는 절대 쓰지 마 (음성이 이모지 이름을 그대로 읽어버림)
- 물결표(~)나 과한 의성어/의태어("쫙!", "반짝반짝" 등)는 쓰지 마
- 소리 내어 읽었을 때 한 호흡에 읽히는 짧은 문장으로 써줘.
  한 문장에 정보를 하나만 담고, 접속사로 길게 잇지 마.
- 반드시 완결된 문장으로 끝내. 문장 도중에 멈추지 마."""

SYSTEM_PROMPT = BASE_PERSONA_PROMPT + """

추가 규칙 (교육 철학, 실시간 짧은 대사 전용):
- 게임 상황에 맞는 과학 지식을 1줄만 곁들여 (침전, 미생물 분해, 소독 등)
- 그 지식이 왜 환경/물 순환에 중요한지 살짝 연결해줘
- 4단계 전체가 "물의 순환"이라는 하나의 이야기로 매듭지어지도록 의식해줘

전체 2~3문장, 총 100자 안팎을 넘기지 마."""

FEEDBACK_SYSTEM_PROMPT = BASE_PERSONA_PROMPT

STAGE_INFO = {
    1: "하수관 레이싱 — 하수관을 타고 이동하며 맑은 버블을 먹고 쓰레기를 피하는 단계",
    2: "거름망 통과 — 다가오는 거름망 구멍 모양에 맞춰 몸으로 포즈를 만들어 통과하는 단계",
    3: "미생물 깨우기 — 산소총을 쏴서 잠자는 미생물을 깨워 오염물을 분해시키는 단계",
    4: "자외선 소독 — 위에서 내려오는 자외선 링 안으로 들어가 소독받는 단계",
}

# 폴백 멘트 (자유 질의용 /chat 엔드포인트에서 AI 오류 시 비상용 대사)
FALLBACK_COMMENTARY = "음, 지금 소리가 잘 안들려! 궁금한 건 잠시 뒤에 다시 물어봐 줘!"


def fallback_feedback(player_name: str) -> dict:
    return {
        "child_message": (
            f"{player_name}, 해냈네! 드디어 깨끗한 물이 되었어. "
            "끝까지 정말 잘했어! "
            "이제 자연으로 돌아갈 시간이야. 돌고 돌아서 우리 꼭 다시 만나자, 안녕!"
        ),
    }


# ─────────────────────────────────────────────
# 유틸
# ─────────────────────────────────────────────
def clamp_sentences(text: str, max_sentences: int = 3) -> str:
    text = text.strip()
    if not text:
        return text

    parts = re.findall(r"[^.!?…]+[.!?…]+", text)
    if not parts:
        return text

    kept = " ".join(p.strip() for p in parts[:max_sentences])
    return kept.strip()


# ─────────────────────────────────────────────
# 응답 캐시
# ─────────────────────────────────────────────
_cache: "OrderedDict[str, dict]" = OrderedDict()
CACHE_MAX = 128
CACHE_ENABLED = os.getenv("CACHE_ENABLED", "1") != "0"


def _cache_key(*parts: str) -> str:
    return hashlib.md5("||".join(parts).encode("utf-8")).hexdigest()


def cache_get(key: str) -> Optional[dict]:
    if not CACHE_ENABLED:
        return None
    hit = _cache.get(key)
    if hit is not None:
        _cache.move_to_end(key)
    return hit


def cache_put(key: str, value: dict) -> None:
    if not CACHE_ENABLED or value.get("audioUrl") is None:
        return
    _cache[key] = value
    _cache.move_to_end(key)
    while len(_cache) > CACHE_MAX:
        _cache.popitem(last=False)


def call_solar(user_prompt: str, max_tokens: int = 320, json_mode: bool = False, system_prompt: str = SYSTEM_PROMPT) -> Optional[str]:
    if client is None:
        return None
    try:
        kwargs = {}
        if json_mode:
            kwargs["response_format"] = {"type": "json_object"}
        resp = client.chat.completions.create(
            model="solar-pro3",
            messages=[
                {"role": "system", "content": system_prompt},
                {"role": "user", "content": user_prompt},
            ],
            max_tokens=max_tokens,
            timeout=8.0,
            reasoning_effort="low",
            **kwargs,
        )
        return resp.choices[0].message.content
    except Exception as e:
        print(f"[Solar 오류] {e}")
        return None


def sanitize_for_tts(text: str) -> str:
    emoji_pattern = re.compile(
        "["
        "\U0001F300-\U0001FAFF"
        "\U00002600-\U000027BF"
        "\U0001F1E6-\U0001F1FF"
        "\U00002190-\U000021FF"
        "\U0000FE00-\U0000FE0F"
        "]+",
        flags=re.UNICODE,
    )
    text = emoji_pattern.sub("", text)
    text = text.replace("~", " ")
    text = re.sub(r"[!]{2,}", "!", text)
    text = re.sub(r"[?]{2,}", "?", text)
    text = re.sub(r"\s{2,}", " ", text)
    return text.strip()


_typecast_client = None

def _get_typecast():
    global _typecast_client
    if _typecast_client is None and TYPECAST_API_KEY:
        from typecast import Typecast
        _typecast_client = Typecast(api_key=TYPECAST_API_KEY)
    return _typecast_client


def call_tts(text: str, emotion: Optional[str] = None) -> Optional[str]:
    if not TYPECAST_API_KEY or not TYPECAST_VOICE_ID:
        return None

    clean_text = sanitize_for_tts(text)
    if not clean_text:
        return None

    preset = emotion or TYPECAST_EMOTION

    try:
        from typecast.models import TTSRequest, Prompt, Output

        client = _get_typecast()
        response = client.text_to_speech(TTSRequest(
            text=clean_text,
            model=TYPECAST_MODEL,
            voice_id=TYPECAST_VOICE_ID,
            prompt=Prompt(emotion_preset=preset),
            output=Output(audio_format="wav", target_lufs=-14.0),
        ))

        os.makedirs(AUDIO_DIR, exist_ok=True)
        filename = f"{uuid.uuid4().hex}.wav"
        with open(os.path.join(AUDIO_DIR, filename), "wb") as f:
            f.write(response.audio_data)

        return f"{PUBLIC_BASE_URL}/audio/{filename}"
    except Exception as e:
        print(f"[Typecast 오류] {e}")
        return None


# ─────────────────────────────────────────────
# ① 자유 질의용 호환: POST /chat (나중을 위해 유지)
# ─────────────────────────────────────────────
class ChatRequest(BaseModel):
    message: str
    context: Optional[str] = ""
    playerName: Optional[str] = ""


@app.post("/chat")
def chat(req: ChatRequest):
    key = _cache_key("chat", req.context, req.message, req.playerName or "")
    cached = cache_get(key)
    if cached:
        return cached

    name_line = f"[플레이어 이름] {req.playerName}\n" if req.playerName else ""
    prompt = f"{name_line}[게임 상황] {req.context}\n[요청] {req.message}"
    reply = call_solar(prompt, max_tokens=280)
    if reply is None:
        reply = FALLBACK_COMMENTARY
    reply = clamp_sentences(reply, 3)
    audio_url = call_tts(reply)

    result = {"reply": reply, "audioUrl": audio_url}
    cache_put(key, result)
    return result


# ─────────────────────────────────────────────
# ② 마무리 피드백 (게임 종료 시 1회 — 핵심 기능)
# ─────────────────────────────────────────────
class StageLog(BaseModel):
    stage: int
    timeSec: float = 0
    success: int = 0
    fail: int = 0
    note: Optional[str] = ""
    purity: float = 0  # 이 스테이지의 최종 정화도(%). 상위 2개를 고르는 기준.


class FeedbackRequest(BaseModel):
    playerName: str = "친구"
    totalPurity: int = 0
    totalTimeSec: float = 0
    stages: list[StageLog] = []


def _pick_top_two_stages(stages: list[StageLog]) -> list[StageLog]:
    """
    칭찬할 스테이지 2개를 정렬해서 고른다.
    1순위: 정화도(purity) 높은 순
    2순위(정화도 동점): success(성공 횟수) 많은 순
    3순위(그것도 동점): 랜덤
    나머지 스테이지는 LLM 프롬프트에서 아예 안 보이게 제외한다 -
    LLM이 "이건 낮아 보이는데 왜 칭찬하지" 하고 헷갈리지 않도록.
    """
    shuffled = stages.copy()
    random.shuffle(shuffled)  # 3순위(랜덤) 처리 - 동점일 때 순서를 미리 섞어둠
    ranked = sorted(shuffled, key=lambda s: (s.purity, s.success), reverse=True)
    return ranked[:2]


@app.post("/feedback")
def feedback(req: FeedbackRequest):
    top_stages = _pick_top_two_stages(req.stages)

    # 1. LLM에게 줄 데이터를 여기서 완전히 세탁합니다. (Stage 단어, 설명문 싹 제거)
    ACTION_NAMES = {
        1: "하수관 이동",
        2: "거름망 통과",
        3: "산소총 쏘기",
        4: "자외선 링 통과"
    }

    log_lines = []
    for s in top_stages:
        action = ACTION_NAMES.get(s.stage, "플레이")
        # 오직 행동 이름과 note(실제 수치)만 넘깁니다.
        line = f"상황: {action} / 기록: {s.note}"
        log_lines.append(line)
    log_text = "\n".join(log_lines)

    # 2. 프롬프트도 훨씬 단순하고 강력하게 바꿉니다.
    prompt = f"""'{req.playerName}'(이)가 게임을 완료했어.
아래는 가장 잘한 2가지 행동의 기록이야:
{log_text}

이 기록을 보고 아래 JSON 형식으로만 답해줘:
{{
  "child_message": "아이용 마무리 멘트. 아래 규칙을 정확히 지켜서 하나의 문단으로 써줘:
    1) 시작 (토씨 하나도 바꾸지 말 것): '{req.playerName}, 해냈네! 드디어 깨끗한 물이 되었어. 얼마나 깨끗해졌는지 볼까?'
    2) 이어서 위의 '상황'과 '기록'에 적힌 내용만 사용해서 칭찬할 것. 
       - 기계적인 단어(Stage 등) 금지. 오직 주어진 기록을 바탕으로 씩씩하게 칭찬할 것.
       - 문장 끝맺음 주의: 모든 문장을 '~했어!'로만 끝내면 어색하므로 절대 반복하지 말 것. 단, 끝맺음을 다양하게 하려다가 앞뒤 문장의 주어와 서술어를 억지로 섞어서 문법이 깨지면 절대 안 됨 (예: '실력도 소독됐어'는 틀린 문장 - 소독되는 건 실력이 아니라 사람/물임). 자연스러운 게 최우선이고, 자연스럽다면 같은 어미(예: '~했어!')를 두 번 써도 상관없음.
       - 전체 시도/발사/기회 횟수(분모)는 절대 언급하지 말 것. 오직 성공한 횟수(성공 결과)만 말할 것 (예: '35번 쏴서 28번 명중했어' 처럼 전체 횟수를 대는 것 금지, '28번이나 명중했어' 처럼 성공 결과만 말할 것. '자외선 링 48번 중 18번이나 들어갔어'도 금지, '자외선 링도 18번이나 딱 맞춰 들어갔어' 처럼 쓸 것).
       - '~잖아'라는 어미는 쓰지 말 것 - 이미 상대가 알고 있는 사실을 재확인시켜줄 때 쓰는 말투라, 지금처럼 정화봇이 새로운 결과를 처음 알려주는 상황에는 안 맞음. 대신 '~어!', '~네!', '~던데!' 처럼 자연스럽게 쓸 것.
       - 각 스테이지 칭찬을 '그래서 정화도가 높게 나왔다'는 결과로 자연스럽게 이어서 마무리할 것 (정화도의 정확한 %는 화면에 이미 떠 있으니 숫자로 다시 읽어줄 필요는 없고, '정화도가 높게 나왔어', '정화도가 쭉쭉 올랐네' 처럼 인과관계만 짚어주면 됨).
       - 스테이지별로 표현할 때 참고할 것:
         · 하수관 이동(버블/쓰레기): 핸들을 틀어서 버블 쪽으로 가거나 쓰레기를 피하는 능동적 조작. '~개나 먹었어', '~번 피했어' 처럼 자연스럽게 쓸 것.
         · 거름망 통과, 산소총 쏘기: 몸을 움직이거나 조준해서 하는 능동적 행동. '~번 시도해서', '~번이나 맞혔어' 같은 표현 자연스럽게 사용 가능 (단, 전체 시도 횟수는 위 규칙대로 언급 금지).
         · 자외선 링 통과: 링이 16개 타일 중 랜덤한 곳에 떨어지고, 떨어지는 순간에야 어디인지 알 수 있어서 빠르게 반응해서 이동해야 하는 것. '~번 시도해서'라는 표현은 쓰지 말고(조준해서 맞춘 게 아니라 반응 속도의 결과이므로), '~번이나 딱 맞춰 들어갔어' 처럼 쓸 것.
       - 말투 예시 (이 흐름을 참고하되 그대로 베끼지 말고, 각 문장이 각자 완결된 문법으로 끝나도록 할 것): '산소총으로 28번이나 명중시켰고, 최대 6번 연속으로 맞혀서 그런지 정화도가 높게 나왔어! 자외선 링도 18번이나 딱 맞춰 들어가서 정화도가 쭉쭉 올랐네!'
       - 절대 없는 내용을 지어내거나 과학 원리를 덧붙여 설명하지 말 것. 
    3) 기대감 (토씨 하나도 바꾸지 말 것): '다음엔 또 얼마나 잘할지 벌써 기대되는걸?'
    4) 끝 (토씨 하나도 바꾸지 말 것): '이제 자연으로 돌아갈 시간이야. 돌고 돌아서 우리 꼭 다시 만나자, 안녕!'
    5) 특수기호나 이모티콘 절대 금지. 총 4~5문장 이내."
}}"""
    raw = call_solar(prompt, max_tokens=600, json_mode=True, system_prompt=FEEDBACK_SYSTEM_PROMPT)
    fallback = fallback_feedback(req.playerName)

    if raw is None:
        result = fallback
    else:
        try:
            result = json.loads(raw)
        except json.JSONDecodeError:
            result = fallback

    child_msg = result.get("child_message", fallback["child_message"])
    audio_url = call_tts(child_msg)
    
    return {
        "child_message": child_msg,
        "audioUrl": audio_url,
    }


# ─────────────────────────────────────────────
# ③ 리더보드
# ─────────────────────────────────────────────
class LeaderboardEntry(BaseModel):
    playerName: str
    purity: float
    grade: str = ""


LEADERBOARD_FILE = os.path.join(os.path.dirname(os.path.abspath(__file__)), "leaderboard.json")


def _load_leaderboard() -> list[dict]:
    if not os.path.exists(LEADERBOARD_FILE):
        return []
    try:
        with open(LEADERBOARD_FILE, "r", encoding="utf-8") as f:
            data = json.load(f)
        return data if isinstance(data, list) else []
    except (json.JSONDecodeError, OSError) as e:
        print(f"[리더보드 파일 오류] {e} — 빈 목록으로 시작")
        return []


def _save_leaderboard() -> None:
    try:
        with open(LEADERBOARD_FILE, "w", encoding="utf-8") as f:
            json.dump(_leaderboard, f, ensure_ascii=False, indent=2)
    except OSError as e:
        print(f"[리더보드 파일 저장 오류] {e}")


_leaderboard: list[dict] = _load_leaderboard()


def _make_display_name(base_name: str) -> str:
    count = sum(1 for e in _leaderboard if e.get("playerName") == base_name)
    return base_name if count == 0 else f"{base_name}#{count + 1}"


@app.post("/leaderboard")
def add_leaderboard(entry: LeaderboardEntry):
    record = entry.model_dump()
    record["displayName"] = _make_display_name(entry.playerName)

    _leaderboard.append(record)
    _leaderboard.sort(key=lambda x: x["purity"], reverse=True)
    _save_leaderboard()

    rank = next(
        (i + 1 for i, e in enumerate(_leaderboard) if e["displayName"] == record["displayName"]),
        len(_leaderboard),
    )
    return {"success": True, "rank": rank, "displayName": record["displayName"]}


@app.get("/leaderboard")
def get_leaderboard():
    return {"entries": _leaderboard[:10]}


@app.delete("/leaderboard/all")
def delete_leaderboard_all():
    _leaderboard.clear()
    _save_leaderboard()
    return {"success": True}


@app.delete("/leaderboard/{name}")
def delete_leaderboard_entry(name: str):
    before = len(_leaderboard)
    _leaderboard[:] = [
        e for e in _leaderboard
        if e.get("displayName") != name and e.get("playerName") != name
    ]
    removed = before - len(_leaderboard)
    _save_leaderboard()
    return {"success": removed > 0, "removed": removed}


# ─────────────────────────────────────────────
# 헬스체크
# ─────────────────────────────────────────────
@app.get("/")
def health():
    return {"status": "ok", "service": "PooshOut 정화봇 서버"}