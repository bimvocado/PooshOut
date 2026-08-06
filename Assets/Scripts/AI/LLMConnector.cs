using System;
using System.Collections;
using System.Text;
using UnityEngine;
using UnityEngine.Networking;

/// <summary>
/// FastAPI 서버의 POST /chat 엔드포인트를 호출해서 가이드 로봇 '정화봇'의 대사를 받아오는 커넥터.
/// Upstage Solar API 호출과 API 키는 서버 쪽으로 옮겼음 — 클라이언트는 API 키를 전혀 갖지 않는다
/// (Standalone APK 디컴파일로 키가 노출되는 문제 해결). 서버 스펙은 docs/server-api-spec.md 참고.
///
/// 게임 이벤트 4종에 훅을 걸어 상황에 맞는 컨텍스트를 요청 본문의 context에 실어 보낸다:
///   1. 스테이지 진입 (StageManager.OnStageChanged)
///   2. 오염물 접촉    (PurificationSystem.OnPollutantContact)
///   3. 자유 질문      (AskFreeQuestion, UI 버튼에서 호출)
///   4. 스테이지 클리어 (StageManager.OnStageCleared)
/// </summary>
public class LLMConnector : Singleton<LLMConnector>
{
    // 서버 주소는 ServerConfig 한 곳에서만 관리 (LLMConnector/SaveLoadManager 공용).
    private const string SERVER_URL = ServerConfig.SERVER_URL;
    private const string ChatEndpoint = SERVER_URL + "/chat";

    [Header("서버 요청 설정")]
    [Tooltip("연속 요청 사이 최소 간격(초). 어린이가 버튼을 연타해도 서버를 과호출하지 않도록 방지.")]
    [SerializeField, Min(3f)] private float requestCooldown = 3f;
    [Tooltip("서버 응답을 이 시간(초) 안에 못 받으면 오프라인으로 간주하고 폴백 메시지를 출력.")]
    [SerializeField, Min(1f)] private float requestTimeoutSeconds = 5f;

    [Header("오프라인 폴백 (서버 미연결 시 정화봇이 대신 하는 말)")]
    [SerializeField]
    private string[] offlineFallbackMessages =
    {
        "지금은 정화봇이 신호가 안 잡혀서 말을 못 해줘! 조금만 기다려줄래?",
        "어라, 정화봇 연결이 잠깐 끊겼나 봐! 계속 탐험해볼까?",
    };

    [Header("정화봇 성격 (시스템 프롬프트 기본값)")]
    [TextArea(3, 6)]
    [SerializeField]
    private string personaPrompt =
        "너는 하수처리장 VR 교육 게임의 가이드 로봇 '정화봇'이야. " +
        "이 게임을 하는 친구들은 7~13세 초등학생이야. " +
        "어려운 단어 대신 쉬운 말과 재미있는 비유를 써서 설명해줘. " +
        "말투는 친근하고 다정한 반말 캐스터 톤이야(예: '~했어!', '~해볼까?'). " +
        "절대 아이를 혼내거나 겁주지 말고, 응답은 2~3문장 이내로 짧게 해줘.";

    [Header("스테이지별 원리 설명")]
    [SerializeField]
    private StagePrincipleInfo[] stagePrinciples =
    {
        new StagePrincipleInfo { stage = 1, stageName = "스크린/침사지", principle = "큰 쓰레기와 모래를 촘촘한 그물망으로 걸러내는 첫 번째 관문" },
        new StagePrincipleInfo { stage = 2, stageName = "침전지", principle = "물보다 무거운 찌꺼기를 가만히 가라앉혀서 걸러내는 곳" },
        new StagePrincipleInfo { stage = 3, stageName = "폭기조", principle = "산소를 넣어주면 착한 미생물들이 힘을 내서 남은 오염물을 분해하는 곳" },
        new StagePrincipleInfo { stage = 4, stageName = "방류", principle = "깨끗해진 물을 마지막으로 소독한 뒤 강으로 돌려보내는 곳" },
    };

    private float _lastRequestTime = -999f;

    // ---------- 이벤트 훅 연결 ----------

    private void OnEnable()
    {
        if (StageManager.Instance != null)
        {
            StageManager.Instance.OnStageChanged += HandleStageEntered;
            StageManager.Instance.OnStageCleared += HandleStageCleared;
        }
        if (PurificationSystem.Instance != null)
        {
            PurificationSystem.Instance.OnPollutantContact += HandlePollutantContact;
        }
    }

    private void OnDisable()
    {
        if (StageManager.Instance != null)
        {
            StageManager.Instance.OnStageChanged -= HandleStageEntered;
            StageManager.Instance.OnStageCleared -= HandleStageCleared;
        }
        if (PurificationSystem.Instance != null)
        {
            PurificationSystem.Instance.OnPollutantContact -= HandlePollutantContact;
        }
    }

    private void Start()
    {
        // OnStageChanged는 "바뀔 때"만 울리므로, 시작 시점의 스테이지는 직접 안내해줌.
        if (StageManager.Instance != null)
        {
            HandleStageEntered(StageManager.Instance.CurrentStage);
        }
    }

    // ---------- 트리거 1: 스테이지 진입 ----------

    private void HandleStageEntered(int stage)
    {
        RequestGuideSpeech(
            BuildStageEnterContext(stage),
            "지금 상황에 맞게 짧게 한마디 해줘.",
            speech => Debug.Log($"[정화봇] {speech}"),
            error => Debug.LogWarning($"[정화봇] 스테이지 안내 요청 실패: {error}"));
    }

    // ---------- 트리거 2: 오염물 접촉 ----------

    private void HandlePollutantContact(string pollutantLabel)
    {
        RequestGuideSpeech(
            BuildPollutantContactContext(pollutantLabel),
            "지금 상황에 맞게 짧게 코칭해줘.",
            speech => Debug.Log($"[정화봇] {speech}"),
            error => Debug.LogWarning($"[정화봇] 오염물 코칭 요청 실패: {error}"));
    }

    // ---------- 트리거 3: 자유 질문 (버튼 입력) ----------

    /// <summary>UI 버튼(질문 입력창의 "물어보기" 버튼 등)에서 호출. 어린이가 직접 입력한 질문을 그대로 전달.</summary>
    public void AskFreeQuestion(string question, Action<string> onResponse, Action<string> onError = null)
    {
        if (string.IsNullOrWhiteSpace(question))
        {
            onError?.Invoke("질문 내용이 비어있음");
            return;
        }

        RequestGuideSpeech(BuildFreeQuestionContext(), question, onResponse, onError);
    }

    // ---------- 트리거 4: 스테이지 클리어 ----------

    private void HandleStageCleared(int clearedStage)
    {
        RequestGuideSpeech(
            BuildStageClearContext(clearedStage),
            "지금 상황에 맞게 짧게 축하해줘.",
            speech => Debug.Log($"[정화봇] {speech}"),
            error => Debug.LogWarning($"[정화봇] 클리어 피드백 요청 실패: {error}"));
    }

    // ---------- 컨텍스트 조립 (게임 상황 → system prompt) ----------

    private string BuildStageEnterContext(int stage)
    {
        StagePrincipleInfo info = FindStagePrinciple(stage);
        string name = info != null ? info.stageName : $"{stage}번째 구역";
        string principle = info != null ? info.principle : "이번 구역의 정화 원리";

        return $"{personaPrompt}\n\n[상황] 플레이어가 방금 '{name}'({stage}번째 스테이지)에 들어왔어. " +
               $"여기는 '{principle}'인 곳이야. 이 원리를 입장 멘트로 짧고 재미있게 설명해줘.";
    }

    private string BuildPollutantContactContext(string pollutantLabel)
    {
        string purityInfo = TryGetPurityText();

        return $"{personaPrompt}\n\n[상황] 플레이어가 오염물 '{pollutantLabel}'에 닿아서 정화도가 깎였어{purityInfo}. " +
               "왜 이게 물을 더럽히는지, 다음엔 어떻게 피하면 좋을지 혼내지 말고 다정하게 코칭해줘.";
    }

    private string BuildFreeQuestionContext()
    {
        int stage = StageManager.Instance != null ? StageManager.Instance.CurrentStage : StageManager.FirstStage;

        return $"{personaPrompt}\n\n[상황] 플레이어는 지금 {stage}번째 스테이지를 플레이하는 중이고, " +
               "정화봇에게 궁금한 걸 자유롭게 물어봤어. 눈높이에 맞게 답해줘.";
    }

    private string BuildStageClearContext(int clearedStage)
    {
        string purityInfo = TryGetPurityText();

        return $"{personaPrompt}\n\n[상황] 플레이어가 {clearedStage}번째 스테이지를 클리어했어{purityInfo}. " +
               "짧게 칭찬해주고, 다음엔 어떤 곳이 기다리고 있을지 기대감을 살짝 심어줘.";
    }

    private string TryGetPurityText()
    {
        if (PurificationSystem.Instance == null) return "";
        return $" (현재 정화도 {PurificationSystem.Instance.Purity:0}%)";
    }

    private StagePrincipleInfo FindStagePrinciple(int stage)
    {
        foreach (StagePrincipleInfo info in stagePrinciples)
        {
            if (info.stage == stage) return info;
        }
        return null;
    }

    // ---------- API 요청 ----------

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

    [Serializable]
    private class StagePrincipleInfo
    {
        public int stage;
        public string stageName;
        public string principle;
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
    }
}
