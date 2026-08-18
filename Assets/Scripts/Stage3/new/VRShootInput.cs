using UnityEngine;
using UnityEngine.InputSystem;

public class VRShootInput : MonoBehaviour, IShootInput {
    [SerializeField] private Transform controllerTransform;

    [Header("Input")]
    [SerializeField] private InputActionProperty triggerAction;

    private void OnEnable() {
        triggerAction.action?.Enable();
    }

    private void OnDisable() {
        triggerAction.action?.Disable();
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
        if (triggerAction.action == null)
            return false;

        return triggerAction.action.WasPressedThisFrame();
    }
}