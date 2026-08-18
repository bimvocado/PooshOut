using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.XR.Interaction.Toolkit.Interactables;

/// <summary>
/// 오토바이식 조향 핸들. 카트에 고정된 핸들을 양손으로 실제로 잡아야만 조향이 동작한다.
/// 왼쪽/오른쪽 그립 포인트에 XRSimpleInteractable을 하나씩 붙여 "잡음" 여부를 판정하고,
/// 잡고 있는 동안 두 손의 높이 차이(기울임)를 핸들 메쉬의 롤 회전으로 시각화한 뒤
/// RailMover의 좌우 입력으로 전달한다. 손을 놓으면 핸들이 서서히 중앙으로 복귀한다.
/// </summary>
public class HandleController : MonoBehaviour
{
    [Header("참조")]
    [SerializeField] private RailMover railMover;
    [SerializeField] private XRSimpleInteractable leftGripPoint;
    [SerializeField] private XRSimpleInteractable rightGripPoint;
    [SerializeField] private Transform handleMesh; // 실제로 회전할 핸들 모델 (비워두면 이 오브젝트)

    [Header("조작 감도")]
    [SerializeField] private float tiltSensitivity = 4f; // 손 높이차 1m당 조향 배율
    [SerializeField] private float deadZone = 0.02f;      // 이 미만의 높이차는 무시 (손떨림 방지)
    [SerializeField] private float maxTiltAngle = 45f;    // 핸들 메쉬가 실제로 회전하는 최대 각도

    [Header("스무딩")]
    [SerializeField] private float steeringSmoothing = 8f;   // 잡고 있을 때 조향 반응 속도
    [SerializeField] private float releaseReturnSpeed = 6f;  // 손을 놓았을 때 중앙 복귀 속도

    /// <summary>양손 모두 그립 포인트를 잡고 있는지.</summary>
    public bool IsGripped => _leftGripped && _rightGripped;
    /// <summary>-1(왼쪽) ~ 1(오른쪽), 스무딩 적용된 현재 조향값.</summary>
    public float SteeringValue { get; private set; }

    private bool _leftGripped;
    private bool _rightGripped;
    private float _currentSteering;
    private Quaternion _baseLocalRotation; // handleMesh에 미리 세팅된 보정 회전(예: Y 180도)을 보존하기 위한 기준값

    private void Awake()
    {
        if (handleMesh == null) handleMesh = transform;
        _baseLocalRotation = handleMesh.localRotation;
    }

    private void OnEnable()
    {
        if (leftGripPoint != null)
        {
            leftGripPoint.selectEntered.AddListener(OnLeftGripEntered);
            leftGripPoint.selectExited.AddListener(OnLeftGripExited);
        }
        if (rightGripPoint != null)
        {
            rightGripPoint.selectEntered.AddListener(OnRightGripEntered);
            rightGripPoint.selectExited.AddListener(OnRightGripExited);
        }
    }

    private void OnDisable()
    {
        if (leftGripPoint != null)
        {
            leftGripPoint.selectEntered.RemoveListener(OnLeftGripEntered);
            leftGripPoint.selectExited.RemoveListener(OnLeftGripExited);
        }
        if (rightGripPoint != null)
        {
            rightGripPoint.selectEntered.RemoveListener(OnRightGripEntered);
            rightGripPoint.selectExited.RemoveListener(OnRightGripExited);
        }

        _leftGripped = false;
        _rightGripped = false;
    }

    // TODO(디버그용, 확인 끝나면 제거): 그립 이벤트 자체가 오는지 확인하기 위한 임시 로그.
    private void OnLeftGripEntered(SelectEnterEventArgs args)
    {
        _leftGripped = true;
        Debug.Log($"[HandleController] LEFT grip ENTER by {args.interactorObject.transform.name}", this);
    }

    private void OnLeftGripExited(SelectExitEventArgs args)
    {
        _leftGripped = false;
        Debug.Log($"[HandleController] LEFT grip EXIT by {args.interactorObject.transform.name}", this);
    }

    private void OnRightGripEntered(SelectEnterEventArgs args)
    {
        _rightGripped = true;
        Debug.Log($"[HandleController] RIGHT grip ENTER by {args.interactorObject.transform.name}", this);
    }

    private void OnRightGripExited(SelectExitEventArgs args)
    {
        _rightGripped = false;
        Debug.Log($"[HandleController] RIGHT grip EXIT by {args.interactorObject.transform.name}", this);
    }

    private void Update()
    {
        float targetSteering = 0f;

        if (IsGripped)
        {
            Vector3 leftHandPos = leftGripPoint.firstInteractorSelecting.transform.position;
            Vector3 rightHandPos = rightGripPoint.firstInteractorSelecting.transform.position;

            float heightDiff = leftHandPos.y - rightHandPos.y;
            if (Mathf.Abs(heightDiff) < deadZone) heightDiff = 0f;

            targetSteering = Mathf.Clamp(heightDiff * tiltSensitivity, -1f, 1f);
        }

        float smoothing = IsGripped ? steeringSmoothing : releaseReturnSpeed;
        _currentSteering = Mathf.MoveTowards(_currentSteering, targetSteering, Time.deltaTime * smoothing);
        SteeringValue = _currentSteering;

        handleMesh.localRotation = _baseLocalRotation * Quaternion.Euler(0f, 0f, -_currentSteering * maxTiltAngle);

        railMover?.SetLateralInput(_currentSteering);
    }
}
