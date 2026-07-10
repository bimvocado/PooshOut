using UnityEngine;

/// <summary>
/// 머리 + 양손 3점 위치로 현재 포즈를 판정.
/// Stage2 PoseGate가 요구 포즈와 일치하는지 확인할 때 사용.
/// </summary>
public class PoseDetector : MonoBehaviour
{
    public enum PoseType { Normal, ArmsUp, ArmsWide, Crouch }

    [SerializeField] private HeadTracker headTracker;
    [SerializeField] private HandTracker handTracker;

    [Header("판정 기준")]
    [SerializeField] private float armsUpHeightAboveHead = 0.1f; // 손이 머리보다 이만큼 위에 있으면 ArmsUp
    [SerializeField] private float armsWideMinDistance = 1.0f;   // 양손 사이 거리가 이 이상이면 ArmsWide
    [SerializeField] private float crouchDepthThreshold = 0.15f; // HeadTracker.IsSquatting 기준과 동일

    public PoseType GetCurrentPose()
    {
        if (headTracker == null || handTracker == null) return PoseType.Normal;

        Vector3 head = headTracker.HeadPosition;
        Vector3 left = handTracker.LeftHandPosition;
        Vector3 right = handTracker.RightHandPosition;

        if (headTracker.IsSquatting(crouchDepthThreshold))
        {
            return PoseType.Crouch;
        }

        bool bothHandsUp = left.y > head.y + armsUpHeightAboveHead && right.y > head.y + armsUpHeightAboveHead;
        if (bothHandsUp)
        {
            return PoseType.ArmsUp;
        }

        float handDistance = Vector3.Distance(left, right);
        if (handDistance >= armsWideMinDistance)
        {
            return PoseType.ArmsWide;
        }

        return PoseType.Normal;
    }

    /// <summary>현재 포즈가 지정한 포즈와 일치하는지.</summary>
    public bool IsPose(PoseType pose) => GetCurrentPose() == pose;
}
