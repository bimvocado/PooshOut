using UnityEngine;
using UnityEngine.Events;
using UnityEngine.SceneManagement;

/// <summary>
/// 정화도를 관리하고,
/// WristUIController의 진행도가 100%가 되면 스테이지 클리어.
/// CurrentPurification은 Stage4 자체 게이지(문 열림 판정용, 최대 제한 없음)이고,
/// 증감 시 전체 게임 공용 PurificationSystem.Instance에도 같은 양만큼 반영해서
/// 엔딩(EndSceneManager)에서 저장되는 최종 정화도에 Stage4 기여분이 포함되게 한다.
/// </summary>
public class PurificationManager : MonoBehaviour, IStageProgressProvider
{
    public static PurificationManager Instance { get; private set; }

    [Header("정화도 설정")]
    [SerializeField] private float maxPurification = 100f;

    [Header("Wrist UI")]
    [SerializeField] private WristUIController wristUIController;

    [Header("이벤트")]
    public UnityEvent<float> onPurificationChanged;
    public UnityEvent onMaxPurification;

    // 진행도 100% 클리어 시 호출 (Inspector에 다른 리스너가 연결되어 있을 수도 있어 유지)
    public UnityEvent onStageClear;

    [Header("Clear UI")]
    [SerializeField] private GameObject clearUIPrefab;
    [SerializeField] private Transform headTransform; // XR Main Camera
    [SerializeField] private float clearUIDistance = 1.5f;

    [SerializeField] private Vector3 clearUIRotationOffset = Vector3.zero;

    private GameObject _clearUIInstance;

    [Header("사운드")]
    [SerializeField] private AudioSource increaseAudioSource;
    [SerializeField] private AudioClip increaseSound;

    [Header("디버깅")]
    [SerializeField] private bool showDebugUI = true;
    [SerializeField] private int debugFontSize = 40;

    [Header("BGM")]
    [SerializeField] private AudioSource bgmAudioSource;
    [SerializeField] private AudioClip bgmClip;

    [Header("링 스포너")]
    [SerializeField] private GameObject ringSpawnerRoot;

    [Range(0f, 1f)]
    [SerializeField] private float bgmVolume = 0.5f;

    [Header("다음 씬")]
    [Tooltip("Clear UI를 보여준 뒤 다음 씬으로 넘어가기까지의 여유 시간(초).")]
    [SerializeField] private float nextSceneDelay = 8f;
    [SerializeField] private string nextSceneName = "End1Scene";

    public float CurrentPurification { get; private set; }

    private bool _hasReachedMax;
    private bool _cleared;

    // LLM 마무리 피드백용 - 정화도 계산과는 별개로 순수 통과/놓침 횟수만 관찰한다.
    private int _ringPassCount;
    private int _ringMissCount;


    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
    }


    private void OnEnable()
    {
        Ring.OnRingPassed += HandleRingPassed;
        Ring.OnRingMissed += HandleRingMissed;
    }

    private void OnDisable()
    {
        Ring.OnRingPassed -= HandleRingPassed;
        Ring.OnRingMissed -= HandleRingMissed;
    }

    private void HandleRingPassed()
    {
        _ringPassCount++;
    }

    private void HandleRingMissed()
    {
        _ringMissCount++;
    }


    private void Start()
    {
        PlayBGM();
    }


    private void Update()
    {
        if (_cleared)
            return;

        if (wristUIController == null)
            return;

        if (wristUIController.IsProgressComplete)
        {
            ClearStage();
        }
    }


    // ==================================================
    // 스테이지 클리어
    // ==================================================

    private void ClearStage()
    {
        if (_cleared)
            return;

        _cleared = true;

        wristUIController?.StopProgressTimer();

        if (ringSpawnerRoot != null)
        {
            ringSpawnerRoot.SetActive(false);
        }

        StopIncreaseSound();

        if (bgmAudioSource != null)
        {
            bgmAudioSource.Stop();
        }

        // 정화도 저장 - Stage4 자체 진행도를 그대로 저장한다.
        PurificationSystem.Instance?.SaveStagePurity(4, CurrentPurification);

        // LLM 마무리 피드백용 로그. 순수 사실만 기록.
        string note = $"자외선 링 {_ringPassCount + _ringMissCount}번 중 {_ringPassCount}번 통과";
        PurificationSystem.Instance?.RecordStageLog(4, _ringPassCount, _ringMissCount, note);

        // Clear UI 생성
        SpawnClearUI();

        onStageClear?.Invoke();

        Debug.Log(
            $"[PurificationManager] Stage Clear / 최종 정화도: {CurrentPurification}"
        );

        // 클리어 UI를 잠깐 보여준 뒤 엔딩 씬으로.
        Invoke(nameof(LoadNextScene), nextSceneDelay);
    }

    private void LoadNextScene()
    {
        if (string.IsNullOrEmpty(nextSceneName))
        {
            Debug.LogWarning($"{nameof(PurificationManager)}: nextSceneName이 비어있어서 씬 전환 안 함.");
            return;
        }

        SceneManager.LoadScene(nextSceneName);
    }

    private void SpawnClearUI()
    {
        if (clearUIPrefab == null ||
            headTransform == null ||
            _clearUIInstance != null)
            return;

        Vector3 headPosition = headTransform.position;
        Vector3 forward = headTransform.forward;
        forward.y = 0f;

        if (forward.sqrMagnitude < 0.001f)
        {
            forward = headTransform.parent != null
                ? headTransform.parent.forward
                : Vector3.forward;
            forward.y = 0f;
        }

        forward.Normalize();
        Vector3 spawnPosition = headPosition + forward * clearUIDistance;
        spawnPosition.y = headPosition.y;

        Vector3 directionToPlayer = headPosition - spawnPosition;
        directionToPlayer.y = 0f;

        Quaternion spawnRotation = Quaternion.LookRotation(directionToPlayer.normalized, Vector3.up);
        spawnRotation *= Quaternion.Euler(clearUIRotationOffset);

        _clearUIInstance = Instantiate(clearUIPrefab, spawnPosition, spawnRotation);
    }


    private void PlayBGM()
    {
        if (bgmAudioSource == null || bgmClip == null)
            return;

        bgmAudioSource.clip = bgmClip;
        bgmAudioSource.volume = bgmVolume;
        bgmAudioSource.loop = true;
        bgmAudioSource.Play();
    }


    public float NormalizedProgress
    {
        get
        {
            if (maxPurification <= 0f)
                return 0f;

            return CurrentPurification / maxPurification;
        }
    }


    public void AddPurificationAmount(float amount)
    {
        if (_cleared || amount <= 0f)
            return;

        float previousPurification = CurrentPurification;
        CurrentPurification += amount;
        CurrentPurification = Mathf.Max(0f, CurrentPurification);

        onPurificationChanged?.Invoke(CurrentPurification);
        PurificationSystem.Instance?.Increase(amount);
        PlayIncreaseSoundIfNeeded();

        if (!_hasReachedMax &&
            previousPurification < maxPurification &&
            CurrentPurification >= maxPurification)
        {
            _hasReachedMax = true;
            onMaxPurification?.Invoke();
        }
    }


    public void DecreasePurificationAmount(float amount)
    {
        if (_hasReachedMax || amount <= 0f)
            return;

        CurrentPurification = Mathf.Clamp(CurrentPurification - amount, 0f, maxPurification);
        onPurificationChanged?.Invoke(CurrentPurification);
        PurificationSystem.Instance?.Decrease(amount);
        StopIncreaseSound();
    }


    public void StopIncreaseSound()
    {
        if (increaseAudioSource != null && increaseAudioSource.isPlaying)
        {
            increaseAudioSource.Stop();
        }
    }


    private void PlayIncreaseSoundIfNeeded()
    {
        if (increaseAudioSource == null || increaseSound == null)
            return;

        if (!increaseAudioSource.isPlaying)
        {
            increaseAudioSource.clip = increaseSound;
            increaseAudioSource.loop = true;
            increaseAudioSource.Play();
        }
    }


    private void OnGUI()
    {
        if (!showDebugUI)
            return;

        GUIStyle style = new GUIStyle(GUI.skin.label)
        {
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