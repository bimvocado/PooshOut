using UnityEngine;
using TMPro;
using UnityEngine.UI;

public class WristUIController : MonoBehaviour {
    [Header("손 추적")]
    [SerializeField] private HandTracker handTracker;

    [Header("손목 UI")]
    [SerializeField] private GameObject wristCanvasRoot;

    // =========================
    // 정화도 UI
    // =========================
    [Header("정화도 UI")]
    [SerializeField] private TextMeshProUGUI purificationText;
    [SerializeField] private StageGaugeUI purificationGauge;

    [Header("정화도 소스")]
    [Tooltip("정화도용 IStageProgressProvider 구현체")]
    [SerializeField] private MonoBehaviour purificationProviderSource;

    private IStageProgressProvider purificationProvider;

    // =========================
    // 진행도 UI
    // =========================
    [Header("진행도 UI")]
    [SerializeField] private TextMeshProUGUI progressText;
    [SerializeField] private Image progressGauge;

    [Header("진행도 시간 설정 (Stage 2~4)")]
    [Tooltip("전체 스테이지 시간(초)")]
    [SerializeField] private float totalStageTime = 60f;

    private float currentStageTime;
    private bool isProgressTimerRunning;

    // =========================
    // 손바닥 감지
    // =========================
    [Header("손바닥 감지")]
    [SerializeField] private float activateAngleThreshold = 60f;
    [SerializeField] private float deactivateAngleThreshold = 100f;
    [SerializeField] private bool invertDetection = false;

    public bool IsUIVisible { get; private set; }

    private void Start() {
        ResolvePurificationProvider();

        currentStageTime = 0f;
        isProgressTimerRunning = false;

        SetVisible(false, true);
    }

    private void Update() {
        // -------------------------
        // 진행도 타이머
        // -------------------------
        if (isProgressTimerRunning) {
            currentStageTime += Time.deltaTime;

            if (currentStageTime >= totalStageTime) {
                currentStageTime = totalStageTime;
                isProgressTimerRunning = false;
            }
        }

        // -------------------------
        // 손목 UI 표시
        // -------------------------
        if (handTracker == null)
            return;

        float angle = GetPalmUpAngle();

        bool nextVisible = IsUIVisible
            ? angle < deactivateAngleThreshold
            : angle < activateAngleThreshold;

        SetVisible(nextVisible);

        if (IsUIVisible) {
            RefreshPurificationUI();
            RefreshProgressUI();
        }
    }

    // ==================================================
    // 정화도
    // ==================================================

    private void ResolvePurificationProvider() {
        purificationProvider =
            purificationProviderSource as IStageProgressProvider;

        if (purificationProvider != null)
            return;

        foreach (var behaviour in
                 FindObjectsByType<MonoBehaviour>(
                     FindObjectsSortMode.None)) {
            if (behaviour is IStageProgressProvider provider) {
                purificationProvider = provider;
                break;
            }
        }

        if (purificationProvider == null) {
            Debug.LogWarning(
                "[WristUIController] 정화도 IStageProgressProvider를 찾지 못했습니다."
            );
        }
    }

    private void RefreshPurificationUI() {
        float normalizedPurity =
            purificationProvider != null
                ? purificationProvider.NormalizedProgress
                : 0f;

        if (purificationText != null) {
            purificationText.text =
                $"{Mathf.RoundToInt(normalizedPurity * 100f)}%";
        }

        if (purificationGauge != null) {
            purificationGauge.SetFillRate(normalizedPurity);
        }
    }

    // ==================================================
    // 진행도
    // ==================================================

    private void RefreshProgressUI() {
        float normalizedProgress =
            totalStageTime > 0f
                ? Mathf.Clamp01(
                    currentStageTime / totalStageTime
                )
                : 0f;

        if (progressText != null) {
            progressText.text =
                $"{Mathf.RoundToInt(normalizedProgress * 100f)}%";
        }

        if (progressGauge != null) {
            progressGauge.fillAmount = normalizedProgress;
        }
    }

    /// <summary>
    /// 실제 스테이지가 시작될 때 호출.
    /// 진행도 시간을 0부터 시작한다.
    /// </summary>
    public void StartProgressTimer() {
        currentStageTime = 0f;
        isProgressTimerRunning = true;
    }

    public void StopProgressTimer() {
        isProgressTimerRunning = false;
    }

    public void ResumeProgressTimer() {
        if (currentStageTime < totalStageTime) {
            isProgressTimerRunning = true;
        }
    }

    public void ResetProgressTimer() {
        currentStageTime = 0f;
        isProgressTimerRunning = false;
    }

    // ==================================================
    // 손목 표시
    // ==================================================

    private float GetPalmUpAngle() {
        Vector3 palmDirection =
            handTracker.LeftHandRotation * Vector3.down;

        if (invertDetection)
            palmDirection = -palmDirection;

        return Vector3.Angle(
            palmDirection,
            Vector3.up
        );
    }

    private void SetVisible(bool visible, bool force = false) {
        if (!force && visible == IsUIVisible)
            return;

        IsUIVisible = visible;

        if (wristCanvasRoot != null) {
            wristCanvasRoot.SetActive(visible);
        }
    }
}