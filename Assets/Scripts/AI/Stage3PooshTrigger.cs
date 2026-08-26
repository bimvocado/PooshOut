using System.Collections;
using UnityEngine;

// Stage3에서 정화봇이 반응할 지점을 Bullet의 static 이벤트에 연결.
// 빗나감 반응은 제거하고 명중 시에만 쿨타임과 확률을 적용하여 가끔씩 반응하도록 수정함.
public class Stage3PooshTrigger : MonoBehaviour
{
    [Tooltip("명중 대사를 다시 재생하기 위한 최소 대기 시간(초)")]
    [SerializeField] private float hitVoiceCooldown = 3.0f;

    [Tooltip("쿨타임이 지났을 때 명중 대사가 나올 확률 (0.0 ~ 1.0)")]
    [SerializeField] private float hitVoiceProbability = 0.5f;

    private float _lastHitVoiceTime = -999f;

    private void OnEnable()
    {
        // 빗나감(OnMissedShot) 이벤트 구독은 삭제하고 명중만 구독
        Bullet.OnMicrobeHit += HandleHit;
    }

    private void OnDisable()
    {
        Bullet.OnMicrobeHit -= HandleHit;
    }

    private void Start()
    {
        StartCoroutine(PlayStageIntro());
    }

    // map_stage3("여기서부터가 진짜야...") 끝난 뒤 이어서 howto_stage3("미생물들이 지금 자고 있어...") 재생.
    private IEnumerator PlayStageIntro()
    {
        PooshLineBank.Instance?.PlayLine("map_stage3");
        yield return null;
        yield return new WaitUntil(() => PooshVoicePlayer.Instance == null || !PooshVoicePlayer.Instance.IsPlaying);

        PooshLineBank.Instance?.PlayLine("howto_stage3");
    }

    private void HandleHit()
    {
        // 마지막으로 대사를 친 시간으로부터 설정한 쿨타임(기본 3초)이 안 지났으면 무시
        if (Time.time - _lastHitVoiceTime < hitVoiceCooldown) return;

        // 쿨타임이 지났어도 매번 말하지 않고 설정한 확률(기본 50%)로만 말함
        if (Random.value > hitVoiceProbability) return;

        _lastHitVoiceTime = Time.time;
        PooshBotAnimator.Instance?.PlayPraise(); // 칭찬 모션(Bye_2)
        PooshLineBank.Instance?.PlayLine("s3_hit");
    }
}