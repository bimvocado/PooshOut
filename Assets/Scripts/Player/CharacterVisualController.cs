using UnityEngine;

/// <summary>
/// 정화도에 따라 캐릭터 외형을 바꿈 (똥 → 점점 깨끗 → 투명 물방울).
/// PurificationSystem 의 이벤트를 구독해서 정화도가 바뀔 때 반응.
///
/// 지금은 최종 3D 모델이 없으니 "색 변화"로 먼저 구현.
/// (더러움 = 탁한 갈색 → 깨끗함 = 맑은 청록). 모델 나오면 여기만 교체하면 됨.
/// </summary>
public class CharacterVisualController : MonoBehaviour
{
    [SerializeField] private Renderer targetRenderer;  // 캐릭터 몸체 렌더러
    [SerializeField] private Color dirtyColor = new Color(0.4f, 0.26f, 0.13f); // 탁한 갈색
    [SerializeField] private Color cleanColor = new Color(0.3f, 0.8f, 0.9f);   // 맑은 청록

    private void OnEnable()
    {
        if (PurificationSystem.Instance != null)
        {
            PurificationSystem.Instance.OnPurityChanged += UpdateVisual;
            UpdateVisual(PurificationSystem.Instance.Purity);
        }
    }

    private void OnDisable()
    {
        if (PurificationSystem.Instance != null)
        {
            PurificationSystem.Instance.OnPurityChanged -= UpdateVisual;
        }
    }

    private void UpdateVisual(float purity)
    {
        if (targetRenderer == null) return;

        // 정화도 0~100 을 0~1 로 바꿔서 두 색 사이를 보간
        float t = purity / 100f;
        Color current = Color.Lerp(dirtyColor, cleanColor, t);

        // 머티리얼 색 변경 (material 은 인스턴스 복제본이라 원본 안 건드림)
        targetRenderer.material.color = current;
    }
}
