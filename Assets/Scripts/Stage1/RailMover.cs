using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 온레일 자동 이동. 웨이포인트를 순서대로 따라가며 일정 속도로 전진하고,
/// HandleController가 넘겨주는 좌우 입력값(-1~1)만큼 진행 방향에 수직으로 오프셋을 준다.
/// </summary>
public class RailMover : MonoBehaviour
{
    [Header("경로 (순서대로 지나갈 지점들)")]
    [SerializeField] private List<Transform> waypoints = new List<Transform>();

    [Header("이동 설정")]
    [SerializeField] private float forwardSpeed = 3f;
    [SerializeField] private float lateralRange = 1.5f;
    [SerializeField] private float lateralSmoothing = 5f;

    public bool IsMoving { get; private set; }
    /// <summary>RingGate 등에서 일시적으로 가속시킬 때 사용.</summary>
    public float ForwardSpeed { get => forwardSpeed; set => forwardSpeed = value; }
    /// <summary>0~1, 스포너가 앞쪽 스폰 위치를 계산할 때 참고용.</summary>
    public float NormalizedProgress { get; private set; }

    public event Action OnReachedEnd;

    private int _targetIndex = 1;
    private float _lateralInput;
    private float _currentLateral;
    private Vector3 _railPosition; // 좌우 오프셋을 뺀 레일 중심 위치

    private void Start()
    {
        if (waypoints.Count > 0)
        {
            _railPosition = waypoints[0].position;
            transform.position = _railPosition;
        }
    }

    private void Update()
    {
        if (!IsMoving || waypoints.Count < 2) return;

        Transform target = waypoints[_targetIndex];
        Vector3 toTarget = target.position - _railPosition;

        if (toTarget.magnitude < 0.05f)
        {
            _targetIndex++;
            if (_targetIndex >= waypoints.Count)
            {
                IsMoving = false;
                OnReachedEnd?.Invoke();
                return;
            }
            target = waypoints[_targetIndex];
            toTarget = target.position - _railPosition;
        }

        Vector3 forwardDir = toTarget.normalized;
        _railPosition += forwardDir * (forwardSpeed * Time.deltaTime);

        Vector3 lateralDir = Vector3.Cross(Vector3.up, forwardDir);
        _currentLateral = Mathf.Lerp(_currentLateral, _lateralInput * lateralRange, Time.deltaTime * lateralSmoothing);

        transform.position = _railPosition + lateralDir * _currentLateral;
        transform.rotation = Quaternion.LookRotation(forwardDir, Vector3.up);

        NormalizedProgress = (float)_targetIndex / (waypoints.Count - 1);
    }

    /// <summary>HandleController 등에서 좌우 조작값을 전달 (-1 ~ 1).</summary>
    public void SetLateralInput(float value) => _lateralInput = Mathf.Clamp(value, -1f, 1f);

    public void StartMoving() => IsMoving = true;
    public void StopMoving() => IsMoving = false;
}
