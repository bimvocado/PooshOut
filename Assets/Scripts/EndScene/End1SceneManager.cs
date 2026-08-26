using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;

public class End1SceneManager : MonoBehaviour
{
    [Header("BGM")]
    [SerializeField] private AudioSource bgmAudioSource;
    [SerializeField] private AudioClip bgmClip;

    [Header("첨벙 후 UI")]
    [SerializeField] private GameObject completeUI;
    [SerializeField] private float uiDelay = 2f;

    [Header("정화봇 (결과 보드 등장 위치)")]
    [Tooltip("결과 보드(completeUI) 근처에 배치해둘 빈 오브젝트. 정화봇이 이 위치에 고정되어 등장함.")]
    [SerializeField] private Transform pooshBotAnchor;

    [Header("다음 씬")]
    [Tooltip("정화봇 피드백이 끝난 뒤 다음 씬으로 넘어가기까지의 여유 시간(초).")]
    [SerializeField] private float nextSceneDelay = 3f;
    [SerializeField] private string nextSceneName = "End2Scene";

    private bool uiStarted;

    private void Awake()
    {
        // 처음에는 완료 UI 숨김
        if (completeUI != null)
        {
            completeUI.SetActive(false);
        }

        // BGM 설정
        if (bgmAudioSource != null)
        {
            bgmAudioSource.playOnAwake = false;
            bgmAudioSource.loop = true;
            bgmAudioSource.spatialBlend = 0f;
        }
    }

    private void Start()
    {
        PlayBGM();

        // 물방울 낙하 연출 동안 정화봇은 완전히 숨김 (아무 멘트도 안 함)
        PooshBotDirector.Instance?.EnterEndingSceneOne();
    }

    private void PlayBGM()
    {
        if (bgmAudioSource == null || bgmClip == null)
        {
            Debug.LogWarning("[End1SceneManager] BGM 설정이 비어있음");
            return;
        }

        bgmAudioSource.clip = bgmClip;

        if (!bgmAudioSource.isPlaying)
        {
            bgmAudioSource.Play();
        }
    }

    /// <summary>
    /// WaterDrop이 바닥에 첨벙했을 때 호출
    /// </summary>
    public void OnWaterSplash()
    {
        // 여러 물방울이 충돌해도 UI는 한 번만 띄움
        if (uiStarted)
            return;

        uiStarted = true;

        Debug.Log("[End1SceneManager] 첨벙 감지 - 2초 후 UI 표시");

        StartCoroutine(ShowUIAfterDelay());
    }

    private IEnumerator ShowUIAfterDelay()
    {
        yield return new WaitForSeconds(uiDelay);

        if (completeUI != null)
        {
            completeUI.SetActive(true);
        }
        else
        {
            Debug.LogWarning("[End1SceneManager] Complete UI가 연결되지 않음");
        }

        // 결과 보드가 뜨는 이 순간 = 정화봇이 pooshBotAnchor 위치에 고정 등장해서
        // "해냈다!~작별인사"까지 합쳐진 통합 피드백 음성을 재생하는 순간.
        // 그 피드백이 다 끝나면(onDone) 잠깐 뒤 다음 씬(순위표)으로 넘어간다.
        PooshBotDirector.Instance?.PlayEndingFeedbackAndFarewell(pooshBotAnchor, onDone =>
        {
            StartCoroutine(RequestFeedbackAndPlay(onDone));
        });
    }

    // ─────────────────────────────────────────────
    // 실제 플레이 데이터로 서버 /feedback 호출 + 응답 음성 재생 + 다음 씬 전환
    // ─────────────────────────────────────────────
    private IEnumerator RequestFeedbackAndPlay(System.Action onDone)
    {
        if (LLMConnector.Instance == null || PurificationSystem.Instance == null)
        {
            Debug.LogWarning("[End1SceneManager] LLMConnector 또는 PurificationSystem이 없어서 피드백을 건너뜀");
            onDone?.Invoke();
            GoToNextScene();
            yield break;
        }

        // 스테이지 1~4 최종 정화도 합산 (스크린샷의 "198%" 같은 값 - 최대 400)
        int totalPurity = 0;
        for (int stage = 1; stage <= 4; stage++)
        {
            totalPurity += Mathf.RoundToInt(PurificationSystem.Instance.GetStagePurity(stage));
        }

        var logs = PurificationSystem.Instance.GetAllStageLogs();

        bool responseReceived = false;
        LLMConnector.FeedbackResponse feedback = null;

        LLMConnector.Instance.RequestFeedback(totalPurity, logs, response => {
            feedback = response;
            responseReceived = true;
        });

        yield return new WaitUntil(() => responseReceived);

        if (feedback != null && !string.IsNullOrEmpty(feedback.audioUrl))
        {
            Debug.Log($"[End1SceneManager] 피드백 수신: {feedback.child_message}");
            PooshVoicePlayer.Instance?.PlayFromUrl(feedback.audioUrl);

            yield return null; // 재생 시작 전 한 프레임은 IsPlaying이 false일 수 있음
            yield return new WaitUntil(() => PooshVoicePlayer.Instance == null || !PooshVoicePlayer.Instance.IsPlaying);
        }
        else
        {
            Debug.LogWarning("[End1SceneManager] 피드백 응답이 없거나 audioUrl이 비어있음 - 멘트 없이 넘어감");
        }

        onDone?.Invoke(); // 정화봇이 사라지는 신호 (PooshBotDirector 쪽에서 처리)

        // 정화봇 피드백이 다 끝난 뒤 잠깐 여유를 두고 다음 씬(순위표)으로 전환.
        yield return new WaitForSeconds(nextSceneDelay);
        GoToNextScene();
    }

    private void GoToNextScene()
    {
        if (string.IsNullOrEmpty(nextSceneName))
        {
            Debug.LogWarning("[End1SceneManager] nextSceneName이 비어있어서 씬 전환 안 함");
            return;
        }

        SceneManager.LoadScene(nextSceneName);
    }
}