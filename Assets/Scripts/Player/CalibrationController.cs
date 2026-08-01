using System;
using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;

/// 게임 시작 시 1회 실행하는 키 캘리브레이션.
///
/// 전제:
/// - XR Origin의 Tracking Origin Mode = "Floor"
///   → 이 경우 HMD(Main Camera) Transform의 world Y값이 곧 "바닥 기준 머리 높이(=키)"가 됨.
///     (Floor 모드에서는 바닥이 y=0 이므로 별도 바닥 보정 계산이 필요 없음)
/// - HeadTracker가 HMD Transform 앵커를 참조하고 있고, BaselineHeight를 보관함.
///
/// 흐름:
///   1. "정면 보고 똑바로 서세요" UI 표시
///   2. HMD가 충분히 안정(거의 안 움직임)될 때까지 대기
///   3. 안정 상태로 몇 초 유지되면 그 순간 HMD Y값을 캡처
///   4. 이상치(너무 낮음/높음)면 재측정 유도, 정상이면 저장 후 완료
///
/// + 측정 결과는 이 씬의 HeadTracker뿐 아니라 GameManager(Singleton, 씬 전환에도 유지)에도 저장함.
///   이후 다른 스테이지 씬으로 넘어가면, 그 씬의 HeadTracker.Start()가
///   GameManager에서 이 값을 읽어와서 재캘리브레이션 없이 그대로 사용함.
///
/// ⚠️ TEMP: autoLoadNextScene 관련 부분은 실기기 테스트(CalibrationTestScene → HeeWonScene)용 임시 코드.
///   나중에 진짜 게임 흐름(타이틀→캘리브레이션→Stage1...)이 StageManager 등으로 짜이면
///   이 스위치를 끄거나 이 블록을 지우면 됨.
public class CalibrationController : MonoBehaviour
{
    [Header("참조")]
    [SerializeField] private Transform hmdTransform;   // XR Origin 하위 Main Camera
    [SerializeField] private HeadTracker headTracker;  // BaselineHeight를 보관하는 쪽

    [Header("측정 설정")]
    [Tooltip("이 시간(초) 동안 HMD가 안정 상태로 유지되어야 측정 성공")]
    [SerializeField] private float requiredStableDuration = 2f;

    [Tooltip("안정 상태 판정: 프레임 간 HMD 이동 속도가 이 값(m/s) 미만이면 '가만히 있음'으로 간주")]
    [SerializeField] private float stableSpeedThreshold = 0.05f;

    [Tooltip("측정 전체 제한 시간(초). 이 시간 넘으면 표준 키로 폴백")]
    [SerializeField] private float maxWaitTime = 10f;

    [Header("아동 대상 유효 범위 (m)")]
    [Tooltip("이 범위를 벗어난 측정값은 비정상(앉음/점프/센서오류)으로 간주")]
    [SerializeField] private float minValidHeight = 1.0f;
    [SerializeField] private float maxValidHeight = 1.7f;

    [Tooltip("측정 실패 시 사용할 아동 표준 키")]
    [SerializeField] private float fallbackHeight = 1.3f;

    [Header("⚠️ TEMP - 실기기 테스트용 자동 씬 전환 (나중에 제거/교체 예정)")]
    [Tooltip("체크하면 캘리브레이션 완료 후 자동으로 다음 씬으로 전환함. 최종 흐름 붙기 전까지만 사용.")]
    [SerializeField] private bool autoLoadNextScene = true;

    [Tooltip("전환할 씬 이름 (Build Settings에 등록되어 있어야 함)")]
    [SerializeField] private string nextSceneName = "HeeWonScene";

    [Tooltip("완료 텍스트를 보여줄 시간(초). 이 시간 지난 뒤 씬 전환.")]
    [SerializeField] private float sceneLoadDelay = 2f;

    // 측정 결과
    public float CalibratedHeight { get; private set; }
    public bool IsCalibrated { get; private set; }

    // UI/사운드 훅 (정화봇 멘트 등을 여기 연결)
    public event Action OnCalibrationStart;      // "똑바로 서세요" 표시 시점
    public event Action<float> OnCalibrationDone; // 측정 완료(최종 키 전달)
    public event Action OnRemeasureRequested;     // 이상치 → 재측정 유도

    private Vector3 _lastPosition;
    private float _debugLogTimer;

    /// 외부(게임 시작 매니저 등)에서 호출.
    public void StartCalibration()
    {
        StopAllCoroutines();
        StartCoroutine(CalibrationRoutine());
    }

    private IEnumerator CalibrationRoutine()
    {
        if (hmdTransform == null)
        {
            Debug.LogError("[Calibration] hmdTransform이 연결되지 않음");
            ApplyResult(fallbackHeight, isFallback: true);
            yield break;
        }

        OnCalibrationStart?.Invoke();
        Debug.Log("[Calibration] 측정 시작 - 앞을 보고 가만히 서주세요");

        while (!IsCalibrated)
        {
            float measured = 0f;
            bool measuredOk = false;

            float elapsed = 0f;
            float stableTimer = 0f;
            _lastPosition = hmdTransform.position;

            while (elapsed < maxWaitTime)
            {
                elapsed += Time.deltaTime;

                float speed = (hmdTransform.position - _lastPosition).magnitude / Mathf.Max(Time.deltaTime, 1e-5f);
                _lastPosition = hmdTransform.position;

                // ⚠️ TEMP DEBUG: 0.3초마다 실시간 Y값/속도 출력 (원인 파악되면 지울 것)
                _debugLogTimer += Time.deltaTime;
                if (_debugLogTimer >= 0.3f)
                {
                    _debugLogTimer = 0f;
                    Debug.Log($"[CalibDebug] hmd.localPos.y={hmdTransform.localPosition.y:F3}, hmd.worldPos.y={hmdTransform.position.y:F3}, XROrigin.y={hmdTransform.root.position.y:F3}, speed={speed:F3}");
                }

                if (speed < stableSpeedThreshold)
                {
                    stableTimer += Time.deltaTime;
                    if (stableTimer >= requiredStableDuration)
                    {
                        measured = hmdTransform.position.y;
                        measuredOk = true;
                        break;
                    }
                }
                else
                {
                    stableTimer = 0f;
                }

                yield return null;
            }

            if (measuredOk && measured >= minValidHeight && measured <= maxValidHeight)
            {
                ApplyResult(measured, isFallback: false);
            }
            else if (measuredOk)
            {
                Debug.LogWarning($"[Calibration] 측정값 {measured:F2}m 이 유효 범위를 벗어남 - 재측정 유도");
                OnRemeasureRequested?.Invoke();
                yield return new WaitForSeconds(1.5f);
            }
            else
            {
                Debug.LogWarning("[Calibration] 시간 초과 - 표준 키로 폴백");
                ApplyResult(fallbackHeight, isFallback: true);
            }
        }
    }

    private void ApplyResult(float height, bool isFallback)
    {
        CalibratedHeight = Mathf.Clamp(height, minValidHeight, maxValidHeight);
        IsCalibrated = true;

        if (headTracker != null)
        {
            headTracker.SetBaselineHeight(CalibratedHeight);
        }

        if (GameManager.Instance != null)
        {
            GameManager.Instance.SetCalibratedHeight(CalibratedHeight);
        }
        else
        {
            Debug.LogWarning("[Calibration] GameManager.Instance가 없어서 씬 전환 시 값이 유지되지 않음. 씬에 GameManager가 있는지 확인 필요.");
        }

        Debug.Log($"[Calibration] 완료 - 키={CalibratedHeight:F2}m{(isFallback ? " (폴백)" : "")}");
        OnCalibrationDone?.Invoke(CalibratedHeight);

        if (autoLoadNextScene)
        {
            StartCoroutine(LoadNextSceneAfterDelay());
        }
    }

    private IEnumerator LoadNextSceneAfterDelay()
    {
        Debug.Log($"[Calibration] {sceneLoadDelay}초 후 '{nextSceneName}' 씬으로 전환 (테스트용)");
        yield return new WaitForSeconds(sceneLoadDelay);

        if (string.IsNullOrEmpty(nextSceneName))
        {
            Debug.LogWarning("[Calibration] nextSceneName이 비어있어서 씬 전환 안 함");
            yield break;
        }

        SceneManager.LoadScene(nextSceneName);
    }

    /// 플레이 도중 재측정이 필요할 때(헤드셋 밀림 등) 외부에서 호출.
    public void Recalibrate()
    {
        IsCalibrated = false;
        StartCalibration();
    }

    /// 테스트용: 씬 시작 시 자동으로 캘리브레이션 시작.
    private void Start()
    {
        StartCalibration();
    }
}