using UnityEngine;
using UnityEngine.InputSystem;

public class VRShootInput : MonoBehaviour, IShootInput {
    [SerializeField] private Transform controllerTransform;

    [Header("Input")]
    [SerializeField] private InputActionReference triggerAction;

    private bool _triggerPressedThisFrame;

    private void OnEnable() {
        if (triggerAction != null) {
            triggerAction.action.performed += OnTriggerPressed;
            triggerAction.action.Enable();
        }
    }

    private void OnDisable() {
        if (triggerAction != null) {
            triggerAction.action.performed -= OnTriggerPressed;
            triggerAction.action.Disable();
        }
    }

    private void OnTriggerPressed(InputAction.CallbackContext context) {
        SetTriggerPressed();
    }

    public bool TryGetAim(out Vector3 origin, out Vector3 direction) {
        if (controllerTransform == null) {
            origin = Vector3.zero;
            direction = Vector3.forward;
            return false;
        }

        origin = controllerTransform.position;
        direction = controllerTransform.forward;

        return true;
    }

    public bool FirePressedThisFrame() {
        bool pressed = _triggerPressedThisFrame;
        _triggerPressedThisFrame = false;

        return pressed;
    }

    public void SetTriggerPressed() {
        _triggerPressedThisFrame = true;
    }
}