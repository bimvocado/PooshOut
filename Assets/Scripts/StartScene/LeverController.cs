using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;
using System.Collections;

public class LeverController: MonoBehaviour {
    [Header("Outline")]
    public GameObject outlineObject;

    [Header("Rotation")]
    public float rotateAngle = 45f;
    public float rotateTime = 0.2f;
    public float stayTime = 0.1f;

    private Quaternion originalRotation;
    private bool isRotating = false;

    void Start() {
        originalRotation = transform.localRotation;

        if (outlineObject != null)
            outlineObject.SetActive(false);
    }

    // Ray가 닿았을 때
    public void HoverEnter() {
        if (outlineObject != null)
            outlineObject.SetActive(true);
    }

    // Ray가 빠졌을 때
    public void HoverExit() {
        if (outlineObject != null)
            outlineObject.SetActive(false);
    }

    // 클릭했을 때
    public void Click() {
        if (!isRotating)
            StartCoroutine(RotateAnimation());
    }

    IEnumerator RotateAnimation() {
        isRotating = true;

        Quaternion startRot = originalRotation;
        Quaternion targetRot =
            originalRotation * Quaternion.Euler(0, 45f, 0);

        // 45도 회전
        float time = 0;

        while (time < rotateTime) {
            time += Time.deltaTime;
            float t = time / rotateTime;

            transform.localRotation =
                Quaternion.Slerp(startRot, targetRot, t);

            yield return null;
        }

        transform.localRotation = targetRot;

        yield return new WaitForSeconds(stayTime);

        // 원위치로 복귀
        time = 0;

        while (time < rotateTime) {
            time += Time.deltaTime;
            float t = time / rotateTime;

            transform.localRotation =
                Quaternion.Slerp(targetRot, startRot, t);

            yield return null;
        }

        transform.localRotation = startRot;
        isRotating = false;
    }
}