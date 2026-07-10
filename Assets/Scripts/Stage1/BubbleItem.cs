using UnityEngine;

/// <summary>
/// 버블 아이템: 플레이어가 접촉하면 정화도를 올리고 사라짐.
/// </summary>
[RequireComponent(typeof(Collider))]
public class BubbleItem : MonoBehaviour
{
    [SerializeField] private float purityAmount = 3f;
    [SerializeField] private string playerTag = "Player";
    [SerializeField] private AudioClip collectSfx;

    private bool _collected;

    private void Reset()
    {
        GetComponent<Collider>().isTrigger = true;
    }

    private void OnTriggerEnter(Collider other)
    {
        if (_collected || !other.CompareTag(playerTag)) return;
        _collected = true;

        PurificationSystem.Instance?.Increase(purityAmount);
        if (collectSfx != null) AudioManager.Instance?.PlaySfx(collectSfx);

        gameObject.SetActive(false);
    }

    /// <summary>스포너가 재사용(풀링)할 때 상태 초기화용.</summary>
    public void ResetItem() => _collected = false;
}
