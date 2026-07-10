using System;
using UnityEngine;

public class GameManager : MonoBehaviour
{
    public static GameManager Instance { get; private set; }

    public enum GameState { Title, Playing, Ending }
    public GameState CurrentState { get; private set; } = GameState.Title;

    public event Action<GameState> OnStateChanged;

    private void Awake()
    {
        // 독립형 싱글톤 세팅
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject); // 씬이 넘어가도 파괴되지 않음
        }
        else
        {
            Destroy(gameObject);
        }
    }

    public void SetState(GameState newState)
    {
        if (CurrentState == newState) return;

        CurrentState = newState;
        Debug.Log($"[GameManager] 상태 변경 → {newState}");
        OnStateChanged?.Invoke(newState);
    }

    public void GoToTitle() => SetState(GameState.Title);
    public void StartPlaying() => SetState(GameState.Playing);
    public void GoToEnding() => SetState(GameState.Ending);
}