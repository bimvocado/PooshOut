using System;
using UnityEngine;

/// <summary>
/// 현재 몇 번째 스테이지인지 관리.
/// 실제 씬 로딩은 나중에 씬이 만들어지면 붙일 예정이라,
/// 지금은 "스테이지 번호"만 관리하는 최소 버전.
/// </summary>
public class StageManager : Singleton<StageManager>
{
    public const int FirstStage = 1;
    public const int LastStage = 4;

    public int CurrentStage { get; private set; } = FirstStage;

    // 스테이지가 바뀔 때 발행 (UI 안내, 정화봇 진입 멘트 등이 나중에 구독)
    public event Action<int> OnStageChanged;

    /// <summary>특정 스테이지로 설정.</summary>
    public void SetStage(int stage)
    {
        stage = Mathf.Clamp(stage, FirstStage, LastStage);
        if (stage == CurrentStage) return;

        CurrentStage = stage;
        Debug.Log($"[StageManager] 스테이지 → {CurrentStage}");
        OnStageChanged?.Invoke(CurrentStage);
    }

    /// <summary>다음 스테이지로. 마지막이면 true 반환(= 엔딩 신호).</summary>
    public bool AdvanceStage()
    {
        if (CurrentStage >= LastStage)
        {
            Debug.Log("[StageManager] 마지막 스테이지 클리어 → 엔딩");
            return true;
        }
        SetStage(CurrentStage + 1);
        return false;
    }

    /// <summary>처음 스테이지로 리셋 (재시작용).</summary>
    public void ResetToFirst()
    {
        CurrentStage = FirstStage;
        OnStageChanged?.Invoke(CurrentStage);
    }
}
