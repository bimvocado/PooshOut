using UnityEngine;
using TMPro;

/// 캘리브레이션 진행 상황/결과를 화면에 텍스트로 표시 (테스트/디버그용).
/// 측정 완료 후 hideDelay초 뒤 자동으로 캔버스를 숨김.
public class CalibrationDebugDisplay : MonoBehaviour
{
    [SerializeField] private CalibrationController calibrationController;
    [SerializeField] private TextMeshProUGUI debugText;

    [Header("완료 후 자동 숨김")]
    [Tooltip("측정 완료 텍스트가 뜬 뒤 이 시간(초)이 지나면 자동으로 캔버스를 숨김")]
    [SerializeField] private float hideDelay = 2f;

    [Tooltip("숨길 대상. 비워두면 이 스크립트가 붙은 오브젝트 자신을 숨김")]
    [SerializeField] private GameObject targetToHide;

    private bool _hideScheduled;

    private void OnEnable()
    {
        if (calibrationController != null)
        {
            calibrationController.OnCalibrationDone += HandleCalibrationDone;
        }
        _hideScheduled = false;
    }

    private void OnDisable()
    {
        if (calibrationController != null)
        {
            calibrationController.OnCalibrationDone -= HandleCalibrationDone;
        }
        CancelInvoke(nameof(HideDisplay));
    }

    private void Update()
    {
        if (calibrationController == null || debugText == null) return;

        if (calibrationController.IsCalibrated)
        {
            debugText.text = $"준비 완료!\n(내부값: {calibrationController.CalibratedHeight:F2}m)";
            debugText.color = Color.green;
        }
        else
        {
            debugText.text = "준비 중이에요...\n앞을 보고 잠깐만 가만히 있어주세요!";
            debugText.color = Color.yellow;
            _hideScheduled = false;
            if (targetToHide != null && !targetToHide.activeSelf)
            {
                targetToHide.SetActive(true);
            }
        }
    }

    private void HandleCalibrationDone(float height)
    {
        if (_hideScheduled) return;
        _hideScheduled = true;
        Invoke(nameof(HideDisplay), hideDelay);
    }

    private void HideDisplay()
    {
        GameObject target = targetToHide != null ? targetToHide : gameObject;
        target.SetActive(false);
    }
}
