using UnityEngine;
using TMPro;

public class WristUIController : MonoBehaviour {
    [Header("손 추적")]
    [SerializeField] private HandTracker handTracker;

    [Header("손목 UI")]
    [SerializeField] private GameObject wristCanvasRoot;

    [Header("정화도 UI")]
    [SerializeField] private TextMeshProUGUI purificationText;
    [SerializeField] private StageGaugeUI purificationGauge;

    [Header("정화도 소스")]
    [Tooltip("IStageProgressProvider 구현체. 비워두면 자동 탐색")]
    [SerializeField] private MonoBehaviour progressProviderSource;

    private IStageProgressProvider progressProvider;

    [Header("손바닥 감지")]
    [SerializeField] private float activateAngleThreshold = 60f;
    [SerializeField] private float deactivateAngleThreshold = 100f;
    [SerializeField] private bool invertDetection = false;

    public bool IsUIVisible { get; private set; }

    private void Start() {
        ResolveProgressProvider();
        SetVisible(false, true);
    }

    private void ResolveProgressProvider() {
        progressProvider =
            progressProviderSource as IStageProgressProvider;

        if (progressProvider != null)
            return;

        foreach (
            var behaviour in FindObjectsByType<MonoBehaviour>(
                FindObjectsSortMode.None
            )) {
            if (behaviour is IStageProgressProvider provider) {
                progressProvider = provider;
                break;
            }
        }

        if (progressProvider == null) {
            Debug.LogWarning(
                "[WristUIController] IStageProgressProvider를 찾지 못했습니다."
            );
        }
    }

    private void Update() {
        if (handTracker == null)
            return;

        float angle = GetPalmUpAngle();

        bool nextVisible = IsUIVisible
            ? angle < deactivateAngleThreshold
            : angle < activateAngleThreshold;

        SetVisible(nextVisible);

        if (IsUIVisible) {
            RefreshPurificationUI();
        }
    }

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

        if (wristCanvasRoot != null)
            wristCanvasRoot.SetActive(visible);
    }

    private void RefreshPurificationUI() {
        float normalizedPurity =
            progressProvider != null
                ? progressProvider.NormalizedProgress
                : 0f;

        // 숫자: 0~100%
        if (purificationText != null) {
            purificationText.text =
                $"{Mathf.RoundToInt(normalizedPurity * 100f)}%";
        }

        // 게이지: 0~1
        if (purificationGauge != null) {
            purificationGauge.SetFillRate(normalizedPurity);
        }
    }
}