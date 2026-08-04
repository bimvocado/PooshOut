using UnityEngine;

public class PlayerShooter : MonoBehaviour {
    [Tooltip("IShootInput을 구현한 컴포넌트 (MouseShootInput 또는 VRShootInput)")]
    [SerializeField] private MonoBehaviour aimInputSource;

    [SerializeField] private Transform muzzlePoint;
    [SerializeField] private GameObject bulletPrefab;
    [SerializeField] private float bulletSpeed = 20f;
    [SerializeField] private AudioClip fireSfx;

    // ✨ [추가] 총구 이펙트 파티클 변수
    [SerializeField] private ParticleSystem muzzleFlashParticle;

    private IShootInput _input;

    private void Awake() {
        _input = aimInputSource as IShootInput;
        if (_input == null) {
            Debug.LogError("[PlayerShooter] aimInputSource가 IShootInput을 구현하지 않았습니다.");
        }
    }

    private void Update() {
        if (_input == null || bulletPrefab == null || muzzlePoint == null) return;

        if (_input.FirePressedThisFrame()) {
            // 총구 정면 방향으로 발사
            Fire(muzzlePoint.forward);
        }
    }

    private void Fire(Vector3 direction) {
        // 1. 총알 스폰
        GameObject bulletObj = Instantiate(bulletPrefab, muzzlePoint.position, Quaternion.LookRotation(direction));
        Bullet bullet = bulletObj.GetComponent<Bullet>();
        if (bullet != null) {
            bullet.Launch(direction, bulletSpeed);
        }

        // 2. 효과음 재생
        // if (fireSfx != null) AudioManager.Instance?.PlaySfx(fireSfx);

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