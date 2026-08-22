# 서버 API 스펙 (Unity 클라이언트 ↔ FastAPI 서버)

Unity 클라이언트가 실제로 호출하는 스펙 그대로 정리한 문서. 클라이언트 쪽 구현은
`Assets/Scripts/AI/LLMConnector.cs`(POST /chat), `Assets/Scripts/Core/SaveLoadManager.cs`(POST·GET /leaderboard)에 있음.

## 공통

- Base URL: `Assets/Scripts/Core/ServerConfig.cs`의 `SERVER_URL` 상수 (기본값 `http://localhost:8000`, 배포 시 이 한 줄만 바꾸면 클라이언트 전체에 반영됨)
- Content-Type: `application/json` (요청/응답 모두)
- 클라이언트 타임아웃: 5초 — 이 안에 응답이 없으면 클라이언트는 오프라인으로 간주하고 자체 폴백 처리함. 서버가 5초 안에 못 끝낼 것 같은 작업(LLM 호출 등)이면 클라이언트 쪽 타임아웃도 같이 늘려야 함(`LLMConnector.requestTimeoutSeconds`).
- 실패 시 HTTP status는 4xx/5xx면 충분함 — 클라이언트는 status만 보고 실패 처리하고 로컬 폴백으로 전환하기 때문에 에러 바디 포맷은 자유.

---

## POST /chat

정화봇(가이드 로봇) 대사 생성 요청. 클라이언트가 게임 상황(컨텍스트)과 실제로 봇이 답할 메시지/지시문을 같이 보내면, 서버가 Upstage Solar API를 호출해서 대사를 만들어 돌려줌. **Upstage API 키는 서버에만 존재** — 클라이언트는 키를 전혀 모름.

### Request

```json
{
  "message": "지금 상황에 맞게 짧게 한마디 해줘.",
  "context": "너는 하수처리장 VR 교육 게임의 가이드 로봇 '정화봇'이야. ... [상황] 플레이어가 방금 '스크린/침사지'(1번째 스테이지)에 들어왔어. ..."
}
```

| 필드 | 타입 | 설명 |
|---|---|---|
| `message` | string | 봇이 실제로 답해야 할 사용자 메시지 또는 지시문. 자유 질문일 땐 어린이가 입력한 질문 그대로, 그 외(스테이지 진입/오염물 접촉/클리어)일 땐 "지금 상황에 맞게 ~해줘" 같은 짧은 지시문. |
| `context` | string | 정화봇의 페르소나 설명 + 현재 게임 상황 설명을 합친 문자열. 그대로 LLM의 system prompt로 써도 되도록 조립해서 보냄. |

서버 구현 참고: `context`를 system 메시지로, `message`를 user 메시지로 Upstage Chat Completions에 그대로 전달하면 됨 (`model`, `max_tokens` 등은 서버가 알아서 정함 — 클라이언트는 관여 안 함).

### Response (200)

```json
{ "reply": "여기는 큰 쓰레기랑 모래를 촘촘한 그물로 걸러내는 곳이야! 같이 깨끗하게 만들어볼까?" }
```

| 필드 | 타입 | 설명 |
|---|---|---|
| `reply` | string | 정화봇이 실제로 말할 대사. 2~3문장 이내 권장(클라이언트 프롬프트에서 그렇게 요청함). |

### 실패 시

- 어떤 이유로든(4xx/5xx, 타임아웃, 연결 불가) 응답을 못 받으면 클라이언트는 자체 오프라인 폴백 대사를 대신 출력함. 에러 바디 포맷은 서버 재량.

---

## POST /leaderboard

한 판이 끝난 플레이어의 기록을 서버 순위표에 추가. **Stage 4 클리어 후 엔딩 화면 전환 시 딱 1회만 호출됨** (VR 멀미 방지를 위해 플레이 중에는 절대 호출 안 함).

### Request

```json
{
  "playerName": "KHW",
  "purity": 87.5,
  "grade": "일급수 황금 물방울"
}
```

| 필드 | 타입 | 설명 |
|---|---|---|
| `playerName` | string | 플레이어 이니셜 (보통 3글자). |
| `purity` | float | 최종 정화도, 0~100. |
| `grade` | string | 등급 문자열 (예: "일급수 황금 물방울"). |

### Response (200)

```json
{ "success": true, "rank": 3 }
```

| 필드 | 타입 | 설명 |
|---|---|---|
| `success` | bool | 저장 성공 여부. |
| `rank` | int | 저장 직후 전체 순위표에서 이 기록의 순위 (1-based). |

### 실패 시

- 클라이언트는 서버 업로드 전에 **로컬 저장을 이미 마친 상태**라 업로드가 실패해도 기록 자체는 유실되지 않음. 실패하면 경고 로그만 남기고 조용히 넘어감 (재시도 큐 등은 지금 범위 밖).

---

## GET /leaderboard

전체 순위표 조회.

### Response (200)

```json
{
  "entries": [
    { "playerName": "KHW", "purity": 95.0, "grade": "일급수 황금 물방울" },
    { "playerName": "YRI", "purity": 87.5, "grade": "이급수 은빛 물방울" }
  ]
}
```

| 필드 | 타입 | 설명 |
|---|---|---|
| `entries` | array | `PlayerData` 객체 배열. **정화도(purity) 내림차순 정렬해서 내려줄 것** — 클라이언트는 받은 순서를 그대로 표시함(재정렬 안 함). |

### 실패 시

- 서버 요청이 실패하면 클라이언트는 로컬에 저장된 순위표(`leaderboard.json`)로 자동 폴백해서 화면에 표시함. 즉 서버가 완전히 죽어도 순위표 화면 자체는 항상 뭔가를 보여줌 (다만 그 기기에 로컬로 쌓인 기록만 보임 — 다른 부스/기기 기록은 안 보임).

---

## DELETE /leaderboard/all

리더보드 전체 초기화. **관리 목적 전용 — Unity 클라이언트는 호출하지 않음** (더미데이터 정리, 행사 종료 후 리셋 등에 직접 curl/스크립트로 호출).

### Response (200)

```json
{ "success": true }
```

---

## DELETE /leaderboard/{name}

특정 닉네임 기록 삭제. `name`은 `playerName` 또는 `displayName`(중복 시 번호 붙은 이름, 예: `똥순이2`) 둘 중 하나와 정확히 일치하면 삭제됨. **관리 목적 전용 — Unity 클라이언트는 호출하지 않음.**

닉네임에 공백/한글이 포함될 수 있으므로 URL 인코딩해서 호출할 것 (예: curl은 `--data-urlencode` 대신 경로 자체를 인코딩해야 함).

### Response (200)

```json
{ "success": true, "removed": 1 }
```

| 필드 | 타입 | 설명 |
|---|---|---|
| `success` | bool | 하나 이상 삭제됐으면 `true`. |
| `removed` | int | 실제로 삭제된 항목 수 (`playerName`/`displayName`이 같은 중복 기록이 여러 개면 한 번에 다 지워짐). |

### 실패 시

- 일치하는 닉네임이 없어도 200과 `{"success": false, "removed": 0}`을 반환함 (존재 여부를 굳이 4xx로 구분하지 않음).

---

## 클라이언트 구현 메모 (참고용, 서버 팀 액션 아님)

- Unity의 `JsonUtility`는 최상위에서 배열(`[...]`)을 바로 파싱 못 해서, `GET /leaderboard` 응답은 `{"entries":[...]}` 형태로 한 번 감싸서 내려줘야 함. 최상위를 `[{...}, {...}]`로 주면 클라이언트가 못 읽음.
- 필드명 대소문자는 정확히 위 스펙대로(`message`, `context`, `reply`, `playerName`, `purity`, `grade`, `entries`, `success`, `rank`) — `JsonUtility`는 필드명이 정확히 일치해야 매핑됨.
