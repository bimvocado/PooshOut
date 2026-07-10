using System.Collections;
using UnityEngine;

/// <summary>
/// 개별 미생물. 튀어나온 뒤 손 접촉(트리거)이 있으면 분해 처리되고,
/// 시간 안에 맞지 않으면 패널티 없이 그냥 들어간다.
/// </summary>
[RequireComponent(typeof(Collider))]
public class MicrobeTarget : MonoBehaviour
{
    [SerializeField] private float lifeTime = 2f;
    [SerializeField] private float purityRewardOnHit = 2f;
    [SerializeField] private string handTag = "Hand";
    [SerializeField] private AudioClip decomposeSfx;

    private MicrobeSpawner _owner;
    private Transform _spawnPoint;
    private bool _resolved;

    private void Reset()
    {
        GetComponent<Collider>().isTrigger = true;
    }

    public void Initialize(MicrobeSpawner owner, Transform spawnPoint)
    {
        _owner = owner;
        _spawnPoint = spawnPoint;
        _resolved = false;
        StartCoroutine(LifeRoutine());
    }

    private IEnumerator LifeRoutine()
    {
        yield return new WaitForSeconds(lifeTime);
        Retreat();
    }

    private void OnTriggerEnter(Collider other)
    {
        if (_resolved || !other.CompareTag(handTag)) return;
        _resolved = true;

        PurificationSystem.Instance?.Increase(purityRewardOnHit);
        _owner?.NotifyDecomposed();
        if (decomposeSfx != null) AudioManager.Instance?.PlaySfx(decomposeSfx);

        Cleanup();
    }

    private void Retreat()
    {
        if (_resolved) return;
        _resolved = true;
        Cleanup();
    }

    private void Cleanup()
    {
        _owner?.ReleaseSpawnPoint(_spawnPoint);
        Destroy(gameObject);
    }
}
