using System;
using System.Text;
using UnityEngine;
using UnityEngine.Networking;

/// Upstage Solar API 호출 프로토타입 (OpenAI 호환 Chat Completions 형식).
/// 목표: "정화봇한테 메시지 보내면 답변이 콘솔에 찍힌다"까지 확인하는 첫 단계.
///
/// ⚠️ 보안 주의: apiKey는 지금은 테스트를 위해 인스펙터에 직접 입력하지만,
/// 이 값이 채워진 상태로 Git에 커밋하면 절대 안 됨!
/// (Standalone APK는 디컴파일로 키가 노출될 수 있음 — 최종 빌드 전에 서버 경유 방식으로 교체 예정)
///
/// 사용법(테스트):
///   1. 빈 오브젝트에 이 스크립트 붙이기
///   2. 인스펙터의 API Key 칸에 발급받은 키 입력 (테스트 후 반드시 지우고 커밋할 것)
///   3. Play 상태에서 TestSend() 호출 (컴포넌트 우클릭 → "테스트 메시지 보내기")
public class LLMConnector : MonoBehaviour
{
    [Header("API 설정 (Upstage Solar)")]
    [SerializeField] private string apiKey = ""; // ⚠️ 여기 채운 채로 커밋 금지
    [SerializeField] private string model = "solar-pro3";

    [Header("정화봇 성격 (시스템 프롬프트)")]
    [TextArea(3, 6)]
    [SerializeField]
    private string systemPrompt =
        "너는 하수처리장 VR 게임의 가이드 로봇 '정화봇'이야. " +
        "7~13세 아이들에게 하수처리 원리를 쉽고 재미있게 설명해줘. " +
        "말투는 친근한 중계 캐스터 톤이고, 응답은 2~3문장 이내로 짧게 해줘.";

    private const string ApiUrl = "https://api.upstage.ai/v1/chat/completions";

    /// 정화봇한테 상황 설명을 보내고 답변을 콜백으로 받음.
    /// 예: SendMessage("플레이어가 물티슈를 밟았어", (답변) => Debug.Log(답변));
    public void SendMessage(string userContext, Action<string> onResponse, Action<string> onError = null)
    {
        StartCoroutine(SendRequest(userContext, onResponse, onError));
    }

    private System.Collections.IEnumerator SendRequest(string userContext, Action<string> onResponse, Action<string> onError)
    {
        if (string.IsNullOrEmpty(apiKey))
        {
            Debug.LogError("[LLMConnector] API Key가 비어있음. 인스펙터에서 입력하세요.");
            onError?.Invoke("API Key 없음");
            yield break;
        }

        // OpenAI Chat Completions API 요청 형식(JSON)을 직접 조립
        string jsonBody = BuildRequestJson(userContext);

        using (UnityWebRequest request = new UnityWebRequest(ApiUrl, "POST"))
        {
            byte[] bodyBytes = Encoding.UTF8.GetBytes(jsonBody);
            request.uploadHandler = new UploadHandlerRaw(bodyBytes);
            request.downloadHandler = new DownloadHandlerBuffer();
            request.SetRequestHeader("Content-Type", "application/json");
            request.SetRequestHeader("Authorization", $"Bearer {apiKey}");

            yield return request.SendWebRequest();

            if (request.result != UnityWebRequest.Result.Success)
            {
                Debug.LogError($"[LLMConnector] 요청 실패: {request.error}\n{request.downloadHandler.text}");
                onError?.Invoke(request.error);
                yield break;
            }

            string responseText = ExtractMessageContent(request.downloadHandler.text);
            onResponse?.Invoke(responseText);
        }
    }

    /// OpenAI API가 요구하는 JSON 형식으로 요청 본문 조립.
    private string BuildRequestJson(string userContext)
    {
        // JsonUtility는 이런 중첩 구조를 다루기 불편해서, 문자열로 직접 조립.
        // 따옴표/줄바꿈 등 특수문자는 이스케이프 처리.
        string escapedSystem = EscapeJson(systemPrompt);
        string escapedUser = EscapeJson(userContext);

        return $@"{{
            ""model"": ""{model}"",
            ""messages"": [
                {{""role"": ""system"", ""content"": ""{escapedSystem}""}},
                {{""role"": ""user"", ""content"": ""{escapedUser}""}}
            ],
            ""max_tokens"": 150
        }}";
    }

    private string EscapeJson(string s)
    {
        return s.Replace("\\", "\\\\")
                 .Replace("\"", "\\\"")
                 .Replace("\r\n", "\\n")
                 .Replace("\r", "\\n")
                 .Replace("\n", "\\n")
                 .Replace("\t", "\\t");
    }

    /// OpenAI 응답 JSON에서 실제 답변 텍스트만 뽑아냄.
    /// (정식 JSON 파서 대신 최소한의 문자열 추출 — 프로토타입 단계용)
    private string ExtractMessageContent(string json)
    {
        try
        {
            const string marker = "\"content\":";
            int idx = json.IndexOf(marker, StringComparison.Ordinal);
            if (idx < 0) return "(응답 파싱 실패) " + json;

            int start = json.IndexOf('"', idx + marker.Length) + 1;
            int end = start;
            // 이스케이프된 따옴표(\")는 건너뛰고 진짜 닫는 따옴표를 찾음
            while (end < json.Length)
            {
                if (json[end] == '"' && json[end - 1] != '\\') break;
                end++;
            }
            string raw = json.Substring(start, end - start);
            return raw.Replace("\\n", "\n").Replace("\\\"", "\"");
        }
        catch (Exception e)
        {
            Debug.LogError($"[LLMConnector] 파싱 에러: {e.Message}");
            return "(파싱 에러)";
        }
    }

    ///테스트용: 인스펙터에서 우클릭 → 이 함수 실행 가능 (Context Menu).
    [ContextMenu("테스트 메시지 보내기")]
    private void TestSend()
    {
        SendMessage(
            "플레이어가 물티슈를 3번 밟았어. 짧게 코칭 멘트 해줘.",
            response => Debug.Log($"[정화봇] {response}"),
            error => Debug.LogError($"[정화봇 에러] {error}")
        );
    }
}