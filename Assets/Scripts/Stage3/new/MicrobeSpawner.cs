using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 미생물 스포너: 지정된 박스 범위(스포너 위치 + spawnAreaOffset을 중심으로 spawnAreaSize 크기) 안에서
/// 랜덤 위치에 미생물을 스폰한다. 여러 종류의 프리팹 중 하나를 랜덤으로 골라 스폰한다.
/// 동시 존재 개체 수는 maxConcurrent로 제한한다.
/// </summary>
public class MicrobeSpawner : MonoBehaviour {
    [Tooltip("스폰될 미생물 프리팹 후보들. 매번 이 중 하나를 랜덤으로 골라서 스폰한다.")]
    [SerializeField] private List<GameObject> microbePrefabs = new List<GameObject>();

    [Header("스폰 범위 (이 오브젝트 위치 기준 로컬 오프셋 + 크기)")]
    [SerializeField] private Vector3 spawnAreaOffset = Vector3.zero;
    [SerializeField] private Vector3 spawnAreaSize = new Vector3(5f, 0f, 5f);

    [Header("동시 존재 개체 수 제한")]
    [SerializeField] private int maxConcurrent = 5;

    [Header("미생물이 맞은 뒤 다가갈 목표 (보통 플레이어)")]
    [SerializeField] private Transform playerTarget;
    [SerializeField] private string playerTag = "Player"; // playerTarget 비어있을 때 자동 탐색용

    [Header("스폰 간격")]
    [SerializeField] private float spawnInterval = 1.5f;

    public int DecomposedCount { get; private set; }

    private Coroutine _routine;
    private int _activeCount;

    private void Awake() {
        if (playerTarget == null) {
            GameObject player = GameObject.FindGameObjectWithTag(playerTag);
            if (player != null) playerTarget = player.transform;
        }
    }

    public void StartSpawning() {
        if (_routine != null) return;
        _routine = StartCoroutine(SpawnLoop());
    }

    public void StopSpawning() {
        if (_routine == null) return;
        StopCoroutine(_routine);
        _routine = null;
    }

    private IEnumerator SpawnLoop() {
        while (true) {
            yield return new WaitForSeconds(spawnInterval);
            SpawnOne();
        }
    }

    private void SpawnOne() {
        if (microbePrefabs.Count == 0) return;
        if (_activeCount >= maxConcurrent) return; // 이미 최대치만큼 떠있으면 스킵

        GameObject prefab = microbePrefabs[Random.Range(0, microbePrefabs.Count)];
        Vector3 position = GetRandomPointInArea();
        Quaternion rotation = GetFacingPlayerRotation(position);

        GameObject instance = Instantiate(prefab, position, rotation);
        MicrobeTarget target = instance.GetComponent<MicrobeTarget>();
        if (target != null) {
            _activeCount++;
            target.Initialize(this, playerTarget);
        }
    }

    /// <summary>스폰 위치에서 플레이어 쪽을 바라보는 회전값을 계산 (위아래로 안 기울어지게 수평만 고려).</summary>
    private Quaternion GetFacingPlayerRotation(Vector3 spawnPosition) {
        if (playerTarget == null) return Quaternion.identity;

        Vector3 dir = playerTarget.position - spawnPosition;
        dir.y = 0f;
        if (dir.sqrMagnitude < 0.0001f) return Quaternion.identity;

        return Quaternion.LookRotation(dir.normalized);
    }

    private Vector3 GetRandomPointInArea() {
        Vector3 center = transform.position + spawnAreaOffset;
        Vector3 half = spawnAreaSize * 0.5f;

        float x = Random.Range(center.x - half.x, center.x + half.x);
        float y = Random.Range(center.y - half.y, center.y + half.y);
        float z = Random.Range(center.z - half.z, center.z + half.z);
        return new Vector3(x, y, z);
    }

    /// <summary>MicrobeTarget이 사라질 때(맞음/시간초과 무관) 호출 - 동시 개체 수 카운트 감소.</summary>
    public void NotifyDespawned() {
        _activeCount = Mathf.Max(0, _activeCount - 1);
    }

    public void NotifyDecomposed() => DecomposedCount++;

    // 스폰 범위를 씬 뷰에서 눈으로 확인할 수 있도록 표시
    private void OnDrawGizmosSelected() {
        Gizmos.color = Color.cyan;
        Gizmos.DrawWireCube(transform.position + spawnAreaOffset, spawnAreaSize);
    }
}