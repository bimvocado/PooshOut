using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 미생물 두더지잡기: 미리 배치된 구멍(spawnPoints) 중 비어있는 곳에서 미생물을 튀어나오게 한다.
/// PumpController의 펌프질 횟수가 많을수록(산소 공급) 스폰 간격이 짧아진다.
/// </summary>
public class MicrobeSpawner : MonoBehaviour
{
    [SerializeField] private GameObject microbePrefab;
    [SerializeField] private List<Transform> spawnPoints = new List<Transform>();
    [SerializeField] private PumpController pumpController;

    [Header("스폰 간격")]
    [SerializeField] private float baseInterval = 1.5f;
    [SerializeField] private float minInterval = 0.4f;
    [SerializeField] private float intervalReductionPerPump = 0.03f;

    public int DecomposedCount { get; private set; }

    private Coroutine _routine;
    private readonly HashSet<Transform> _occupied = new HashSet<Transform>();

    public void StartSpawning()
    {
        if (_routine != null) return;
        _routine = StartCoroutine(SpawnLoop());
    }

    public void StopSpawning()
    {
        if (_routine == null) return;
        StopCoroutine(_routine);
        _routine = null;
    }

    private IEnumerator SpawnLoop()
    {
        while (true)
        {
            float interval = baseInterval;
            if (pumpController != null)
            {
                interval -= pumpController.PumpCount * intervalReductionPerPump;
            }
            interval = Mathf.Max(minInterval, interval);
            yield return new WaitForSeconds(interval);

            SpawnOne();
        }
    }

    private void SpawnOne()
    {
        if (microbePrefab == null || spawnPoints.Count == 0) return;

        List<Transform> free = spawnPoints.FindAll(p => !_occupied.Contains(p));
        if (free.Count == 0) return;

        Transform point = free[Random.Range(0, free.Count)];
        _occupied.Add(point);

        GameObject instance = Instantiate(microbePrefab, point.position, point.rotation);
        MicrobeTarget target = instance.GetComponent<MicrobeTarget>();
        if (target != null)
        {
            target.Initialize(this, point);
        }
    }

    /// <summary>MicrobeTarget이 사라질 때(성공/실패 무관) 구멍을 다시 비워준다.</summary>
    public void ReleaseSpawnPoint(Transform point) => _occupied.Remove(point);

    public void NotifyDecomposed() => DecomposedCount++;
}
