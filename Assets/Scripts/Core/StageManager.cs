using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// 현재 몇 번째 스테이지인지 관리하고, 스테이지가 바뀌면 (매핑된 씬이 있는 경우) 그 씬을 로드한다.
/// 씬이 아직 준비 안 된 스테이지는 매핑을 비워두면 번호만 바뀌고 씬 전환은 건너뛴다.
/// </summary>
public class StageManager : Singleton<StageManager>
{
    public const int FirstStage = 1;
    public const int LastStage = 4;

    [Serializable]
    public class StageSceneEntry
    {
        public int stage;
        public string sceneName;
    }

    [Header("스테이지 → 씬 매핑")]
    [Tooltip("스테이지 번호가 바뀔 때 여기 매핑된 씬을 로드한다. " +
             "매핑이 없는 스테이지는 씬 전환 없이 번호만 바뀐다 (아직 씬이 준비 안 된 스테이지용).")]
    [SerializeField]
    private List<StageSceneEntry> stageScenes = new() {
        new StageSceneEntry { stage = 1, sceneName = "Stage1" },
        new StageSceneEntry { stage = 2, sceneName = "Stage2" },
        new StageSceneEntry { stage = 3, sceneName = "Stage3" },
        new StageSceneEntry { stage = 4, sceneName = "Stage4" },
    };

    [Header("독립 실행/테스트")]
    [Tooltip("이 오브젝트가 (이전 씬에서 넘어온 StageManager 없이) 씬의 '첫' StageManager로 활성화될 때 " +
             "시작할 스테이지 번호. 앞 스테이지부터 정상적으로 이어져 들어온 경우엔 이미 있는 " +
             "StageManager가 우선이라 이 값은 무시된다. 씬을 단독으로 열어 바로 Play할 때를 위한 값.")]
    [SerializeField] private int initialStage = FirstStage;

    public int CurrentStage { get; private set; } = FirstStage;

    // 스테이지가 바뀔 때 발행 (UI 안내, 정화봇 진입 멘트 등이 구독)
    public event Action<int> OnStageChanged;

    // 스테이지를 클리어한 순간 발행 (인자는 방금 클리어한 스테이지 번호). 정화봇 클리어 피드백이 구독.
    public event Action<int> OnStageCleared;

    protected override void Awake()
    {
        base.Awake();

        // base.Awake()에서 중복 인스턴스면 이미 Destroy 처리됨.
        // 이 씬의 것이 실제로 살아남은 인스턴스일 때만 초기 스테이지를 적용한다.
        if (Instance == this)
        {
            CurrentStage = Mathf.Clamp(initialStage, FirstStage, LastStage);
        }
    }

    /// <summary>특정 스테이지로 설정하고, 매핑된 씬이 있으면 그 씬을 로드한다.</summary>
    public void SetStage(int stage)
    {
        stage = Mathf.Clamp(stage, FirstStage, LastStage);
        if (stage == CurrentStage) return;

        CurrentStage = stage;
        Debug.Log($"[StageManager] 스테이지 → {CurrentStage}");
        OnStageChanged?.Invoke(CurrentStage);

        LoadSceneForStage(CurrentStage);
    }

    /// <summary>다음 스테이지로. 마지막이면 true 반환(= 엔딩 신호).</summary>
    public bool AdvanceStage()
    {
        int clearedStage = CurrentStage;
        OnStageCleared?.Invoke(clearedStage);

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

    private void LoadSceneForStage(int stage)
    {
        string sceneName = null;

        foreach (var entry in stageScenes)
        {
            if (entry.stage == stage)
            {
                sceneName = entry.sceneName;
                break;
            }
        }

        if (string.IsNullOrEmpty(sceneName))
        {
            Debug.LogWarning($"[StageManager] 스테이지 {stage}에 매핑된 씬이 없어 씬 전환을 건너뜀.");
            return;
        }

        SceneManager.LoadScene(sceneName);
    }
}
