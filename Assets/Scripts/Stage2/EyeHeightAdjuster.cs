using UnityEngine;

/// 캘리브레이션된 키에 맞춰 XR Origin의 Y를 올려서,
/// 플레이어 키와 상관없이 눈높이가 항상 targetEyeHeight에 오도록 맞춘다.
///
/// 전제: XR Origin의 Tracking Origin Mode = Floor (바닥이 y=0)
///
/// ⚠️ 주의: 이 보정이 적용된 뒤에는 HMD의 world Y가 "실제 키 + offset"이 된다.
///   CalibrationController는 world Y를 키로 읽으므로, 재캘리브레이션(Recalibrate) 전에는
///   반드시 ResetOffset()을 먼저 호출해서 XR Origin을 0으로 되돌려야 값이 틀어지지 않는다.
public class EyeHeightAdjuster : MonoBehaviour
{
    [Tooltip("게이트 머리 구멍 중심 높이(m). 모든 플레이어의 눈이 이 높이에 오게 된다.")]
    [SerializeField] private float targetEyeHeight = 1.5f;

    [Header("보정 한계")]
    [Tooltip("상한. 에디터 시뮬레이터처럼 카메라 높이가 0인 환경에서는 targetEyeHeight만큼 올려야 하므로 넉넉히 둔다.")]
    [SerializeField] private float maxOffset = 1.6f;
    [Tooltip("키가 큰 사람은 살짝 내려가는데, 바닥을 뚫지 않도록 하한을 둔다.")]
    [SerializeField] private float minOffset = -0.3f;

    [Header("캘리브레이션 없이 이 씬만 단독 테스트할 때")]
    [Tooltip("체크하면, 캘리브레이션 값이 없을 때 카메라의 현재 높이를 직접 재서 보정한다. " +
             "실제 게임 흐름에선 캘리브레이션 값이 들어오는 순간 덮어써지므로 켜둬도 무방.")]
    [SerializeField] private bool useFallbackWhenUncalibrated = true;

    [Tooltip("HMD 카메라(XR Origin 하위 Main Camera). Floor 모드에서 이 Transform의 localPosition.y가 곧 바닥 기준 키다. " +
             "비워두면 자식에서 자동으로 찾는다.")]
    [SerializeField] private Transform hmdCamera;

    [Tooltip("카메라 높이를 못 읽었을 때만 쓰는 최후의 가정 키(m).")]
    [SerializeField] private float lastResortHeight = 1.3f;

    /// 현재 적용된 보정값(m). 디버그/재캘리브레이션 판단용.
    public float CurrentOffset { get; private set; }

    /// 실제 캘리브레이션 값으로 보정됐는지 (폴백이면 false).
    public bool IsCalibratedOffset { get; private set; }

    private void OnEnable()
    {
        if (GameManager.Instance == null) return;

        GameManager.Instance.OnHeightCalibrated += HandleCalibrated;

        // 캘리브레이션이 이미 끝난 상태로 이 씬에 진입한 경우(스테이지 전환 등) 즉시 반영
        if (GameManager.Instance.IsHeightCalibrated)
        {
            HandleCalibrated(GameManager.Instance.CalibratedHeight);
        }
    }

    private void OnDisable()
    {
        if (GameManager.Instance != null)
        {
            GameManager.Instance.OnHeightCalibrated -= HandleCalibrated;
        }
    }

    private void Awake()
    {
        if (hmdCamera == null)
        {
            Camera cam = GetComponentInChildren<Camera>();
            if (cam != null) hmdCamera = cam.transform;
        }
    }

    private void Start()
    {
        // 캘리브레이션 씬을 거치지 않고 이 씬을 바로 Play한 경우(에디터 단독 테스트).
        // 실제 게임 흐름에서는 이미 값이 들어와 있어서 여기 걸리지 않는다.
        if (IsCalibratedOffset || !useFallbackWhenUncalibrated) return;

        // 키를 "가정"하지 않고 카메라의 실제 높이를 직접 잰다.
        // Floor 모드에서 카메라의 localPosition.y = 바닥 기준 실제 높이이고,
        // 이 값은 XR Origin의 Y offset과 무관하므로 몇 번을 다시 재도 정확하다.
        float measured = MeasureCurrentHeight();
        ApplyOffset(measured);
        Debug.LogWarning($"[EyeHeightAdjuster] 캘리브레이션 값 없음 → 카메라 실측 {measured:F2}m로 보정 (단독 테스트 모드)");
    }

    /// XR Origin 기준으로 카메라가 얼마나 위에 있는지(m).
    /// XR Origin과 카메라 사이에 Camera Offset 같은 중간 오브젝트가 껴 있어도
    /// world Y 차이로 계산하므로 항상 정확하다.
    ///
    /// - 실기기: 이 값이 곧 플레이어의 실제 키
    /// - 에디터 시뮬레이터: 카메라가 XR Origin과 같은 높이면 0에 가까움
    ///   → 이 경우 offset이 targetEyeHeight 그대로가 되어 의도한 눈높이가 나온다.
    private float MeasureCurrentHeight()
    {
        if (hmdCamera == null) return lastResortHeight;
        return hmdCamera.position.y - transform.position.y;
    }

    /// 플레이 중에도 현재 카메라 높이를 다시 재서 눈높이를 맞추고 싶을 때 호출 (테스트용).
    [ContextMenu("지금 카메라 높이로 다시 맞추기")]
    public void ReadjustFromCurrentHeight()
    {
        ApplyOffset(MeasureCurrentHeight());
    }

    private void HandleCalibrated(float playerHeight)
    {
        IsCalibratedOffset = true;
        ApplyOffset(playerHeight);
    }

    private void ApplyOffset(float playerHeight)
    {
        if (playerHeight < 0f)
        {
            Debug.LogWarning("[EyeHeightAdjuster] 높이 값이 음수라 보정을 건너뜀");
            return;
        }

        CurrentOffset = Mathf.Clamp(targetEyeHeight - playerHeight, minOffset, maxOffset);

        Vector3 pos = transform.position;
        pos.y = CurrentOffset;
        transform.position = pos;

        Debug.Log($"[EyeHeightAdjuster] 키 {playerHeight:F2}m → XR Origin Y = {CurrentOffset:F2} (목표 눈높이 {targetEyeHeight:F2}m)");
    }

    /// 재캘리브레이션 직전에 호출. 보정을 풀어서 HMD의 world Y가 다시 실제 키가 되게 한다.
    public void ResetOffset()
    {
        CurrentOffset = 0f;
        IsCalibratedOffset = false;

        Vector3 pos = transform.position;
        pos.y = 0f;
        transform.position = pos;
        Debug.Log("[EyeHeightAdjuster] 보정 해제 (재측정 준비)");
    }
}