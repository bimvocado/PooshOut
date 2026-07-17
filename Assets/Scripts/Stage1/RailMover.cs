using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 온레일 자동 이동 (물살에 떠내려가는 느낌).
/// 웨이포인트들을 Catmull-Rom 스플라인으로 보간해 부드러운 경로를 만들고,
/// 등속(아크렝스 기준)으로 그 경로를 따라 전진한다.
/// HandleController가 넘겨주는 좌우 입력값(-1~1)은 경로 중심 기준 좌우 오프셋으로만 반영되며
/// (상하 이동 없음), 진행 방향 회전도 스무딩되어 코너에서 시점이 뚝뚝 끊기지 않는다.
/// </summary>
public class RailMover : MonoBehaviour
{
    [Header("경로 (RailCourse가 자동으로 채워줌, 직접 넣어도 됨)")]
    [SerializeField] private List<Transform> waypoints = new List<Transform>();
    [SerializeField] private int samplesPerSegment = 12; // 스플라인 샘플링 밀도 (구간당)

    [Header("이동 설정")]
    [SerializeField] private float forwardSpeed = 3f;
    [SerializeField] private float lateralRange = 1.2f;    // 파이프 중심 기준 좌우 클램프 반경
    [SerializeField] private float lateralSmoothing = 5f;
    [SerializeField] private float rotationSmoothing = 6f; // 시점 회전 스무딩 (코너 멀미 방지)

    public bool IsMoving { get; private set; }
    /// <summary>RingGate 등에서 일시적으로 가속시킬 때 사용.</summary>
    public float ForwardSpeed { get => forwardSpeed; set => forwardSpeed = value; }
    /// <summary>0~1, 스포너가 앞쪽 스폰 위치를 계산할 때 참고용.</summary>
    public float NormalizedProgress => _totalLength > 0f ? Mathf.Clamp01(_distanceTraveled / _totalLength) : 0f;
    public float DistanceTraveled => _distanceTraveled;
    public float TotalLength => _totalLength;
    public float LateralOffset => _currentLateral;

    public event Action OnReachedEnd;

    private struct Sample
    {
        public Vector3 position;
        public float cumulativeDistance;
    }

    private readonly List<Sample> _samples = new List<Sample>();
    private float _totalLength;
    private float _distanceTraveled;
    private float _lateralInput;
    private float _currentLateral;

    private void Start()
    {
        if (waypoints.Count >= 2) BuildSpline();
    }

    /// <summary>RailCourse 등에서 세그먼트를 이어붙인 최종 경로를 주입할 때 사용.</summary>
    public void SetWaypoints(List<Transform> points)
    {
        waypoints = points;
        BuildSpline();
    }

    private void BuildSpline()
    {
        _samples.Clear();
        _totalLength = 0f;

        if (waypoints.Count < 2)
        {
            Debug.LogWarning("[RailMover] 웨이포인트가 2개 미만이라 경로를 만들 수 없습니다.");
            return;
        }

        Vector3 prevPos = GetCatmullRomPoint(0, 0f);
        _samples.Add(new Sample { position = prevPos, cumulativeDistance = 0f });

        int segmentCount = waypoints.Count - 1;
        for (int seg = 0; seg < segmentCount; seg++)
        {
            for (int i = 1; i <= samplesPerSegment; i++)
            {
                float t = (float)i / samplesPerSegment;
                Vector3 pos = GetCatmullRomPoint(seg, t);
                _totalLength += Vector3.Distance(prevPos, pos);
                _samples.Add(new Sample { position = pos, cumulativeDistance = _totalLength });
                prevPos = pos;
            }
        }

        _distanceTraveled = 0f;
        SampleAt(0f, out Vector3 startPos, out Vector3 startTangent);
        transform.SetPositionAndRotation(startPos, Quaternion.LookRotation(startTangent, Vector3.up));
    }

    private Vector3 GetCatmullRomPoint(int segmentIndex, float t)
    {
        Vector3 p0 = GetWaypoint(segmentIndex - 1);
        Vector3 p1 = GetWaypoint(segmentIndex);
        Vector3 p2 = GetWaypoint(segmentIndex + 1);
        Vector3 p3 = GetWaypoint(segmentIndex + 2);

        float t2 = t * t;
        float t3 = t2 * t;

        return 0.5f * (
            2f * p1 +
            (-p0 + p2) * t +
            (2f * p0 - 5f * p1 + 4f * p2 - p3) * t2 +
            (-p0 + 3f * p1 - 3f * p2 + p3) * t3
        );
    }

    private Vector3 GetWaypoint(int index)
    {
        index = Mathf.Clamp(index, 0, waypoints.Count - 1);
        return waypoints[index].position;
    }

    private void Update()
    {
        if (!IsMoving || _samples.Count < 2) return;

        _distanceTraveled += forwardSpeed * Time.deltaTime;

        bool reachedEnd = _distanceTraveled >= _totalLength;
        if (reachedEnd) _distanceTraveled = _totalLength;

        ApplyTransform();

        if (reachedEnd)
        {
            IsMoving = false;
            OnReachedEnd?.Invoke();
        }
    }

    private void ApplyTransform()
    {
        SampleAt(_distanceTraveled, out Vector3 centerPos, out Vector3 forwardDir);

        // 상하 이동 없음: up과의 외적이라 결과 벡터는 항상 수평(y=0)이 보장됨.
        Vector3 lateralDir = Vector3.Cross(Vector3.up, forwardDir).normalized;

        float targetLateral = _lateralInput * lateralRange;
        _currentLateral = Mathf.Lerp(_currentLateral, targetLateral, Time.deltaTime * lateralSmoothing);
        _currentLateral = Mathf.Clamp(_currentLateral, -lateralRange, lateralRange); // 경로 중심 기준 좌우 클램핑

        transform.position = centerPos + lateralDir * _currentLateral;

        Quaternion targetRot = Quaternion.LookRotation(forwardDir, Vector3.up);
        transform.rotation = Quaternion.Slerp(transform.rotation, targetRot, Time.deltaTime * rotationSmoothing);
    }

    /// <summary>이진 탐색으로 누적 거리에 해당하는 스플라인 상의 위치/진행방향을 구함.</summary>
    private void SampleAt(float distance, out Vector3 position, out Vector3 tangent)
    {
        distance = Mathf.Clamp(distance, 0f, _totalLength);

        int lo = 0, hi = _samples.Count - 1;
        while (lo < hi)
        {
            int mid = (lo + hi) / 2;
            if (_samples[mid].cumulativeDistance < distance) lo = mid + 1;
            else hi = mid;
        }

        int upperIndex = Mathf.Max(lo, 1);
        Sample upper = _samples[upperIndex];
        Sample lower = _samples[upperIndex - 1];

        float segmentLength = upper.cumulativeDistance - lower.cumulativeDistance;
        float t = segmentLength > 0.0001f ? (distance - lower.cumulativeDistance) / segmentLength : 0f;

        position = Vector3.Lerp(lower.position, upper.position, t);

        Vector3 delta = upper.position - lower.position;
        tangent = delta.sqrMagnitude > 0.0001f ? delta.normalized : transform.forward;
    }

    /// <summary>현재 위치보다 aheadDistance 만큼 앞선 스플라인 상의 위치 (스포너용).</summary>
    public Vector3 GetPositionAheadOf(float aheadDistance)
    {
        SampleAt(_distanceTraveled + aheadDistance, out Vector3 pos, out _);
        return pos;
    }

    /// <summary>현재 위치보다 aheadDistance 만큼 앞선 지점의 진행 방향 (스포너용).</summary>
    public Vector3 GetTangentAheadOf(float aheadDistance)
    {
        SampleAt(_distanceTraveled + aheadDistance, out _, out Vector3 tangent);
        return tangent;
    }

    /// <summary>HandleController 등에서 좌우 조작값을 전달 (-1 ~ 1).</summary>
    public void SetLateralInput(float value) => _lateralInput = Mathf.Clamp(value, -1f, 1f);

    public void StartMoving() => IsMoving = true;
    public void StopMoving() => IsMoving = false;
}
