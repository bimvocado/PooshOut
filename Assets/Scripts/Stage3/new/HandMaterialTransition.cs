using UnityEngine;

/// <summary>
/// Stage3 진행도(Stage3Manager.NormalizedProgress)에 따라 손 머티리얼을
/// 원본 머티리얼에서 waterhand 머티리얼로 점진적으로 보간한다.
///
/// 원본은 Opaque, waterhand는 Transparent라서 Surface Type이 서로 다르다.
/// Material.Lerp를 그냥 쓰면 _SrcBlend/_DstBlend/_ZWrite 같은 블렌드 상태 값까지
/// 숫자로 보간되어 중간에 존재하지 않는 블렌드 모드가 되어버리고, 그 결과
/// 색만 바뀌고 투명해지는 느낌은 안 나는 문제가 있었다.
/// 그래서 블렌드 상태(Surface/Blend/ZWrite/큐/키워드)는 처음부터 waterhand 쪽
/// (Transparent)으로 고정해두고, 실제로 부드럽게 바뀌어야 하는
/// 색상(알파 포함)/발광색/금속성/광택만 직접 보간한다.
/// </summary>
public class HandMaterialTransition : MonoBehaviour
{
    [SerializeField] private SkinnedMeshRenderer handRenderer;
    [SerializeField] private Material waterHandMaterial;
    [SerializeField] private Stage3Manager stage3Manager;

    private Material _blendMaterial;

    private Color _originalBaseColor;
    private Color _originalEmission;
    private float _originalMetallic;
    private float _originalSmoothness;

    private Color _targetBaseColor;
    private Color _targetEmission;
    private float _targetMetallic;
    private float _targetSmoothness;

    private void Awake()
    {
        if (handRenderer == null || waterHandMaterial == null)
        {
            Debug.LogWarning($"{nameof(HandMaterialTransition)}: handRenderer 또는 waterHandMaterial이 연결되지 않았습니다.");
            return;
        }

        Material original = handRenderer.sharedMaterial;

        _originalBaseColor = GetColorOrDefault(original, "_BaseColor", Color.white);
        _originalEmission = GetColorOrDefault(original, "_EmissionColor", Color.black);
        _originalMetallic = GetFloatOrDefault(original, "_Metallic", 0f);
        _originalSmoothness = GetFloatOrDefault(original, "_Smoothness", 0.5f);

        _targetBaseColor = GetColorOrDefault(waterHandMaterial, "_BaseColor", _originalBaseColor);
        _targetEmission = GetColorOrDefault(waterHandMaterial, "_EmissionColor", _originalEmission);
        _targetMetallic = GetFloatOrDefault(waterHandMaterial, "_Metallic", _originalMetallic);
        _targetSmoothness = GetFloatOrDefault(waterHandMaterial, "_Smoothness", _originalSmoothness);

        // 블렌드 상태(Surface/Blend/ZWrite/큐/키워드)는 waterhand 쪽으로 고정한 채 시작.
        // 시작 시점엔 알파를 원본 값으로 맞춰두므로 육안으로는 그대로 불투명해 보인다.
        _blendMaterial = new Material(waterHandMaterial);
        handRenderer.material = _blendMaterial;

        ApplyBlend(0f);
    }

    private void Update()
    {
        if (_blendMaterial == null || stage3Manager == null)
            return;

        float t = Mathf.Clamp01(stage3Manager.NormalizedProgress);
        ApplyBlend(t);
    }

    private void ApplyBlend(float t)
    {
        _blendMaterial.SetColor("_BaseColor", Color.Lerp(_originalBaseColor, _targetBaseColor, t));
        _blendMaterial.SetColor("_EmissionColor", Color.Lerp(_originalEmission, _targetEmission, t));
        _blendMaterial.SetFloat("_Metallic", Mathf.Lerp(_originalMetallic, _targetMetallic, t));
        _blendMaterial.SetFloat("_Smoothness", Mathf.Lerp(_originalSmoothness, _targetSmoothness, t));
    }

    private static Color GetColorOrDefault(Material mat, string property, Color fallback) =>
        mat != null && mat.HasProperty(property) ? mat.GetColor(property) : fallback;

    private static float GetFloatOrDefault(Material mat, string property, float fallback) =>
        mat != null && mat.HasProperty(property) ? mat.GetFloat(property) : fallback;
}
