using UnityEngine;

/// 스테이지 2 (유입 펌프장 / 거름망) 전체 흐름 제어.
/// 거름망 포즈 게이트를 순서대로 내보내고, 전부 판정이 끝나면 다음 스테이지로 넘긴다.
public class Stage2Manager : MonoBehaviour
{
    private const int StageNumber = 2;

    [SerializeField] private PoseGateSpawner gateSpawner;

    [Tooltip("마지막 게이트 판정 후 다음 스테이지로 넘어가기까지의 여유 시간(초). 정화봇 클리어 멘트가 나갈 시간.")]
    [SerializeField] private float clearDelay = 2f;

    private int _resolvedGateCount;
    private int _totalGateCount;
    private bool _gatesFinished;
    private bool _stageCompleted;

    private void OnEnable()
    {
        if (StageManager.Instance != null)
            StageManager.Instance.OnStageChanged += HandleStageChanged;
    }

    private void OnDisable()
    {
        if (StageManager.Instance != null)
            StageManager.Instance.OnStageChanged -= HandleStageChanged;

        if (gateSpawner != null)
        {
            gateSpawner.OnAllGatesSpawned -= HandleAllGatesSpawned;
            foreach (var gate in gateSpawner.SpawnedGates)
            {
                if (gate != null) gate.OnGateResolved -= HandleGateResolved;
            }
        }
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
        _stageCompleted = false;

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

        CheckStageDone();
    }

    private void HandleGateResolved(bool success)
    {
        _resolvedGateCount++;
        CheckStageDone();
    }

    private void CheckStageDone()
    {
        if (_stageCompleted) return;
        if (!_gatesFinished || _resolvedGateCount < _totalGateCount) return;

        _stageCompleted = true;
        Debug.Log("[Stage2Manager] 모든 게이트 판정 완료 - 다음 스테이지로");
        Invoke(nameof(AdvanceToNextStage), clearDelay);
    }

    private void AdvanceToNextStage()
    {
        StageManager.Instance?.AdvanceStage();
    }
}