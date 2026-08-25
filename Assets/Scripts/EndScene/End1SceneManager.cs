using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;

public class End1SceneManager : MonoBehaviour {
    [Header("BGM")]
    [SerializeField] private AudioSource bgmAudioSource;
    [SerializeField] private AudioClip bgmClip;

    [Header("첨벙 후 UI")]
    [SerializeField] private GameObject completeUI;
    [SerializeField] private float uiDelay = 2f;

    [Header("다음 씬")]
    [Tooltip("완료 UI(정화도 결과)를 보여준 뒤 다음 씬으로 넘어가기까지의 여유 시간(초).")]
    [SerializeField] private float nextSceneDelay = 3f;
    [SerializeField] private string nextSceneName = "End2Scene";

    private bool uiStarted;

    private void Awake() {
        // 처음에는 완료 UI 숨김
        if (completeUI != null) {
            completeUI.SetActive(false);
        }

        // BGM 설정
        if (bgmAudioSource != null) {
            bgmAudioSource.playOnAwake = false;
            bgmAudioSource.loop = true;
            bgmAudioSource.spatialBlend = 0f;
        }
    }

    private void Start() {
        PlayBGM();
    }

    private void PlayBGM() {
        if (bgmAudioSource == null || bgmClip == null) {
            Debug.LogWarning("[End1SceneManager] BGM 설정이 비어있음");
            return;
        }

        bgmAudioSource.clip = bgmClip;

        if (!bgmAudioSource.isPlaying) {
            bgmAudioSource.Play();
        }
    }

    /// <summary>
    /// WaterDrop이 바닥에 첨벙했을 때 호출
    /// </summary>
    public void OnWaterSplash() {
        // 여러 물방울이 충돌해도 UI는 한 번만 띄움
        if (uiStarted)
            return;

        uiStarted = true;

        Debug.Log("[End1SceneManager] 첨벙 감지 - 2초 후 UI 표시");

        StartCoroutine(ShowUIAfterDelay());
    }

    private IEnumerator ShowUIAfterDelay() {
        yield return new WaitForSeconds(uiDelay);

        if (completeUI != null) {
            completeUI.SetActive(true);
        }
        else {
            Debug.LogWarning("[End1SceneManager] Complete UI가 연결되지 않음");
        }

        // 정화도 결과를 잠깐 보여준 뒤 다음 씬(순위표)으로.
        yield return new WaitForSeconds(nextSceneDelay);

        if (string.IsNullOrEmpty(nextSceneName)) {
            Debug.LogWarning("[End1SceneManager] nextSceneName이 비어있어서 씬 전환 안 함");
            yield break;
        }

        SceneManager.LoadScene(nextSceneName);
    }
}