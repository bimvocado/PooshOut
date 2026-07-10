using System;
using UnityEngine;

/// <summary>
/// 거름망 실루엣 하나. 플레이어가 트리거 영역에 들어오는 순간의 포즈를 판정해서
/// 요구 포즈(PoseDetector.PoseType)와 일치하면 통과, 아니면 오염물 접촉으로 처리.
/// </summary>
[RequireComponent(typeof(Collider))]
public class PoseGate : MonoBehaviour
{
    [SerializeField] private PoseDetector poseDetector;
    [SerializeField] private PoseDetector.PoseType requiredPose = PoseDetector.PoseType.ArmsUp;
    [SerializeField] private float purityRewardOnPass = 4f;
    [SerializeField] private float purityPenaltyOnFail = 4f;
    [SerializeField] private string playerTag = "Player";

    public event Action<bool> OnGateResolved; // true = 통과 성공

    private bool _resolved;

    private void Reset()
    {
        GetComponent<Collider>().isTrigger = true;
    }

    /// <summary>스포너가 순서를 정할 때 요구 포즈를 지정.</summary>
    public void SetRequiredPose(PoseDetector.PoseType pose) => requiredPose = pose;

    private void OnTriggerEnter(Collider other)
    {
        if (_resolved || !other.CompareTag(playerTag) || poseDetector == null) return;
        _resolved = true;

        bool success = poseDetector.IsPose(requiredPose);
        if (success)
        {
            PurificationSystem.Instance?.Increase(purityRewardOnPass);
        }
        else
        {
            PurificationSystem.Instance?.Decrease(purityPenaltyOnFail);
        }

        OnGateResolved?.Invoke(success);
        Debug.Log($"[PoseGate] 요구 포즈={requiredPose}, 결과={(success ? "통과" : "실패")}");
    }
}
