using UnityEngine;

[RequireComponent(typeof(CharacterController))]
public class CharacterControllerTest : MonoBehaviour {
    [Header("이동")]
    [SerializeField] private float moveSpeed = 3f;

    [Header("중력")]
    [SerializeField] private float gravity = -9.81f;
    [SerializeField] private float groundedForce = -2f;
    [SerializeField] private float maxFallSpeed = -30f;

    [Header("경사")]
    [SerializeField] private float groundCheckDistance = 0.5f;
    [SerializeField] private LayerMask groundLayer = ~0;

    [Header("이동 방향")]
    [SerializeField] private Transform directionReference;

    private CharacterController _controller;
    private float _verticalVelocity;

    private void Awake() {
        _controller = GetComponent<CharacterController>();
    }

    private void Update() {
        float h = 0f;
        float v = 0f;

        if (Input.GetKey(KeyCode.W) || Input.GetKey(KeyCode.UpArrow))
            v += 1f;

        if (Input.GetKey(KeyCode.S) || Input.GetKey(KeyCode.DownArrow))
            v -= 1f;

        if (Input.GetKey(KeyCode.D) || Input.GetKey(KeyCode.RightArrow))
            h += 1f;

        if (Input.GetKey(KeyCode.A) || Input.GetKey(KeyCode.LeftArrow))
            h -= 1f;

        Transform reference =
            directionReference != null
                ? directionReference
                : transform;

        // 카메라 기준 앞/오른쪽
        Vector3 forward = reference.forward;
        Vector3 right = reference.right;

        forward.y = 0f;
        right.y = 0f;

        forward.Normalize();
        right.Normalize();

        Vector3 moveDirection =
            forward * v +
            right * h;

        if (moveDirection.sqrMagnitude > 1f)
            moveDirection.Normalize();

        // =========================
        // 경사면 감지
        // =========================

        if (GetGroundNormal(out Vector3 groundNormal)) {
            float slopeAngle =
                Vector3.Angle(groundNormal, Vector3.up);

            // CharacterController가 올라갈 수 있는 경사면이면
            if (slopeAngle <= _controller.slopeLimit) {
                // 이동 방향을 경사면에 맞춤
                moveDirection =
                    Vector3.ProjectOnPlane(
                        moveDirection,
                        groundNormal
                    ).normalized;
            }
        }

        // =========================
        // 중력
        // =========================

        if (_controller.isGrounded) {
            if (_verticalVelocity < 0f)
                _verticalVelocity = groundedForce;
        }
        else {
            _verticalVelocity += gravity * Time.deltaTime;

            if (_verticalVelocity < maxFallSpeed)
                _verticalVelocity = maxFallSpeed;
        }

        // =========================
        // 이동
        // =========================

        Vector3 velocity =
            moveDirection * moveSpeed;

        // 경사면을 이동할 때는 moveDirection 자체에
        // Y값이 들어있으므로 공중일 때만 중력을 강하게 적용
        if (!_controller.isGrounded) {
            velocity.y += _verticalVelocity;
        }
        else {
            // 바닥에 붙어있도록 아주 살짝 아래로
            velocity.y += groundedForce;
        }

        _controller.Move(
            velocity * Time.deltaTime
        );
    }

    private bool GetGroundNormal(out Vector3 groundNormal) {
        // CharacterController 중심 기준으로 아래쪽 검사
        Vector3 origin =
            transform.position +
            _controller.center;

        float radius =
            _controller.radius * 0.8f;

        float castDistance =
            (_controller.height * 0.5f)
            + groundCheckDistance;

        if (Physics.SphereCast(
            origin,
            radius,
            Vector3.down,
            out RaycastHit hit,
            castDistance,
            groundLayer,
            QueryTriggerInteraction.Ignore)) {
            groundNormal = hit.normal;
            return true;
        }

        groundNormal = Vector3.up;
        return false;
    }
}