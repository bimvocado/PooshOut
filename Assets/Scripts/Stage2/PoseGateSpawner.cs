using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// OhShape 스타일: 한 지점(spawnPoint)에서 게이트가 계속 생성되고,
/// 각 게이트는 PoseGate.cs의 이동 로직으로 스스로 플레이어 쪽으로 다가온다.
/// 요구 포즈는 각 프리팹 자체에 미리 설정돼 있고(PoseGate.RequiredPose),
/// 스포너는 gatePrefabs 리스트에서 순서대로(또는 랜덤으로) 하나를 골라서 생성만 한다.
/// </summary>
public class PoseGateSpawner : MonoBehaviour
{
    [Tooltip("포즈별 게이트 프리팹들. 각 프리팹의 PoseGate 컴포넌트에 Required Pose가 미리 설정되어 있어야 함.")]
    [SerializeField] private List<GameObject> gatePrefabs = new List<GameObject>();

    [Tooltip("게이트가 처음 나타나는 지점. 딱 1개만 있으면 됨 (OhShape처럼 한 곳에서 계속 생성).")]
    [SerializeField] private Transform spawnPoint;

    [Tooltip("총 몇 개의 게이트를 생성할지")]
    [SerializeField] private int gateCount = 9;

    [Tooltip("체크하면 gatePrefabs 리스트 순서대로(0->1->2...) 생성. 해제하면 랜덤으로 생성.")]
    [SerializeField] private bool spawnInOrder = true;

    [SerializeField] private float intervalBetweenGates = 4f;

    [Header("아동 체형 대응 (캘리브레이션 연동)")]
    [SerializeField] private bool applyHeightScaling = true;
    [SerializeField] private float referenceAdultHeight = 1.65f;
    [SerializeField] private HeadTracker headTracker;

    [Tooltip("씬의 PoseDetector(_XR). 프리팹 애셋 자체는 씬 오브젝트를 참조할 수 없어서, 스폰 직후 코드로 연결해줘야 함.")]
    [SerializeField] private PoseDetector poseDetector;
    [SerializeField] private float minScaleRatio = 0.6f;

    [Header("테스트용")]
    [Tooltip("체크하면 씬 Play 시작과 동시에 자동으로 스폰 시작 (Stage2Manager 연결 없이 단독 테스트용)")]
    [SerializeField] private bool autoStartOnPlay = false;

    public event Action OnAllGatesSpawned;

    private readonly List<PoseGate> _spawnedGates = new List<PoseGate>();
    public IReadOnlyList<PoseGate> SpawnedGates => _spawnedGates;

    private void Awake()
    {
        if (headTracker == null)
        {
            headTracker = FindObjectOfType<HeadTracker>();
        }
    }

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

        for (int i = 0; i < gateCount; i++)
        {
            GameObject prefab = spawnInOrder
                ? gatePrefabs[i % gatePrefabs.Count]           // 순서대로: 0,1,2,...,8
                : gatePrefabs[UnityEngine.Random.Range(0, gatePrefabs.Count)]; // 랜덤

            SpawnGate(prefab);
            yield return wait;
        }

        OnAllGatesSpawned?.Invoke();
    }

    private float GetHeightScaleRatio()
    {
        if (!applyHeightScaling || headTracker == null || !headTracker.IsCalibrated || referenceAdultHeight <= 0f)
            return 1f;

        float ratio = headTracker.BaselineHeight / referenceAdultHeight;
        return Mathf.Max(ratio, minScaleRatio);
    }

    private void SpawnGate(GameObject prefab)
    {
        if (prefab == null || spawnPoint == null) return;

        float scaleRatio = GetHeightScaleRatio();

        Vector3 spawnPosition = spawnPoint.position;
        spawnPosition.y *= scaleRatio;

        GameObject instance = Instantiate(prefab, spawnPosition, spawnPoint.rotation);
        instance.transform.localScale = prefab.transform.localScale * scaleRatio;

        PoseGate gate = instance.GetComponent<PoseGate>();
        if (gate != null)
        {
            // 프리팹 애셋은 씬 오브젝트(_XR)를 참조 못 하므로, 생성 직후 코드로 연결해줌
            if (poseDetector != null)
            {
                gate.SetPoseDetector(poseDetector);
            }
            _spawnedGates.Add(gate);
        }
    }
}