using UnityEngine;
using TMPro;

/// <summary>
/// 왼손 손목을 뒤집어 손바닥이 하늘을 향하면 손목에 상태창 UI(정화도/맵 진행도)를 띄움.
/// 손바닥이 다시 아래로 돌아가면 UI를 끔.
///
/// 판정 기준(OpenXR grip pose 규격):
/// 손을 자연스럽게 늘어뜨린 자세(손바닥이 아래/몸 쪽)에서 컨트롤러의 로컬 +Y축은 "손등 바깥쪽(위)"을
/// 향하도록 정의되어 있음. 즉 로컬 -Y축이 손바닥이 향하는 방향.
/// 손목을 돌려 손바닥이 하늘을 보면 이 -Y축이 월드 Up과 가까워짐 → 그 각도로 판정.
/// ※ 리그/컨트롤러 모델에 따라 축 정의가 다르게 붙어있을 수 있어서, 실기기에서 반대로 동작하면
///    invertDetection 체크박스로 뒤집으면 됨 (아래 "씬 세팅" 6번 참고).
///
/// WristCanvas(월드스페이스 캔버스)는 미리 왼손 컨트롤러의 자식으로 배치해두고,
/// 이 스크립트는 활성/비활성과 텍스트 갱신만 담당함 (위치는 에디터에서 직접 배치하는 게
/// 코드로 오프셋 값을 맞추는 것보다 훨씬 정확하고 직관적임).
///
/// 스테이지 전용 컴포넌트(RailMover 등)를 직접 참조하지 않고 IStageProgressProvider를 통해
/// 진행도를 읽으므로, 모든 스테이지에서 그대로 재사용할 수 있음. progressProviderSource를
/// 비워두면 씬에서 IStageProgressProvider를 구현한 컴포넌트를 자동으로 찾는다.
/// </summary>
public class WristUIController : MonoBehaviour
{
    [Header("참조")]
    [SerializeField] private HandTracker handTracker;

    [Header("UI (WristCanvas 프리팹을 왼손 컨트롤러 자식으로 배치한 뒤 연결)")]
    [SerializeField] private GameObject wristCanvasRoot;
    [SerializeField] private TextMeshProUGUI purificationText;
    [SerializeField] private TextMeshProUGUI progressText;

    [Header("진행도 소스 (IStageProgressProvider 구현체, 예: Stage1의 RailMover). 비워두면 씬에서 자동 탐색")]
    [SerializeField] private MonoBehaviour progressProviderSource;

    private IStageProgressProvider progressProvider;

    [Header("감지 설정 (손바닥이 위를 향하는 각도 기준, 0=완전히 위)")]
    [Tooltip("이 각도보다 작아지면 UI를 켬")]
    [SerializeField] private float activateAngleThreshold = 60f;
    [Tooltip("켜진 상태에서 이 각도보다 커지면 UI를 끔. activateAngleThreshold보다 크게 잡아야 " +
             "경계 각도에서 UI가 깜빡이지 않음 (히스테리시스).")]
    [SerializeField] private float deactivateAngleThreshold = 100f;
    [SerializeField] private bool invertDetection = false;

    public bool IsUIVisible { get; private set; }

    private void Start()
    {
        ResolveProgressProvider();
        SetVisible(false, force: true);
    }

    /// <summary>수동으로 지정된 소스가 있으면 그걸 쓰고, 없으면 씬에서 IStageProgressProvider 구현체를 찾는다.</summary>
    private void ResolveProgressProvider()
    {
        progressProvider = progressProviderSource as IStageProgressProvider;
        if (progressProvider != null) return;

        foreach (var behaviour in FindObjectsByType<MonoBehaviour>(FindObjectsSortMode.None))
        {
            if (behaviour is IStageProgressProvider provider)
            {
                progressProvider = provider;
                break;
            }
        }
    }

    private void Update()
    {
        if (handTracker == null) return;

        float angle = GetPalmUpAngle();
        bool nextVisible = IsUIVisible ? angle < deactivateAngleThreshold : angle < activateAngleThreshold;
        SetVisible(nextVisible);

        if (IsUIVisible)
        {
            RefreshTexts();
        }
    }

    /// <summary>손바닥이 향하는 방향과 월드 Up 사이의 각도 (0 = 완전히 위, 180 = 완전히 아래).</summary>
    private float GetPalmUpAngle()
    {
        Vector3 palmDirection = handTracker.LeftHandRotation * Vector3.down; // 로컬 -Y = 손바닥 방향
        if (invertDetection) palmDirection = -palmDirection;
        return Vector3.Angle(palmDirection, Vector3.up);
    }

    private void SetVisible(bool visible, bool force = false)
    {
        if (!force && visible == IsUIVisible) return;

        IsUIVisible = visible;
        if (wristCanvasRoot != null) wristCanvasRoot.SetActive(visible);
    }

    private void RefreshTexts()
    {
        if (purificationText != null)
        {
            float purity = PurificationSystem.Instance != null ? PurificationSystem.Instance.Purity : 0f;
            purificationText.text = $"정화도 {Mathf.RoundToInt(purity)}%";
        }

        if (progressText != null)
        {
            float progress = progressProvider != null ? progressProvider.NormalizedProgress * 100f : 0f;
            progressText.text = $"진행도 {Mathf.RoundToInt(progress)}%";
        }
    }
}
