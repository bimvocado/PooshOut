using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 거름망 게이트를 미리 배치된 지점에 순서대로/일정 간격으로 생성하고,
/// 각 게이트에 요구 포즈를 랜덤 배정한다.
/// </summary>
public class PoseGateSpawner : MonoBehaviour
{
    [SerializeField] private GameObject poseGatePrefab;
    [SerializeField] private List<Transform> spawnPoints = new List<Transform>();
    [SerializeField]
    private PoseDetector.PoseType[] posePool =
    {
        PoseDetector.PoseType.ArmsUp,
        PoseDetector.PoseType.ArmsWide,
        PoseDetector.PoseType.Crouch
    };
    [SerializeField] private float intervalBetweenGates = 2.5f;

    public event Action OnAllGatesSpawned;

    private readonly List<PoseGate> _spawnedGates = new List<PoseGate>();
    public IReadOnlyList<PoseGate> SpawnedGates => _spawnedGates;

    public void StartSpawning()
    {
        StopAllCoroutines();
        _spawnedGates.Clear();
        StartCoroutine(SpawnSequence());
    }

    private IEnumerator SpawnSequence()
    {
        var wait = new WaitForSeconds(intervalBetweenGates);
        foreach (Transform point in spawnPoints)
        {
            SpawnGateAt(point);
            yield return wait;
        }
        OnAllGatesSpawned?.Invoke();
    }

    private void SpawnGateAt(Transform point)
    {
        if (poseGatePrefab == null || point == null) return;

        GameObject instance = Instantiate(poseGatePrefab, point.position, point.rotation);
        PoseGate gate = instance.GetComponent<PoseGate>();
        if (gate != null)
        {
            PoseDetector.PoseType pose = posePool[UnityEngine.Random.Range(0, posePool.Length)];
            gate.SetRequiredPose(pose);
            _spawnedGates.Add(gate);
        }
    }
}
