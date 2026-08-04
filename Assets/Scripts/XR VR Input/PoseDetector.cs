using UnityEngine;

/// 머리 + 양손 3점 위치로 현재 포즈를 판정.
/// Stage2 PoseGate가 요구 포즈와 일치하는지 확인할 때 사용.
///
/// v2 리팩토링: 왼팔/오른팔 각각을 독립적으로 "내림(Down)/뻗기(Extended)/들기(Up)" 3단계로 먼저 판정한 뒤,
/// 두 팔의 조합(3×3=9가지)으로 최종 포즈를 결정하는 방식으로 변경.
/// 팔마다 따로 하드코딩하지 않아도 9개 포즈가 자동으로 나옴.
///
/// ⚠️ 주의: PoseType enum 순서/구성이 이전 버전과 달라졌음.
/// 이미 만들어둔 PoseGate 프리팹(PoseGate_Normal 등)의 "Required Pose" 드롭다운이
/// 의도한 값과 다르게 바뀌어 있을 수 있으니, 이 스크립트 적용 후 각 프리팹에서
/// Required Pose를 다시 한번 확인/재선택해야 함.
///
/// 아동 체형 대응: 손이 몸통에서 얼마나 "옆으로 뻗어있는지(horizontal distance)" 판정 기준을
/// HeadTracker.BaselineHeight 비율로 스케일링해서, 팔이 짧은 아이도 정상 판정되게 함.
public class PoseDetector : MonoBehaviour
{
    /// 팔 하나의 상태 (왼팔/오른팔 각각 독립 판정).
    public enum ArmState { Down, Extended, Up }

    /// 왼팔×오른팔 조합으로 나오는 9가지 최종 포즈.
    public enum PoseType
    {
        Normal,             // 정자세 (Down, Down)
        LeftUp,             // 왼팔들기 (Up, Down)
        RightUp,            // 오른팔들기 (Down, Up)
        BothUp,             // 양팔들기 (Up, Up)
        TPose,              // T자모양 (Extended, Extended)
        LeftExtended,       // 왼팔뻗기 (Extended, Down)
        RightExtended,      // 오른팔뻗기 (Down, Extended)
        GShape,             // ㄱ자모양 (Extended, Up) - 왼팔 뻗고 오른팔 들기
        NShape              // ㄴ자모양 (Up, Extended) - 왼팔 들고 오른팔 뻗기
    }

    [SerializeField] private HeadTracker headTracker;
    [SerializeField] private HandTracker handTracker;

    [Header("판정 기준 (성인 평균 키 기준값)")]
    [Tooltip("손이 머리보다 이만큼 위에 있으면 Up으로 판정")]
    [SerializeField] private float armsUpHeightAboveHead = 0.1f;

    [Tooltip("손이 몸(머리 중심선) 기준 수평으로 이만큼 떨어져 있으면 Extended로 판정")]
    [SerializeField] private float extendedMinDistanceFromCenter = 0.4f;

    [Header("아동 체형 보정")]
    [SerializeField] private float referenceAdultHeight = 1.65f;
    [SerializeField] private float minScaleRatio = 0.6f;

    private float GetScaleRatio()
    {
        if (headTracker == null || !headTracker.IsCalibrated || referenceAdultHeight <= 0f)
            return 1f;

        float ratio = headTracker.BaselineHeight / referenceAdultHeight;
        return Mathf.Max(ratio, minScaleRatio);
    }

    /// 손 하나의 상태(Down/Extended/Up)를 머리 위치 기준 상대값으로 판정.
    private ArmState GetArmState(Vector3 handPos, Vector3 headPos, float scaleRatio)
    {
        // 1순위: 손이 머리보다 확실히 위에 있으면 Up
        float verticalOffset = handPos.y - headPos.y;
        if (verticalOffset > armsUpHeightAboveHead)
        {
            return ArmState.Up;
        }

        // 2순위: 손이 몸 중심선(머리 XZ 기준)에서 수평으로 충분히 떨어져 있으면 Extended
        float horizontalDistance = new Vector2(handPos.x - headPos.x, handPos.z - headPos.z).magnitude;
        float adjustedMinDistance = extendedMinDistanceFromCenter * scaleRatio;
        if (horizontalDistance >= adjustedMinDistance)
        {
            return ArmState.Extended;
        }

        // 그 외(머리보다 안 높고, 몸에 가까움)는 Down
        return ArmState.Down;
    }

    /// 왼팔/오른팔 상태 조합을 9가지 포즈 중 하나로 변환.
    private PoseType CombineArmStates(ArmState left, ArmState right)
    {
        return (left, right) switch
        {
            (ArmState.Down, ArmState.Down) => PoseType.Normal,
            (ArmState.Up, ArmState.Down) => PoseType.LeftUp,
            (ArmState.Down, ArmState.Up) => PoseType.RightUp,
            (ArmState.Up, ArmState.Up) => PoseType.BothUp,
            (ArmState.Extended, ArmState.Extended) => PoseType.TPose,
            (ArmState.Extended, ArmState.Down) => PoseType.LeftExtended,
            (ArmState.Down, ArmState.Extended) => PoseType.RightExtended,
            (ArmState.Extended, ArmState.Up) => PoseType.GShape,
            (ArmState.Up, ArmState.Extended) => PoseType.NShape,
            _ => PoseType.Normal // 이론상 3x3=9라 도달 안 하지만 안전망으로 기본값
        };
    }

    public PoseType GetCurrentPose()
    {
        if (headTracker == null || handTracker == null) return PoseType.Normal;

        Vector3 head = headTracker.HeadPosition;
        Vector3 left = handTracker.LeftHandPosition;
        Vector3 right = handTracker.RightHandPosition;
        float scaleRatio = GetScaleRatio();

        ArmState leftState = GetArmState(left, head, scaleRatio);
        ArmState rightState = GetArmState(right, head, scaleRatio);

        return CombineArmStates(leftState, rightState);
    }

    /// 현재 포즈가 지정한 포즈와 일치하는지.
    public bool IsPose(PoseType pose) => GetCurrentPose() == pose;
}