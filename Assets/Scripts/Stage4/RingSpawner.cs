using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 16개 타일 중 하나를 랜덤으로 골라, 랜덤한 시간 간격마다 링을 하나씩 스폰하는 매니저.
/// - 직전에 스폰된 타일은 이번 스폰에서 제외
/// - 스폰 간격은 minInterval~maxInterval 사이에서 매번 랜덤
/// 
/// 세팅 방법:
/// 1. 빈 오브젝트에 이 스크립트 추가 (예: "RingSpawner")
/// 2. tiles 배열에 16개 타일(TileController가 붙은 오브젝트)을 순서대로 드래그
/// 3. ringPrefab에 링 프리팹 연결
/// 4. spawnHeight: 타일 기준 얼마나 위에서 링이 떨어질지
/// </summary>
public class RingSpawner : MonoBehaviour {
    [Header("타일 참조 (16개)")]
    [SerializeField] private TileController[] tiles;

    [Header("링 스폰 설정")]
    [SerializeField] private GameObject ringPrefab;
    [SerializeField] private float spawnHeight = 5f;
    [SerializeField] private float minInterval = 1.5f; // 다음 스폰까지 최소 대기 시간
    [SerializeField] private float maxInterval = 4f;    // 다음 스폰까지 최대 대기 시간
    [SerializeField] private Vector3 spawnRotationEuler = new Vector3(90f, 0f, 0f); // 링이 눕도록 회전 (X축 90도 기본값)

    private float _timer;
    private float _nextSpawnTime;
    private int _lastSpawnedIndex = -1;
    private bool _isSpawning;

    private void Awake() {
        // 타일 인덱스 세팅 (Ring 판정 등에서 활용 가능)
        for (int i = 0; i < tiles.Length; i++) {
            if (tiles[i] != null) {
                tiles[i].TileIndex = i;
            }
        }
    }

    private void Start() {
        // 별도의 매니저가 없으면 자동으로 스폰 시작
        StartSpawning();
    }

    private void Update() {
        if (!_isSpawning) return;

        _timer += Time.deltaTime;

        if (_timer >= _nextSpawnTime) {
            _timer = 0f;
            SpawnOneRing();
            _nextSpawnTime = Random.Range(minInterval, maxInterval);
        }
    }

    /// <summary>
    /// 외부(Stage4Manager 등)에서 스폰을 시작할 때 호출.
    /// </summary>
    public void StartSpawning() {
        _isSpawning = true;
        _timer = 0f;
        _nextSpawnTime = Random.Range(minInterval, maxInterval);
    }

    /// <summary>
    /// 외부에서 스폰을 멈출 때 호출.
    /// </summary>
    public void StopSpawning() {
        _isSpawning = false;
    }

    private void SpawnOneRing() {
        if (tiles == null || tiles.Length == 0 || ringPrefab == null) return;

        int index = PickRandomTileIndex();
        SpawnRingAt(tiles[index]);
        _lastSpawnedIndex = index;
    }

    private int PickRandomTileIndex() {
        // 직전에 스폰된 타일 제외한 후보 목록 생성
        List<int> candidates = new List<int>();
        for (int i = 0; i < tiles.Length; i++) {
            if (i != _lastSpawnedIndex) {
                candidates.Add(i);
            }
        }

        // 타일이 1개뿐이라 후보가 없는 극단적 상황 대비
        if (candidates.Count == 0) {
            candidates.Add(0);
        }

        return candidates[Random.Range(0, candidates.Count)];
    }

    private void SpawnRingAt(TileController tile) {
        if (tile == null) return;

        // 타일 점등
        tile.SetLit(true);

        // 타일 바로 위, spawnHeight 만큼 띄운 위치에서 스폰
        Vector3 spawnPos = tile.transform.position + Vector3.up * spawnHeight;
        Quaternion spawnRotation = Quaternion.Euler(spawnRotationEuler);
        GameObject ringObj = Instantiate(ringPrefab, spawnPos, spawnRotation);

        Ring ring = ringObj.GetComponent<Ring>();
        if (ring != null) {
            ring.Init(tile);
        }
    }
}