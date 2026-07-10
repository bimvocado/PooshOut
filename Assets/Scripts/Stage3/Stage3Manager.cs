using UnityEngine;

/// <summary>
/// 스테이지 3 (폭기조) 전체 흐름 제어.
/// 펌프질로 산소를 공급하면서 미생물을 두더지잡기 방식으로 분해시킨다.
/// 목표 분해 수를 채우면 다음 스테이지로 이동.
/// </summary>
public class Stage3Manager : MonoBehaviour
{
    private const int StageNumber = 3;

    [SerializeField] private PumpController pumpController;
    [SerializeField] private MicrobeSpawner microbeSpawner;
    [SerializeField] private int decomposeTarget = 15;

    private void OnEnable()
    {
        if (StageManager.Instance != null)
            StageManager.Instance.OnStageChanged += HandleStageChanged;
    }

    private void OnDisable()
    {
        if (StageManager.Instance != null)
            StageManager.Instance.OnStageChanged -= HandleStageChanged;
    }

    private void Start()
    {
        if (StageManager.Instance != null && StageManager.Instance.CurrentStage == StageNumber)
        {
            HandleStageChanged(StageNumber);
        }
    }

    private void HandleStageChanged(int stage)
    {
        if (stage != StageNumber)
        {
            microbeSpawner?.StopSpawning();
            return;
        }
        StartStage();
    }

    private void StartStage()
    {
        Debug.Log("[Stage3Manager] 스테이지 3 시작");
        pumpController?.ResetCount();
        microbeSpawner?.StartSpawning();
    }

    private void Update()
    {
        if (StageManager.Instance == null || StageManager.Instance.CurrentStage != StageNumber) return;
        if (microbeSpawner == null) return;

        if (microbeSpawner.DecomposedCount >= decomposeTarget)
        {
            microbeSpawner.StopSpawning();
            Debug.Log("[Stage3Manager] 목표 분해량 달성 - 다음 스테이지로");
            StageManager.Instance.AdvanceStage();
        }
    }
}
