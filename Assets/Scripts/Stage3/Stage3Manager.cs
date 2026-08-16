using UnityEngine;

/// <summary>
/// 스테이지 3 흐름 제어.
/// 씬이 시작하면 미생물 스폰과 BGM을 시작하고,
/// 목표 분해 수를 채우면 스폰을 중지한다.
/// </summary>
public class Stage3Manager : MonoBehaviour {
    [Header("Stage")]
    [SerializeField] private MicrobeSpawner microbeSpawner;
    [SerializeField] private int decomposeTarget = 15;

    [Header("BGM")]
    [SerializeField] private AudioSource bgmAudioSource;
    [SerializeField] private AudioClip bgmClip;
    [Range(0f, 1f)]
    [SerializeField] private float bgmVolume = 0.5f;

    private bool _cleared;

    private void Start() {
        StartStage();
    }

    private void StartStage() {
        Debug.Log("[Stage3Manager] 스테이지 3 시작");

        // 미생물 스폰 시작
        microbeSpawner?.StartSpawning();

        // BGM 시작
        PlayBGM();
    }

    private void PlayBGM() {
        if (bgmAudioSource == null || bgmClip == null)
            return;

        bgmAudioSource.clip = bgmClip;
        bgmAudioSource.volume = bgmVolume;

        // 반복 재생
        bgmAudioSource.loop = true;

        bgmAudioSource.Play();
    }

    private void Update() {
        if (_cleared || microbeSpawner == null)
            return;

        if (microbeSpawner.DecomposedCount >= decomposeTarget) {
            _cleared = true;

            microbeSpawner.StopSpawning();

            Debug.Log("[Stage3Manager] 목표 분해량 달성");
        }
    }
}