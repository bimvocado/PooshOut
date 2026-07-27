using UnityEngine;
using TMPro;

/// 캘리브레이션 진행 상황/결과를 화면에 텍스트로 표시 (테스트/디버그용).
///
/// 세팅:
/// 1. World Space Canvas 생성 (GameObject > UI > Canvas, Render Mode를 World Space로 변경)
///    - 플레이어 눈높이 근처, 살짝 앞쪽에 위치시키면 보기 편함 (예: Position Z = 1~1.5)
/// 2. Canvas 하위에 TextMeshPro - Text 오브젝트 생성
/// 3. 이 스크립트를 Canvas나 별도 빈 오브젝트에 붙이고, calibrationController / debugText 연결
public class CalibrationDebugDisplay : MonoBehaviour
{
    [SerializeField] private CalibrationController calibrationController;
    [SerializeField] private TextMeshProUGUI debugText;

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
        }
    }
}
