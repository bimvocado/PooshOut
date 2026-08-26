using UnityEngine;

/// <summary>
/// 스테이지 1 (스크린/침사지) 전체 흐름 제어.
/// StageManager가 1번 스테이지가 되면 레일 이동과 오염물 스폰을 시작하고,
/// 레일 끝에 도달하면 정화도 결과에 따라 다음 스테이지로 넘긴다.
/// </summary>
public class Stage1Manager : MonoBehaviour, IStageProgressProvider
{
    private const int StageNumber = 1;

    [SerializeField] private RailMover railMover;
    [SerializeField] private PollutantSpawner spawner;
    [SerializeField] private float clearThreshold = 30f; // 이 정화도 미만이면 클리어 실패
    [Tooltip("레일 끝(완주) 도달 시 지급하는 정화도 보너스")]
    [SerializeField] private float completionPurityBonus = 70f;

    private bool _subscribedToRailEnd;
    private bool _reachedEnd;

    // LLM 마무리 피드백용 - 정화도 계산(BubbleItem이 직접 처리)과는 별개로,
    // 순수하게 "몇 개 먹었는지" 관찰만 해서 옆에서 카운트한다.
    private int _bubbleCount;
    private int _trashCount;

    /// <summary>
    /// 현재 Stage1 진행도(레일 이동 거리 기준).
    /// 0 = 0%, 1 = 100%.
    /// 레일 끝에 도달하면(완주) 다음 씬으로 넘어가도 값이 100%로 고정되도록 강제한다.
    /// </summary>
    public float NormalizedProgress => _reachedEnd || (railMover != null && railMover.NormalizedProgress >= 1f)
        ? 1f
        : (railMover != null ? Mathf.Clamp01(railMover.NormalizedProgress) : 0f);

    private void OnEnable()
    {
        if (StageManager.Instance != null)
            StageManager.Instance.OnStageChanged += HandleStageChanged;

        BubbleItem.OnItemCollected += HandleItemCollected;
    }

    private void OnDisable()
    {
        if (StageManager.Instance != null)
            StageManager.Instance.OnStageChanged -= HandleStageChanged;

        BubbleItem.OnItemCollected -= HandleItemCollected;

        if (railMover != null && _subscribedToRailEnd)
        {
            railMover.OnReachedEnd -= HandleRailEnd;
            _subscribedToRailEnd = false;
        }
    }

    private void HandleItemCollected(BubbleItem.ItemType itemType)
    {
        if (itemType == BubbleItem.ItemType.Bubble) _bubbleCount++;
        else _trashCount++;
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
        _reachedEnd = false;
        _bubbleCount = 0;
        _trashCount = 0;

        if (railMover != null)
        {
            if (!_subscribedToRailEnd)
            {
                railMover.OnReachedEnd += HandleRailEnd;
                _subscribedToRailEnd = true;
            }
            railMover.StartMoving();
        }
        spawner?.StartSpawning();
    }

    private void StopStage()
    {
        spawner?.StopSpawning();
        if (railMover != null && _subscribedToRailEnd)
        {
            railMover.OnReachedEnd -= HandleRailEnd;
            _subscribedToRailEnd = false;
        }
    }

    private void HandleRailEnd()
    {
        Debug.Log("[Stage1Manager] 레일 종료 지점 도달");
        spawner?.StopSpawning();
        _reachedEnd = true; // 진행도 100% 확정 - 다음 씬으로 넘어가도 값 유지

        PurificationSystem.Instance?.Increase(completionPurityBonus); // 완주 보너스

        bool cleared = PurificationSystem.Instance == null || PurificationSystem.Instance.MeetsThreshold(clearThreshold);
        if (cleared)
        {
            PurificationSystem.Instance?.SaveStagePurity(StageNumber);

            // LLM 마무리 피드백용 로그. "잘함/못함" 판단은 안 담고 순수 사실만 적는다.
            string note = $"버블 {_bubbleCount}개 획득, 쓰레기 {_trashCount}개 접촉";
            PurificationSystem.Instance?.RecordStageLog(StageNumber, _bubbleCount, _trashCount, note);

            StageManager.Instance?.AdvanceStage();
        }
        else
        {
            Debug.Log("[Stage1Manager] 정화도 기준 미달 - 재도전 필요");
        }
    }
}