using UnityEngine;

/// <summary>
/// 실제로 날아가는 총알.
/// Rigidbody 속도로 직진하며, 미생물(MicrobeTarget)에 트리거로 닿으면 Hit()을 호출하고 사라진다.
/// 아무것도 안 맞고 lifeTime이 지나면 자동으로 사라진다.
///
/// 세팅: Collider Is Trigger 체크, Rigidbody Use Gravity 체크 해제 (직선으로 날아가야 하므로).
/// </summary>
[RequireComponent(typeof(Rigidbody))]
[RequireComponent(typeof(Collider))]
public class Bullet : MonoBehaviour {
    [SerializeField] private float lifeTime = 3f;

    [Tooltip("모델이 기본 정면(Z+)과 다르게 눕혀져/돌려져 있다면 그 차이만큼 오일러각으로 입력. " +
             "예: 프리팹을 X축으로 90도 눕혀놨다면 (90, 0, 0)")]
    [SerializeField] private Vector3 modelRotationOffset = Vector3.zero;

    private Rigidbody _rb;

    private void Awake() {
        _rb = GetComponent<Rigidbody>();
        _rb.useGravity = false;
        _rb.isKinematic = false;
        _rb.collisionDetectionMode = CollisionDetectionMode.ContinuousDynamic; // 빠른 속도라 관통 방지용
    }

    private void Start() {
        Destroy(gameObject, lifeTime);
    }

    public void Launch(Vector3 direction, float speed) {
        Vector3 dir = direction.normalized;
        transform.rotation = Quaternion.LookRotation(dir) * Quaternion.Euler(modelRotationOffset);
        // Unity 버전에 따라 프로퍼티명이 다름: 최신(Unity 6+)은 rb.linearVelocity, 그 이전은 rb.velocity
        _rb.linearVelocity = dir * speed;
    }

    private void OnTriggerEnter(Collider other) {
        // 임시 디버그: 뭘 맞았는지, MicrobeTarget을 찾았는지 확인용 (원인 찾은 뒤 지워도 됨)
        Debug.Log($"[Bullet] 충돌 대상: {other.gameObject.name}");

        // 콜라이더가 루트가 아니라 자식 오브젝트(Visual 등)에 있을 수도 있으니 부모까지 찾는다
        MicrobeTarget target = other.GetComponentInParent<MicrobeTarget>();
        Debug.Log($"[Bullet] MicrobeTarget 찾음? {target != null}");
        if (target != null) {
            target.Hit();
        }

        Destroy(gameObject);
    }
}