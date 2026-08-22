using UnityEngine;

[RequireComponent(typeof(Rigidbody))]
public class WaterDrop : MonoBehaviour {
    [Header("튀어나오는 힘")]
    [SerializeField] private float forwardForce = 2f;
    [SerializeField] private float upwardForce = 1.5f;

    [Header("낙하")]
    [SerializeField] private float gravity = 3f;
    [SerializeField] private float maxFallSpeed = 2f;

    private Rigidbody rb;

    private void Awake() {
        rb = GetComponent<Rigidbody>();

        // Unity 기본 중력은 끄고 직접 제어
        rb.useGravity = false;
    }

    private void Start() {
        // 처음 생성될 때 앞 + 위 방향으로 튀어나옴
        Vector3 launchDirection =
            transform.forward * forwardForce +
            Vector3.up * upwardForce;

        rb.AddForce(
            launchDirection,
            ForceMode.Impulse
        );
    }

    private void FixedUpdate() {
        // 약한 중력 직접 적용
        rb.AddForce(
            Vector3.down * gravity,
            ForceMode.Acceleration
        );

        // 너무 빠르게 떨어지는 것 방지
        if (rb.linearVelocity.y < -maxFallSpeed) {
            Vector3 velocity = rb.linearVelocity;
            velocity.y = -maxFallSpeed;

            rb.linearVelocity = velocity;
        }
    }
}