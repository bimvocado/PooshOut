using UnityEngine;

/// 머리(헤드셋) 위치 추적 + 키 기준선(캘리브레이션) 관리.
///
/// ★ XRI 기준: 인스펙터에서 XR Origin > Camera Offset > Main Camera 오브젝트를
///   headAnchor에 드래그하면 됨.
public class HeadTracker : MonoBehaviour
{
    [Header("머리 앵커 (리그의 카메라 오브젝트를 드래그)")]
    [SerializeField] private Transform headAnchor;

    /// 캘리브레이션으로 저장된 "평소 서 있을 때 머리 높이".
    public float BaselineHeight { get; private set; }

    /// 캘리브레이션 완료 여부.
    public bool IsCalibrated { get; private set; }

    /// 현재 머리 위치 (월드 좌표).
    public Vector3 HeadPosition => headAnchor != null ? headAnchor.position : Vector3.zero;

    /// 기준선 대비 현재 머리 높이 차이. (음수 = 기준보다 낮아짐 = 스쿼트 중)
    public float HeightOffsetFromBaseline =>
        IsCalibrated ? HeadPosition.y - BaselineHeight : 0f;

    /// 현재 머리 높이를 기준선으로 저장 (키 캘리브레이션).
    /// 게임 시작 시 플레이어가 편하게 서 있을 때 호출.
    /// 부스 운영 시 매 판 시작마다 다시 호출해야 함 (플레이어가 바뀌므로).
    public void Calibrate()
    {
        if (headAnchor == null)
        {
            Debug.LogWarning("[HeadTracker] headAnchor가 연결되지 않음. 인스펙터에서 리그의 카메라를 드래그하세요.");
            return;
        }
        BaselineHeight = HeadPosition.y;
        IsCalibrated = true;
        Debug.Log($"[HeadTracker] 캘리브레이션 완료. 기준 높이 = {BaselineHeight:F2}m");
    }

    /// 스쿼트 감지: 기준선보다 threshold(미터) 이상 내려갔는지.
    public bool IsSquatting(float threshold = 0.15f)
    {
        return IsCalibrated && HeightOffsetFromBaseline < -threshold;
    }

    /// 외부(CalibrationController)에서 안정성 검증을 마친 키 값을 주입.
    /// Calibrate()는 즉시 캡처용 간이 버전이고, 이건 대기/검증 로직을 거친 값을 받는 용도.
    
    public void SetBaselineHeight(float height)
    {
        BaselineHeight = height;
        IsCalibrated = true;
        Debug.Log($"[HeadTracker] 외부 캘리브레이션 값 적용. 기준 높이 = {BaselineHeight:F2}m");
    }
}
