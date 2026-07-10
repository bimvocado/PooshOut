using System;
using UnityEngine;

/// <summary>
/// 머리 높이가 짧은 시간 안에 급격히 상승하면 점프로 인식.
/// Stage4 DiveController가 다이빙 트리거로 사용.
/// </summary>
public class JumpDetector : MonoBehaviour
{
    [SerializeField] private HeadTracker headTracker;
    [SerializeField] private float jumpVelocityThreshold = 1.2f; // m/s, 이 이상 위로 움직이면 점프 후보
    [SerializeField] private float jumpCooldown = 0.5f;

    public event Action OnJumpDetected;

    private float _prevHeight;
    private float _cooldownTimer;
    private bool _initialized;

    private void Update()
    {
        if (headTracker == null) return;

        float height = headTracker.HeadPosition.y;
        if (!_initialized)
        {
            _prevHeight = height;
            _initialized = true;
            return;
        }

        float dt = Time.deltaTime;
        if (dt <= 0f) return;

        float verticalVelocity = (height - _prevHeight) / dt;
        _prevHeight = height;

        _cooldownTimer -= dt;
        if (verticalVelocity >= jumpVelocityThreshold && _cooldownTimer <= 0f)
        {
            _cooldownTimer = jumpCooldown;
            OnJumpDetected?.Invoke();
        }
    }
}
