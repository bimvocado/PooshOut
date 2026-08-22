using System;
using System.Collections;
using UnityEngine;

/// 스테이지 2 (유입 펌프장 / 거름망) 전체 흐름 제어.
/// 거름망 포즈 게이트를 순서대로 내보내고, 전부 판정이 끝나면 다음 스테이지로 넘긴다.
public class Stage2Manager : MonoBehaviour, IStageProgressProvider
{
    private const int StageNumber = 2;

    [SerializeField] private PoseGateSpawner gateSpawner;

    [Tooltip("마지막 게이트 판정 후 다음 스테이지로 넘어가기까지의 여유 시간(초). 정화봇 클리어 멘트가 나갈 시간.")]
    [SerializeField] private float clearDelay = 2f;

    [Tooltip("게이트 성공/실패 시 화면 색 플래시. 비어 있어도 스테이지 진행에는 지장 없음.")]
    [SerializeField] private StageFeedbackFlash feedbackFlash;

    [Header("지도 안내 UI 대기")]
    [Tooltip("StageStartUIController가 켜고 끄는 그 지도 패널 오브젝트. " +
             "이 오브젝트가 '켜졌다가 꺼지는 순간'을 감지해서 그 다음에 게이트를 스폰한다. " +
             "StageStartUIController 자체는 공용 스크립트라 건드리지 않고, 여기서 상태만 지켜본다. " +
             "비워두면 예전처럼 씬 시작과 동시에 바로 스폰됨 (지도 UI 없는 테스트 씬 등에서 유용).")]
    [SerializeField] private GameObject mapUiPanel;

    [Tooltip("mapUiPanel이 지정됐는데 끝내 한 번도 켜지지 않을 경우(설정 실수 등)를 대비한 안전장치. " +
             "이 시간(초)이 지나도 대기 중이면 그냥 게이트를 스폰해버린다. 0이면 무한 대기.")]
    [SerializeField] private float mapWaitTimeout = 15f;

    [Header("테스트용")]
    [Tooltip("체크하면 StageManager(씬 전환 흐름) 없이 이 씬만 단독 Play해도 스테이지가 시작됨. " +
             "PoseGateSpawner의 Auto Start On Play와 같은 용도 - 실제 게임 흐름에 붙일 땐 꺼둘 것.")]
    [SerializeField] private bool autoStartForTesting = false;

    private int _resolvedGateCount;
    private int _totalGateCount;
    private bool _gatesFinished;
    private bool _stageCompleted;
    private bool _gatesStarted;

    public event Action OnStageCompleted;

    /// <summary>
    /// 현재 Stage2 정화도(점수).
    /// PurificationSystem.Purity는 이미 0~100으로 클램프되어 있지만,
    /// 게이트 점수 합계가 100을 넘도록 설계돼 있어도(100+N) 게이지는 100%에서 멈추도록
    /// 여기서 한 번 더 0~1로 클램프한다.
    /// 0 = 0%, 1 = 100%(또는 그 이상 점수를 받았어도 게이지는 가득 참)
    /// </summary>
    public float NormalizedProgress
    {
        get
        {
            if (PurificationSystem.Instance == null) return 0f;
            return Mathf.Clamp01(PurificationSystem.Instance.Purity / 100f);
        }
    }

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
            gateSpawner.OnGateSpawned -= HandleGateSpawned;
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
        else if (autoStartForTesting)
        {
            Debug.LogWarning("[Stage2Manager] StageManager 없음 - 단독 테스트 모드로 강제 시작");
            StartStage();
        }
    }

    private void HandleStageChanged(int stage)
    {
        if (stage != StageNumber) return;
        StartStage();
    }

    private void StartStage()
    {
        Debug.Log("[Stage2Manager] 스테이지 2 준비");
        _resolvedGateCount = 0;
        _gatesFinished = false;
        _stageCompleted = false;
        _gatesStarted = false;

        if (mapUiPanel != null)
        {
            StartCoroutine(WaitForMapUiToCloseThenSpawn());
        }
        else
        {
            BeginGateSpawning();
        }
    }

    /// mapUiPanel이 켜져 있는 상태를 한 번이라도 확인한 뒤, 그게 다시 꺼지는 순간을 "지도 안내 종료"로 본다.
    /// StageStartUIController가 Time.timeScale = 0으로 게임을 멈추는 동안에도 감지되도록
    /// WaitForSecondsRealtime 기준으로 폴링한다.
    private IEnumerator WaitForMapUiToCloseThenSpawn()
    {
        bool sawActive = false;
        float elapsed = 0f;
        var wait = new WaitForSecondsRealtime(0.1f);

        while (true)
        {
            if (mapUiPanel.activeInHierarchy)
            {
                sawActive = true;
            }
            else if (sawActive)
            {
                break; // 켜져 있다가 방금 꺼짐 = 지도 안내 종료
            }

            elapsed += 0.1f;
            if (mapWaitTimeout > 0f && elapsed >= mapWaitTimeout)
            {
                Debug.LogWarning("[Stage2Manager] 지도 UI 대기 시간 초과 - 안전장치로 게이트 스폰 시작");
                break;
            }

            yield return wait;
        }

        BeginGateSpawning();
    }

    private void BeginGateSpawning()
    {
        if (_gatesStarted) return;
        _gatesStarted = true;

        Debug.Log("[Stage2Manager] 지도 안내 종료 - 게이트 스폰 시작");

        if (gateSpawner != null)
        {
            // 게이트가 태어나는 즉시 판정 이벤트를 구독한다.
            // "다 스폰되고 나서 한꺼번에 구독"하면 먼저 나온 게이트들의 판정을
            // 놓쳐서 화면 플래시가 안 뜨는 문제가 있었음 - 이 방식으로 그 지연을 없앤다.
            gateSpawner.OnGateSpawned += HandleGateSpawned;
            gateSpawner.OnAllGatesSpawned += HandleAllGatesSpawned;
            gateSpawner.StartSpawning();
        }
    }

    /// 게이트 하나가 생성되는 그 프레임에 바로 호출된다. 즉시 구독해서 판정 지연/누락을 막는다.
    private void HandleGateSpawned(PoseGate gate)
    {
        gate.OnGateResolved += HandleGateResolved;
    }

    private void HandleAllGatesSpawned()
    {
        _gatesFinished = true;
        _totalGateCount = gateSpawner.SpawnedGates.Count;

        // 구독은 이미 HandleGateSpawned에서 게이트별로 끝났으므로 여기선 개수 확정만 한다.
        CheckStageDone();
    }

    private void HandleGateResolved(bool success)
    {
        _resolvedGateCount++;

        if (feedbackFlash != null)
        {
            if (success) feedbackFlash.FlashSuccess();
            else feedbackFlash.FlashFail();
        }

        CheckStageDone();
    }

    private void CheckStageDone() {
        if (_stageCompleted) return;
        if (!_gatesFinished || _resolvedGateCount < _totalGateCount) return;

        _stageCompleted = true;

        PurificationSystem.Instance?.SaveStagePurity(2);

        Debug.Log("[Stage2Manager] 모든 게이트 판정 완료");

        // 완료 UI 등에 알림
        OnStageCompleted?.Invoke();

        Invoke(nameof(AdvanceToNextStage), clearDelay);
    }

    private void AdvanceToNextStage()
    {
        StageManager.Instance?.AdvanceStage();
    }
}