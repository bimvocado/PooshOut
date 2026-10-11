using System.Collections;
using UnityEngine;

/// <summary>
/// 스테이지1 아이템(버블/쓰레기): 플레이어가 접촉하면 정화도를 올리거나 내리고,
/// 일정 시간 동안 RailMover 속도에 배율을 적용한 뒤 사라짐.
/// </summary>
[RequireComponent(typeof(Collider))]
public class BubbleItem : MonoBehaviour
{
    public enum ItemType { Bubble, Trash }

    [Header("아이템 종류")]
    [SerializeField] private ItemType itemType = ItemType.Bubble;

    [Header("참조")]
    [SerializeField] private RailMover railMover;

    [Header("정화도")]
    [SerializeField] private float bubblePurityGain = 5f;
    [SerializeField] private float trashPurityPenalty = 5f;

    [Header("속도 효과 (RailMover.ForwardSpeed 기준 배율)")]
    [SerializeField] private float bubbleSpeedMultiplier = 1.5f;
    [SerializeField] private float trashSpeedMultiplier = 0.5f;
    [SerializeField] private float speedEffectDuration = 3f;

    [Header("공통")]
    [SerializeField] private string playerTag = "Player";
    [SerializeField] private AudioClip collectSfx;

    private bool _collected;

    /// 아이템(버블/쓰레기)을 획득한 순간 발행. 풀링되어 인스턴스가 계속 바뀌므로
    /// Stage1PooshTrigger 등 외부에서는 이 static 이벤트 하나만 구독하면 모든 아이템을 다 받을 수 있음.
    public static event System.Action<ItemType> OnItemCollected;

    private void Reset()
    {
        GetComponent<Collider>().isTrigger = true;
    }

    /// <summary>스포너 등에서 아이템 종류를 지정할 때 사용.</summary>
    public void SetItemType(ItemType newType) => itemType = newType;

    /// <summary>
    /// 프리팹 애셋 자체에는 씬의 RailMover를 미리 연결해둘 수 없으므로,
    /// 스포너가 인스턴스를 생성/재사용할 때마다 실제 RailMover를 주입해준다.
    /// </summary>
    public void SetRailMover(RailMover mover) => railMover = mover;

    private void OnTriggerEnter(Collider other)
    {
        if (_collected || !other.CompareTag(playerTag)) return;
        _collected = true;

        Debug.Log($"[BubbleItem] 플레이어 충돌 감지: {name} ({itemType})");

        OnItemCollected?.Invoke(itemType);

        if (itemType == ItemType.Bubble)
        {
            PurificationSystem.Instance?.Increase(bubblePurityGain);
            ApplySpeedEffect(bubbleSpeedMultiplier);
            Stage1SoundManager.Instance?.PlayItemCollect();
            Stage1VFXManager.Instance?.PlaySpeedLineEffect();
        }
        else
        {
            PurificationSystem.Instance?.Decrease(trashPurityPenalty);
            ApplySpeedEffect(trashSpeedMultiplier);
            Stage1SoundManager.Instance?.PlayObstacleHit();
        }

        if (collectSfx != null) AudioManager.Instance?.PlaySfx(collectSfx);

        // gameObject.SetActive(false)로 자기 자신만 끄면, Trash-*_glow 프리팹처럼 글로우 파티클(루트)
        // 밑에 이 오브젝트가 중첩 프리팹 자식으로 들어있는 경우 파티클은 계속 살아남아 "쓰레기 없이
        // 파티클만 남는" 문제가 생긴다. PollutantSpawner가 실제로 풀링하는 루트를 꺼야 한다.
        PooledItemUtil.DeactivatePooledRoot(transform);
    }

    /// <summary>
    /// railMover.ForwardSpeed에 배율을 곱했다가 duration 후 되돌린다.
    /// 아이템 오브젝트는 접촉 즉시 SetActive(false)로 비활성화되어 자기 자신의 코루틴은
    /// 곧바로 멈추므로, 코루틴은 계속 살아있는 railMover에서 돌린다.
    /// </summary>
    private void ApplySpeedEffect(float multiplier)
    {
        if (railMover == null) return;
        railMover.StartCoroutine(SpeedEffectRoutine(multiplier));
    }

    private IEnumerator SpeedEffectRoutine(float multiplier)
    {
        railMover.ForwardSpeed *= multiplier;
        yield return new WaitForSeconds(speedEffectDuration);
        railMover.ForwardSpeed /= multiplier;
    }

    /// <summary>스포너가 재사용(풀링)할 때 상태 초기화용.</summary>
    public void ResetItem() => _collected = false;
}