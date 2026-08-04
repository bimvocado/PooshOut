using UnityEngine;

/// <summary>
/// VR 컨트롤러 레이 기반 조준 입력.
/// controllerTransform의 위치/정면 방향을 그대로 조준 레이로 사용한다.
///
/// 트리거 버튼 입력은 프로젝트에서 쓰는 XR SDK(XR Interaction Toolkit, OpenXR 등)에 맞춰
/// SetTriggerPressed()를 호출해주면 된다. 예시(XR Interaction Toolkit, Input System 기반):
///
///   public InputActionReference triggerAction;
///   private void OnEnable()  => triggerAction.action.performed += OnTriggerPerformed;
///   private void OnDisable() => triggerAction.action.performed -= OnTriggerPerformed;
///   private void OnTriggerPerformed(InputAction.CallbackContext ctx) => SetTriggerPressed();
///
/// 지금은 마우스(MouseShootInput)로 테스트하다가, 나중에 PlayerShooter의
/// aimInputSource 참조만 이 컴포넌트로 바꿔 끼우면 로직 변경 없이 VR로 전환된다.
/// </summary>
public class VRShootInput : MonoBehaviour, IShootInput
{
    [SerializeField] private Transform controllerTransform;

    private bool _triggerPressedThisFrame;

    public bool TryGetAim(out Vector3 origin, out Vector3 direction)
    {
        if (controllerTransform == null)
        {
            origin = Vector3.zero;
            direction = Vector3.forward;
            return false;
        }

        origin = controllerTransform.position;
        direction = controllerTransform.forward;
        return true;
    }

    public bool FirePressedThisFrame()
    {
        bool pressed = _triggerPressedThisFrame;
        _triggerPressedThisFrame = false; // 한 프레임만 유효하도록 소비 후 리셋
        return pressed;
    }

    /// <summary>VR 트리거 버튼이 눌렸을 때 외부(XR 입력 이벤트)에서 호출.</summary>
    public void SetTriggerPressed()
    {
        _triggerPressedThisFrame = true;
    }
}
