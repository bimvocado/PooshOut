using System;
using UnityEngine;

/// 게임 전체 흐름을 관리하는 총괄 매니저.
/// 지금 게임이 어느 상태(타이틀/스테이지 플레이 중/엔딩)인지 들고 있고,
/// 상태가 바뀔 때 OnStateChanged 이벤트로 알려줌.
///
/// + 키 캘리브레이션 결과도 여기서 들고 있음 (Singleton이라 씬 전환에도 안 사라짐).
///   캘리브레이션은 게임 시작 시 1회만 실행하고, 이후 스테이지 씬이 바뀌어도
///   각 씬의 HeadTracker가 여기서 값을 읽어가는 방식으로 재사용함.
public class GameManager : Singleton<GameManager>
{
    // 게임의 큰 흐름 상태
    public enum GameState
    {
        Title,      // 타이틀 화면
        Playing,    // 스테이지 플레이 중
        Ending      // 엔딩/결과 화면
    }

    public GameState CurrentState { get; private set; } = GameState.Title;

    // 상태가 바뀔 때 발행되는 이벤트. UI 등이 구독해서 반응.
    public event Action<GameState> OnStateChanged;

    /// 상태를 바꾸고 구독자들에게 알림.
    public void SetState(GameState newState)
    {
        if (CurrentState == newState) return;

        CurrentState = newState;
        Debug.Log($"[GameManager] 상태 변경 → {newState}");
        OnStateChanged?.Invoke(newState);
    }

    // --- 편의 함수들 (다른 스크립트에서 읽기 쉽게) ---

    public void GoToTitle() => SetState(GameState.Title);
    public void StartPlaying() => SetState(GameState.Playing);
    public void GoToEnding() => SetState(GameState.Ending);

    // --- 키 캘리브레이션 값 (씬 전환에도 유지됨) ---

    /// 캘리브레이션된 키(m). 캘리브레이션 안 됐으면 의미 없는 값이니 IsHeightCalibrated 먼저 확인.
    public float CalibratedHeight { get; private set; }

    /// 이번 플레이(부스 한 판)에서 캘리브레이션이 완료됐는지.
    public bool IsHeightCalibrated { get; private set; }

    /// 캘리브레이션 값이 갱신될 때 발행 (다른 씬의 HeadTracker 등이 구독 가능).
    public event Action<float> OnHeightCalibrated;

    /// CalibrationController가 측정 완료 시 호출.
    public void SetCalibratedHeight(float height)
    {
        CalibratedHeight = height;
        IsHeightCalibrated = true;
        Debug.Log($"[GameManager] 캘리브레이션 값 저장 = {CalibratedHeight:F2}m");
        OnHeightCalibrated?.Invoke(height);
    }

    /// 다음 플레이어를 위해 리셋 (부스에서 다음 사람 플레이 시작할 때 호출).
    public void ResetCalibration()
    {
        IsHeightCalibrated = false;
        CalibratedHeight = 0f;
    }

    // --- 선택된 칭호(플레이어 이름) (씬 전환에도 유지됨) ---

    /// 타이틀 화면에서 선택한 칭호. 선택 전이면 빈 문자열이니 HasPlayerName 먼저 확인.
    public string PlayerName { get; private set; } = string.Empty;

    /// 이번 플레이(부스 한 판)에서 칭호가 선택됐는지.
    public bool HasPlayerName { get; private set; }

    /// 칭호가 선택될 때 발행 (다른 씬의 UI 등이 구독 가능).
    public event Action<string> OnPlayerNameSet;

    /// StartSceneManager가 칭호 선택 시 호출. 이후 스테이지/엔딩 씬이 바뀌어도 여기서 값을 읽어감.
    public void SetPlayerName(string name)
    {
        PlayerName = name;
        HasPlayerName = !string.IsNullOrEmpty(name);
        Debug.Log($"[GameManager] 플레이어 칭호 저장 = {PlayerName}");
        OnPlayerNameSet?.Invoke(PlayerName);
    }

    /// 다음 플레이어를 위해 리셋 (부스에서 다음 사람 플레이 시작할 때 호출).
    public void ResetPlayerName()
    {
        PlayerName = string.Empty;
        HasPlayerName = false;
    }
}