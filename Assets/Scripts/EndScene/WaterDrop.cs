using UnityEngine;

[RequireComponent(typeof(Rigidbody))]
public class WaterDrop : MonoBehaviour {
    [Header("튀어나오는 힘")]
    [SerializeField] private float forwardForce = 2f;
    [SerializeField] private float upwardForce = 1.5f;

    [Header("낙하")]
    [SerializeField] private float gravity = 3f;
    [SerializeField] private float maxFallSpeed = 2f;

    [Header("첨벙 효과")]
    [SerializeField] private ParticleSystem splashParticle;
    [SerializeField] private AudioSource sfxAudioSource;
    [SerializeField] private AudioClip splashSfx;

    [Header("씬 연출")]
    [SerializeField] private End1SceneManager sceneManager;

    private Rigidbody rb;
    private bool hasSplashed;

    private void Awake() {
        rb = GetComponent<Rigidbody>();
        rb.useGravity = false;

        if (sfxAudioSource != null) {
            sfxAudioSource.playOnAwake = false;
            sfxAudioSource.loop = false;
            sfxAudioSource.spatialBlend = 0f;
        }
    }

    private void Start() {
        Vector3 launchDirection =
            transform.forward * forwardForce +
            Vector3.up * upwardForce;

        rb.AddForce(
            launchDirection,
            ForceMode.Impulse
        );

        Destroy(gameObject, 8f);
    }

    private void FixedUpdate() {
        rb.AddForce(
            Vector3.down * gravity,
            ForceMode.Acceleration
        );

        if (rb.linearVelocity.y < -maxFallSpeed) {
            Vector3 velocity = rb.linearVelocity;
            velocity.y = -maxFallSpeed;

            rb.linearVelocity = velocity;
        }
    }

    private void OnTriggerEnter(Collider other) {
        if (hasSplashed)
            return;

        if (!other.CompareTag("Floor"))
            return;

        hasSplashed = true;

        // 첨벙 파티클
        if (splashParticle != null) {
            splashParticle.Play();
        }

        // 첨벙 효과음
        if (sfxAudioSource != null && splashSfx != null) {
            sfxAudioSource.PlayOneShot(splashSfx);
        }

        // End1SceneManager에게 첨벙 알림
        if (sceneManager != null) {
            sceneManager.OnWaterSplash();
        }
    }
}