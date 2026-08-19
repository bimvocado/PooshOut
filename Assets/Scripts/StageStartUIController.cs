using System.Collections;
using UnityEngine;

public class StageStartUIController : MonoBehaviour
{
    [Header("설정")]
    [SerializeField] private GameObject uiPanel;      // 스테이지 시작 UI 패널
    [SerializeField] private float showDuration = 10f; // n초 (보여줄 시간)

    private void Start()
    {
        StartCoroutine(ShowStageStartUI());
    }

    private IEnumerator ShowStageStartUI()
    {
        // UI 켜기
        if (uiPanel != null)
            uiPanel.SetActive(true);

        // 다른 게임 로직 정지 (물리, Update 기반 이동 등 timeScale에 의존하는 것들 멈춤)
        Time.timeScale = 0f;

        // timeScale이 0이어도 흐르는 실제 시간 기준으로 대기
        yield return new WaitForSecondsRealtime(showDuration);

        // 정지 해제
        Time.timeScale = 1f;

        // UI 끄기
        if (uiPanel != null)
            uiPanel.SetActive(false);
    }
}
