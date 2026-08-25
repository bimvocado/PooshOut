using UnityEngine;

public class CutsceneCameraOrbit : MonoBehaviour {
    [Header("회전 중심")]
    [SerializeField] private Transform centerPoint;

    [Header("궤도 설정")]
    [SerializeField] private float radius = 10f;
    [SerializeField] private float elevationAngle = 20f;
    [SerializeField] private float startAzimuth = 0f;
    [SerializeField] private float rotationSpeed = 5f;
    [SerializeField] private bool lookAtCenter = true;

    private float _azimuth;

    private void Start() {
        _azimuth = startAzimuth;
    }

    private void Update() {
        if (centerPoint == null) {
            return;
        }

        _azimuth += rotationSpeed * Time.deltaTime;

        float elevationRad = elevationAngle * Mathf.Deg2Rad;
        float azimuthRad = _azimuth * Mathf.Deg2Rad;

        Vector3 offset = new Vector3(
            radius * Mathf.Cos(elevationRad) * Mathf.Cos(azimuthRad),
            radius * Mathf.Sin(elevationRad),
            radius * Mathf.Cos(elevationRad) * Mathf.Sin(azimuthRad)
        );

        transform.position = centerPoint.position + offset;

        if (lookAtCenter) {
            transform.LookAt(centerPoint.position);
        }
    }
}
