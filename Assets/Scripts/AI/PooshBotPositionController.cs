using UnityEngine;

// 정화봇의 위치/노출 상태를 하나로 통합 관리.
// PooshBotDirector가 상황(인트로/스테이지/엔딩)에 맞게 SetMode를 호출해서 전환한다.
// PooshBotViewFollower.cs를 대체함 - 그건 삭제해도 됨.
public class PooshBotPositionController : MonoBehaviour
{
    public enum Mode
    {
        Hidden,          // 완전히 안 보임 (엔딩1, 최종 퇴장 후)
        Front,           // 플레이어 정면에 고정 (인트로 인사, 엔딩2 등장/피드백/작별)
        TopRightFollow,  // 시야 오른쪽 위를 계속 따라다님 (Stage1,2,4)
        FixedPoint,      // 지정된 위치에 고정 배치, 안 따라다님 (Stage3)
    }

    [Tooltip("플레이어 시야 기준점. XR Origin의 Main Camera(또는 CenterEyeAnchor)를 연결.")]
    [SerializeField] private Transform xrCamera;

    [Header("정면 등장 (Front)")]
    [SerializeField] private float frontDistance = 1.5f;
    [SerializeField] private float frontHeightOffset = -0.2f;

    [Header("오른쪽 위 따라다니기 (TopRightFollow)")]
    [SerializeField] private float topRightRight = 0.5f;
    [SerializeField] private float topRightUp = 0.3f;
    [SerializeField] private float topRightForward = 1.2f;

    [Header("공통 (VR 멀미 방지 - 값 너무 키우지 말 것)")]
    [Tooltip("정면(Front) 등장 시 위치를 따라잡는 속도. TopRightFollow는 카트처럼 빠른 이동에서도 " +
             "절대 안 뒤처지도록 아예 순간이동으로 처리하므로 이 값의 영향을 안 받음.")]
    [SerializeField] private float positionLerpSpeed = 10f;
    [Tooltip("고개 돌림에 맞춰 정화봇이 플레이어를 바라보는 회전 속도. 위치보다 느리게 유지해야 시선 돌릴 때 안 어지럽다.")]
    [SerializeField] private float rotationLerpSpeed = 3f;
    [SerializeField] private bool lookAtPlayer = true;

    private Mode _mode = Mode.Hidden;

    public Mode CurrentMode => _mode;

    // fixedPoint는 Mode.FixedPoint일 때만 사용. 그 외 모드에서는 null로 둬도 됨.
    public void SetMode(Mode mode, Transform fixedPoint = null)
    {
        _mode = mode;
        SetVisible(mode != Mode.Hidden);

        // 고정 위치는 순간이동으로 배치한다. Stage3 진입할 때마다 걸어오는 모습은 어색하니까.
        if (mode == Mode.FixedPoint && fixedPoint != null)
        {
            transform.position = fixedPoint.position;
            transform.rotation = fixedPoint.rotation;
        }
    }

    private void SetVisible(bool visible)
    {
        foreach (Renderer r in GetComponentsInChildren<Renderer>())
        {
            r.enabled = visible;
        }
    }

    [Header("모델 방향 보정")]
    [Tooltip("체크하면 회전 계산 시 180도를 더해줌. 정화봇 모델의 정면(파란 Z축)이 얼굴 반대쪽으로 만들어져 있어서 " +
             "플레이어를 보게 돌릴 때 뒤통수가 보이는 경우 체크. 에디터에서는 안 보이고 Play 중에만 문제가 나타남.")]
    [SerializeField] private bool flipForward = true;

    private void LateUpdate()
    {
        if (xrCamera == null) return;
        if (_mode == Mode.Hidden || _mode == Mode.FixedPoint) return;

        Vector3 targetPos = _mode == Mode.Front
            ? xrCamera.position + xrCamera.forward * frontDistance + Vector3.up * frontHeightOffset
            : xrCamera.position + xrCamera.forward * topRightForward + xrCamera.right * topRightRight + xrCamera.up * topRightUp;

        if (_mode == Mode.TopRightFollow)
        {
            // 카트/레일처럼 빠르게 움직이는 스테이지에서는 Lerp로는 항상 일정 거리만큼 뒤처지는
            // 구조적 한계가 있어서, 위치는 아예 순간이동으로 매 프레임 딱 맞춰버린다.
            // (위치가 카메라를 순간적으로 따라가는 건 화면 흔들림이 없어 멀미 유발 안 함 - 문제되는 건 회전.)
            transform.position = targetPos;
        }
        else
        {
            transform.position = Vector3.Lerp(transform.position, targetPos, Time.unscaledDeltaTime * positionLerpSpeed);
        }

        if (lookAtPlayer)
        {
            Vector3 lookDir = transform.position - xrCamera.position;
            if (lookDir.sqrMagnitude > 0.0001f)
            {
                Quaternion targetRot = Quaternion.LookRotation(lookDir);
                if (flipForward) targetRot *= Quaternion.Euler(0f, 180f, 0f);
                transform.rotation = Quaternion.Slerp(transform.rotation, targetRot, Time.unscaledDeltaTime * rotationLerpSpeed);
            }
        }
    }
}