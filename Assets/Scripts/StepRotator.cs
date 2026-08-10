using UnityEngine;

/// <summary>
/// 1초마다 Y축 기준으로 90도씩 회전 (0 -> 90 -> 180 -> 270 -> 0 -> ...).
/// 부드럽게 돌아가는 게 아니라 "딱딱" 끊어서 각도가 바뀜.
/// 
/// 세팅 방법: 회전시킬 오브젝트에 이 스크립트만 붙이면 바로 동작.
/// </summary>
public class StepRotator : MonoBehaviour {
    [SerializeField] private float rotationInterval = 1f; // 회전 간격 (초)
    [SerializeField] private float stepAngle = 90f;        // 한 번에 회전할 각도

    private float _timer;
    private int _stepIndex;

    private void Update() {
        _timer += Time.deltaTime;

        if (_timer >= rotationInterval) {
            _timer = 0f;
            _stepIndex++;

            float targetY = (_stepIndex * stepAngle) % 360f;
            transform.rotation = Quaternion.Euler(0f, targetY, 0f);
        }
    }
}