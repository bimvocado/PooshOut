using UnityEngine;

/// <summary>
/// 휴지 핸들(롤러)을 양손으로 잡고 좌우로 기울이는 조작.
/// 왼손/오른손 높이 차이를 조향값(-1~1)으로 변환해 RailMover에 전달.
/// </summary>
public class HandleController : MonoBehaviour
{
    [SerializeField] private HandTracker handTracker;
    [SerializeField] private RailMover railMover;

    [Header("조작 감도")]
    [SerializeField] private float tiltSensitivity = 4f; // 손 높이차 1m당 조향 배율
    [SerializeField] private float deadZone = 0.02f;      // 이 미만의 높이차는 무시 (손떨림 방지)

    public float SteeringValue { get; private set; }

    private void Update()
    {
        if (handTracker == null || railMover == null) return;

        float heightDiff = handTracker.LeftHandPosition.y - handTracker.RightHandPosition.y;
        if (Mathf.Abs(heightDiff) < deadZone) heightDiff = 0f;

        SteeringValue = Mathf.Clamp(heightDiff * tiltSensitivity, -1f, 1f);
        railMover.SetLateralInput(SteeringValue);
    }
}
