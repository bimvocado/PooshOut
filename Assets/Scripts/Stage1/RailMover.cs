using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 온레일 자동 이동. 웨이포인트를 순서대로 따라가며 일정 속도로 전진하고,
/// HandleController가 넘겨주는 좌우 입력값(-1~1)만큼 진행 방향에 수직으로 오프셋을 준다.
/// 실제 이동은 CharacterController.Move()로 처리해서 벽 Collider에 물리적으로 막히도록 한다
/// (Transform 직접 대입 방식은 Collider와 충돌 판정을 하지 않아 뚫고 지나감).
/// headTracker를 연결하면 매 프레임 캡슐의 center/height를 실제 트래킹된 머리 위치로
/// 맞춰서, 리그 안에서 플레이어가 물리적으로 좌우로 기운 만큼도 충돌 판정에 반영한다
/// (연결 안 하면 defaultCenter/defaultHeight/defaultRadius 고정값만 사용).
/// </summary>
[RequireComponent(typeof(CharacterController))]
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

    [Header("CharacterController 기본값 (플레이어 캡슐 크기)")]
    [SerializeField] private float defaultRadius = 0.3f;
    [SerializeField] private float defaultHeight = 1.7f;
    [SerializeField] private Vector3 defaultCenter = new Vector3(0f, 0.85f, 0f);

    [Header("VR 헤드 추적 동기화 (선택 — 없으면 기본값 캡슐만 사용)")]
    [Tooltip("연결하면 매 프레임 이 헤드 위치를 기준으로 캡슐 center/height를 갱신해서, " +
             "플레이어가 리그 안에서 실제로 좌우로 기운 만큼도 벽 충돌 판정에 들어가게 한다.")]
    [SerializeField] private HeadTracker headTracker;
    [SerializeField] private float headCapsulePadding = 0.15f; // 머리 위로 여유 높이
    [SerializeField] private float minCapsuleHeight = 1.0f;    // 스쿼트 등으로 너무 낮아지는 것 방지

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
    private CharacterController _controller;

    private void Awake()
    {
        _controller = GetComponent<CharacterController>();
        if (_controller == null)
        {
            // RequireComponent는 "Add Component"로 새로 붙일 때만 자동 추가되고,
            // 이미 저장된 씬에 스크립트만 먼저 있던 경우엔 안 붙어있을 수 있어 방어적으로 추가.
            _controller = gameObject.AddComponent<CharacterController>();
            Debug.LogWarning("[RailMover] CharacterController가 없어서 런타임에 자동으로 추가했습니다. " +
                "에디터에서 직접 추가하고 값을 맞춰두는 걸 권장합니다.");
        }

        if (GetComponent<Camera>() != null)
        {
            Debug.LogError("[RailMover] 이 오브젝트에 Camera 컴포넌트가 같이 붙어있습니다. " +
                "RailMover는 카메라가 아니라 XR 리그의 루트 오브젝트(예: PoopCharacter)에 있어야 합니다. " +
                "카메라에 붙어있으면 XR 헤드 트래킹이 매 프레임 갱신하는 Transform과 충돌해 오동작합니다.");
        }
    }

    private void Start()
    {
        ConfigureController();

        if (_samples.Count < 2) BuildSamples(); // RailCourse가 Awake에서 이미 채웠다면 재사용
        if (_samples.Count < 2) return; // 웨이포인트가 없으면 대기

        _distanceTraveled = 0f;
        // 최초 배치는 텔레포트라 직접 대입해도 안전 (이후 이동은 ApplyTransform에서 CharacterController.Move()로만 처리)
        SampleAt(0f, out Vector3 startPos, out Vector3 startTangent);
        startPos.y = 0f; // 웨이포인트 높이와 무관하게 항상 y=0에서 진행 (파이프 메쉬 자체 높이는 건드리지 않음)
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

    /// <summary>캡슐 기본 크기 세팅. headTracker가 있으면 이후 Update에서 매 프레임 덮어씀.</summary>
    private void ConfigureController()
    {
        _controller.radius = defaultRadius;
        _controller.height = defaultHeight;
        _controller.center = defaultCenter;
        _controller.skinWidth = Mathf.Max(0.01f, defaultRadius * 0.1f);
        _controller.minMoveDistance = 0f; // VR의 미세한 이동도 놓치지 않도록
    }

    /// <summary>RailCourse 등 외부에서 웨이포인트를 설정하고 스플라인 샘플을 다시 생성.</summary>
    public void SetWaypoints(List<Transform> newWaypoints)
    {
        waypoints = newWaypoints;
        BuildSamples();
    }

    /// <summary>웨이포인트를 Catmull-Rom 스플라인으로 촘촘히 샘플링해 _samples/_totalLength를 채움.</summary>
    private void BuildSamples()
    {
        _samples.Clear();
        _totalLength = 0f;

        if (waypoints.Count < 2)
        {
            Debug.LogWarning("[RailMover] 웨이포인트가 2개 미만이라 경로를 만들 수 없습니다.");
            return;
        }

        Vector3 previousPoint = GetCatmullRomPoint(0, 0f);
        _samples.Add(new Sample { position = previousPoint, cumulativeDistance = 0f });

        int segmentCount = waypoints.Count - 1;
        for (int segmentIndex = 0; segmentIndex < segmentCount; segmentIndex++)
        {
            for (int step = 1; step <= samplesPerSegment; step++)
            {
                float t = step / (float)samplesPerSegment;
                Vector3 point = GetCatmullRomPoint(segmentIndex, t);
                _totalLength += Vector3.Distance(previousPoint, point);
                _samples.Add(new Sample { position = point, cumulativeDistance = _totalLength });
                previousPoint = point;
            }
        }
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
        centerPos.y = 0f; // 웨이포인트 높이와 무관하게 항상 y=0에서 진행 (파이프 메쉬 자체 높이는 건드리지 않음)

        // 상하 이동 없음: up과의 외적이라 결과 벡터는 항상 수평(y=0)이 보장됨.
        Vector3 lateralDir = Vector3.Cross(Vector3.up, forwardDir).normalized;

        float targetLateral = _lateralInput * lateralRange;
        _currentLateral = Mathf.Lerp(_currentLateral, targetLateral, Time.deltaTime * lateralSmoothing);
        _currentLateral = Mathf.Clamp(_currentLateral, -lateralRange, lateralRange); // 경로 중심 기준 좌우 클램핑

        Vector3 targetPosition = centerPos + lateralDir * _currentLateral;

        SyncCapsuleToHead(); // Move() 호출 전에 캡슐을 실제 머리 위치로 맞춰야 그 프레임 충돌 판정에 반영됨
        _controller.Move(targetPosition - transform.position); // Move()로만 이동해야 벽 Collider에 물리적으로 막힘

        Quaternion targetRot = Quaternion.LookRotation(forwardDir, Vector3.up);
        transform.rotation = Quaternion.Slerp(transform.rotation, targetRot, Time.deltaTime * rotationSmoothing);
    }

    /// <summary>이진 탐색으로 누적 거리에 해당하는 스플라인 상의 위치/진행방향을 구함 (부수효과 없는 순수 조회).</summary>
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

    /// <summary>
    /// headTracker가 연결돼 있으면 캡슐 center/height를 실제 머리의 리그-로컬 위치로 갱신.
    /// 리그 원점은 고정이어도 플레이어가 몸을 기울여 머리가 좌우로 벗어난 만큼 캡슐도 따라가서,
    /// 그 상태로 Move()가 벽에 부딪히면 실제로 막히게 된다.
    /// </summary>
    private void SyncCapsuleToHead()
    {
        if (headTracker == null) return;

        Vector3 headLocal = transform.InverseTransformPoint(headTracker.HeadPosition);
        float height = Mathf.Max(minCapsuleHeight, headLocal.y + headCapsulePadding);

        _controller.height = height;
        _controller.center = new Vector3(headLocal.x, height * 0.5f, headLocal.z);
    }

    /// <summary>HandleController 등에서 좌우 조작값을 전달 (-1 ~ 1).</summary>
    public void SetLateralInput(float value) => _lateralInput = Mathf.Clamp(value, -1f, 1f);

    public void StartMoving() => IsMoving = true;
    public void StopMoving() => IsMoving = false;
}
