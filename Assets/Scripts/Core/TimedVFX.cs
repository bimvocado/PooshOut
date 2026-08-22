using UnityEngine;

/// <summary>
/// 일정 시간(lifetime) 후 자기 자신을 자동으로 파괴하는 1회성 이펙트용 컴포넌트.
/// 속도선/스파크처럼 재생 후 사라져야 하는 파티클 프리팹 루트에 붙여서 사용.
/// </summary>
public class TimedVFX : MonoBehaviour
{
    [SerializeField] private float lifetime = 1.5f;

    /// <summary>에디터 프리팹 생성 스크립트 등에서 저장 전에 lifetime을 지정할 때 사용.</summary>
    public void SetLifetime(float value) => lifetime = value;

    private void OnEnable()
    {
        Destroy(gameObject, lifetime);
    }
}
