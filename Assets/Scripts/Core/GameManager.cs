using System;
using UnityEngine;

/// <summary>
/// 게임 전체 흐름을 관리하는 총괄 매니저.
/// 지금 게임이 어느 상태(타이틀/스테이지 플레이 중/엔딩)인지 들고 있고,
/// 상태가 바뀔 때 OnStateChanged 이벤트로 알려줌.
/// </summary>
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

    /// <summary>상태를 바꾸고 구독자들에게 알림.</summary>
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
}
