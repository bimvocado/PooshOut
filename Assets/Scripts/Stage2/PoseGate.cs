using System;
using UnityEngine;

/// 거름망 실루엣 하나. 플레이어가 트리거 영역에 들어오는 순간의 포즈를 판정해서
/// 요구 포즈(PoseDetector.PoseType)와 일치하면 통과, 아니면 오염물 접촉으로 처리.
[RequireComponent(typeof(Collider))]
public class PoseGate : MonoBehaviour
{
    [SerializeField] private PoseDetector poseDetector;
    [SerializeField] private PoseDetector.PoseType requiredPose = PoseDetector.PoseType.Normal;
    [SerializeField] private float purityRewardOnPass = 4f;
    [SerializeField] private float purityPenaltyOnFail = 4f;
    [SerializeField] private string playerTag = "Player";

    [Header("이동 설정")]
    [SerializeField] private float approachSpeed = 1.5f;
    [SerializeField] private Vector3 moveDirection = Vector3.back;
    [SerializeField] private float destroyPastZ = -2f;

    public event Action<bool> OnGateResolved;

    private bool _resolved;

    private void Reset()
    {
        GetComponent<Collider>().isTrigger = true;
    }

    public void SetRequiredPose(PoseDetector.PoseType pose) => requiredPose = pose;

    /// <summary>스포너가 런타임에 씬의 PoseDetector를 연결해줄 때 사용. 프리팹 애셋 자체는 씬 오브젝트를 참조 못 하므로 필수.</summary>
    public void SetPoseDetector(PoseDetector detector) => poseDetector = detector;

    private void Update()
    {
        transform.position += moveDirection.normalized * approachSpeed * Time.deltaTime;

        if (transform.position.z <= destroyPastZ)
        {
            if (!_resolved)
            {
                _resolved = true;
                PurificationSystem.Instance?.Decrease(purityPenaltyOnFail);
                OnGateResolved?.Invoke(false);
                Debug.Log("[PoseGate] 판정 없이 통과됨 - 실패 처리");
            }
            Destroy(gameObject);
        }
    }

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