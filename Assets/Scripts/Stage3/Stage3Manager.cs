using UnityEngine;

/// <summary>
/// 스테이지 3 흐름 제어.
/// 미생물 분해 수를 기준으로 진행도를 제공한다.
/// </summary>
public class Stage3Manager : MonoBehaviour, IStageProgressProvider {
    [Header("Stage")]
    [SerializeField] private MicrobeSpawner microbeSpawner;

    [SerializeField] private int decomposeTarget = 15;

    [Header("BGM")]
    [SerializeField] private AudioSource bgmAudioSource;
    [SerializeField] private AudioClip bgmClip;

    [Range(0f, 1f)]
    [SerializeField] private float bgmVolume = 0.5f;

    private bool _cleared;

    /// <summary>
    /// 현재 Stage3 진행도.
    /// 0 = 0%, 1 = 100%
    /// </summary>
    public float NormalizedProgress {
        get {
            if (microbeSpawner == null || decomposeTarget <= 0)
                return 0f;

            return Mathf.Clamp01(
                (float)microbeSpawner.DecomposedCount
                / decomposeTarget
            );
        }
    }

    private void Start() {
        StartStage();
    }

    private void StartStage() {
        Debug.Log("[Stage3Manager] 스테이지 3 시작");

        microbeSpawner?.StartSpawning();

        PlayBGM();
    }

    private void PlayBGM() {
        if (bgmAudioSource == null || bgmClip == null)
            return;

        bgmAudioSource.clip = bgmClip;
        bgmAudioSource.volume = bgmVolume;
        bgmAudioSource.loop = true;

        bgmAudioSource.Play();
    }

    private void Update() {
        if (_cleared || microbeSpawner == null)
            return;

        if (microbeSpawner.DecomposedCount >= decomposeTarget) {
            ClearStage();
        }
    }

    private void ClearStage() {
        if (_cleared)
            return;

        _cleared = true;

        microbeSpawner.StopSpawning();

        // Stage3 종료 시 현재 정화도 저장
        if (PurificationSystem.Instance != null) {
            PurificationSystem.Instance.SaveStagePurity(3);
        }

        Debug.Log("[Stage3Manager] 목표 분해량 달성");
    }
}