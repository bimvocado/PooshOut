using UnityEngine;

/// <summary>
/// 마우스 테스트용 조준 입력.
/// 카메라에서 마우스 스크린 좌표로 레이를 쏴서 조준 방향을 만들고,
/// 좌클릭을 발사 입력으로 사용한다.
/// </summary>
public class MouseShootInput : MonoBehaviour, IShootInput
{
    [SerializeField] private Camera aimCamera;

    private void Awake()
    {
        if (aimCamera == null) aimCamera = Camera.main;
    }

    public bool TryGetAim(out Vector3 origin, out Vector3 direction)
    {
        origin = Vector3.zero;
        direction = Vector3.forward;

        if (aimCamera == null) return false;

        Ray ray = aimCamera.ScreenPointToRay(Input.mousePosition);
        origin = ray.origin;
        direction = ray.direction;
        return true;
    }

    public bool FirePressedThisFrame() => Input.GetMouseButtonDown(0);
}
