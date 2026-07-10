using UnityEngine;

/// <summary>
/// 양손(컨트롤러) 위치 추적.
///
/// ★ Meta SDK / XRI 공용 스크립트 — 코드 수정 없이 둘 다 지원.
///   차이는 인스펙터에서 어떤 오브젝트를 드래그하느냐 뿐:
///   - Meta SDK 선택 시: OVRCameraRig > TrackingSpace > LeftHandAnchor / RightHandAnchor
///   - XRI 선택 시:      XR Origin > Camera Offset > Left Controller / Right Controller
/// </summary>
public class HandTracker : MonoBehaviour
{
    [Header("손 앵커 (리그의 왼손/오른손 오브젝트를 드래그)")]
    [SerializeField] private Transform leftHandAnchor;
    [SerializeField] private Transform rightHandAnchor;

    public Vector3 LeftHandPosition => leftHandAnchor != null ? leftHandAnchor.position : Vector3.zero;
    public Vector3 RightHandPosition => rightHandAnchor != null ? rightHandAnchor.position : Vector3.zero;

    public Quaternion LeftHandRotation => leftHandAnchor != null ? leftHandAnchor.rotation : Quaternion.identity;
    public Quaternion RightHandRotation => rightHandAnchor != null ? rightHandAnchor.rotation : Quaternion.identity;

    // --- 속도 (velocity) ---
    // 가속도 센서 대신 위치 변화량으로 직접 계산.
    // 이유: 시뮬레이터에선 센서값이 부정확할 수 있지만 위치는 항상 정확함.
    //       위치 기반이라 Meta/XRI/시뮬레이터/실기기 모두 동일하게 동작. (Stage 2 스윕 감지용)

    public Vector3 LeftHandVelocity { get; private set; }
    public Vector3 RightHandVelocity { get; private set; }

    private Vector3 _prevLeftPos;
    private Vector3 _prevRightPos;

    private void Start()
    {
        _prevLeftPos = LeftHandPosition;
        _prevRightPos = RightHandPosition;
    }

    private void Update()
    {
        float dt = Time.deltaTime;
        if (dt <= 0f) return;

        LeftHandVelocity = (LeftHandPosition - _prevLeftPos) / dt;
        RightHandVelocity = (RightHandPosition - _prevRightPos) / dt;

        _prevLeftPos = LeftHandPosition;
        _prevRightPos = RightHandPosition;
    }
}
