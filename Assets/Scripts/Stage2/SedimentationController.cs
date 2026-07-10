using System;
using UnityEngine;

/// <summary>
/// 양손으로 물을 젓는 수영 동작을 감지해서 캐릭터를 서서히 가라앉힌다.
/// 손 속도가 임계값 이상이면 "스트로크"로 인정하고 깊이를 누적한다.
/// </summary>
public class SedimentationController : MonoBehaviour
{
    [SerializeField] private HandTracker handTracker;
    [SerializeField] private Transform sinkingTarget; // 가라앉힐 대상 (플레이어 리그 루트 등)

    [Header("판정 기준")]
    [SerializeField] private float strokeSpeedThreshold = 0.8f; // m/s
    [SerializeField] private float sinkPerStroke = 0.05f;       // 스트로크 1회당 내려가는 높이(m)
    [SerializeField] private float targetDepth = 2f;            // 목표까지 가라앉혀야 하는 총 높이(m)
    [SerializeField] private float strokeCooldown = 0.3f;       // 연타 방지

    public event Action OnSedimentationComplete;
    public float CurrentDepth { get; private set; }
    public bool IsComplete { get; private set; }

    private float _cooldownTimer;
    private Vector3 _startPosition;
    private bool _active;

    public void BeginSedimentation()
    {
        _active = true;
        IsComplete = false;
        CurrentDepth = 0f;
        if (sinkingTarget != null) _startPosition = sinkingTarget.position;
    }

    private void Update()
    {
        if (!_active || IsComplete || handTracker == null) return;

        _cooldownTimer -= Time.deltaTime;

        bool leftStroke = handTracker.LeftHandVelocity.magnitude > strokeSpeedThreshold;
        bool rightStroke = handTracker.RightHandVelocity.magnitude > strokeSpeedThreshold;

        if ((leftStroke || rightStroke) && _cooldownTimer <= 0f)
        {
            _cooldownTimer = strokeCooldown;
            CurrentDepth += sinkPerStroke;

            if (sinkingTarget != null)
            {
                Vector3 pos = sinkingTarget.position;
                pos.y = _startPosition.y - CurrentDepth;
                sinkingTarget.position = pos;
            }

            if (CurrentDepth >= targetDepth)
            {
                IsComplete = true;
                _active = false;
                OnSedimentationComplete?.Invoke();
            }
        }
    }
}
