using System;
using System.Collections;
using UnityEngine;

// 정화봇의 전체 등장/이동/멘트 흐름을 지휘.
// 다른 매니저들(GameManager, SaveLoadManager 등)과 동일하게 Singleton<T>를 상속해서
// DontDestroyOnLoad를 그 베이스 클래스가 알아서 처리해준다 (Persistent 기본값 true).
// 이 스크립트가 붙은 정화봇 오브젝트는 딱 한 씬(제일 처음, StartScene)에만 배치하면 되고,
// 다른 씬에는 이 오브젝트를 또 놓지 않는다 - Stage1~4/엔딩씬에서는 이미 떠 있는 이 오브젝트를
// 향해 아래 public 함수만 호출하면 됨.
//
// 닉네임은 인자로 안 받고 GameManager.Instance.PlayerName에서 직접 읽는다
// (StartSceneManager가 GameManager.SetPlayerName()을 이미 호출해뒀다는 전제).
[RequireComponent(typeof(PooshLineBank))]
[RequireComponent(typeof(PooshVoicePlayer))]
[RequireComponent(typeof(PooshBotPositionController))]
public class PooshBotDirector : Singleton<PooshBotDirector>
{
    [Tooltip("StartScene에 배치된 CalibrationController. 비워두면 캘리브레이션 없이 정해진 시간만 기다리고 넘어감.")]
    [SerializeField] private CalibrationController calibrationController;

    private PooshLineBank _lineBank;
    private PooshVoicePlayer _voicePlayer;
    private PooshBotPositionController _position;

    protected override void Awake()
    {
        base.Awake(); // Singleton<T>가 중복 인스턴스 처리 + DontDestroyOnLoad까지 해줌

        _lineBank = GetComponent<PooshLineBank>();
        _voicePlayer = GetComponent<PooshVoicePlayer>();
        _position = GetComponent<PooshBotPositionController>();
    }

    // ─────────────────────────────────────────────
    // 인트로: 닉네임 선택 씬에서, 닉네임 확정되는 순간(GameManager.SetPlayerName 호출 직후) 호출
    // 정면 인사 -> 캘리브레이션 시작 -> (캘리브레이션 완료 멘트 트는 순간 오른쪽 위로 이동 시작)
    // ─────────────────────────────────────────────
    public void PlayIntro()
    {
        StartCoroutine(IntroSequence());
    }

    private IEnumerator IntroSequence()
    {
        string nickname = GameManager.Instance.PlayerName;

        _position.SetMode(PooshBotPositionController.Mode.Front);

        PooshBotAnimator.Instance?.PlayGreeting();
        _lineBank.PlayLine($"intro_greeting_{nickname}");
        yield return WaitForSpeechEnd();

        _lineBank.PlayLine("intro_calibration_start");
        yield return WaitForSpeechEnd();

        // 실제 캘리브레이션 완료를 기다림. 없으면(테스트 등) 그냥 바로 다음으로 넘어감.
        if (calibrationController != null)
        {
            yield return WaitForCalibration();
        }

        _position.SetMode(PooshBotPositionController.Mode.TopRightFollow); // 이동 시작
        _lineBank.PlayLine("intro_calibration_done");
        yield return WaitForSpeechEnd();
    }

    private IEnumerator WaitForCalibration()
    {
        bool done = false;
        void OnDone(float height) => done = true;

        calibrationController.OnCalibrationDone += OnDone;
        calibrationController.StartCalibration();

        yield return new WaitUntil(() => done);

        calibrationController.OnCalibrationDone -= OnDone;
    }

    // ─────────────────────────────────────────────
    // Stage 3: 진입 시 지정된 위치에 고정 배치, 이탈 시 다시 오른쪽 위 따라다니기로 복귀
    // Stage3 씬에 배치용 빈 오브젝트(Transform) 하나 만들어서 anchor로 넘겨주면 됨
    // ─────────────────────────────────────────────
    public void EnterStage3(Transform anchor)
    {
        _position.SetMode(PooshBotPositionController.Mode.FixedPoint, anchor);
    }

    public void ExitStage3()
    {
        _position.SetMode(PooshBotPositionController.Mode.TopRightFollow);
    }

    // Stage 1, 2, 4는 기본이 TopRightFollow라 씬 진입 시 이것만 호출하면 됨 (인트로 끝나고 이미 TopRightFollow 상태면 생략 가능)
    public void EnterFollowStage()
    {
        _position.SetMode(PooshBotPositionController.Mode.TopRightFollow);
    }

    // ─────────────────────────────────────────────
    // 엔딩씬 1 - 두 단계로 구성됨:
    //
    // 1단계) 물방울(WaterDrop) 낙하~첨벙 연출 동안: 정화봇 완전히 숨김, 아무 멘트도 안 함.
    //        End1SceneManager.Awake()/Start() 시점에 이 함수 호출.
    // 2단계) 첨벙 후 결과 보드("게임 클리어! 축하해요!")가 뜨는 바로 그 순간: 그 보드 근처의
    //        지정된 위치에 정화봇이 등장(고정 배치 - 카메라 따라 계속 움직이는 Front가 아니라
    //        Stage3처럼 한 자리에 딱 고정)해서, "해냈다! {닉네임}, 드디어 물이 됐어!
    //        [플레이 내용 반영 개인화 피드백] ... 그때 또 보자, {닉네임}!"까지 전부 하나로 이어진
    //        음성을 재생. 이 안에 작별 인사까지 자연스럽게 포함되어 있으므로 별도의
    //        "ending_farewell" 같은 고정 멘트는 더 이상 필요 없음.
    //        음성 다 끝나면 정화봇은 완전히 사라지고, 이후 엔딩씬2(리더보드)에는 정화봇이 등장하지 않음.
    //
    // anchor: 결과 보드 옆/근처에 배치해둘 빈 오브젝트(Transform). End1 씬에 하나 만들어서 넘기면 됨
    //         (Stage3PooshAnchor랑 같은 개념이지만, 이건 이벤트 타이밍이 씬 활성화가 아니라
    //         "결과 보드가 뜨는 순간"이라 자동 OnEnable 트리거 방식 대신 직접 함수 호출로 넘김).
    //
    // onShowFeedbackBoard: End1SceneManager.ShowUIAfterDelay()에서 completeUI를 활성화하는 그 타이밍에
    // 이 콜백을 함께 호출해주면 됨. 콜백 안에서 결과 보드 UI 표시 + 서버 /feedback 요청(통합 멘트 텍스트
    // 생성) + 그 오디오 재생을 처리하고, 다 끝나면 반드시 넘겨받은 Action()을 호출해줘야
    // 정화봇이 사라지는 다음 단계로 넘어감.
    //
    // 서버 쪽 확인 필요: /feedback 시스템 프롬프트에 아래 지침 추가 -
    //   "응답은 반드시 '해냈다! {playerName}, 드디어 물이 됐어!' 로 정확히 시작하라.
    //    이어서 플레이 로그 중 잘한 점을 딱 2가지만 골라 짧게 칭찬하라 (예: '거름망도 2개나
    //    통과했고, 자외선 링에서 반응도 정말 빨랐어!'). 못한 점이나 아쉬운 점은 절대 언급하지 마라.
    //    그다음 '다음엔 또 얼마나 잘할지 벌써 기대되는걸?' 같은 한 문장으로 다음 플레이에 대한
    //    기대감을 표현하라. 마지막으로 '그때 또 보자, {playerName}!' 정도의 짧은 작별 인사로
    //    마무리하라. 전체적으로 코칭/지적하는 톤이 아니라 순수하게 칭찬하고 응원하는 톤을 유지하라."
    // ─────────────────────────────────────────────
    public void EnterEndingSceneOne()
    {
        _position.SetMode(PooshBotPositionController.Mode.Hidden);
    }

    public void PlayEndingFeedbackAndFarewell(Transform anchor, Action<Action> onShowFeedbackBoard)
    {
        StartCoroutine(EndingFeedbackSequence(anchor, onShowFeedbackBoard));
    }

    private IEnumerator EndingFeedbackSequence(Transform anchor, Action<Action> onShowFeedbackBoard)
    {
        _position.SetMode(PooshBotPositionController.Mode.FixedPoint, anchor);

        // "해냈다!~작별인사"까지 다 포함된 통합 멘트 재생은 콜백(결과 보드 UI) 쪽에서 처리.
        bool feedbackDone = false;
        onShowFeedbackBoard?.Invoke(() => feedbackDone = true);
        yield return new WaitUntil(() => feedbackDone);

        _position.SetMode(PooshBotPositionController.Mode.Hidden); // 완전히 사라짐 - 엔딩씬2엔 등장 안 함
    }

    private IEnumerator WaitForSpeechEnd()
    {
        yield return null; // PlayLine 호출 직후 한 프레임은 아직 재생 시작 전이라 IsPlaying이 false일 수 있음
        yield return new WaitUntil(() => !_voicePlayer.IsPlaying);
    }
}