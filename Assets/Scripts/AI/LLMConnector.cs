using System;
using System.Collections;
using System.Collections.Generic;
using System.Text;
using UnityEngine;
using UnityEngine.Networking;

/// <summary>
/// FastAPI 서버와 통신하는 커넥터. 두 가지 역할만 남김:
///   1. 자유 질문 (AskFreeQuestion) - 나중에 STT 붙여서 아이가 버튼 누르고 말로 물어보면
///      호출할 예정. POST /chat 사용.
///   2. 마무리 피드백 (RequestFeedback) - 엔딩씬1에서 결과 보드 뜨는 순간 1회 호출.
///      POST /feedback 사용.
///
/// 원래 있던 자동 트리거 3종(스테이지 진입/오염물 접촉/스테이지 클리어)은 실사용 안 해서
/// 제거함 - 해당 상황들은 전부 wav로 미리 뽑아둔 고정 멘트(PooshLineBank)로 대체됨.
/// Upstage API 키는 서버 쪽에만 있음 - 클라이언트는 API 키를 전혀 갖지 않는다
/// (Standalone APK 디컴파일로 키가 노출되는 문제 방지). 서버 스펙은 docs/server-api-spec.md 참고.
/// </summary>
public class LLMConnector : Singleton<LLMConnector>
{
    // 서버 주소는 ServerConfig 한 곳에서만 관리 (LLMConnector/SaveLoadManager 공용).
    private const string SERVER_URL = ServerConfig.SERVER_URL;
    private const string ChatEndpoint = SERVER_URL + "/chat";
    private const string FeedbackEndpoint = SERVER_URL + "/feedback";

    [Header("서버 요청 설정")]
    [Tooltip("연속 요청 사이 최소 간격(초). 어린이가 버튼을 연타해도 서버를 과호출하지 않도록 방지.")]
    [SerializeField, Min(3f)] private float requestCooldown = 3f;
    [Tooltip("서버 응답을 이 시간(초) 안에 못 받으면 오프라인으로 간주하고 폴백 메시지를 출력.")]
    [SerializeField, Min(1f)] private float requestTimeoutSeconds = 10f;

    [Header("오프라인 폴백 (서버 미연결 시 정화봇이 대신 하는 말)")]
    [SerializeField]
    private string[] offlineFallbackMessages =
    {
        "지금은 정화봇이 신호가 안 잡혀서 말을 못 해줘! 조금만 기다려줄래?",
        "어라, 정화봇 연결이 잠깐 끊겼나 봐! 계속 탐험해볼까?",
    };

    [Header("정화봇 성격 (자유 질문용 시스템 프롬프트)")]
    [TextArea(3, 6)]
    [SerializeField]
    private string personaPrompt =
        "너는 하수처리장 VR 교육 게임의 가이드 로봇 '정화봇'이야. " +
        "이 게임을 하는 친구들은 7~13세 초등학생이야. " +
        "어려운 단어 대신 쉬운 말과 재미있는 비유를 써서 설명해줘. " +
        "말투는 친근하고 다정한 반말 캐스터 톤이야(예: '~했어!', '~해볼까?'). " +
        "절대 아이를 혼내거나 겁주지 말고, 응답은 2~3문장 이내로 짧게 해줘.";

    private float _lastRequestTime = -999f;

    // ─────────────────────────────────────────────
    // ① 자유 질문 (나중에 STT 연동 예정 - 버튼 누르고 말하면 텍스트로 변환된 걸 여기로 전달)
    // ─────────────────────────────────────────────

    /// <summary>UI 버튼("물어보기" 등)에서 호출. 어린이가 직접 입력(또는 STT로 변환)한 질문을 그대로 전달.</summary>
    public void AskFreeQuestion(string question, Action<string> onResponse, Action<string> onError = null)
    {
        if (string.IsNullOrWhiteSpace(question))
        {
            onError?.Invoke("질문 내용이 비어있음");
            return;
        }

        RequestGuideSpeech(BuildFreeQuestionContext(), question, onResponse, onError);
    }

    private string BuildFreeQuestionContext()
    {
        int stage = StageManager.Instance != null ? StageManager.Instance.CurrentStage : StageManager.FirstStage;

        return $"{personaPrompt}\n\n[상황] 플레이어는 지금 {stage}번째 스테이지를 플레이하는 중이고, " +
               "정화봇에게 궁금한 걸 자유롭게 물어봤어. 눈높이에 맞게 답해줘.";
    }

    // ---------- API 요청 (자유 질문용 /chat) ----------

    private void RequestGuideSpeech(string context, string userMessage, Action<string> onResponse, Action<string> onError)
    {
        float elapsed = Time.time - _lastRequestTime;
        if (elapsed < requestCooldown)
        {
            string message = $"요청 쿨다운 중 ({requestCooldown - elapsed:0.0}초 남음)";
            Debug.Log($"[LLMConnector] 요청 무시: {message}");
            onError?.Invoke(message);
            return;
        }

        _lastRequestTime = Time.time;
        StartCoroutine(SendRequest(context, userMessage, onResponse, onError));
    }

    private IEnumerator SendRequest(string context, string userMessage, Action<string> onResponse, Action<string> onError)
    {
        string jsonBody = BuildRequestJson(userMessage, context);

        using (UnityWebRequest request = new UnityWebRequest(ChatEndpoint, "POST"))
        {
            byte[] bodyBytes = Encoding.UTF8.GetBytes(jsonBody);
            request.uploadHandler = new UploadHandlerRaw(bodyBytes);
            request.downloadHandler = new DownloadHandlerBuffer();
            request.SetRequestHeader("Content-Type", "application/json");
            request.timeout = Mathf.CeilToInt(requestTimeoutSeconds);

            yield return request.SendWebRequest();

            if (request.result != UnityWebRequest.Result.Success)
            {
                Debug.LogWarning($"[LLMConnector] 서버 요청 실패, 오프라인 폴백으로 대체: {request.error}");
                onResponse?.Invoke(PickOfflineFallback());
                onError?.Invoke(request.error);
                yield break;
            }

            string responseText = ExtractReply(request.downloadHandler.text);
            onResponse?.Invoke(responseText);
        }
    }

    private string PickOfflineFallback()
    {
        if (offlineFallbackMessages == null || offlineFallbackMessages.Length == 0)
            return "지금은 정화봇이 잠깐 자리를 비웠어!";
        return offlineFallbackMessages[UnityEngine.Random.Range(0, offlineFallbackMessages.Length)];
    }

    /// <summary>서버 /chat 요청 본문을 JsonUtility로 안전하게 조립. 스펙: docs/server-api-spec.md</summary>
    private string BuildRequestJson(string message, string context)
    {
        var body = new ChatRequest { message = message, context = context };
        return JsonUtility.ToJson(body);
    }

    /// <summary>서버 /chat 응답 JSON을 JsonUtility로 파싱해 reply만 추출.</summary>
    private string ExtractReply(string json)
    {
        try
        {
            ChatResponse response = JsonUtility.FromJson<ChatResponse>(json);
            if (response == null || string.IsNullOrEmpty(response.reply))
            {
                Debug.LogError($"[LLMConnector] 응답 파싱 실패(reply 없음): {json}");
                return PickOfflineFallback();
            }

            // TTS 음성이 왔으면 재생 (audioUrl이 없으면 PooshVoicePlayer가 알아서 무시하고 텍스트만 표시됨)
            if (PooshVoicePlayer.Instance != null)
            {
                PooshVoicePlayer.Instance.PlayFromUrl(response.audioUrl);
            }

            return response.reply;
        }
        catch (Exception e)
        {
            Debug.LogError($"[LLMConnector] 파싱 에러: {e.Message}\n{json}");
            return PickOfflineFallback();
        }
    }

    /// <summary>테스트용: 인스펙터에서 우클릭 → 이 함수 실행 가능 (Context Menu).</summary>
    [ContextMenu("테스트: 자유 질문 보내기")]
    private void TestSend()
    {
        AskFreeQuestion(
            "정화봇, 미생물은 왜 우리 편이야?",
            speech => Debug.Log($"[정화봇] {speech}"),
            error => Debug.LogError($"[정화봇 에러] {error}"));
    }

    // ---------- POST /chat JSON 스키마 (JsonUtility 직렬화용, 서버 스펙: docs/server-api-spec.md) ----------

    [Serializable]
    private class ChatRequest
    {
        public string message;
        public string context;
    }

    [Serializable]
    private class ChatResponse
    {
        public string reply;
        public string audioUrl;
    }

    // ─────────────────────────────────────────────
    // ② 마무리 피드백 (엔딩씬1에서 결과 보드 뜨는 순간 1회 호출)
    // 서버 /feedback 스펙(main.py의 FeedbackRequest/StageLog)과 필드명을 정확히 맞춰야 함.
    // ─────────────────────────────────────────────

    [Serializable]
    private class ServerStageLog
    {
        public int stage;
        public int success;
        public int fail;
        public string note;
        public float purity;
    }

    [Serializable]
    private class FeedbackRequestJson
    {
        public string playerName;
        public int totalPurity;
        public List<ServerStageLog> stages;
    }

    [Serializable]
    public class FeedbackResponse
    {
        public string child_message;
        public string audioUrl;
        // guardian_summary는 서버에서 더 이상 안 보냄 (필요 없어져서 제거됨)
    }

    /// <summary>
    /// 엔딩씬1에서 결과 보드가 뜨는 순간 1회 호출. PurificationSystem에 쌓여있는
    /// 스테이지별 플레이 로그(StagePlayLog)를 그대로 서버로 넘겨서, LLM이 만든 통합
    /// 피드백(고정 오프닝+칭찬+기대감+작별까지 하나로 이어진 문장) 음성을 받아온다.
    /// </summary>
    public void RequestFeedback(int totalPurity, List<StagePlayLog> logs, Action<FeedbackResponse> onResponse)
    {
        StartCoroutine(SendFeedbackRequest(totalPurity, logs, onResponse));
    }

    private IEnumerator SendFeedbackRequest(int totalPurity, List<StagePlayLog> logs, Action<FeedbackResponse> onResponse)
    {
        var requestData = new FeedbackRequestJson
        {
            playerName = GameManager.Instance != null && GameManager.Instance.HasPlayerName
                ? GameManager.Instance.PlayerName
                : "친구",
            totalPurity = totalPurity,
            stages = new List<ServerStageLog>(),
        };

        if (logs != null)
        {
            foreach (var log in logs)
            {
                requestData.stages.Add(new ServerStageLog
                {
                    stage = log.stage,
                    success = log.success,
                    fail = log.fail,
                    note = log.note,
                    purity = log.purity,
                });
            }
        }

        string jsonBody = JsonUtility.ToJson(requestData);

        using (UnityWebRequest request = new UnityWebRequest(FeedbackEndpoint, "POST"))
        {
            byte[] bodyBytes = Encoding.UTF8.GetBytes(jsonBody);
            request.uploadHandler = new UploadHandlerRaw(bodyBytes);
            request.downloadHandler = new DownloadHandlerBuffer();
            request.SetRequestHeader("Content-Type", "application/json");
            // 마무리 피드백은 LLM+TTS를 실시간으로 거치므로 일반 /chat보다 여유 있게 잡는다.
            request.timeout = 20;

            yield return request.SendWebRequest();

            if (request.result != UnityWebRequest.Result.Success)
            {
                Debug.LogError($"[LLMConnector] /feedback 요청 실패: {request.error}");
                onResponse?.Invoke(null);
                yield break;
            }

            try
            {
                FeedbackResponse response = JsonUtility.FromJson<FeedbackResponse>(request.downloadHandler.text);
                onResponse?.Invoke(response);
            }
            catch (Exception e)
            {
                Debug.LogError($"[LLMConnector] /feedback 응답 파싱 실패: {e.Message}\n{request.downloadHandler.text}");
                onResponse?.Invoke(null);
            }
        }
    }
}