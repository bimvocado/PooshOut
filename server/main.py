"""
출똥! 하수처리장 대탐험 — 정화봇 FastAPI 서버
실행: uvicorn main:app --host 0.0.0.0 --port 8000
필요: pip install fastapi uvicorn openai python-dotenv requests
API 키: .env 파일에 UPSTAGE_API_KEY, HUMELO_API_KEY 넣기 (코드에 하드코딩 금지)
"""

import os
import re
import json
import uuid
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
# 예: PUBLIC_BASE_URL=http://192.168.0.10:8000
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
# 시스템 프롬프트 (서버에서 관리 — 수정 시 서버 재시작만 하면 됨)
# ─────────────────────────────────────────────
SYSTEM_PROMPT = """너는 하수처리장 VR 교육 게임의 가이드 로봇 '정화봇'이야. 이 게임을 하는 친구들은
7~13세 초등학생이야. 어려운 단어 대신 쉬운 말과 재밌는 비유를 써서 설명해줘.
말투는 친근하고 다정한 반말 캐릭터 톤이야(예: '~했어!', '~해볼까?').
절대 아이를 혼내거나 겁주지 말고, 응답은 2~3문장 이내로 짧게 해줘.

추가 규칙 (교육 철학):
- 게임 상황에 맞는 과학 지식을 1줄만 곁들여 (침전, 미생물 분해, 소독 등)
- 그 지식이 왜 환경/물 순환에 중요한지 살짝 연결해줘
- 4단계 전체가 "물의 순환"이라는 하나의 이야기로 매듭지어지도록 의식해줘

음성으로 읽히는 대사이므로 반드시 지킬 것:
- 이모지는 절대 쓰지 마 (음성이 이모지 이름을 그대로 읽어버림)
- 물결표(~)나 과한 의성어/의태어("쫙!", "반짝반짝" 등)는 쓰지 마
- 소리 내어 읽었을 때 한 호흡에 읽히는 짧은 문장으로 써줘.
  한 문장에 정보를 하나만 담고, 접속사로 길게 잇지 마.
- 반드시 완결된 문장으로 끝내. 문장 도중에 멈추지 마.
- 전체 2~3문장, 총 100자 안팎을 넘기지 마."""

STAGE_INFO = {
    1: "변기탈출 & 하수관 레이싱 — 물이 하수관을 타고 이동하는 단계",
    2: "거름망 포즈 통과 & 침전 — 큰 찌꺼기를 거르고 무거운 것을 가라앉히는 단계",
    3: "미생물 팡팡 — 미생물이 오염물을 분해하는 단계",
    4: "하이테크 소독 & 한강 방류 — 소독 후 깨끗한 물을 강으로 보내는 단계",
}

# 폴백 멘트 (LLM 실패 시)
FALLBACK_COMMENTARY = "지금까지 정말 잘하고 있어! 남은 게이트도 힘내서 통과해보자!"
FALLBACK_FEEDBACK = {
    "child_message": "오늘 정말 잘했어! 네 덕분에 물이 깨끗해졌어. 다음에 또 만나자!",
    "guardian_summary": "하수처리 4단계(침전-미생물분해-소독-방류) 체험을 완료했습니다.",
}


# ─────────────────────────────────────────────
# 유틸
# ─────────────────────────────────────────────
def clamp_sentences(text: str, max_sentences: int = 3) -> str:
    """
    문장 단위로 자른다. 글자 수로 자르면 말이 중간에 끊기므로 절대 그렇게 하지 않는다.

    - 문장부호(. ! ? …)로 끝나는 완결 문장만 남긴다.
    - max_tokens에 걸려 잘린 마지막 조각(문장부호 없이 끝난 부분)은 통째로 버린다.
      TTS가 "물이 깨끗해지려면 미" 같은 토막을 읽는 사고를 막기 위함.
    - 완결 문장이 하나도 없으면(=전체가 잘린 경우) 원문을 그대로 돌려준다.
      이땐 폴백 멘트보다는 있는 그대로 내보내는 편이 낫다.
    """
    text = text.strip()
    if not text:
        return text

    # 문장부호 뒤에서 끊되, 부호는 문장에 남긴다
    parts = re.findall(r"[^.!?…]+[.!?…]+", text)
    if not parts:
        return text  # 완결 문장이 아예 없음 → 원문 유지

    kept = " ".join(p.strip() for p in parts[:max_sentences])
    return kept.strip()


# ─────────────────────────────────────────────
# 응답 캐시 (지연시간 대책)
#
# 부스에서는 아이가 바뀌어도 "스테이지 진입/클리어" 멘트는 같은 상황에서 반복된다.
# 첫 아이가 한 번 생성하면 그 뒤로는 캐시에서 즉시 꺼내 쓰므로 대기 시간이 0에 가까워진다.
# 마무리 피드백처럼 플레이 로그가 매번 다른 요청은 자연히 캐시에 걸리지 않으므로
# "AI가 매번 새로 만든다"는 성격도 그대로 유지된다.
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
        _cache.move_to_end(key)  # 최근 사용으로 갱신
        print("[캐시] 적중 — 즉시 응답")
    return hit


def cache_put(key: str, value: dict) -> None:
    if not CACHE_ENABLED or value.get("audioUrl") is None:
        return  # 음성 생성이 실패한 응답은 캐시하지 않는다
    _cache[key] = value
    _cache.move_to_end(key)
    while len(_cache) > CACHE_MAX:
        _cache.popitem(last=False)


def call_solar(user_prompt: str, max_tokens: int = 320, json_mode: bool = False) -> Optional[str]:
    """Upstage Solar 호출. 실패 시 None."""
    if client is None:
        return None
    try:
        kwargs = {}
        if json_mode:
            kwargs["response_format"] = {"type": "json_object"}
        resp = client.chat.completions.create(
            model="solar-pro3",
            messages=[
                {"role": "system", "content": SYSTEM_PROMPT},
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
    """
    TTS로 보내기 전 텍스트 정제.
    이모지를 그대로 보내면 "눈웃음"처럼 이름을 읽어버려서 반드시 제거해야 함.
    화면에 표시되는 원본 reply는 건드리지 않고, 음성용 텍스트만 정제한다.
    """
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


# Typecast 클라이언트는 서버 시작 시 한 번만 만들어 재사용한다.
_typecast_client = None


def _get_typecast():
    global _typecast_client
    if _typecast_client is None and TYPECAST_API_KEY:
        from typecast import Typecast   # 서버 기동 속도를 위해 지연 임포트
        _typecast_client = Typecast(api_key=TYPECAST_API_KEY)
    return _typecast_client


def call_tts(text: str, emotion: Optional[str] = None) -> Optional[str]:
    """
    Typecast로 음성 생성 → 로컬 wav 저장 후 URL 반환. 실패 시 None(텍스트만 표시).

    emotion을 넘기지 않으면 TYPECAST_EMOTION(기본 normal)을 쓴다.
    프리셋마다 음높이가 달라서 섞어 쓰면 대사마다 목소리 톤이 오락가락하므로,
    사전 생성한 고정 멘트와 같은 프리셋 하나로 통일하는 것을 권장한다.
    """
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
            # 사전 생성한 고정 멘트와 음량을 맞춰야 같은 캐릭터처럼 들린다.
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
# ① 기존 스펙 호환: POST /chat (팀장 LLMConnector가 이미 이 형식으로 호출 중)
# ─────────────────────────────────────────────
class ChatRequest(BaseModel):
    message: str
    context: Optional[str] = ""
    playerName: Optional[str] = ""   # 아이가 고른 닉네임 (호명용, 없으면 생략)


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
# ② Stage 2 중간 해설 (중계 스타일, 1회 호출)
# ─────────────────────────────────────────────
class Progress(BaseModel):
    """스테이지 공통 진행 상황. 4개 스테이지 모두 '시도 → 성공/실패' 구조라 이 형태로 통일된다."""
    total: int = 0          # 이 스테이지의 전체 시도 횟수
    attempted: int = 0      # 지금까지 시도한 횟수
    succeeded: int = 0
    failed: int = 0
    streak: int = 0         # 현재 연속 성공 수


class CommentaryRequest(BaseModel):
    eventType: str = "PROGRESS"   # STAGE_ENTER | PROGRESS | MISTAKE | STAGE_CLEAR
    stage: int = 1
    playerName: Optional[str] = ""
    purity: int = 0               # 현재 정화도. 캐시 적중률을 위해 20단위 반올림해서 보내길 권장
    progress: Progress = Progress()
    detail: Optional[str] = ""    # 스테이지별 자유 문자열
                                  #  Stage1: "물티슈"  Stage2: "TPose"
                                  #  Stage3: "미생물 3마리"  Stage4: "C3, 1.2초 (빠름)"


@app.post("/commentary")
def commentary(req: CommentaryRequest):
    key = _cache_key(
        "commentary", req.eventType, str(req.stage), req.playerName or "",
        str(req.purity), req.progress.model_dump_json(), req.detail or "",
    )
    cached = cache_get(key)
    if cached:
        return cached

    stage_desc = STAGE_INFO.get(req.stage, "")
    p = req.progress

    name_line = f"플레이어 이름은 '{req.playerName}'이야.\n" if req.playerName else ""
    detail_line = f"세부 상황: {req.detail}\n" if req.detail else ""

    if req.eventType == "STAGE_ENTER":
        situation = f"아이가 방금 {stage_desc}에 들어왔어. 이 단계가 뭘 하는 곳인지 짧게 안내해줘."
    elif req.eventType == "STAGE_CLEAR":
        situation = (f"아이가 {stage_desc}를 클리어했어. "
                     f"{p.total}번 중 {p.succeeded}번 성공했어. "
                     "방금 배운 원리를 짧게 정리하고 칭찬해줘.")
    elif req.eventType == "MISTAKE":
        situation = "아이가 방금 실패했어. 혼내지 말고 다정하게 격려해줘."
    else:  # PROGRESS
        situation = (f"아이가 {stage_desc}를 플레이 중이야. "
                     f"{p.attempted}번 시도해서 {p.succeeded}번 성공, {p.failed}번 실패. "
                     f"현재 {p.streak}연속 성공 중이야. "
                     "스포츠 중계 캐스터처럼 이 흐름을 짚어주면서 응원해줘. "
                     "숫자를 자연스럽게 녹여서 '진짜 지켜보고 있다'는 느낌이 나게 해줘.")

    prompt = f"{name_line}{detail_line}{situation}"
    reply = call_solar(prompt, max_tokens=280)
    if reply is None:
        reply = FALLBACK_COMMENTARY
    reply = clamp_sentences(reply, 3)
    audio_url = call_tts(reply)

    result = {"reply": reply, "audioUrl": audio_url}
    cache_put(key, result)
    return result


# ─────────────────────────────────────────────
# ③ 마무리 피드백 (게임 종료 시 1회 — 핵심 기능)
# ─────────────────────────────────────────────
class StageLog(BaseModel):
    stage: int
    timeSec: float = 0
    success: int = 0
    fail: int = 0
    note: Optional[str] = ""   # 스테이지별 자유 메모. AI가 플레이 패턴을 해석하는 핵심 재료.
                               # 예: "TPose 2회 실패", "평균 반응 1.8초, 먼 칸에서 주로 놓침"


class FeedbackRequest(BaseModel):
    playerName: str = "친구"
    totalPurity: int = 0
    totalTimeSec: float = 0
    stages: list[StageLog] = []


@app.post("/feedback")
def feedback(req: FeedbackRequest):
    log_lines = []
    for s in req.stages:
        info = STAGE_INFO.get(s.stage, f"Stage {s.stage}")
        line = f"- Stage {s.stage} ({info}): 성공 {s.success}회, 실패 {s.fail}회, 소요 {int(s.timeSec)}초"
        if s.note:
            line += f" / {s.note}"
        log_lines.append(line)
    log_text = "\n".join(log_lines)

    prompt = f"""'{req.playerName}'(이)가 게임을 완료했어. 최종 정화도는 {req.totalPurity}%야.
플레이 기록:
{log_text}

이 기록을 보고 아래 JSON 형식으로만 답해줘 (다른 말 없이 JSON만):
{{
  "child_message": "아이용 마무리 멘트 — 이름을 부르며, 이 아이만의 플레이 특징(잘한 스테이지/아쉬운 스테이지)을 구체적으로 짚어서 칭찬+격려. 마지막에 '물의 순환' 이야기로 매듭. 3~4문장.",
  "guardian_summary": "보호자용 요약 — 아이가 어떤 하수처리 개념을 체험/학습했는지 1~2문장, 존댓말."
}}"""
    raw = call_solar(prompt, max_tokens=600, json_mode=True)

    if raw is None:
        result = dict(FALLBACK_FEEDBACK)
    else:
        try:
            result = json.loads(raw)
        except json.JSONDecodeError:
            result = dict(FALLBACK_FEEDBACK)

    child_msg = result.get("child_message", FALLBACK_FEEDBACK["child_message"])
    audio_url = call_tts(child_msg)
    return {
        "child_message": child_msg,
        "guardian_summary": result.get("guardian_summary", FALLBACK_FEEDBACK["guardian_summary"]),
        "audioUrl": audio_url,
    }


# ─────────────────────────────────────────────
# ④ 리더보드 (파일 저장 — 서버 재시작해도 기록 유지됨)
# ─────────────────────────────────────────────
class LeaderboardEntry(BaseModel):
    playerName: str      # 아이가 고른 닉네임 (번호 없이, 예: "똥순이")
    purity: int
    grade: str = ""


# 리더보드를 JSON 파일로 저장. 서버 프로세스가 재시작돼도 이 파일에서 다시 읽어온다.
# (호스팅 환경이 컨테이너를 통째로 새로 만드는 경우 — 예: 무료 플랜 재배포 — 에는
#  디스크 자체가 초기화될 수 있으니 완전한 영구 저장이 필요하면 별도 DB/퍼시스턴트 디스크 필요)
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
    """같은 닉네임을 고른 사람이 여럿일 때 뒤에 번호를 붙여 구분한다. (똥순이 → 똥순이2 → 똥순이3)"""
    count = sum(1 for e in _leaderboard if e.get("playerName") == base_name)
    return base_name if count == 0 else f"{base_name}{count + 1}"


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
    """리더보드 전체 초기화. 더미데이터 정리 등 관리 목적으로만 사용."""
    _leaderboard.clear()
    _save_leaderboard()
    return {"success": True}


# 주의: 이 라우트는 반드시 "/leaderboard/all" 보다 아래에 있어야 한다.
# 순서가 바뀌면 DELETE /leaderboard/all 요청도 name="all"로 여기서 잡아먹혀버린다.
@app.delete("/leaderboard/{name}")
def delete_leaderboard_entry(name: str):
    """특정 닉네임(playerName 또는 displayName) 기록 삭제. 중복 번호가 붙은 기록도 함께 지워진다."""
    before = len(_leaderboard)
    _leaderboard[:] = [
        e for e in _leaderboard
        if e.get("displayName") != name and e.get("playerName") != name
    ]
    removed = before - len(_leaderboard)
    _save_leaderboard()
    return {"success": removed > 0, "removed": removed}


# ─────────────────────────────────────────────
# 헬스체크 (Unity에서 서버 살아있는지 확인용)
# ─────────────────────────────────────────────
@app.get("/")
def health():
    return {"status": "ok", "service": "PooshOut 정화봇 서버"}
