using System.Collections;
using UnityEngine;

public class StageStartUIController : MonoBehaviour {
    [Header("설정")]
    [SerializeField] private GameObject uiPanel;
    [SerializeField] private float showDuration = 10f;

    [Header("손목 UI")]
    [SerializeField] private WristUIController wristUIController;

    private void Start() {
        StartCoroutine(ShowStageStartUI());
    }

    private IEnumerator ShowStageStartUI() {
        // 시작 안내 UI 켜기
        if (uiPanel != null)
            uiPanel.SetActive(true);

        // 게임 정지
        Time.timeScale = 0f;

        // 실제 시간 기준으로 대기
        yield return new WaitForSecondsRealtime(showDuration);

        // 시작 안내 UI 끄기
        if (uiPanel != null)
            uiPanel.SetActive(false);

        // 게임 시작
        Time.timeScale = 1f;

        // ★ 여기서부터 진행도 시간 측정 시작
        if (wristUIController != null) {
            wristUIController.StartProgressTimer();
        }

        Debug.Log("[StageStartUI] 스테이지 시작 - 진행도 타이머 시작");
    }
}