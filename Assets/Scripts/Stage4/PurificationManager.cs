using UnityEngine;
using UnityEngine.Events;

/// <summary>
/// 정화도(0~100)를 관리하는 매니저.
/// 씬에 하나만 존재하면 됨 (싱글톤 패턴).
/// 
/// 세팅 방법:
/// 1. 빈 오브젝트 만들어서 이 스크립트 추가 (예: "PurificationManager")
/// 2. onMaxPurification 이벤트에 DoorController.OpenDoor() 등을 인스펙터에서 연결
/// 3. UI 텍스트/게이지 바가 있다면 onPurificationChanged 이벤트에 UI 업데이트 함수 연결
/// </summary>
public class PurificationManager : MonoBehaviour
{
    public static PurificationManager Instance { get; private set; }

    [Header("정화도 설정")]
    [SerializeField] private float maxPurification = 100f;
    [SerializeField] private float amountPerSuccess = 10f; // 링 하나 성공 시 오르는 양

    [Header("이벤트")]
    public UnityEvent<float> onPurificationChanged; // 현재값, 0~100 UI 연동용
    public UnityEvent onMaxPurification; // 만점 도달 시 1회 호출 (문 열기 등 연결)

    public float CurrentPurification { get; private set; }
    private bool _hasReachedMax;

    private void Awake()
    {
        // 씬에 하나만 존재하도록 보장
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
    }

    /// <summary>
    /// 링 성공(착지 순간 플레이어가 타일 위에 있었을 때) 시 호출.
    /// </summary>
    public void AddPurification()
    {
        if (_hasReachedMax) return; // 이미 만점이면 더 이상 처리 안 함

        CurrentPurification = Mathf.Clamp(CurrentPurification + amountPerSuccess, 0f, maxPurification);
        onPurificationChanged?.Invoke(CurrentPurification);

        if (CurrentPurification >= maxPurification)
        {
            _hasReachedMax = true;
            onMaxPurification?.Invoke();
        }
    }
}
