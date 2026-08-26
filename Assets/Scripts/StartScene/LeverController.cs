using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.Events;
using System.Collections;

public class LeverController : MonoBehaviour {
    [Header("Input")]
    [SerializeField] private InputActionReference triggerAction;

    [Header("Rotation")]
    [SerializeField] private float hoverAngle = 10f;
    [SerializeField] private float clickAngle = 45f;
    [SerializeField] private float rotateTime = 0.2f;
    [SerializeField] private float stayTime = 0.1f;

    [Header("이벤트")]
    [Tooltip("레버가 당겨질 때(클릭될 때) 1회 호출됨.")]
    public UnityEvent OnLeverPulled;

    private Quaternion originalRotation;
    private bool isHovered;
    private bool isRotating;

    private void Start() {
        originalRotation = transform.localRotation;
    }

    private void OnEnable() {
        if (triggerAction != null)
            triggerAction.action.Enable();
    }

    private void OnDisable() {
        if (triggerAction != null)
            triggerAction.action.Disable();
    }

    private void Update() {
        if (!isHovered || isRotating || triggerAction == null)
            return;

        if (triggerAction.action.WasPressedThisFrame()) {
            Click();
        }
    }

    public void HoverEnter() {
        isHovered = true;

        if (!isRotating) {
            transform.localRotation =
                originalRotation *
                Quaternion.Euler(0f, 0f, hoverAngle);
        }
    }

    public void HoverExit() {
        isHovered = false;

        if (!isRotating)
            transform.localRotation = originalRotation;
    }

    public void Click() {
        if (!isRotating) {
            StartCoroutine(RotateAnimation());
            OnLeverPulled?.Invoke();
        }
    }

    private IEnumerator RotateAnimation() {
        isRotating = true;

        Quaternion startRot = transform.localRotation;
        Quaternion targetRot =
            originalRotation *
            Quaternion.Euler(0f, 0f, -clickAngle);

        float time = 0f;

        while (time < rotateTime) {
            time += Time.deltaTime;

            transform.localRotation =
                Quaternion.Slerp(
                    startRot,
                    targetRot,
                    time / rotateTime
                );

            yield return null;
        }

        transform.localRotation = targetRot;

        yield return new WaitForSeconds(stayTime);

        time = 0f;

        Quaternion returnTarget =
            isHovered
            ? originalRotation * Quaternion.Euler(0f, 0f, hoverAngle)
            : originalRotation;

        while (time < rotateTime) {
            time += Time.deltaTime;

            transform.localRotation =
                Quaternion.Slerp(
                    targetRot,
                    returnTarget,
                    time / rotateTime
                );

            yield return null;
        }

        transform.localRotation = returnTarget;
        isRotating = false;
    }
}