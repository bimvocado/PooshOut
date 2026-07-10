using UnityEngine;

/// <summary>
/// 스테이지 1 (스크린/침사지) 전체 흐름 제어.
/// StageManager가 1번 스테이지가 되면 레일 이동과 오염물 스폰을 시작하고,
/// 레일 끝에 도달하면 정화도 결과에 따라 다음 스테이지로 넘긴다.
/// </summary>
public class Stage1Manager : MonoBehaviour
{
    private const int StageNumber = 1;

    [SerializeField] private RailMover railMover;
    [SerializeField] private PollutantSpawner spawner;
    [SerializeField] private float clearThreshold = 30f; // 이 정화도 미만이면 클리어 실패

    private void OnEnable()
    {
        if (StageManager.Instance != null)
            StageManager.Instance.OnStageChanged += HandleStageChanged;
    }

    private void OnDisable()
    {
        if (StageManager.Instance != null)
            StageManager.Instance.OnStageChanged -= HandleStageChanged;

        if (railMover != null) railMover.OnReachedEnd -= HandleRailEnd;
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
        if (stage != StageNumber)
        {
            StopStage();
            return;
        }
        StartStage();
    }

    private void StartStage()
    {
        Debug.Log("[Stage1Manager] 스테이지 1 시작");

        if (railMover != null)
        {
            railMover.OnReachedEnd += HandleRailEnd;
            railMover.StartMoving();
        }
        spawner?.StartSpawning();
    }

    private void StopStage()
    {
        spawner?.StopSpawning();
        if (railMover != null) railMover.OnReachedEnd -= HandleRailEnd;
    }

    private void HandleRailEnd()
    {
        Debug.Log("[Stage1Manager] 레일 종료 지점 도달");
        spawner?.StopSpawning();

        bool cleared = PurificationSystem.Instance == null || PurificationSystem.Instance.MeetsThreshold(clearThreshold);
        if (cleared)
        {
            StageManager.Instance?.AdvanceStage();
        }
        else
        {
            Debug.Log("[Stage1Manager] 정화도 기준 미달 - 재도전 필요");
        }
    }
}
