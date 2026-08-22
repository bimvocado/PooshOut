using UnityEngine;
using UnityEngine.Events;

/// <summary>
/// 정화도를 관리하고,
/// WristUIController의 진행도가 100%가 되면 스테이지 클리어.
/// CurrentPurification은 Stage4 자체 게이지(문 열림 판정용, 최대 제한 없음)이고,
/// 증감 시 전체 게임 공용 PurificationSystem.Instance에도 같은 양만큼 반영해서
/// 엔딩(EndSceneManager)에서 저장되는 최종 정화도에 Stage4 기여분이 포함되게 한다.
/// </summary>
public class PurificationManager : MonoBehaviour, IStageProgressProvider {
    public static PurificationManager Instance { get; private set; }

    [Header("정화도 설정")]
    [SerializeField] private float maxPurification = 100f;

    [Header("Wrist UI")]
    [SerializeField] private WristUIController wristUIController;

    [Header("이벤트")]
    public UnityEvent<float> onPurificationChanged;
    public UnityEvent onMaxPurification;

    // ★ 진행도 100% 클리어 시 호출
    public UnityEvent onStageClear;

    [Header("Clear UI")]
    [SerializeField] private GameObject clearUIPrefab;
    [SerializeField] private Transform headTransform; // XR Main Camera
    [SerializeField] private float clearUIDistance = 1.5f;

    // 프리팹 방향이 안 맞을 때 Inspector에서 조절
    [SerializeField] private Vector3 clearUIRotationOffset = Vector3.zero;

    private GameObject _clearUIInstance;

    [Header("사운드 (선택, 나중에 연결 가능)")]
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

    public float CurrentPurification { get; private set; }

    private bool _hasReachedMax;
    private bool _cleared;


    private void Awake() {
        if (Instance != null && Instance != this) {
            Destroy(gameObject);
            return;
        }

        Instance = this;
    }


    private void Start() {
        PlayBGM();
    }


    private void Update() {
        if (_cleared)
            return;

        if (wristUIController == null)
            return;

        // ★ WristUI의 진행도가 100%인지 직접 확인
        if (wristUIController.IsProgressComplete) {
            ClearStage();
        }
    }


    // ==================================================
    // 스테이지 클리어
    // ==================================================

    private void ClearStage() {
        if (_cleared)
            return;

        _cleared = true;

        // 진행도 타이머 정지
        wristUIController?.StopProgressTimer();

        // 링 생성 중지
        if (ringSpawnerRoot != null) {
            ringSpawnerRoot.SetActive(false);
        }

        // 정화도 증가 사운드 정지
        StopIncreaseSound();

        // BGM 정지
        if (bgmAudioSource != null) {
            bgmAudioSource.Stop();
        }

        // 정화도 저장
        PurificationSystem.Instance?.SaveStagePurity(4);

        // Clear UI 생성
        SpawnClearUI();

        onStageClear?.Invoke();

        Debug.Log(
            $"[PurificationManager] Stage Clear / 최종 정화도: {CurrentPurification}"
        );
    }

    private void SpawnClearUI() {
        if (clearUIPrefab == null ||
            headTransform == null ||
            _clearUIInstance != null)
            return;

        Vector3 headPosition = headTransform.position;

        // 머리가 보는 방향에서 위/아래 방향 제거
        Vector3 forward = headTransform.forward;
        forward.y = 0f;

        // 혹시 완전히 위/아래를 보고 있는 경우 방어
        if (forward.sqrMagnitude < 0.001f) {
            forward = headTransform.parent != null
                ? headTransform.parent.forward
                : Vector3.forward;

            forward.y = 0f;
        }

        forward.Normalize();

        // X/Z로만 앞쪽에 이동
        Vector3 spawnPosition =
            headPosition + forward * clearUIDistance;

        // ★ 높이는 무조건 현재 눈높이 유지
        spawnPosition.y = headPosition.y;

        // UI가 플레이어를 바라보게 함
        Vector3 directionToPlayer =
            headPosition - spawnPosition;

        directionToPlayer.y = 0f;

        Quaternion spawnRotation =
            Quaternion.LookRotation(
                directionToPlayer.normalized,
                Vector3.up
            );

        // 프리팹 방향 보정
        spawnRotation *=
            Quaternion.Euler(clearUIRotationOffset);

        _clearUIInstance = Instantiate(
            clearUIPrefab,
            spawnPosition,
            spawnRotation
        );
    }


    // ==================================================
    // BGM
    // ==================================================

    private void PlayBGM() {
        if (bgmAudioSource == null || bgmClip == null)
            return;

        bgmAudioSource.clip = bgmClip;
        bgmAudioSource.volume = bgmVolume;
        bgmAudioSource.loop = true;
        bgmAudioSource.Play();
    }


    // ==================================================
    // IStageProgressProvider
    // ==================================================

    public float NormalizedProgress {
        get {
            if (maxPurification <= 0f)
                return 0f;

            return CurrentPurification / maxPurification;
        }
    }


    // ==================================================
    // 정화도 증가 / 감소
    // ==================================================

    public void AddPurificationAmount(float amount) {
        if (_cleared || amount <= 0f)
            return;

        float previousPurification = CurrentPurification;

        // 최대 제한 없음
        CurrentPurification += amount;

        // 음수 방지만
        CurrentPurification =
            Mathf.Max(0f, CurrentPurification);

        onPurificationChanged?.Invoke(
            CurrentPurification
        );

        // Stage1~3과 동일하게 전체 게임 공용 PurificationSystem에도 반영.
        // 이게 없으면 Stage4에서 올린 정화도가 엔딩에서 저장되는 최종 기록에 안 들어감.
        PurificationSystem.Instance?.Increase(amount);

        PlayIncreaseSoundIfNeeded();

        // 100을 "처음" 넘어갔을 때만 이벤트 발생
        if (!_hasReachedMax &&
            previousPurification < maxPurification &&
            CurrentPurification >= maxPurification) {
            _hasReachedMax = true;

            onMaxPurification?.Invoke();
        }
    }


    public void DecreasePurificationAmount(float amount) {
        if (_hasReachedMax || amount <= 0f)
            return;

        CurrentPurification = Mathf.Clamp(
            CurrentPurification - amount,
            0f,
            maxPurification
        );

        onPurificationChanged?.Invoke(
            CurrentPurification
        );

        PurificationSystem.Instance?.Decrease(amount);

        StopIncreaseSound();
    }


    // ==================================================
    // 사운드
    // ==================================================

    public void StopIncreaseSound() {
        if (increaseAudioSource != null &&
            increaseAudioSource.isPlaying) {
            increaseAudioSource.Stop();
        }
    }


    private void PlayIncreaseSoundIfNeeded() {
        if (increaseAudioSource == null ||
            increaseSound == null)
            return;

        if (!increaseAudioSource.isPlaying) {
            increaseAudioSource.clip = increaseSound;
            increaseAudioSource.loop = true;
            increaseAudioSource.Play();
        }
    }


    // ==================================================
    // 디버그
    // ==================================================

    private void OnGUI() {
        if (!showDebugUI)
            return;

        GUIStyle style =
            new GUIStyle(GUI.skin.label) {
                fontSize = debugFontSize,
                normal = { textColor = Color.white }
            };

        GUI.Label(
            new Rect(
                20,
                20,
                500,
                debugFontSize + 20
            ),
            $"정화도: {CurrentPurification:F0} / {maxPurification:F0}",
            style
        );
    }
}
