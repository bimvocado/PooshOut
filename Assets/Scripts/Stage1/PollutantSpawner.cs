using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 레일 진행 경로를 따라 버블/오염물을 일정 간격으로 생성.
/// RailMover의 스플라인 상에서 "앞쪽 지점"을 물어봐서 배치하기 때문에
/// 곡선 구간에서도 파이프 벽 밖(혹은 안쪽)에 스폰되지 않는다.
/// 오브젝트는 풀링되어 재사용되며, 플레이어가 못 맞고 지나친 아이템은
/// 일정 시간 후 자동으로 풀에 반환된다.
/// </summary>
public class PollutantSpawner : MonoBehaviour
{
    [Header("참조")]
    [SerializeField] private RailMover railMover;

    [Header("프리팹")]
    [SerializeField] private GameObject bubblePrefab;
    [SerializeField] private List<GameObject> pollutantPrefabs = new List<GameObject>(); // 물티슈/기름/빨대

    [Header("스폰 설정")]
    [SerializeField] private float spawnInterval = 1.2f;
    [SerializeField] private float lateralSpread = 1f;
    [SerializeField] private float aheadDistance = 8f;
    [Tooltip("스폰 Y 위치를 이 값으로 고정한다. GetPositionAheadOf가 반환하는 파이프 웨이포인트의 원본 Y는 " +
             "RailMover.railHeight로 보정되지 않은 값이라 구간마다 들쭉날쭉하고 실제 플레이어 주행 높이와도 " +
             "어긋날 수 있어, 스포너에서 별도로 고정한다.")]
    [SerializeField] private float spawnHeight = 1f;
    [Range(0f, 1f)]
    [SerializeField] private float bubbleRatio = 0.5f; // 버블이 나올 확률 (나머지는 오염물)

    [Header("회수(풀링) 설정")]
    [SerializeField] private float maxLifetime = 15f;   // 못 맞고 지나친 아이템 강제 회수 시간
    [SerializeField] private float cleanupInterval = 0.5f;

    private Coroutine _spawnRoutine;
    private Coroutine _cleanupRoutine;

    private readonly Dictionary<GameObject, Queue<GameObject>> _pools = new Dictionary<GameObject, Queue<GameObject>>();
    private readonly List<ActiveItem> _active = new List<ActiveItem>();

    private struct ActiveItem
    {
        public GameObject instance;
        public GameObject prefab;
        public float spawnTime;
    }

    public void StartSpawning()
    {
        if (_spawnRoutine == null) _spawnRoutine = StartCoroutine(SpawnLoop());
        if (_cleanupRoutine == null) _cleanupRoutine = StartCoroutine(CleanupLoop());
    }

    public void StopSpawning()
    {
        if (_spawnRoutine != null)
        {
            StopCoroutine(_spawnRoutine);
            _spawnRoutine = null;
        }
        if (_cleanupRoutine != null)
        {
            StopCoroutine(_cleanupRoutine);
            _cleanupRoutine = null;
        }
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

    private IEnumerator CleanupLoop()
    {
        var wait = new WaitForSeconds(cleanupInterval);
        while (true)
        {
            yield return wait;
            RecycleFinishedItems();
        }
    }

    private void SpawnOne()
    {
        if (railMover == null) return;

        bool spawnBubble = Random.value < bubbleRatio || pollutantPrefabs.Count == 0;
        GameObject prefab = spawnBubble
            ? bubblePrefab
            : pollutantPrefabs[Random.Range(0, pollutantPrefabs.Count)];
        if (prefab == null) return;

        Vector3 centerPos = railMover.GetPositionAheadOf(aheadDistance);
        centerPos.y = spawnHeight;
        Vector3 tangent = railMover.GetTangentAheadOf(aheadDistance);
        Vector3 lateralDir = Vector3.Cross(Vector3.up, tangent).normalized;

        float lateral = Random.Range(-lateralSpread, lateralSpread);
        Vector3 spawnPos = centerPos + lateralDir * lateral;

        GameObject instance = GetFromPool(prefab);
        instance.transform.position = spawnPos; // 회전은 프리팹에 설정된 그대로 유지 (강제 정렬하지 않음)

        BubbleItem bubbleItem = instance.GetComponent<BubbleItem>();
        bubbleItem?.SetRailMover(railMover);
        bubbleItem?.ResetItem();
        instance.GetComponent<PollutantObject>()?.ResetItem();

        instance.SetActive(true);
        _active.Add(new ActiveItem { instance = instance, prefab = prefab, spawnTime = Time.time });
    }

    private GameObject GetFromPool(GameObject prefab)
    {
        if (!_pools.TryGetValue(prefab, out Queue<GameObject> queue))
        {
            queue = new Queue<GameObject>();
            _pools[prefab] = queue;
        }

        return queue.Count > 0 ? queue.Dequeue() : Instantiate(prefab, transform);
    }

    private void RecycleFinishedItems()
    {
        for (int i = _active.Count - 1; i >= 0; i--)
        {
            ActiveItem item = _active[i];
            if (item.instance == null)
            {
                _active.RemoveAt(i);
                continue;
            }

            bool consumed = !item.instance.activeSelf; // Bubble/Pollutant가 접촉 시 스스로 비활성화함
            bool expired = Time.time - item.spawnTime > maxLifetime;

            if (consumed || expired)
            {
                if (!consumed) item.instance.SetActive(false);
                _pools[item.prefab].Enqueue(item.instance);
                _active.RemoveAt(i);
            }
        }
    }
}
