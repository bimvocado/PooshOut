using UnityEngine;

/// <summary>
/// [디버그 전용] RailMover/웨이포인트 없이, 플레이어가 실제로 바라보는 방향(카메라 forward)
/// 기준으로 자동 전진하면서, 키보드(A/D, ←/→) 입력으로 좌우 조향까지 흉내내는 테스트용 스크립트.
///
/// XR Origin의 transform.forward는 몸통(리그) 방향이라 고개를 돌려도 안 바뀌기 때문에,
/// 꺾인 파이프처럼 곡선 경로를 지나가려면 "고개를 돌린 방향"을 기준으로 전진해야 합니다.
/// 그래서 이 스크립트는 카메라(HMD)의 forward를 기준으로 이동 방향을 계산합니다.
///
/// 사용법:
///  1. 이동시킬 오브젝트(예: XR Origin)에 이 스크립트를 추가
///  2. Head Transform에 Main Camera(HMD) 드래그 (비워두면 Camera.main 자동 사용)
///  3. Play 모드에서 고개를 돌리는(또는 마우스로 보는) 방향으로 자동 전진
///  4. A/D 또는 ←/→ 키로 좌우 조향 추가 테스트
/// </summary>
public class DebugRailInput : MonoBehaviour {
    [Header("시선 기준")]
    [Tooltip("이 방향을 기준으로 전진합니다. 비워두면 Camera.main을 자동으로 씁니다.")]
    [SerializeField] private Transform headTransform;

    [Header("전진 설정")]
    [SerializeField] private float forwardSpeed = 3f;
    [Tooltip("켜면 위아래(pitch)는 무시하고 수평 방향으로만 전진합니다. 파이프가 위아래로도 꺾이면 꺼주세요.")]
    [SerializeField] private bool flattenPitch = false;

    [Header("조향(좌우) 설정")]
    [SerializeField] private float lateralRange = 1.5f;      // 좌우로 벗어날 수 있는 최대 폭
    [SerializeField] private float inputSmoothing = 6f;      // 값이 목표치까지 부드럽게 도달하는 속도

    [SerializeField] private bool showDebugOverlay = true;

    private float _currentLateral;
    private Vector3 _railPosition; // 좌우 오프셋을 뺀, 순수하게 전진만 한 위치
    private Transform _head;

    private void Start() {
        _railPosition = transform.position;
        _head = headTransform != null ? headTransform : (Camera.main != null ? Camera.main.transform : null);
    }

    private void Update() {
        if (_head == null) return;

        Vector3 forwardDir = _head.forward;
        if (flattenPitch) {
            forwardDir.y = 0f;
        }
        forwardDir.Normalize();

        // 전진: 플레이어가 바라보는 방향으로 계속 이동
        _railPosition += forwardDir * (forwardSpeed * Time.deltaTime);

        // 조향: 키보드 입력을 시선 기준 좌우 오프셋으로 변환
        float targetValue = Input.GetAxisRaw("Horizontal"); // A/D, ←/→ : -1 ~ 1
        _currentLateral = Mathf.Lerp(_currentLateral, targetValue * lateralRange, Time.deltaTime * inputSmoothing);

        Vector3 lateralDir = Vector3.Cross(Vector3.up, forwardDir);
        transform.position = _railPosition + lateralDir * _currentLateral;
    }

    private void OnGUI() {
        if (!showDebugOverlay) return;

        GUI.Label(new Rect(10, 10, 400, 20), "[DebugRailInput] Head-direction forward mover");
        GUI.Label(new Rect(10, 30, 400, 20), $"LateralOffset: {_currentLateral:F3}");
        GUI.Label(new Rect(10, 50, 400, 20), $"Position: {transform.position:F2}");
        GUI.Label(new Rect(10, 70, 400, 20), _head != null ? $"Head: {_head.name}" : "Head transform not found (Camera.main null)");
        GUI.Label(new Rect(10, 90, 400, 20), "A/D or ←/→ to steer, moves toward where you look");
    }
}