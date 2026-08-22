using UnityEngine;

/// <summary>
/// 오염물(물티슈/기름/빨대): 플레이어가 접촉하면 정화도를 깎고 사라짐.
/// </summary>
[RequireComponent(typeof(Collider))]
public class PollutantObject : MonoBehaviour
{
    public enum PollutantType { WetWipe, Oil, Straw }

    [SerializeField] private PollutantType type = PollutantType.WetWipe;
    [SerializeField] private float purityPenalty = 5f;
    [SerializeField] private string playerTag = "Player";
    [SerializeField] private AudioClip hitSfx;

    public PollutantType Type => type;

    private bool _hit;

    private void Reset()
    {
        GetComponent<Collider>().isTrigger = true;
    }

    private void OnTriggerEnter(Collider other)
    {
        if (_hit || !other.CompareTag(playerTag)) return;
        _hit = true;

        Debug.Log($"[PollutantObject] 플레이어 충돌 감지: {name} ({type})");

        PurificationSystem.Instance?.ReportPollutantContact(type.ToString(), purityPenalty);
        if (hitSfx != null) AudioManager.Instance?.PlaySfx(hitSfx);

        // gameObject.SetActive(false)로 자기 자신만 끄면, 글로우 파티클(루트) 밑에 이 오브젝트가
        // 중첩 프리팹 자식으로 들어있는 경우 파티클은 계속 살아남는다. PollutantSpawner가 실제로
        // 풀링하는 루트를 꺼야 한다.
        PooledItemUtil.DeactivatePooledRoot(transform);
    }

    /// <summary>스포너가 풀링 시 타입을 바꿔가며 재사용.</summary>
    public void SetType(PollutantType newType) => type = newType;
    public void ResetItem() => _hit = false;
}
