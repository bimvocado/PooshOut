using UnityEngine;

// 정화봇 Animator Controller(PooshBot.controller)를 제어하는 스크립트.
// PooshBotTalkTest.cs를 대체함 — 이제 IsTalking뿐 아니라 Cheer/Bye1/Bye2까지 관리.
//
// 매 프레임 PooshVoicePlayer가 재생 중인지 보고 IsTalking을 자동으로 켜고 끔.
// 나머지(격려/칭찬/인사)는 상황에 맞게 아래 공개 함수를 호출해서 트리거만 쏘면 됨.
[RequireComponent(typeof(Animator))]
public class PooshBotAnimator : Singleton<PooshBotAnimator>
{
    private static readonly int IsTalkingHash = Animator.StringToHash("IsTalking");
    private static readonly int UseCheerTalkHash = Animator.StringToHash("UseCheerTalk");
    private static readonly int IdleVarHash = Animator.StringToHash("IdleVar");
    private static readonly int Bye1Hash = Animator.StringToHash("Bye1");
    private static readonly int Bye2Hash = Animator.StringToHash("Bye2");

    [Header("Idle 변주 (귀 쫑긋)")]
    [SerializeField] private bool autoIdleVariation = true;
    [SerializeField] private float idleVarMinInterval = 8f;
    [SerializeField] private float idleVarMaxInterval = 15f;

    private Animator _animator;
    private float _nextIdleVarTime;

    protected override void Awake()
    {
        base.Awake();
        _animator = GetComponent<Animator>();

        // 지도 UI 등에서 Time.timeScale = 0으로 게임이 멈춰도 정화봇 애니메이션(입 움직임 등)은
        // 계속 재생되어야 하므로, Animator가 게임 시간이 아니라 실제 시간을 기준으로 돌게 한다.
        // 이게 없으면 IsTalking 등 파라미터는 제대로 켜져도 애니메이션 클립 자체는 멈춰있다.
        _animator.updateMode = AnimatorUpdateMode.UnscaledTime;

        ScheduleNextIdleVar();
    }

    private void Update()
    {
        bool talking = PooshVoicePlayer.Instance != null && PooshVoicePlayer.Instance.IsPlaying;
        _animator.SetBool(IsTalkingHash, talking);

        // 말이 끝나면 격려 모드도 같이 꺼줌. 다음 대사부턴 다시 일반 Talk로 돌아감.
        if (!talking)
        {
            _animator.SetBool(UseCheerTalkHash, false);
        }

        if (autoIdleVariation && !talking && Time.unscaledTime >= _nextIdleVarTime)
        {
            _animator.SetTrigger(IdleVarHash);
            ScheduleNextIdleVar();
        }
    }

    private void ScheduleNextIdleVar()
    {
        _nextIdleVarTime = Time.unscaledTime + Random.Range(idleVarMinInterval, idleVarMaxInterval);
    }

    // 격려 멘트 재생 직전에 호출. 이후 Update()가 알아서 Talk 대신 Cheer를 틀어줌.
    public void StartCheerTalk()
    {
        _animator.SetBool(UseCheerTalkHash, true);
    }

    // 칭찬 멘트용 1회성 리액션 (윙크+만세)
    public void PlayPraise()
    {
        _animator.SetTrigger(Bye2Hash);
    }

    // 첫 인사 (윙크+한손 인사)
    public void PlayGreeting()
    {
        _animator.SetTrigger(Bye1Hash);
    }

    // 작별 인사 (같은 모션, 호출 시점만 다름)
    public void PlayFarewell()
    {
        _animator.SetTrigger(Bye1Hash);
    }
}