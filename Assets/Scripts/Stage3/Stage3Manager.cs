using UnityEngine;

/// <summary>
/// 스테이지 3 흐름 제어.
/// 미생물 분해 수를 기준으로 진행도를 제공한다.
/// </summary>
public class Stage3Manager : MonoBehaviour, IStageProgressProvider
{
    [Header("Stage")]
    [SerializeField] private MicrobeSpawner microbeSpawner;


    [Header("BGM")]
    [SerializeField] private AudioSource bgmAudioSource;
    [SerializeField] private AudioClip bgmClip;

    [Range(0f, 1f)]
    [SerializeField] private float bgmVolume = 0.5f;

    private bool _cleared;

    [SerializeField] private float scorePerDecompose = 2f;
    private int _lastDecomposedCount;

    [Header("진행도")]
    [SerializeField] private WristUIController wristUIController;

    [Header("Clear UI")]
    [SerializeField] private GameObject clearUIPrefab;
    [SerializeField] private Transform headTransform; // XR Origin의 Main Camera
    [SerializeField] private float clearUIDistance = 3f;

    [Tooltip("클리어 UI를 보여준 뒤 다음 스테이지로 넘어가기까지의 여유 시간(초).")]
    [SerializeField] private float clearDelay = 2f;

    private GameObject _clearUIInstance;

    // LLM 마무리 피드백용 - 정화도 계산과는 별개로 순수 명중/빗나감 횟수와 최대 연속 명중만 관찰한다.
    private int _hitCount;
    private int _missCount;
    private int _currentStreak;
    private int _maxStreak;


    /// <summary>
    /// 현재 Stage3 진행도 (손목 UI가 보여주는 값).
    /// 0 = 0%, 1 = 100%. 이번 스테이지에서 분해한 미생물 수 기준.
    /// ClearStage()에서 저장하는 값도 이 값을 그대로 써야 손목 UI와 일치한다.
    /// </summary>
    public float NormalizedProgress
    {
        get
        {
            if (microbeSpawner == null)
                return 0f;

            float score =
                microbeSpawner.DecomposedCount *
                scorePerDecompose;

            return score / 100f;
        }
    }

    private void OnEnable()
    {
        Bullet.OnMicrobeHit += HandleHit;
        Bullet.OnMissedShot += HandleMiss;
    }

    private void OnDisable()
    {
        Bullet.OnMicrobeHit -= HandleHit;
        Bullet.OnMissedShot -= HandleMiss;
    }

    private void HandleHit()
    {
        _hitCount++;
        _currentStreak++;
        if (_currentStreak > _maxStreak) _maxStreak = _currentStreak;
    }

    private void HandleMiss()
    {
        _missCount++;
        _currentStreak = 0;
    }

    private void Start()
    {
        StartStage();
    }

    private void StartStage()
    {
        Debug.Log("[Stage3Manager] 스테이지 3 시작");

        _hitCount = 0;
        _missCount = 0;
        _currentStreak = 0;
        _maxStreak = 0;

        microbeSpawner?.StartSpawning();

        PlayBGM();
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

    private void Update()
    {
        if (_cleared)
            return;

        if (wristUIController == null)
            return;

        // WristUI의 진행도가 100%인지 Stage3에서 직접 확인
        if (wristUIController.IsProgressComplete)
        {
            ClearStage();
        }
    }

    private void ClearStage()
    {
        if (_cleared)
            return;

        _cleared = true;

        // 미생물 스폰 정지
        microbeSpawner?.StopSpawning();

        // 진행도 타이머 정지
        wristUIController?.StopProgressTimer();

        // 정화도 저장 - 손목 UI에 보이던 값(NormalizedProgress)을 그대로 저장한다.
        // PurificationSystem.Purity(게임 시작부터 누적되는 전역 값)를 저장하면
        // 손목 UI에 보이는 값과 달라지므로 여기서는 쓰지 않는다.
        if (PurificationSystem.Instance != null) {
            PurificationSystem.Instance.SaveStagePurity(3, NormalizedProgress * 100f);
        }

        // LLM 마무리 피드백용 로그. 순수 사실만 기록.
        int totalShots = _hitCount + _missCount;
        string note = $"버블건 {totalShots}번 쏴서 {_hitCount}번 명중, 최대 연속 명중 {_maxStreak}회";
        PurificationSystem.Instance?.RecordStageLog(3, _hitCount, _missCount, note);

        // 눈앞에 Clear UI 생성
        SpawnClearUI();

        Debug.Log("[Stage3Manager] Stage3 Clear");

        // 클리어 UI를 잠깐 보여준 뒤 다음 스테이지로.
        Invoke(nameof(AdvanceToNextStage), clearDelay);
    }

    private void AdvanceToNextStage() {
        StageManager.Instance?.AdvanceStage();
    }

    private void SpawnClearUI()
    {
        if (clearUIPrefab == null ||
            headTransform == null ||
            _clearUIInstance != null)
            return;

        // 플레이어 눈앞 위치
        Vector3 spawnPosition =
            headTransform.position +
            headTransform.forward * clearUIDistance;

        // 카메라의 좌우 회전만 사용
        Vector3 forward = headTransform.forward;
        forward.y = 0f;
        forward.Normalize();

        Quaternion spawnRotation =
            Quaternion.LookRotation(forward, Vector3.up);

        _clearUIInstance = Instantiate(
            clearUIPrefab,
            spawnPosition,
            spawnRotation
        );
    }
}