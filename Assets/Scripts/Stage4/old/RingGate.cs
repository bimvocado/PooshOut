using System;
using System.Collections;
using UnityEngine;

/// <summary>
/// UV 살균 링 / 섬유 필터 링. 플레이어가 통과하면 정화도 보너스와 함께
/// RailMover 속도를 일시적으로 가속시킨다.
/// </summary>
[RequireComponent(typeof(Collider))]
public class RingGate : MonoBehaviour
{
    public enum RingType { UV, Fiber }

    [SerializeField] private RingType ringType = RingType.UV;
    [SerializeField] private RailMover railMover;
    [SerializeField] private float purityReward = 3f;
    [SerializeField] private float speedBoostMultiplier = 1.3f;
    [SerializeField] private float boostDuration = 1.5f;
    [SerializeField] private string playerTag = "Player";

    public event Action<RingGate> OnPassed;

    private bool _passed;

    private void Reset()
    {
        GetComponent<Collider>().isTrigger = true;
    }

    public void SetRingType(RingType type) => ringType = type;

    private void OnTriggerEnter(Collider other)
    {
        if (_passed || !other.CompareTag(playerTag)) return;
        _passed = true;

        PurificationSystem.Instance?.Increase(purityReward);
        if (railMover != null) StartCoroutine(BoostSpeed());

        OnPassed?.Invoke(this);
        Debug.Log($"[RingGate] {ringType} 링 통과");
    }

    private IEnumerator BoostSpeed()
    {
        float original = railMover.ForwardSpeed;
        railMover.ForwardSpeed = original * speedBoostMultiplier;
        yield return new WaitForSeconds(boostDuration);
        railMover.ForwardSpeed = original;
    }
}
