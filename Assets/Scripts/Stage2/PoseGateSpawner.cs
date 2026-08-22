using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// 한 지점(spawnPoint)에서 게이트가 계속 생성되고,
/// 각 게이트는 PoseGate.cs의 이동 로직으로 스스로 플레이어 쪽으로 다가온다.
/// 요구 포즈는 각 프리팹 자체에 미리 설정돼 있고(PoseGate.RequiredPose),
/// 스포너는 gatePrefabs 리스트에서 순서대로(또는 랜덤으로) 하나를 골라서 생성만 한다.
/// 아동 체형 대응은 여기서 하지 않고 EyeHeightAdjuster(XR Origin)가 담당한다.
public class PoseGateSpawner : MonoBehaviour
{
    [Tooltip("포즈별 게이트 프리팹들. 각 프리팹의 PoseGate 컴포넌트에 Required Pose가 미리 설정되어 있어야 함.")]
    [SerializeField] private List<GameObject> gatePrefabs = new List<GameObject>();

    [Tooltip("게이트가 처음 나타나는 지점. 딱 1개만 있으면 됨 (OhShape처럼 한 곳에서 계속 생성).")]
    [SerializeField] private Transform spawnPoint;

    [Tooltip("총 몇 개의 게이트를 생성할지")]
    [SerializeField] private int gateCount = 12;

    [Tooltip("체크하면 gatePrefabs 리스트 순서대로(0->1->2...) 생성. 해제하면 랜덤으로 생성.")]
    [SerializeField] private bool spawnInOrder = true;

    [SerializeField] private float intervalBetweenGates = 4.5f;

    [Tooltip("씬의 PoseDetector(_XR). 프리팹 애셋 자체는 씬 오브젝트를 참조할 수 없어서, 스폰 직후 코드로 연결해줘야 함.")]
    [SerializeField] private PoseDetector poseDetector;

    [Header("테스트용")]
    [Tooltip("체크하면 씬 Play 시작과 동시에 자동으로 스폰 시작 (Stage2Manager 연결 없이 단독 테스트용)")]
    [SerializeField] private bool autoStartOnPlay = false;

    public event Action OnAllGatesSpawned;

    /// 게이트 하나가 생성되는 즉시(그 프레임 안에) 발생.
    /// Stage2Manager 등이 이 이벤트를 받아서 그 자리에서 바로 gate.OnGateResolved를 구독하면,
    /// "다 스폰된 뒤에 한꺼번에 구독"할 때 생기는 지연/누락 없이 모든 게이트의 판정을 놓치지 않고 받을 수 있다.
    public event Action<PoseGate> OnGateSpawned;

    private readonly List<PoseGate> _spawnedGates = new List<PoseGate>();
    public IReadOnlyList<PoseGate> SpawnedGates => _spawnedGates;

    private void Start()
    {
        if (autoStartOnPlay)
        {
            StartSpawning();
        }
    }

    public void StartSpawning()
    {
        StopAllCoroutines();
        _spawnedGates.Clear();
        StartCoroutine(SpawnSequence());
    }

    private IEnumerator SpawnSequence()
    {
        var wait = new WaitForSeconds(intervalBetweenGates);
        int lastIndex = -1;   // 직전에 나온 포즈 (연속 중복 방지용)

        for (int i = 0; i < gateCount; i++)
        {
            int index;

            if (spawnInOrder)
            {
                index = i % gatePrefabs.Count;
            }
            else
            {
                // 랜덤이되, 직전과 같은 포즈는 다시 뽑지 않음.
                // 같은 포즈가 연달아 나오면 지루하고 난이도 체감도 떨어짐 (OhShape 방식).
                if (gatePrefabs.Count <= 1)
                {
                    index = 0;
                }
                else
                {
                    do
                    {
                        index = UnityEngine.Random.Range(0, gatePrefabs.Count);
                    }
                    while (index == lastIndex);
                }
            }

            lastIndex = index;
            SpawnGate(gatePrefabs[index]);
            yield return wait;
        }

        OnAllGatesSpawned?.Invoke();
    }

    private void SpawnGate(GameObject prefab)
    {
        if (prefab == null || spawnPoint == null) return;

        // 게이트 크기/위치는 건드리지 않는다.
        // 아동 체형 대응은 EyeHeightAdjuster가 XR Origin Y를 올려서 처리 —
        // 게이트를 줄이면 지지대가 레일 홈과 어긋나 미관이 깨지기 때문.
        GameObject instance = Instantiate(prefab, spawnPoint.position, spawnPoint.rotation);

        PoseGate gate = instance.GetComponent<PoseGate>();
        if (gate != null)
        {
            // 프리팹 애셋은 씬 오브젝트(_XR)를 참조 못 하므로, 생성 직후 코드로 연결해줌
            if (poseDetector != null)
            {
                gate.SetPoseDetector(poseDetector);
            }
            _spawnedGates.Add(gate);

            // 생성 즉시 알림 - 구독자가 이 프레임에서 바로 OnGateResolved를 걸 수 있게 한다.
            OnGateSpawned?.Invoke(gate);
        }
    }
}