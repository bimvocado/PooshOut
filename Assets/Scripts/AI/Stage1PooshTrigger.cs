using System.Collections;
using UnityEngine;

// Stage1에서 정화봇이 반응할 지점을 연결. Stage1Manager와는 독립적으로 동작 -
// 그 코드는 전혀 건드리지 않는다.
//
// BubbleItem/PollutantObject는 풀링되어 인스턴스가 계속 생겼다 사라지므로,
// 인스턴스별로 구독하는 대신 static 이벤트(OnItemCollected/OnPollutantHit)를 한 번만 구독한다.
public class Stage1PooshTrigger : MonoBehaviour
{
    private const int StageNumber = 1;

    private bool _stageActive;
    private bool _introPlayed;

    private void OnEnable()
    {
        if (StageManager.Instance != null)
            StageManager.Instance.OnStageChanged += HandleStageChanged;

        BubbleItem.OnItemCollected += HandleItemCollected;
        PollutantObject.OnPollutantHit += HandlePollutantHit;
    }

    private void OnDisable()
    {
        if (StageManager.Instance != null)
            StageManager.Instance.OnStageChanged -= HandleStageChanged;

        BubbleItem.OnItemCollected -= HandleItemCollected;
        PollutantObject.OnPollutantHit -= HandlePollutantHit;
    }

    // StageManager.CurrentStage는 기본값이 이미 1이라, Stage1을 단독으로 Play하면
    // "1로 바뀌었다"는 변화 자체가 없어서 OnStageChanged 이벤트가 아예 안 터진다.
    // Stage1Manager.Start()가 이미 이렇게 자체적으로 한 번 확인하고 있어서, 여기도 똑같이 맞춘다.
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
            // StartScene 인트로를 거쳤으면 이미 TopRightFollow 상태라 그냥 같은 값 재설정되는 것뿐이고,
            // Stage1만 단독으로 빌드해서 실행한 경우(PlayIntro가 호출된 적 없는 경우)엔
            // 기본값(Hidden) 상태로 남아있던 정화봇을 여기서 보이게 만들어준다.
            PooshBotDirector.Instance?.EnterFollowStage();
        }

        if (_stageActive && !_introPlayed)
        {
            _introPlayed = true;
            StartCoroutine(PlayStageIntro());
        }
    }

    // map_stage1("우리는 지금 하수관으로...") 끝난 뒤 이어서 howto_stage1("핸들을 좌우로...") 재생.
    // 동시에 PlayLine을 두 번 부르면 뒤에 부른 게 앞의 재생을 끊어버려서 순서대로 기다렸다 재생한다.
    private IEnumerator PlayStageIntro()
    {
        PooshLineBank.Instance?.PlayLine("map_stage1");
        yield return null;
        yield return new WaitUntil(() => PooshVoicePlayer.Instance == null || !PooshVoicePlayer.Instance.IsPlaying);

        PooshLineBank.Instance?.PlayLine("howto_stage1");
    }

    // 버블(좋은 아이템) = 칭찬 모션(Bye_2, 윙크+만세) / 쓰레기(Trash) = 격려 모션(Cheer)
    private void HandleItemCollected(BubbleItem.ItemType type)
    {
        if (!_stageActive) return;

        if (type == BubbleItem.ItemType.Trash)
        {
            PooshBotAnimator.Instance?.StartCheerTalk();
            PooshLineBank.Instance?.PlayLine("s1_hit");
        }
        else
        {
            PooshBotAnimator.Instance?.PlayPraise();
            PooshLineBank.Instance?.PlayLine("s1_bubble");
        }
    }

    // 오염물 접촉 = 격려 모션(Cheer)
    private void HandlePollutantHit(PollutantObject.PollutantType type)
    {
        if (!_stageActive) return;

        PooshBotAnimator.Instance?.StartCheerTalk();
        PooshLineBank.Instance?.PlayLine("s1_hit");
    }
}