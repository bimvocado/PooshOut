using System.Collections;
using UnityEngine;

// Stage2에서 정화봇이 반응할 지점을 PoseGateSpawner/PoseGate 이벤트에 연결.
// Stage2Manager와는 독립적으로 동작 - 기존 Stage2Manager 코드는 건드리지 않는다.
// Stage1PooshTrigger와 동일한 패턴(지도소개, 단독 빌드 안전장치, 칭찬/격려 모션)으로 통일.
public class Stage2PooshTrigger : MonoBehaviour
{
    private const int StageNumber = 2;

    [SerializeField] private PoseGateSpawner gateSpawner;

    [Tooltip("몇 연속 성공마다 s2_streak 특별 멘트를 재생할지")]
    [SerializeField] private int streakInterval = 3;

    private bool _stageActive;
    private bool _introPlayed;
    private int _consecutiveSuccess;

    private void OnEnable()
    {
        if (StageManager.Instance != null)
            StageManager.Instance.OnStageChanged += HandleStageChanged;

        if (gateSpawner != null)
            gateSpawner.OnGateSpawned += HandleGateSpawned;
    }

    private void OnDisable()
    {
        if (StageManager.Instance != null)
            StageManager.Instance.OnStageChanged -= HandleStageChanged;

        if (gateSpawner != null)
            gateSpawner.OnGateSpawned -= HandleGateSpawned;
    }

    // StageManager.CurrentStage 기본값이 1이라, Stage2를 단독으로 Play/빌드하면
    // OnStageChanged 이벤트가 안 터질 수 있음 (Stage1PooshTrigger와 동일한 이유의 안전장치).
    // 참고: Stage2를 이어서(StartScene→Stage1→Stage2) 플레이하는 정상 흐름에서는
    // Stage1 클리어 시 AdvanceStage()가 호출되어 이 값이 2로 "바뀌므로" OnStageChanged가
    // 정상적으로 터지고, 이 Start() 체크는 아무 영향을 안 준다(중복 호출 없음).
    private void Start()
    {
        if (StageManager.Instance != null && StageManager.Instance.CurrentStage == StageNumber)
        {
            HandleStageChanged(StageNumber);
        }
    }

    private void HandleStageChanged(int stage)
    {
        _stageActive = stage == StageNumber;

        if (_stageActive)
        {
            PooshBotDirector.Instance?.EnterFollowStage();
        }

        if (_stageActive && !_introPlayed)
        {
            _introPlayed = true;
            _consecutiveSuccess = 0;
            StartCoroutine(PlayStageIntro());
        }
    }

    // map_stage2("잘했어! 이제 유입 펌프장에...") 끝난 뒤 이어서 howto_stage2("앞에서 거름망이...") 재생.
    private IEnumerator PlayStageIntro()
    {
        PooshLineBank.Instance?.PlayLine("map_stage2");
        yield return null;
        yield return new WaitUntil(() => PooshVoicePlayer.Instance == null || !PooshVoicePlayer.Instance.IsPlaying);

        PooshLineBank.Instance?.PlayLine("howto_stage2");
    }

    // 게이트가 생성되는 즉시 구독. Stage2Manager와 동일한 패턴 -
    // 나중에 한꺼번에 구독하면 먼저 판정된 게이트를 놓칠 수 있어서 이 방식으로 통일.
    private void HandleGateSpawned(PoseGate gate)
    {
        gate.OnGateResolved += success => HandleGateResolved(gate, success);
    }

    private void HandleGateResolved(PoseGate gate, bool success)
    {
        if (!_stageActive) return;

        if (success)
        {
            _consecutiveSuccess++;
            PooshBotAnimator.Instance?.PlayPraise(); // 칭찬 모션(Bye_2, 윙크+만세)

            // streakInterval(기본 3)의 배수로 연속 성공하면 일반 통과 멘트 대신 특별 멘트
            bool isStreak = _consecutiveSuccess > 0 && _consecutiveSuccess % streakInterval == 0;
            PooshLineBank.Instance?.PlayLine(isStreak ? "s2_streak" : "s2_pass");
            return;
        }

        _consecutiveSuccess = 0;
        PooshBotAnimator.Instance?.StartCheerTalk(); // 격려 모션(Cheer)

        string key = gate.RequiredPose switch
        {
            PoseDetector.PoseType.TPose => "s2_fail_tpose",
            PoseDetector.PoseType.BothUp => "s2_fail_bothup",
            PoseDetector.PoseType.Normal => "s2_fail_normal",
            _ => "s2_fail_common",   // LeftUp, RightUp, LeftExtended, RightExtended, GShape, NShape
        };

        PooshLineBank.Instance?.PlayLine(key);
    }
}