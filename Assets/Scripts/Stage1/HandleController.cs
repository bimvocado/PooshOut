using UnityEngine;

/// <summary>
/// 오토바이식 조향 핸들. VR 실기기에서 그립 판정(XRSimpleInteractable 거리 체크)이 불안정해서,
/// "잡아야만 동작"하는 방식을 버리고 처음부터 항상 양손이 핸들을 잡고 있는 것으로 간주한다.
/// leftHandTransform/rightHandTransform(실제 컨트롤러 트랜스폼)의 높이 차이를 핸들 메쉬 회전과
/// RailMover의 좌우 입력으로 전달한다.
///
/// 핸들 회전은 오일러 각 대입이 아니라, Start()에 저장해둔 초기 로컬 회전(rest pose)에
/// 단일 축 회전(AngleAxis)만 곱해서 적용한다 (짐벌락 방지, rest pose 불변 보장).
/// 회전축 자체는 손으로 축 벡터를 맞추는 대신 ComputeRotationAxis()가 매 프레임 계산한다:
/// axisMode가 ReferenceTransform이면 axisReferenceTransform(예: 스티어링 칼럼)의 로컬 축을,
/// OffsetFromAxis면 기준 축에 인스펙터 오프셋을 적용한 벡터를 쓴다. 어느 쪽이든 handleMesh의
/// "부모" 로컬 공간으로 변환한 뒤 AngleAxis에 넘긴다 (localRotation 곱셈이 부모 로컬 공간 기준이라서).
/// </summary>
public class HandleController : MonoBehaviour
{
    [Header("참조")]
    [SerializeField] private RailMover railMover;
    [SerializeField] private Transform leftHandTransform;  // 실제 왼쪽 컨트롤러(Interactor) 트랜스폼
    [SerializeField] private Transform rightHandTransform; // 실제 오른쪽 컨트롤러(Interactor) 트랜스폼
    [SerializeField] private Transform handleMesh; // 실제로 회전할 핸들 모델 (비워두면 이 오브젝트)

    [Header("조작 감도 (손 움직임 → 조향값)")]
    [SerializeField] private float tiltSensitivity = 4f; // 손 높이차 1m당 조향 배율
    [SerializeField] private float deadZone = 0.02f;      // 이 미만의 높이차는 무시 (손떨림 방지)

    [Header("핸들 시각 회전 (조향값 → 회전각)")]
    [Tooltip("회전축을 어떻게 구할지.\n" +
             "ReferenceTransform: axisReferenceTransform(예: 스티어링 칼럼/Cylinder)의 로컬 축을 그대로 회전축으로 사용. " +
             "모델을 다시 임포트해도 축이 안 틀어져서 더 안정적.\n" +
             "OffsetFromAxis: offsetBaseAxis를 axisOffsetEuler만큼 돌린 벡터를 회전축으로 사용. " +
             "참조 오브젝트가 없을 때, 인스펙터에서 오프셋을 눈으로 맞춰가며 쓰는 방식.")]
    [SerializeField] private RotationAxisMode axisMode = RotationAxisMode.ReferenceTransform;
    [Tooltip("ReferenceTransform 모드에서 회전축의 기준이 되는 트랜스폼 (예: 스티어링 칼럼에 해당하는 Cylinder). " +
             "비워두면 handleMesh 자신의 로컬 축을 사용.")]
    [SerializeField] private Transform axisReferenceTransform;
    [Tooltip("axisReferenceTransform의 로컬 축 중 어느 것이 칼럼이 뻗은(=회전축) 방향인지.")]
    [SerializeField] private LocalAxis axisReferenceLocalAxis = LocalAxis.Up;
    [Tooltip("OffsetFromAxis 모드에서 보정 전 기준 축.")]
    [SerializeField] private LocalAxis offsetBaseAxis = LocalAxis.Up;
    [Tooltip("OffsetFromAxis 모드에서 offsetBaseAxis에 적용할 보정 오일러 각(도). " +
             "도넛이 자기 면 위에서 딱 맞게 돌 때까지 값을 조금씩 조정.")]
    [SerializeField] private Vector3 axisOffsetEuler = Vector3.zero;
    [Tooltip("조향값(-1~1)이 최대일 때 rest pose 기준으로 회전할 각도(도).")]
    [SerializeField] private float rotationSensitivity = -45f;

    private enum RotationAxisMode { ReferenceTransform, OffsetFromAxis }
    private enum LocalAxis { Right, Up, Forward }

    [Header("스무딩")]
    [SerializeField] private float steeringSmoothing = 8f; // 조향 반응 속도

    [Header("카트/플레이어 이동 반영 (조향값 → RailMover 입력)")]
    [Tooltip("체크하면 핸들 조향값의 부호를 뒤집어서 RailMover로 전달 (이동 방향이 핸들과 반대로 보일 때 사용).")]
    [SerializeField] private bool invertMovementDirection = false;
    [Tooltip("조향값에 곱해서 RailMover.SetLateralInput으로 보내는 배율. " +
             "1보다 크면 같은 손 움직임으로도 RailMover가 더 빠른 좌우 이동 속도로 받아들임.")]
    [SerializeField] private float movementIntensity = 1f;

    [Header("키보드 테스트 입력 (에디터 전용, VR 헤드셋 없이 조향 검증용)")]
    [Tooltip("J/L 키로 조향을 테스트할지 여부. 에디터 빌드에서만 컴파일되므로 실제 빌드(VR)에는 영향 없음.")]
    [SerializeField] private bool enableKeyboardTestInput = true;
    [Tooltip("J 또는 L을 누르고 있을 때 초당 조향값이 증가하는 속도 (얼마나 빨리 -1/1에 도달하는지).")]
    [SerializeField] private float keyboardSteeringSpeed = 2f;
    [Tooltip("J/L에서 손을 뗐을 때 초당 조향값이 0으로 복귀하는 속도.")]
    [SerializeField] private float keyboardReturnSpeed = 4f;

    /// <summary>-1(왼쪽) ~ 1(오른쪽), 스무딩 적용된 현재 조향값 (반전/강도 배율 적용 전 원본).</summary>
    public float SteeringValue { get; private set; }

    private float _currentSteering;
    private float _keyboardSteering; // J/L로 만든 가상의 손 움직임 값 (targetSteering 계산에만 쓰임)
    private Quaternion _initialHandleRotation; // handleMesh의 rest pose (Start 시점 로컬 회전), 절대 덮어쓰지 않음

    private void Start()
    {
        if (handleMesh == null) handleMesh = transform;
        _initialHandleRotation = handleMesh.localRotation;
    }

    private void Update()
    {
        float targetSteering = 0f;
        bool keyboardActive = false;

#if UNITY_EDITOR
        if (enableKeyboardTestInput)
        {
            float keyboardTarget = 0f;
            if (Input.GetKey(KeyCode.J)) keyboardTarget = -1f;
            else if (Input.GetKey(KeyCode.L)) keyboardTarget = 1f;

            float keyboardRate = keyboardTarget != 0f ? keyboardSteeringSpeed : keyboardReturnSpeed;
            _keyboardSteering = Mathf.MoveTowards(_keyboardSteering, keyboardTarget, Time.deltaTime * keyboardRate);

            // 키를 뗀 뒤 중앙으로 복귀하는 도중에도(아직 0이 아니면) 손 추적 대신 키보드 값을 계속 사용
            keyboardActive = keyboardTarget != 0f || !Mathf.Approximately(_keyboardSteering, 0f);
        }
#endif

        if (keyboardActive)
        {
            targetSteering = _keyboardSteering;
        }
        else if (leftHandTransform != null && rightHandTransform != null)
        {
            float heightDiff = leftHandTransform.position.y - rightHandTransform.position.y;
            if (Mathf.Abs(heightDiff) < deadZone) heightDiff = 0f;

            targetSteering = Mathf.Clamp(heightDiff * tiltSensitivity, -1f, 1f);
        }

        _currentSteering = Mathf.MoveTowards(_currentSteering, targetSteering, Time.deltaTime * steeringSmoothing);
        SteeringValue = _currentSteering;

        handleMesh.localRotation = _initialHandleRotation * Quaternion.AngleAxis(_currentSteering * rotationSensitivity, ComputeRotationAxis());

        float movementValue = _currentSteering * movementIntensity;
        if (invertMovementDirection) movementValue = -movementValue;
        railMover?.SetLateralInput(movementValue);
    }

    private static Vector3 ToVector(LocalAxis axis)
    {
        switch (axis)
        {
            case LocalAxis.Right: return Vector3.right;
            case LocalAxis.Forward: return Vector3.forward;
            default: return Vector3.up;
        }
    }

    /// <summary>
    /// handleMesh.localRotation = _initialHandleRotation * Quaternion.AngleAxis(angle, 이 축) 에 쓰일,
    /// handleMesh의 부모 로컬 공간 기준 회전축을 계산. (Quaternion 곱셈이 부모 로컬 공간에서 이뤄지므로
    /// ReferenceTransform 모드에서 구한 월드 축은 반드시 부모 로컬 공간으로 변환해야 함.)
    /// </summary>
    private Vector3 ComputeRotationAxis()
    {
        if (axisMode == RotationAxisMode.ReferenceTransform)
        {
            Transform reference = axisReferenceTransform != null ? axisReferenceTransform : handleMesh;
            Vector3 worldAxis = reference.TransformDirection(ToVector(axisReferenceLocalAxis));
            Vector3 localAxis = handleMesh.parent != null
                ? handleMesh.parent.InverseTransformDirection(worldAxis)
                : worldAxis;
            return localAxis.sqrMagnitude > 0.0001f ? localAxis.normalized : Vector3.up;
        }

        return (Quaternion.Euler(axisOffsetEuler) * ToVector(offsetBaseAxis)).normalized;
    }
}
