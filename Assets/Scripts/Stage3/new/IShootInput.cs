using UnityEngine;

/// <summary>
/// 조준/발사 입력을 추상화한 인터페이스.
/// 마우스(테스트용)와 VR 컨트롤러 레이(실제 빌드용)를 동일한 방식으로 다루기 위해 분리했다.
/// PlayerShooter는 이 인터페이스만 알면 되고, 구현체(MouseShootInput / VRShootInput)만
/// 갈아끼우면 입력 방식이 바뀐다.
/// </summary>
public interface IShootInput
{
    /// <summary>현재 조준 원점(origin)과 방향(direction)을 가져온다. 실패 시 false.</summary>
    bool TryGetAim(out Vector3 origin, out Vector3 direction);

    /// <summary>이번 프레임에 발사 입력(마우스 클릭 / VR 트리거)이 들어왔는지.</summary>
    bool FirePressedThisFrame();
}
