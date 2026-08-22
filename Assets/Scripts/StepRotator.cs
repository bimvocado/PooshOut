using UnityEngine;

public class StepRotator : MonoBehaviour {
    public enum RotationAxis {
        X,
        Y,
        Z
    }

    [SerializeField] private float rotationInterval = 1f;
    [SerializeField] private float stepAngle = 90f;

    [SerializeField] private RotationAxis rotationAxis = RotationAxis.Y;

    private float _timer;

    private void Update() {
        _timer += Time.deltaTime;

        if (_timer >= rotationInterval) {
            _timer -= rotationInterval;

            Vector3 euler = transform.localEulerAngles;

            switch (rotationAxis) {
                case RotationAxis.X:
                    euler.x += stepAngle;
                    break;

                case RotationAxis.Y:
                    euler.y += stepAngle;
                    break;

                case RotationAxis.Z:
                    euler.z += stepAngle;
                    break;
            }

            transform.localEulerAngles = euler;
        }
    }
}