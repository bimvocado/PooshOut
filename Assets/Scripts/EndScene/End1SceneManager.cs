using System.Collections;
using UnityEngine;

public class End1SceneManager : MonoBehaviour {
    [Header("BGM")]
    [SerializeField] private AudioSource bgmAudioSource;
    [SerializeField] private AudioClip bgmClip;

    [Header("첨벙 후 UI")]
    [SerializeField] private GameObject completeUI;
    [SerializeField] private float uiDelay = 2f;

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
    }
}