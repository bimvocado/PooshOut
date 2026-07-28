using UnityEngine;

/// <summary>
/// 플레이어 총 발사 로직.
/// aimInputSource에 꽂힌 컴포넌트(MouseShootInput 또는 VRShootInput 등, IShootInput 구현체)로부터
/// 조준 방향과 발사 입력을 받아 총알을 실제로 스폰/발사시킨다.
///
/// 나중에 VR로 전환할 때는 이 스크립트를 건드릴 필요 없이,
/// Inspector에서 aimInputSource를 MouseShootInput → VRShootInput 컴포넌트로 바꿔 끼우기만 하면 된다.
/// </summary>
public class PlayerShooter : MonoBehaviour
{
    [Tooltip("IShootInput을 구현한 컴포넌트 (MouseShootInput 또는 VRShootInput)")]
    [SerializeField] private MonoBehaviour aimInputSource;

    [SerializeField] private Transform muzzlePoint;
    [SerializeField] private GameObject bulletPrefab;
    [SerializeField] private float bulletSpeed = 20f;
    [SerializeField] private AudioClip fireSfx;

    private IShootInput _input;

    private void Awake()
    {
        _input = aimInputSource as IShootInput;
        if (_input == null)
        {
            Debug.LogError("[PlayerShooter] aimInputSource가 IShootInput을 구현하지 않았습니다. " +
                            "MouseShootInput 또는 VRShootInput 컴포넌트를 연결해주세요.");
        }
    }

    private void Update()
    {
        if (_input == null || bulletPrefab == null || muzzlePoint == null) return;

        if (_input.FirePressedThisFrame() && _input.TryGetAim(out _, out Vector3 direction))
        {
            Fire(direction);
        }
    }

    private void Fire(Vector3 direction)
    {
        GameObject bulletObj = Instantiate(bulletPrefab, muzzlePoint.position, Quaternion.LookRotation(direction));
        Bullet bullet = bulletObj.GetComponent<Bullet>();
        if (bullet != null)
        {
            bullet.Launch(direction, bulletSpeed);
        }

        if (fireSfx != null) AudioManager.Instance?.PlaySfx(fireSfx);
    }
}
