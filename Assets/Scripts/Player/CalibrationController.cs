using System;
using System.Collections;
using UnityEngine;

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
/// (예전에 있던 autoLoadNextScene 자동 씬 전환 테스트 코드는 제거함 - 이제 진짜 게임 흐름
///  [PooshBotDirector.IntroSequence → StageSceneLoader]이 씬 전환을 전담하므로, 이 스크립트가
///  임의로 다른 씬을 불러오면 그 흐름과 충돌한다.)
public class CalibrationController : MonoBehaviour
{
    [Header("참조")]
    [SerializeField] private Transform hmdTransform;   // XR Origin 하위 Main Camera
    [SerializeField] private HeadTracker headTracker;  // BaselineHeight를 보관하는 쪽

    [Header("XR Origin 오프셋 보정")]
    [Tooltip("Floor 모드는 카메라의 world Y를 그대로 키로 쓰는데, XR Origin이나 그 자식(Camera Offset 등)에 " +
             "Y축 오프셋이 남아있으면(예: Device 모드였을 때 수동으로 넣어둔 눈높이 보정값) 그만큼 키가 " +
             "부풀려져 측정된다. 여기에 XR Origin(XR Rig) Transform을 연결하면 InverseTransformPoint로 " +
             "그 전체 오프셋 체인(XR Origin + Camera Offset 등 중간 계층 전부)을 한 번에 상쇄하고 계산한다. " +
             "비워두면 예전처럼 world Y를 그대로 사용.")]
    [SerializeField] private Transform xrOriginForOffsetCorrection;

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

    // 측정 결과
    public float CalibratedHeight { get; private set; }
    public bool IsCalibrated { get; private set; }

    // UI/사운드 훅 (정화봇 멘트 등을 여기 연결)
    public event Action OnCalibrationStart;      // "똑바로 서세요" 표시 시점
    public event Action<float> OnCalibrationDone; // 측정 완료(최종 키 전달)
    public event Action OnRemeasureRequested;     // 이상치 → 재측정 유도

    private Vector3 _lastPosition;
    private float _debugLogTimer;

    /// XR Origin 기준 로컬 Y값 = 바닥 기준 실제 키. InverseTransformPoint를 쓰면 XR Origin 자신의
    /// 오프셋뿐 아니라 그 밑의 Camera Offset 등 중간 계층에 남아있는 오프셋까지 한 번에 다 상쇄된다.
    /// (단순히 XR Origin.position.y만 빼면 Camera Offset처럼 중간에 낀 오프셋은 못 잡아냄 - 이번에 실제로 겪은 문제)
    /// xrOriginForOffsetCorrection이 비어있으면 예전처럼 world Y 그대로.
    private float GetFloorRelativeHeight()
    {
        if (xrOriginForOffsetCorrection == null)
        {
            return hmdTransform.position.y;
        }
        return xrOriginForOffsetCorrection.InverseTransformPoint(hmdTransform.position).y;
    }

    /// 외부(게임 시작 매니저 등)에서 호출. 이전에 이미 측정한 적 있어도 항상 새로 측정한다
    /// (IsCalibrated가 true로 남아있으면 CalibrationRoutine의 while 루프가 안 돌아서
    /// 아무 일도 안 일어나는 채로 조용히 끝나버리는 문제가 있었음 - 여기서 리셋해서 방지).
    public void StartCalibration()
    {
        IsCalibrated = false;
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

                _debugLogTimer += Time.deltaTime;
                if (_debugLogTimer >= 0.3f)
                {
                    _debugLogTimer = 0f;
                    Debug.Log($"[CalibDebug] hmd.localPos.y={hmdTransform.localPosition.y:F3}, hmd.worldPos.y={hmdTransform.position.y:F3}, XROrigin.y={hmdTransform.root.position.y:F3}, corrected={GetFloorRelativeHeight():F3}, speed={speed:F3}");
                }

                if (speed < stableSpeedThreshold)
                {
                    stableTimer += Time.deltaTime;
                    if (stableTimer >= requiredStableDuration)
                    {
                        measured = GetFloorRelativeHeight();
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
    }

    /// 플레이 도중 재측정이 필요할 때(헤드셋 밀림 등) 외부에서 호출.
    public void Recalibrate()
    {
        IsCalibrated = false;
        StartCalibration();
    }
}