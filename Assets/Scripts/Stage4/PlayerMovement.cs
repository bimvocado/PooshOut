using UnityEngine;
using UnityEngine.XR;

/// <summary>
/// 플레이어 이동 스크립트.
/// - VR: 양쪽 컨트롤러 중 기울어진 쪽의 "향하는 방향"으로 이동 (수평 이동만, 위아래 제외)
/// - 에디터/키보드 디버깅: WASD로 이동 (카메라 정면 기준)
/// 
/// 세팅 방법:
/// 1. XR Origin(플레이어 루트) 오브젝트에 CharacterController 컴포넌트 추가
/// 2. 이 스크립트를 같은 오브젝트에 추가
/// 3. Head Transform에는 XR Origin 하위의 Main Camera(HMD) 연결 (WASD 이동 시 방향 기준)
/// 
/// 참고: 컨트롤러 기울기 판정은 InputDevices(UnityEngine.XR) 기반으로 작성했습니다.
/// XR Interaction Toolkit의 Input Action 방식(New Input System)을 쓰고 계시면
/// deviceRotation 대신 Input Action Reference로 교체가 필요할 수 있습니다.
/// </summary>
[RequireComponent(typeof(CharacterController))]
public class PlayerMovement : MonoBehaviour
{
    [Header("이동 설정")]
    [SerializeField] private float moveSpeed = 2f;
    [SerializeField] private float tiltThreshold = 15f; // 이 각도(도) 이상 기울여야 이동 시작

    [Header("참조")]
    [SerializeField] private Transform headTransform; // WASD 디버깅 이동 시 방향 기준 (보통 Main Camera)

    private CharacterController _characterController;

    private void Awake()
    {
        _characterController = GetComponent<CharacterController>();
    }

    private void Update()
    {
        Vector3 moveDir = GetVRMoveDirection();

        // VR 컨트롤러 입력이 없으면(에디터 디버깅 등) 키보드 입력 사용
        if (moveDir == Vector3.zero)
        {
            moveDir = GetDebugMoveDirection();
        }

        if (moveDir != Vector3.zero)
        {
            _characterController.Move(moveDir * moveSpeed * Time.deltaTime);
        }
    }

    /// <summary>
    /// 양쪽 컨트롤러 중 기울기 임계값을 넘은 컨트롤러의 "향하는 방향"을 반환.
    /// 위아래 성분은 제거해서 수평 이동만 되도록 함.
    /// </summary>
    private Vector3 GetVRMoveDirection()
    {
        Vector3 dir = TryGetControllerForwardDir(XRNode.RightHand);
        if (dir != Vector3.zero) return dir;

        dir = TryGetControllerForwardDir(XRNode.LeftHand);
        return dir;
    }

    private Vector3 TryGetControllerForwardDir(XRNode node)
    {
        InputDevice device = InputDevices.GetDeviceAtXRNode(node);
        if (!device.isValid) return Vector3.zero;

        if (!device.TryGetFeatureValue(CommonUsages.deviceRotation, out Quaternion rotation))
        {
            return Vector3.zero;
        }

        Vector3 controllerForward = rotation * Vector3.forward;

        // 컨트롤러가 수평면 기준으로 얼마나 아래(앞)로 숙여졌는지 각도 계산
        // controllerForward.y가 음수로 커질수록(아래를 향할수록) 더 많이 기울인 것으로 판단
        float pitchAngle = Mathf.Asin(Mathf.Clamp(-controllerForward.y, -1f, 1f)) * Mathf.Rad2Deg;

        if (pitchAngle < tiltThreshold) return Vector3.zero;

        // 수평 성분만 남기고 정규화 (위아래 이동 제외)
        Vector3 horizontalDir = new Vector3(controllerForward.x, 0f, controllerForward.z);
        if (horizontalDir.sqrMagnitude < 0.0001f) return Vector3.zero;

        return horizontalDir.normalized;
    }

    /// <summary>
    /// 에디터 디버깅용 WASD 입력. 헤드 방향(정면) 기준 수평 이동.
    /// </summary>
    private Vector3 GetDebugMoveDirection()
    {
        float h = Input.GetAxisRaw("Horizontal"); // A/D
        float v = Input.GetAxisRaw("Vertical");   // W/S

        if (Mathf.Approximately(h, 0f) && Mathf.Approximately(v, 0f))
        {
            return Vector3.zero;
        }

        Transform reference = headTransform != null ? headTransform : transform;

        Vector3 forward = new Vector3(reference.forward.x, 0f, reference.forward.z).normalized;
        Vector3 right = new Vector3(reference.right.x, 0f, reference.right.z).normalized;

        return (forward * v + right * h).normalized;
    }
}
