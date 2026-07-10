using UnityEngine;

/// <summary>
/// 플레이어 캐릭터의 기본 상태 관리.
/// 위치값은 XRInputHandler를 통해 받음 (IPositionSource/Mock 방식은 세팅 완료로 제거).
///
/// ★ Meta SDK / XRI 공용 — XRInputHandler가 SDK 차이를 흡수하므로 이 스크립트는 수정 불필요.
/// </summary>
public class PlayerController : MonoBehaviour
{
    [SerializeField] private XRInputHandler xrInput;

    public Vector3 HeadPosition => xrInput != null ? xrInput.HeadPosition : Vector3.zero;
    public Vector3 LeftHandPosition => xrInput != null ? xrInput.LeftHandPosition : Vector3.zero;
    public Vector3 RightHandPosition => xrInput != null ? xrInput.RightHandPosition : Vector3.zero;

    /// <summary>양손 사이 중간 지점 (침전/조향 계산에 사용 가능).</summary>
    public Vector3 HandsMidpoint()
    {
        return (LeftHandPosition + RightHandPosition) * 0.5f;
    }

    /// <summary>머리 기준 상대 벡터 (포즈 판정용 — 아동 체형 대응의 핵심).</summary>
    public Vector3 LeftHandRelativeToHead() => LeftHandPosition - HeadPosition;
    public Vector3 RightHandRelativeToHead() => RightHandPosition - HeadPosition;
}
