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
/// 4. (선택) increaseAudioSource / increaseSound에 사운드를 연결하면 정화도가 오르는 동안 자동 재생됨
/// 5. showDebugUI 체크박스로 화면 좌상단 디버깅용 수치 표시 여부 조절 가능
/// </summary>
public class PurificationManager : MonoBehaviour {
    public static PurificationManager Instance { get; private set; }

    [Header("정화도 설정")]
    [SerializeField] private float maxPurification = 100f;

    [Header("이벤트")]
    public UnityEvent<float> onPurificationChanged; // 현재값, 0~100 UI 연동용
    public UnityEvent onMaxPurification; // 만점 도달 시 1회 호출 (문 열기 등 연결)

    [Header("사운드 (선택, 나중에 연결 가능)")]
    [SerializeField] private AudioSource increaseAudioSource; // 정화도 상승 중 재생할 AudioSource (루프 사운드 추천)
    [SerializeField] private AudioClip increaseSound;

    [Header("디버깅")]
    [SerializeField] private bool showDebugUI = true;
    [SerializeField] private int debugFontSize = 40; // 디버깅 텍스트 폰트 크기

    public float CurrentPurification { get; private set; }
    private bool _hasReachedMax;

    private void Awake() {
        // 씬에 하나만 존재하도록 보장
        if (Instance != null && Instance != this) {
            Destroy(gameObject);
            return;
        }
        Instance = this;
    }

    /// <summary>
    /// 정화도를 amount만큼 더함. Ring이 매 프레임(Time.deltaTime 기반)으로 호출해서
    /// "타일 위에 있는 시간에 비례해서" 오르는 형태로 사용.
    /// </summary>
    public void AddPurificationAmount(float amount) {
        if (_hasReachedMax || amount <= 0f) return;

        CurrentPurification = Mathf.Clamp(CurrentPurification + amount, 0f, maxPurification);
        onPurificationChanged?.Invoke(CurrentPurification);

        PlayIncreaseSoundIfNeeded();

        if (CurrentPurification >= maxPurification) {
            _hasReachedMax = true;
            onMaxPurification?.Invoke();
        }
    }

    /// <summary>
    /// 정화도를 amount만큼 깎음. Ring이 매 프레임 호출해서
    /// "타일 밖에 있는 시간에 비례해서" 떨어지는 형태로 사용.
    /// 이미 만점을 찍어서 문이 열린 상태라면 더 이상 깎지 않음.
    /// </summary>
    public void DecreasePurificationAmount(float amount) {
        if (_hasReachedMax || amount <= 0f) return;

        CurrentPurification = Mathf.Clamp(CurrentPurification - amount, 0f, maxPurification);
        onPurificationChanged?.Invoke(CurrentPurification);

        StopIncreaseSound();
    }

    /// <summary>
    /// 정화도가 더 이상 오르지 않을 때(플레이어가 타일에서 벗어났을 때 등) 호출해서
    /// 상승 사운드를 멈추고 싶을 때 사용. 지금은 비어있고, 사운드 연결 시 채우면 됨.
    /// </summary>
    public void StopIncreaseSound() {
        if (increaseAudioSource != null && increaseAudioSource.isPlaying) {
            increaseAudioSource.Stop();
        }
    }

    private void PlayIncreaseSoundIfNeeded() {
        if (increaseAudioSource == null || increaseSound == null) return;

        if (!increaseAudioSource.isPlaying) {
            increaseAudioSource.clip = increaseSound;
            increaseAudioSource.loop = true;
            increaseAudioSource.Play();
        }
    }

    private void OnGUI() {
        if (!showDebugUI) return;

        GUIStyle style = new GUIStyle(GUI.skin.label) {
            fontSize = debugFontSize,
            normal = { textColor = Color.white }
        };

        GUI.Label(new Rect(20, 20, 500, debugFontSize + 20), $"정화도: {CurrentPurification:F0} / {maxPurification:F0}", style);
    }
}