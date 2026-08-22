using UnityEngine;

public class EndSceneManager : MonoBehaviour {
    [Header("BGM")]
    [SerializeField] private AudioSource bgmSource;
    [SerializeField] private AudioClip bgmClip;

    private void Start() {
        PlayBGM();
    }

    private void PlayBGM() {
        if (bgmSource == null || bgmClip == null) {
            Debug.LogWarning("BGM AudioSource 또는 AudioClip이 설정되지 않았습니다.");
            return;
        }

        bgmSource.clip = bgmClip;
        bgmSource.loop = true;
        bgmSource.Play();
    }
}