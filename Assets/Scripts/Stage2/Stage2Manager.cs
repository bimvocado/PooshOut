using UnityEngine;

/// <summary>
/// 스테이지 2 (침전지) 전체 흐름 제어.
/// 거름망 포즈 게이트를 모두 통과시킨 뒤, 수영 동작으로 가라앉히는 두 단계로 구성.
/// </summary>
public class Stage2Manager : MonoBehaviour
{
    private const int StageNumber = 2;

    [SerializeField] private PoseGateSpawner gateSpawner;
    [SerializeField] private SedimentationController sedimentation;

    private int _resolvedGateCount;
    private int _totalGateCount;
    private bool _gatesFinished;

    private void OnEnable()
    {
        if (StageManager.Instance != null)
            StageManager.Instance.OnStageChanged += HandleStageChanged;
    }

    private void OnDisable()
    {
        if (StageManager.Instance != null)
            StageManager.Instance.OnStageChanged -= HandleStageChanged;

        if (gateSpawner != null) gateSpawner.OnAllGatesSpawned -= HandleAllGatesSpawned;
        if (sedimentation != null) sedimentation.OnSedimentationComplete -= HandleSedimentationComplete;
    }

    private void Start()
    {
        if (StageManager.Instance != null && StageManager.Instance.CurrentStage == StageNumber)
        {
            HandleStageChanged(StageNumber);
        }
    }

    private void HandleStageChanged(int stage)
    {
        if (stage != StageNumber) return;
        StartStage();
    }

    private void StartStage()
    {
        Debug.Log("[Stage2Manager] 스테이지 2 시작");
        _resolvedGateCount = 0;
        _gatesFinished = false;

        if (gateSpawner != null)
        {
            gateSpawner.OnAllGatesSpawned += HandleAllGatesSpawned;
            gateSpawner.StartSpawning();
        }
    }

    private void HandleAllGatesSpawned()
    {
        _gatesFinished = true;
        _totalGateCount = gateSpawner.SpawnedGates.Count;
        foreach (var gate in gateSpawner.SpawnedGates)
        {
            gate.OnGateResolved += HandleGateResolved;
        }
        CheckGatesDone();
    }

    private void HandleGateResolved(bool success)
    {
        _resolvedGateCount++;
        CheckGatesDone();
    }

    private void CheckGatesDone()
    {
        if (!_gatesFinished || _resolvedGateCount < _totalGateCount) return;

        Debug.Log("[Stage2Manager] 모든 게이트 통과 완료 - 침전 단계 시작");
        if (sedimentation != null)
        {
            sedimentation.OnSedimentationComplete += HandleSedimentationComplete;
            sedimentation.BeginSedimentation();
        }
    }

    private void HandleSedimentationComplete()
    {
        Debug.Log("[Stage2Manager] 침전 완료 - 다음 스테이지로");
        StageManager.Instance?.AdvanceStage();
    }
}
