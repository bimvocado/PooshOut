using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 미생물 스포너: 지정된 박스 범위(스포너 위치 + spawnAreaOffset을 중심으로 spawnAreaSize 크기) 안에서
/// 랜덤 위치에 미생물을 스폰한다. 여러 종류의 프리팹 중 하나를 랜덤으로 골라 스폰한다.
/// 동시 존재 개체 수는 maxConcurrent로 제한한다.
/// 메인 카메라와 너무 가까운 위치에는 스폰하지 않는다.
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
    [SerializeField] private Transform eyeTarget;
    [SerializeField] private Vector3 eyeTargetOffset = new Vector3(0f, -0.5f, 0f); // 카메라 기준 오프셋
    [SerializeField] private string playerTag = "Player";

    [Header("메인 카메라와의 최소 스폰 거리")]
    [SerializeField] private float minSpawnDistanceFromPlayer = 2f; // 이 반경(메인 카메라 중심) 안에는 스폰 안 함
    [SerializeField] private int maxSpawnAttempts = 10; // 조건 맞는 위치 찾기 위한 최대 재시도 횟수

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

        if (eyeTarget == null && Camera.main != null) {
            GameObject eyeObj = new GameObject("MicrobeEyeTarget");
            eyeObj.transform.SetParent(Camera.main.transform);
            eyeObj.transform.localPosition = eyeTargetOffset; // 카메라 로컬 기준 오프셋
            eyeTarget = eyeObj.transform;
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
        if (_activeCount >= maxConcurrent) return;

        if (!TryGetValidSpawnPosition(out Vector3 position)) return; // 조건 맞는 위치를 못 찾으면 이번 스폰은 스킵

        GameObject prefab = microbePrefabs[Random.Range(0, microbePrefabs.Count)];
        Quaternion rotation = GetFacingPlayerRotation(position);

        GameObject instance = Instantiate(prefab, position, rotation);
        MicrobeTarget target = instance.GetComponent<MicrobeTarget>();
        if (target != null) {
            _activeCount++;
            target.Initialize(this, eyeTarget); // playerTarget → eyeTarget으로 변경
        }
    }

    /// <summary>
    /// 메인 카메라와 minSpawnDistanceFromPlayer 이상 떨어진 랜덤 위치를 찾는다.
    /// maxSpawnAttempts 안에 조건 맞는 위치를 못 찾으면 실패(false) 반환.
    /// </summary>
    private bool TryGetValidSpawnPosition(out Vector3 position) {
        Vector3 referencePoint = Camera.main != null ? Camera.main.transform.position : transform.position;

        for (int i = 0; i < maxSpawnAttempts; i++) {
            Vector3 candidate = GetRandomPointInArea();

            if (Vector3.Distance(candidate, referencePoint) >= minSpawnDistanceFromPlayer) {
                position = candidate;
                return true;
            }
        }

        position = Vector3.zero;
        return false;
    }

    /// <summary>스폰 위치에서 플레이어 쪽을 바라보는 회전값을 계산 (위아래 기울임 포함).</summary>
    private Quaternion GetFacingPlayerRotation(Vector3 spawnPosition) {
        if (playerTarget == null) return Quaternion.identity;

        // Y축 고정(dir.y = 0f)을 제거하여 3D 전체 방향 계산
        Vector3 dir = playerTarget.position - spawnPosition;

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

        // 메인 카메라 주변 스폰 금지 반경도 같이 표시
        Vector3 referencePoint = Camera.main != null ? Camera.main.transform.position : transform.position;
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(referencePoint, minSpawnDistanceFromPlayer);
    }
}