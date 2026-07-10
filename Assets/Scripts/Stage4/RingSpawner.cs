using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 방류 구간의 UV/섬유 필터 링을 미리 배치된 지점에 순서대로 생성.
/// </summary>
public class RingSpawner : MonoBehaviour
{
    [SerializeField] private GameObject uvRingPrefab;
    [SerializeField] private GameObject fiberRingPrefab;
    [SerializeField] private List<Transform> spawnPoints = new List<Transform>();
    [SerializeField] private float intervalBetweenRings = 1.5f;

    public event Action OnAllRingsSpawned;

    private readonly List<RingGate> _spawnedRings = new List<RingGate>();
    public IReadOnlyList<RingGate> SpawnedRings => _spawnedRings;

    public void StartSpawning()
    {
        StopAllCoroutines();
        _spawnedRings.Clear();
        StartCoroutine(SpawnSequence());
    }

    private IEnumerator SpawnSequence()
    {
        var wait = new WaitForSeconds(intervalBetweenRings);
        foreach (Transform point in spawnPoints)
        {
            SpawnRingAt(point);
            yield return wait;
        }
        OnAllRingsSpawned?.Invoke();
    }

    private void SpawnRingAt(Transform point)
    {
        if (point == null) return;

        bool useUv = UnityEngine.Random.value < 0.5f || fiberRingPrefab == null;
        GameObject prefab = useUv ? uvRingPrefab : fiberRingPrefab;
        if (prefab == null) return;

        GameObject instance = Instantiate(prefab, point.position, point.rotation);
        RingGate gate = instance.GetComponent<RingGate>();
        if (gate != null)
        {
            gate.SetRingType(useUv ? RingGate.RingType.UV : RingGate.RingType.Fiber);
            _spawnedRings.Add(gate);
        }
    }
}
