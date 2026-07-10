using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 레일 진행 경로를 따라 버블/오염물을 일정 간격으로 생성.
/// RailMover 앞쪽 지점을 기준으로 좌우로 흩뿌려 배치한다.
/// </summary>
public class PollutantSpawner : MonoBehaviour
{
    [Header("참조")]
    [SerializeField] private RailMover railMover;
    [SerializeField] private Transform spawnAheadPoint; // 없으면 railMover 앞쪽을 계산해서 사용

    [Header("프리팹")]
    [SerializeField] private GameObject bubblePrefab;
    [SerializeField] private List<GameObject> pollutantPrefabs = new List<GameObject>(); // 물티슈/기름/빨대

    [Header("스폰 설정")]
    [SerializeField] private float spawnInterval = 1.2f;
    [SerializeField] private float lateralSpread = 1.5f;
    [SerializeField] private float aheadDistance = 8f;
    [Range(0f, 1f)]
    [SerializeField] private float bubbleRatio = 0.5f; // 버블이 나올 확률 (나머지는 오염물)

    private Coroutine _spawnRoutine;

    public void StartSpawning()
    {
        if (_spawnRoutine != null) return;
        _spawnRoutine = StartCoroutine(SpawnLoop());
    }

    public void StopSpawning()
    {
        if (_spawnRoutine == null) return;
        StopCoroutine(_spawnRoutine);
        _spawnRoutine = null;
    }

    private IEnumerator SpawnLoop()
    {
        var wait = new WaitForSeconds(spawnInterval);
        while (true)
        {
            SpawnOne();
            yield return wait;
        }
    }

    private void SpawnOne()
    {
        if (railMover == null) return;

        Vector3 basePos = spawnAheadPoint != null
            ? spawnAheadPoint.position
            : railMover.transform.position + railMover.transform.forward * aheadDistance;

        float lateral = Random.Range(-lateralSpread, lateralSpread);
        Vector3 spawnPos = basePos + railMover.transform.right * lateral;

        bool spawnBubble = Random.value < bubbleRatio || pollutantPrefabs.Count == 0;
        GameObject prefab = spawnBubble
            ? bubblePrefab
            : pollutantPrefabs[Random.Range(0, pollutantPrefabs.Count)];

        if (prefab == null) return;
        Instantiate(prefab, spawnPos, Quaternion.identity);
    }
}
