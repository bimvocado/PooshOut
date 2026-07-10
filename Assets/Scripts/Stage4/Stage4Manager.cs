using UnityEngine;

/// <summary>
/// 스테이지 4 (방류) 전체 흐름 제어.
/// 링을 통과하며 가속하다가 마지막 지점에서 다이빙 → 엔딩 연출로 이어진다.
/// </summary>
public class Stage4Manager : MonoBehaviour
{
    private const int StageNumber = 4;

    [SerializeField] private RailMover railMover;
    [SerializeField] private RingSpawner ringSpawner;
    [SerializeField] private DiveController diveController;
    [SerializeField] private EndingSequence endingSequence;

    private void OnEnable()
    {
        if (StageManager.Instance != null)
            StageManager.Instance.OnStageChanged += HandleStageChanged;
        if (diveController != null)
            diveController.OnDiveTriggered += HandleDiveTriggered;
    }

    private void OnDisable()
    {
        if (StageManager.Instance != null)
            StageManager.Instance.OnStageChanged -= HandleStageChanged;
        if (diveController != null)
            diveController.OnDiveTriggered -= HandleDiveTriggered;
        if (railMover != null)
            railMover.OnReachedEnd -= HandleRailEnd;
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
        Debug.Log("[Stage4Manager] 스테이지 4 시작");

        if (railMover != null)
        {
            railMover.OnReachedEnd += HandleRailEnd;
            railMover.StartMoving();
        }
        ringSpawner?.StartSpawning();
    }

    private void HandleRailEnd()
    {
        Debug.Log("[Stage4Manager] 한강 진입 지점 도달 - 다이빙 대기");
        railMover?.StopMoving();
        diveController?.ArmDive();
    }

    private void HandleDiveTriggered()
    {
        Debug.Log("[Stage4Manager] 다이빙 실행 - 엔딩 연출 시작");
        endingSequence?.Play();
    }
}
