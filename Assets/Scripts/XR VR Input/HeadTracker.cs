using UnityEngine;

/// <summary>
/// 머리(헤드셋) 위치 추적 + 키 기준선(캘리브레이션) 관리.
///
/// ★ Meta SDK / XRI 공용 스크립트 — 코드 수정 없이 둘 다 지원.
///   차이는 인스펙터에서 어떤 오브젝트를 드래그하느냐 뿐:
///   - Meta SDK 선택 시: OVRCameraRig > TrackingSpace > CenterEyeAnchor 를 headAnchor에 드래그
///   - XRI 선택 시:      XR Origin > Camera Offset > Main Camera 를 headAnchor에 드래그
/// </summary>
public class HeadTracker : MonoBehaviour
{
    [Header("머리 앵커 (리그의 카메라 오브젝트를 드래그)")]
    [SerializeField] private Transform headAnchor;

    /// <summary>캘리브레이션으로 저장된 "평소 서 있을 때 머리 높이".</summary>
    public float BaselineHeight { get; private set; }

    /// <summary>캘리브레이션 완료 여부.</summary>
    public bool IsCalibrated { get; private set; }

    /// <summary>현재 머리 위치 (월드 좌표).</summary>
    public Vector3 HeadPosition => headAnchor != null ? headAnchor.position : Vector3.zero;

    /// <summary>기준선 대비 현재 머리 높이 차이. (음수 = 기준보다 낮아짐 = 스쿼트 중)</summary>
    public float HeightOffsetFromBaseline =>
        IsCalibrated ? HeadPosition.y - BaselineHeight : 0f;

    /// <summary>
    /// 현재 머리 높이를 기준선으로 저장 (키 캘리브레이션).
    /// 게임 시작 시 플레이어가 편하게 서 있을 때 호출.
    /// 부스 운영 시 매 판 시작마다 다시 호출해야 함 (플레이어가 바뀌므로).
    /// </summary>
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

    /// <summary>스쿼트 감지: 기준선보다 threshold(미터) 이상 내려갔는지.</summary>
    public bool IsSquatting(float threshold = 0.15f)
    {
        return IsCalibrated && HeightOffsetFromBaseline < -threshold;
    }
}
