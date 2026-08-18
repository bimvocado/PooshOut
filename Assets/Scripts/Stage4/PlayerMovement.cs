using UnityEngine;

[RequireComponent(typeof(CharacterController))]
public class PlayerMovement : MonoBehaviour {
    [Header("이동 설정")]
    [SerializeField] private float moveSpeed = 2f;
    [SerializeField] private float tiltThreshold = 20f;

    [Header("참조")]
    [SerializeField] private Transform headTransform;
    [SerializeField] private Transform leftController;
    [SerializeField] private Transform rightController;

    private CharacterController _characterController;

    private float _leftStartX;
    private float _rightStartX;

    private void Awake() {
        _characterController = GetComponent<CharacterController>();
    }

    private void Start() {
        _leftStartX = NormalizeAngle(leftController.localEulerAngles.x);
        _rightStartX = NormalizeAngle(rightController.localEulerAngles.x);
    }

    private void Update() {
        Vector3 moveDir = GetVRMoveDirection();

        if (moveDir == Vector3.zero) {
            moveDir = GetDebugMoveDirection();
        }

        if (moveDir != Vector3.zero) {
            _characterController.Move(
                moveDir * moveSpeed * Time.deltaTime
            );
        }
    }

    private Vector3 GetVRMoveDirection() {
        if (leftController == null ||
            rightController == null ||
            headTransform == null) {
            return Vector3.zero;
        }

        float leftCurrentX =
            NormalizeAngle(leftController.localEulerAngles.x);

        float rightCurrentX =
            NormalizeAngle(rightController.localEulerAngles.x);

        float leftDelta =
            Mathf.DeltaAngle(_leftStartX, leftCurrentX);

        float rightDelta =
            Mathf.DeltaAngle(_rightStartX, rightCurrentX);

        // 둘 다 초기 자세보다 X축으로 충분히 기울였을 때
        if (leftDelta < tiltThreshold ||
            rightDelta < tiltThreshold) {
            return Vector3.zero;
        }

        Vector3 moveDir = headTransform.forward;
        moveDir.y = 0f;

        return moveDir.normalized;
    }

    private float NormalizeAngle(float angle) {
        if (angle > 180f)
            angle -= 360f;

        return angle;
    }

    private Vector3 GetDebugMoveDirection() {
        float h = 0f;
        float v = 0f;

        if (Input.GetKey(KeyCode.UpArrow)) v += 1f;
        if (Input.GetKey(KeyCode.DownArrow)) v -= 1f;
        if (Input.GetKey(KeyCode.RightArrow)) h += 1f;
        if (Input.GetKey(KeyCode.LeftArrow)) h -= 1f;

        if (Mathf.Approximately(h, 0f) &&
            Mathf.Approximately(v, 0f)) {
            return Vector3.zero;
        }

        Transform reference =
            headTransform != null ? headTransform : transform;

        Vector3 forward = new Vector3(
            reference.forward.x,
            0f,
            reference.forward.z
        ).normalized;

        Vector3 right = new Vector3(
            reference.right.x,
            0f,
            reference.right.z
        ).normalized;

        return (forward * v + right * h).normalized;
    }
}