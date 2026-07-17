using UnityEngine;
using UnityEngine.XR; // XRI(Unity 표준) 버튼 입력용 — Unity 기본 내장이라 패키지 필요 없음

/// <summary>
/// XR 입력 통합 창구. 다른 스크립트들은 얘만 참조하면 됨.
/// - 위치/회전: HeadTracker, HandTracker 를 묶어서 제공
/// - 버튼 입력: XRI(Unity 표준) 방식
/// </summary>
public class XRInputHandler : MonoBehaviour
{
    [Header("트래커 연결")]
    [SerializeField] private HeadTracker headTracker;
    [SerializeField] private HandTracker handTracker;

    // --- 위치/회전 (SDK 무관, 그냥 트래커에서 전달) ---

    public Vector3 HeadPosition => headTracker.HeadPosition;
    public Vector3 LeftHandPosition => handTracker.LeftHandPosition;
    public Vector3 RightHandPosition => handTracker.RightHandPosition;
    public Vector3 LeftHandVelocity => handTracker.LeftHandVelocity;
    public Vector3 RightHandVelocity => handTracker.RightHandVelocity;

    public HeadTracker Head => headTracker;
    public HandTracker Hands => handTracker;

    // --- 버튼 입력 (XRI) ---

    /// <summary>
    /// 트리거(검지 버튼)가 눌렸는지. Stage 4 버튼 다이빙 등에 사용.
    /// </summary>
    public bool GetTriggerPressed()
    {
        return GetTriggerPressed_XRI();
    }

    #region XRI 버튼 입력

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
