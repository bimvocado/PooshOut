using UnityEngine;
using UnityEngine.Events;

/// <summary>
/// 정화도(0~100)를 관리하는 매니저.
/// IStageProgressProvider를 구현해서
/// WristUIController가 현재 정화도를 0~1 진행도로 읽을 수 있게 한다.
/// </summary>
public class PurificationManager : MonoBehaviour, IStageProgressProvider {
    public static PurificationManager Instance { get; private set; }

    [Header("정화도 설정")]
    [SerializeField] private float maxPurification = 100f;

    [Header("이벤트")]
    public UnityEvent<float> onPurificationChanged;
    public UnityEvent onMaxPurification;

    [Header("사운드 (선택, 나중에 연결 가능)")]
    [SerializeField] private AudioSource increaseAudioSource;
    [SerializeField] private AudioClip increaseSound;

    [Header("디버깅")]
    [SerializeField] private bool showDebugUI = true;
    [SerializeField] private int debugFontSize = 40;

    public float CurrentPurification { get; private set; }

    /// <summary>
    /// IStageProgressProvider 구현.
    /// WristUIController에서는 이 값을 0~1 정화도로 사용한다.
    /// </summary>
    public float NormalizedProgress {
        get {
            if (maxPurification <= 0f)
                return 0f;

            return Mathf.Clamp01(
                CurrentPurification / maxPurification
            );
        }
    }

    private bool _hasReachedMax;

    private void Awake() {
        if (Instance != null && Instance != this) {
            Destroy(gameObject);
            return;
        }

        Instance = this;
    }

    /// <summary>
    /// 정화도를 amount만큼 증가.
    /// </summary>
    public void AddPurificationAmount(float amount) {
        if (_hasReachedMax || amount <= 0f)
            return;

        CurrentPurification = Mathf.Clamp(
            CurrentPurification + amount,
            0f,
            maxPurification
        );

        onPurificationChanged?.Invoke(CurrentPurification);

        PlayIncreaseSoundIfNeeded();

        if (CurrentPurification >= maxPurification) {
            _hasReachedMax = true;
            onMaxPurification?.Invoke();
        }
    }

    /// <summary>
    /// 정화도를 amount만큼 감소.
    /// </summary>
    public void DecreasePurificationAmount(float amount) {
        if (_hasReachedMax || amount <= 0f)
            return;

        CurrentPurification = Mathf.Clamp(
            CurrentPurification - amount,
            0f,
            maxPurification
        );

        onPurificationChanged?.Invoke(CurrentPurification);

        StopIncreaseSound();
    }

    public void StopIncreaseSound() {
        if (increaseAudioSource != null &&
            increaseAudioSource.isPlaying) {
            increaseAudioSource.Stop();
        }
    }

    private void PlayIncreaseSoundIfNeeded() {
        if (increaseAudioSource == null || increaseSound == null)
            return;

        if (!increaseAudioSource.isPlaying) {
            increaseAudioSource.clip = increaseSound;
            increaseAudioSource.loop = true;
            increaseAudioSource.Play();
        }
    }

    private void OnGUI() {
        if (!showDebugUI)
            return;

        GUIStyle style = new GUIStyle(GUI.skin.label) {
            fontSize = debugFontSize,
            normal = { textColor = Color.white }
        };

        GUI.Label(
            new Rect(20, 20, 500, debugFontSize + 20),
            $"정화도: {CurrentPurification:F0} / {maxPurification:F0}",
            style
        );
    }
}