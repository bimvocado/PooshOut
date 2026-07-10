using UnityEngine;

/// <summary>
/// 스테이지 3 진행 중 정화도에 따라 캐릭터가 점점 투명한 물방울로 변하는 연출.
/// 렌더러의 알파값과 (선택) 파티클 효과로 표현.
/// </summary>
public class TransformVisual : MonoBehaviour
{
    [SerializeField] private Renderer targetRenderer;
    [SerializeField] private ParticleSystem transformParticles;
    [SerializeField] private float transparencyStartThreshold = 60f; // 이 정화도부터 투명해지기 시작

    private void OnEnable()
    {
        if (PurificationSystem.Instance != null)
        {
            PurificationSystem.Instance.OnPurityChanged += HandlePurityChanged;
            HandlePurityChanged(PurificationSystem.Instance.Purity);
        }
    }

    private void OnDisable()
    {
        if (PurificationSystem.Instance != null)
        {
            PurificationSystem.Instance.OnPurityChanged -= HandlePurityChanged;
        }
    }

    private void HandlePurityChanged(float purity)
    {
        if (purity < transparencyStartThreshold)
        {
            SetAlpha(1f);
            return;
        }

        float t = Mathf.InverseLerp(transparencyStartThreshold, 100f, purity);
        SetAlpha(Mathf.Lerp(1f, 0.25f, t));

        if (t > 0.01f && transformParticles != null && !transformParticles.isPlaying)
        {
            transformParticles.Play();
        }
    }

    private void SetAlpha(float alpha)
    {
        if (targetRenderer == null) return;
        Color color = targetRenderer.material.color;
        color.a = alpha;
        targetRenderer.material.color = color;
    }
}
