using UnityEngine;

/// <summary>
/// 스테이지 3 흐름 제어.
/// 미생물 분해 수를 기준으로 진행도를 제공한다.
/// </summary>
public class Stage3Manager : MonoBehaviour, IStageProgressProvider {
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

    private GameObject _clearUIInstance;


    /// <summary>
    /// 현재 Stage3 진행도.
    /// 0 = 0%, 1 = 100%
    /// </summary>
    public float NormalizedProgress {
        get {
            if (microbeSpawner == null)
                return 0f;

            float score =
                microbeSpawner.DecomposedCount *
                scorePerDecompose;

            return score / 100f;
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
        if (_cleared)
            return;

        if (wristUIController == null)
            return;

        // WristUI의 진행도가 100%인지 Stage3에서 직접 확인
        if (wristUIController.IsProgressComplete) {
            ClearStage();
        }
    }

    private void ClearStage() {
        if (_cleared)
            return;

        _cleared = true;

        // 미생물 스폰 정지
        microbeSpawner?.StopSpawning();

        // 진행도 타이머 정지
        wristUIController?.StopProgressTimer();

        // 정화도 저장
        if (PurificationSystem.Instance != null) {
            PurificationSystem.Instance.SaveStagePurity(3);
        }

        // 눈앞에 Clear UI 생성
        SpawnClearUI();

        Debug.Log("[Stage3Manager] Stage3 Clear");
    }

    private void SpawnClearUI() {
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