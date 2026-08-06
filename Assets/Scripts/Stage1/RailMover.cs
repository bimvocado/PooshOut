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
    [Header("경로 (순서대로 지나갈 지점들)")]
    [SerializeField] private List<Transform> waypoints = new List<Transform>();

    [Header("이동 설정")]
    [SerializeField] private float forwardSpeed = 3f;
    [SerializeField] private float lateralRange = 1.5f;
    [SerializeField] private float lateralSmoothing = 5f;

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
    public float NormalizedProgress { get; private set; }

    public event Action OnReachedEnd;

    private int _targetIndex = 1;
    private float _lateralInput;
    private float _currentLateral;
    private Vector3 _railPosition; // 좌우 오프셋을 뺀 레일 중심 위치
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

        if (waypoints.Count > 0)
        {
            _railPosition = waypoints[0].position;
            // 최초 배치는 텔레포트라 직접 대입해도 안전 (CharacterController는 이후 Move()로만 이동)
            transform.position = _railPosition;
        }
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

        SyncCapsuleToHead(); // Move() 호출 전에 캡슐을 실제 머리 위치로 맞춰야 그 프레임 충돌 판정에 반영됨

        Vector3 targetPosition = _railPosition + lateralDir * _currentLateral;
        _controller.Move(targetPosition - transform.position);
        transform.rotation = Quaternion.LookRotation(forwardDir, Vector3.up);

        NormalizedProgress = (float)_targetIndex / (waypoints.Count - 1);
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
