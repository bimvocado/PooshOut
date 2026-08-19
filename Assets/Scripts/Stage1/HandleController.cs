using UnityEngine;

/// <summary>
/// 오토바이식 조향 핸들. VR 실기기에서 그립 판정(XRSimpleInteractable 거리 체크)이 불안정해서,
/// "잡아야만 동작"하는 방식을 버리고 처음부터 항상 양손이 핸들을 잡고 있는 것으로 간주한다.
/// leftHandTransform/rightHandTransform(실제 컨트롤러 트랜스폼)의 높이 차이를 그대로 핸들 메쉬의
/// 롤 회전으로 시각화한 뒤 RailMover의 좌우 입력으로 전달한다.
/// </summary>
public class HandleController : MonoBehaviour
{
    [Header("참조")]
    [SerializeField] private RailMover railMover;
    [SerializeField] private Transform leftHandTransform;  // 실제 왼쪽 컨트롤러(Interactor) 트랜스폼
    [SerializeField] private Transform rightHandTransform; // 실제 오른쪽 컨트롤러(Interactor) 트랜스폼
    [SerializeField] private Transform handleMesh; // 실제로 회전할 핸들 모델 (비워두면 이 오브젝트)

    [Header("조작 감도")]
    [SerializeField] private float tiltSensitivity = 4f; // 손 높이차 1m당 조향 배율
    [SerializeField] private float deadZone = 0.02f;      // 이 미만의 높이차는 무시 (손떨림 방지)
    [SerializeField] private float maxTiltAngle = 45f;    // 핸들 메쉬가 실제로 회전하는 최대 각도

    [Header("스무딩")]
    [SerializeField] private float steeringSmoothing = 8f; // 조향 반응 속도

    /// <summary>-1(왼쪽) ~ 1(오른쪽), 스무딩 적용된 현재 조향값.</summary>
    public float SteeringValue { get; private set; }

    private float _currentSteering;
    private Quaternion _baseLocalRotation; // handleMesh에 미리 세팅된 보정 회전(예: Y 180도)을 보존하기 위한 기준값

    private void Awake()
    {
        if (handleMesh == null) handleMesh = transform;
        _baseLocalRotation = handleMesh.localRotation;
    }

    private void Update()
    {
        float targetSteering = 0f;

        if (leftHandTransform != null && rightHandTransform != null)
        {
            float heightDiff = leftHandTransform.position.y - rightHandTransform.position.y;
            if (Mathf.Abs(heightDiff) < deadZone) heightDiff = 0f;

            targetSteering = Mathf.Clamp(heightDiff * tiltSensitivity, -1f, 1f);
        }

        _currentSteering = Mathf.MoveTowards(_currentSteering, targetSteering, Time.deltaTime * steeringSmoothing);
        SteeringValue = _currentSteering;

        handleMesh.localRotation = _baseLocalRotation * Quaternion.Euler(0f, 0f, -_currentSteering * maxTiltAngle);

        railMover?.SetLateralInput(_currentSteering);
    }
}
