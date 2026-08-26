using System.Collections;
using UnityEngine;

// Stage4에서 정화봇이 반응할 지점을 Ring의 static 이벤트에 연결.
// PurificationManager, Ring, RingSpawner, TileController는 전혀 건드리지 않는다.
//
// Stage3PooshTrigger와 동일한 원칙: 링이 계속 떨어지는 구조라 매번 반응하면 시끄러우므로,
// 통과/놓침 각각 쿨타임+확률로 가끔만 반응한다. s4_fast/s4_close 같은 특별 반응은 제외
// (반응속도/아슬아슬함을 판정할 타이밍 로직이 아직 없어서 - 나중에 추가되면 그때 반영).
public class Stage4PooshTrigger : MonoBehaviour
{
    [Header("통과(s4_pass) 반응")]
    [SerializeField] private float passVoiceCooldown = 3f;
    [SerializeField] private float passVoiceProbability = 0.5f;

    [Header("놓침(s4_miss) 반응")]
    [SerializeField] private float missVoiceCooldown = 4f;
    [SerializeField] private float missVoiceProbability = 0.4f;

    private float _lastPassVoiceTime = -999f;
    private float _lastMissVoiceTime = -999f;

    private void OnEnable()
    {
        Ring.OnRingPassed += HandlePassed;
        Ring.OnRingMissed += HandleMissed;
    }

    private void OnDisable()
    {
        Ring.OnRingPassed -= HandlePassed;
        Ring.OnRingMissed -= HandleMissed;
    }

    private void Start()
    {
        // Stage3(FixedPoint)에서 넘어왔거나, 이전 상태가 남아있어도 Stage4는 무조건 따라다니기 모드여야 함.
        PooshBotDirector.Instance?.EnterFollowStage();
        StartCoroutine(PlayStageIntro());
    }

    // map_stage4("거의 다 왔어...") 끝난 뒤 이어서 howto_stage4("위에서 자외선 링이...") 재생.
    private IEnumerator PlayStageIntro()
    {
        PooshLineBank.Instance?.PlayLine("map_stage4");
        yield return null;
        yield return new WaitUntil(() => PooshVoicePlayer.Instance == null || !PooshVoicePlayer.Instance.IsPlaying);

        PooshLineBank.Instance?.PlayLine("howto_stage4");
    }

    private void HandlePassed()
    {
        if (Time.time - _lastPassVoiceTime < passVoiceCooldown) return;
        if (Random.value > passVoiceProbability) return;

        _lastPassVoiceTime = Time.time;
        PooshBotAnimator.Instance?.PlayPraise(); // 칭찬 모션(Bye_2)
        PooshLineBank.Instance?.PlayLine("s4_pass");
    }

    private void HandleMissed()
    {
        if (Time.time - _lastMissVoiceTime < missVoiceCooldown) return;
        if (Random.value > missVoiceProbability) return;

        _lastMissVoiceTime = Time.time;
        PooshBotAnimator.Instance?.StartCheerTalk(); // 격려 모션(Cheer)
        PooshLineBank.Instance?.PlayLine("s4_miss");
    }
}