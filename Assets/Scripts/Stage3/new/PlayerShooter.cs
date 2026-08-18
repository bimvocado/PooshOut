using UnityEngine;

public class PlayerShooter : MonoBehaviour {
    [Tooltip("IShootInput을 구현한 컴포넌트 (MouseShootInput 또는 VRShootInput)")]
    [SerializeField] private MonoBehaviour aimInputSource;

    [SerializeField] private Transform muzzlePoint;
    [SerializeField] private GameObject bulletPrefab;
    [SerializeField] private float bulletSpeed = 20f;

    [Header("Sound")]
    [SerializeField] private AudioSource audioSource;
    [SerializeField] private AudioClip fireSfx;

    [Header("Effect")]
    [SerializeField] private ParticleSystem muzzleFlashParticle;

    private IShootInput _input;

    private void Awake() {
        _input = aimInputSource as IShootInput;

        if (_input == null) {
            Debug.LogError("[PlayerShooter] aimInputSource가 IShootInput을 구현하지 않았습니다.");
        }
    }

    private void Update() {
        if (_input == null || bulletPrefab == null || muzzlePoint == null)
            return;

        if (_input.FirePressedThisFrame()) {
            Fire(muzzlePoint.forward);
        }
    }

    private void Fire(Vector3 direction) {
        // 1. 총알 생성
        GameObject bulletObj = Instantiate(
            bulletPrefab,
            muzzlePoint.position,
            Quaternion.LookRotation(direction)
        );

        Bullet bullet = bulletObj.GetComponent<Bullet>();

        if (bullet != null) {
            bullet.Launch(direction, bulletSpeed);
        }

        // 2. 총 발사음
        if (audioSource != null && fireSfx != null) {
            audioSource.PlayOneShot(fireSfx);
        }

        // 3. 총구 파티클
        if (muzzleFlashParticle != null) {
            muzzleFlashParticle.Play();
        }
    }

    private void OnDrawGizmosSelected() {
        if (muzzlePoint != null) {
            Gizmos.color = Color.red;
            Gizmos.DrawSphere(muzzlePoint.position, 0.05f);
            Gizmos.DrawRay(muzzlePoint.position, muzzlePoint.forward * 0.5f);
        }
    }
}