using UnityEngine;
using UnityEngine.UI;

public class StageGaugeUI : MonoBehaviour {
    [SerializeField] private Image gaugeFillImage;

    public void SetFillRate(float fillRate) {
        if (gaugeFillImage == null)
            return;

        gaugeFillImage.fillAmount = Mathf.Clamp01(fillRate);
    }

    public void SetValue(float currentValue, float maxValue) {
        if (maxValue <= 0f)
            return;

        SetFillRate(currentValue / maxValue);
    }
}