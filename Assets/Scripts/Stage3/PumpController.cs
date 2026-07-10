using System;
using UnityEngine;

/// <summary>
/// 레버를 위아래로 펌프질하는 동작 인식 (한 손 기준 수직 왕복 운동).
/// 방향이 전환되는 순간(위→아래 또는 아래→위)마다 펌프 1회로 카운트.
/// </summary>
public class PumpController : MonoBehaviour
{
    [SerializeField] private HandTracker handTracker;
    [SerializeField] private bool useRightHand = true;
    [SerializeField] private float minStrokeSpeed = 0.5f; // m/s, 이 이상이어야 유효한 펌프질
    [SerializeField] private float strokeCooldown = 0.25f;

    public event Action OnPump;
    public int PumpCount { get; private set; }

    private float _prevVelocityY;
    private float _cooldownTimer;

    private void Update()
    {
        if (handTracker == null) return;

        float velocityY = useRightHand ? handTracker.RightHandVelocity.y : handTracker.LeftHandVelocity.y;
        _cooldownTimer -= Time.deltaTime;

        bool directionChanged = Mathf.Sign(velocityY) != Mathf.Sign(_prevVelocityY);
        bool fastEnough = Mathf.Abs(_prevVelocityY) > minStrokeSpeed;

        if (directionChanged && fastEnough && _cooldownTimer <= 0f)
        {
            _cooldownTimer = strokeCooldown;
            PumpCount++;
            OnPump?.Invoke();
        }

        _prevVelocityY = velocityY;
    }

    public void ResetCount() => PumpCount = 0;
}
