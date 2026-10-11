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

    [Header("Clear UI")]
    [SerializeField] private GameObject clearUIPrefab;
    [SerializeField] private Transform headTransform; // XR Origin의 Main Camera
    [SerializeField] private float clearUIDistance = 1.5f;
    [SerializeField] private Vector3 clearUIRotationOffset = Vector3.zero;
    [Tooltip("클리어 UI를 보여준 뒤 다음 스테이지로 넘어가기까지의 여유 시간(초).")]
    [SerializeField] private float clearDelay = 2f;

    private GameObject _clearUIInstance;

    private bool _subscribedToRailEnd;
    private bool _reachedEnd;
    private bool _cleared;

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
        _cleared = false;
        _bubbleCount = 0;
        _trashCount = 0;

        if (_clearUIInstance != null)
        {
            Destroy(_clearUIInstance);
            _clearUIInstance = null;
        }

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

        // 클리어 딜레이 대기 중에 씬/스테이지가 바뀌는 경우를 대비한 안전장치.
        CancelInvoke(nameof(AdvanceToNextStage));
    }

#if UNITY_EDITOR
    /// <summary>
    /// [에디터 전용] F9를 누르면 레일을 끝까지 타지 않고 바로 완주 처리.
    /// Clear UI 위치/회전 조정처럼 반복 확인이 필요한 작업을 빠르게 테스트하기 위한 디버그 키.
    /// #if UNITY_EDITOR로 감싸서 빌드에는 포함되지 않는다.
    /// </summary>
    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.F9))
        {
            Debug.Log("[Stage1Manager] 디버그: F9로 즉시 완주 처리");
            HandleRailEnd();
        }
    }
#endif

    private void HandleRailEnd()
    {
        Debug.Log("[Stage1Manager] 레일 종료 지점 도달");
        spawner?.StopSpawning();
        _reachedEnd = true; // 진행도 100% 확정 - 다음 씬으로 넘어가도 값 유지

        PurificationSystem.Instance?.Increase(completionPurityBonus); // 완주 보너스

        bool cleared = PurificationSystem.Instance == null || PurificationSystem.Instance.MeetsThreshold(clearThreshold);
        if (cleared)
        {
            ClearStage();
        }
        else
        {
            Debug.Log("[Stage1Manager] 정화도 기준 미달 - 재도전 필요");
        }
    }

    /// <summary>
    /// 완주 + 정화도 기준 통과 시 호출. 스테이지1 종료 처리를 하고 눈앞에 클리어 UI를 띄운 뒤,
    /// 잠깐 보여주고 나서 다음 스테이지로 넘어간다.
    /// </summary>
    private void ClearStage()
    {
        if (_cleared)
            return;

        _cleared = true;

        PurificationSystem.Instance?.SaveStagePurity(StageNumber);

        // LLM 마무리 피드백용 로그. "잘함/못함" 판단은 안 담고 순수 사실만 적는다.
        string note = $"버블 {_bubbleCount}개 획득, 쓰레기 {_trashCount}개 접촉";
        PurificationSystem.Instance?.RecordStageLog(StageNumber, _bubbleCount, _trashCount, note);

        SpawnClearUI();

        Invoke(nameof(AdvanceToNextStage), clearDelay);
    }

    private void AdvanceToNextStage()
    {
        StageManager.Instance?.AdvanceStage();
    }

    // ==================================================
    // Clear UI
    // ==================================================

    private void SpawnClearUI()
    {
        if (clearUIPrefab == null ||
            headTransform == null ||
            _clearUIInstance != null)
            return;

        Vector3 headPosition = headTransform.position;

        // 머리가 보는 방향에서 위/아래 방향 제거
        Vector3 forward = headTransform.forward;
        forward.y = 0f;
        forward.Normalize();

        Vector3 spawnPosition = headPosition + forward * clearUIDistance;

        // 높이는 무조건 현재 눈높이 유지
        spawnPosition.y = headPosition.y;

        // UI가 플레이어를 바라보게 함
        Vector3 directionToPlayer = headPosition - spawnPosition;
        directionToPlayer.y = 0f;

        Quaternion spawnRotation = Quaternion.LookRotation(
            directionToPlayer.normalized,
            Vector3.up
        );

        // 프리팹 방향 보정
        spawnRotation *= Quaternion.Euler(clearUIRotationOffset);

        _clearUIInstance = Instantiate(
            clearUIPrefab,
            spawnPosition,
            spawnRotation
        );
    }
}