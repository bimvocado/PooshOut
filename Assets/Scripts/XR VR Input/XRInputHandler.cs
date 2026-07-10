using UnityEngine;
using UnityEngine.XR; // XRI(Unity 표준) 버튼 입력용 — Unity 기본 내장이라 패키지 필요 없음

/// <summary>
/// XR 입력 통합 창구. 다른 스크립트들은 얘만 참조하면 됨.
/// - 위치/회전: HeadTracker, HandTracker 를 묶어서 제공 (SDK 무관, 공용)
/// - 버튼 입력: SDK마다 방식이 달라서 두 벌 다 작성해둠
///
/// ★★★ 팀에서 SDK 확정되면 아래 처리 ★★★
///
/// [Meta SDK 로 확정되면]
///   1. "XRI 버튼 입력" 이라고 표시된 region 전체 삭제
///   2. GetTriggerPressed() 안에서 XRI 쪽 호출 지우고 Meta 쪽 호출만 남기기
///   3. 맨 위 using UnityEngine.XR; 삭제
///
/// [XRI 로 확정되면]
///   1. "Meta SDK 버튼 입력" 이라고 표시된 region 전체 삭제
///   2. GetTriggerPressed() 안에서 Meta 쪽 호출 지우고 XRI 쪽 호출만 남기기
///   (위치 추적은 HeadTracker/HandTracker가 Transform 기반이라 수정 필요 없음.
///    인스펙터에서 XR Origin의 카메라/컨트롤러를 다시 드래그하면 끝)
/// </summary>
public class XRInputHandler : MonoBehaviour
{
    [Header("트래커 연결")]
    [SerializeField] private HeadTracker headTracker;
    [SerializeField] private HandTracker handTracker;

    [Header("SDK 선택 (팀 확정 전 임시 스위치)")]
    [Tooltip("true = Meta SDK(OVRInput) 방식으로 버튼 읽기 / false = XRI(Unity 표준) 방식")]
    [SerializeField] private bool useMetaSdk = true;

    // --- 위치/회전 (SDK 무관, 그냥 트래커에서 전달) ---

    public Vector3 HeadPosition => headTracker.HeadPosition;
    public Vector3 LeftHandPosition => handTracker.LeftHandPosition;
    public Vector3 RightHandPosition => handTracker.RightHandPosition;
    public Vector3 LeftHandVelocity => handTracker.LeftHandVelocity;
    public Vector3 RightHandVelocity => handTracker.RightHandVelocity;

    public HeadTracker Head => headTracker;
    public HandTracker Hands => handTracker;

    // --- 버튼 입력 (SDK별로 다름 — 확정 후 한쪽 삭제) ---

    /// <summary>
    /// 트리거(검지 버튼)가 눌렸는지. Stage 4 버튼 다이빙 등에 사용.
    /// </summary>
    public bool GetTriggerPressed()
    {
        if (useMetaSdk)
        {
            return GetTriggerPressed_Meta();   // ← XRI 확정 시 이 줄 삭제
        }
        return GetTriggerPressed_XRI();        // ← Meta 확정 시 이 줄과 위 if문 정리
    }

    #region Meta SDK 버튼 입력 (XRI 확정 시 이 region 전체 삭제)

    private bool GetTriggerPressed_Meta()
    {
        // OVRInput: Meta XR Core SDK 제공. 양손 트리거 중 하나라도 눌리면 true.
        return OVRInput.Get(OVRInput.Button.PrimaryIndexTrigger, OVRInput.Controller.LTouch)
            || OVRInput.Get(OVRInput.Button.PrimaryIndexTrigger, OVRInput.Controller.RTouch);
    }

    #endregion

    #region XRI 버튼 입력 (Meta SDK 확정 시 이 region 전체 삭제)

    private bool GetTriggerPressed_XRI()
    {
        // UnityEngine.XR.InputDevices: Unity 기본 내장 XR 입력. XRI 패키지 없어도 동작.
        bool left = GetTriggerFromDevice(XRNode.LeftHand);
        bool right = GetTriggerFromDevice(XRNode.RightHand);
        return left || right;
    }

    private bool GetTriggerFromDevice(XRNode node)
    {
        InputDevice device = InputDevices.GetDeviceAtXRNode(node);
        if (device.isValid && device.TryGetFeatureValue(CommonUsages.triggerButton, out bool pressed))
        {
            return pressed;
        }
        return false;
    }

    #endregion
}
