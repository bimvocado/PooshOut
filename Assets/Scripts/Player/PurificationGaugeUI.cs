using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 정화도 게이지 UI.
/// PurificationSystem 의 OnPurityChanged 이벤트를 구독해서,
/// 정화도가 바뀔 때만 게이지를 갱신함 (매 프레임 확인 안 함).
///
/// 씬에 Slider 하나 만들어서 inspector 의 gaugeSlider 에 연결하면 됨.
/// (VR/일반 상관없이 동작 — 세팅 전에도 테스트 가능)
/// </summary>
public class PurificationGaugeUI : MonoBehaviour
{
    [SerializeField] private Slider gaugeSlider;   // 0~1 범위 슬라이더
    [SerializeField] private Text purityText;      // "80%" 같은 텍스트 (선택)

    private void OnEnable()
    {
        // 이벤트 구독. PurificationSystem 이 아직 없을 수도 있으니 null 체크.
        if (PurificationSystem.Instance != null)
        {
            PurificationSystem.Instance.OnPurityChanged += UpdateGauge;
            UpdateGauge(PurificationSystem.Instance.Purity); // 시작값 반영
        }
    }

    private void OnDisable()
    {
        // 구독 해제 (안 하면 메모리 누수/에러 위험)
        if (PurificationSystem.Instance != null)
        {
            PurificationSystem.Instance.OnPurityChanged -= UpdateGauge;
        }
    }

    private void UpdateGauge(float purity)
    {
        if (gaugeSlider != null)
        {
            gaugeSlider.value = purity / 100f; // 0~100 → 0~1
        }
        if (purityText != null)
        {
            purityText.text = Mathf.RoundToInt(purity) + "%";
        }
    }
}
