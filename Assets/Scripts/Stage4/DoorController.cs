using System.Collections;
using UnityEngine;

/// <summary>
/// 정화도가 만점이 되면 문(바닥)을 지정된 축 기준으로 서서히 회전시켜 여는 스크립트.
/// 피봇이 힌지(문 끝) 쪽에 있다고 가정.
/// 
/// 세팅 방법:
/// 1. 문 오브젝트(피봇이 힌지 쪽으로 세팅된)에 이 스크립트 추가
/// 2. PurificationManager의 onMaxPurification 이벤트에 이 스크립트의 OpenDoor() 함수를 인스펙터에서 연결
/// 3. rotationAxis: 로컬 기준 회전축 (기본 X축, 문 구조에 따라 Y/Z로 변경 가능)
/// 4. targetAngle: 90도 (필요시 조절)
/// 5. rotationDuration: 얼마 동안 회전할지(초)
/// </summary>
public class DoorController : MonoBehaviour
{
    [Header("회전 설정")]
    [SerializeField] private Vector3 rotationAxis = Vector3.right; // 로컬 X축 기본값
    [SerializeField] private float targetAngle = 90f;
    [SerializeField] private float rotationDuration = 1.5f;

    private bool _isOpening;
    private bool _hasOpened;

    /// <summary>
    /// PurificationManager.onMaxPurification 이벤트에서 호출.
    /// </summary>
    public void OpenDoor()
    {
        if (_isOpening || _hasOpened) return;
        StartCoroutine(RotateDoorRoutine());
    }

    private IEnumerator RotateDoorRoutine()
    {
        _isOpening = true;

        Quaternion startRotation = transform.localRotation;
        Quaternion endRotation = startRotation * Quaternion.AngleAxis(targetAngle, rotationAxis);

        float elapsed = 0f;
        while (elapsed < rotationDuration)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / rotationDuration);
            // 부드러운 가감속을 위해 SmoothStep 사용
            float smoothT = Mathf.SmoothStep(0f, 1f, t);
            transform.localRotation = Quaternion.Slerp(startRotation, endRotation, smoothT);
            yield return null;
        }

        transform.localRotation = endRotation;
        _isOpening = false;
        _hasOpened = true;

        // TODO: 문이 열린 후 동작(다음 스테이지 로드 등)이 정해지면 여기에 추가
    }
}
