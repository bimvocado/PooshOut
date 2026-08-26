using TMPro;
using UnityEngine;

public class StagePuritySummaryUI : MonoBehaviour
{
    [Header("Current Stage")]
    [Tooltip("0이면 StageManager.CurrentStage를 사용합니다.")]
    [SerializeField] private int currentStage = 0;

    [Header("Auto Binding")]
    [SerializeField] private Transform stageScoreGrid;
    [SerializeField] private TMP_Text totalLabelText;
    [SerializeField] private TMP_Text totalValueText;

    [Header("Labels")]
    [SerializeField] private string stageDetailText = "Purity";
    [SerializeField] private string totalLabel = "Total";

    private void Awake()
    {
        Refresh();
    }

    private void OnEnable()
    {
        Refresh();
    }

    public void SetCurrentStage(int stage)
    {
        currentStage = Mathf.Clamp(stage, StageManager.FirstStage, StageManager.LastStage);
        Refresh();
    }

    public void Refresh()
    {
        if (PurificationSystem.Instance == null &&
            FindStage3Manager() == null &&
            FindPurificationManager() == null)
            return;

        ResolveReferences();

        float total = 0f;
        int rowCount = stageScoreGrid != null ? stageScoreGrid.childCount : 0;
        int visibleStage = GetVisibleStage();

        for (int i = 0; i < rowCount; i++)
        {
            Transform row = stageScoreGrid.GetChild(i);
            int stageNumber = i + 1;
            bool visible = stageNumber <= visibleStage;

            row.gameObject.SetActive(visible);
            if (!visible)
                continue;

            float purity = GetStagePurity(stageNumber);
            total += purity;

            TMP_Text[] texts = row.GetComponentsInChildren<TMP_Text>(true);
            if (texts.Length > 0) texts[0].text = $"스테이지{stageNumber}";
            if (texts.Length > 1) texts[1].text = stageDetailText;
            if (texts.Length > 2) texts[2].text = $"{purity:F0}%";
        }

        if (totalLabelText != null) totalLabelText.text = totalLabel;
        if (totalValueText != null) totalValueText.text = $"{total:F0}%";
    }

    private void ResolveReferences()
    {
        if (stageScoreGrid == null)
        {
            Transform scoreRoot = transform.Find("UI-Finish-Score");
            Transform searchRoot = scoreRoot != null ? scoreRoot : transform;
            stageScoreGrid = searchRoot.Find("StageScoreGrid");
        }

        if (totalLabelText == null || totalValueText == null)
        {
            Transform scoreRoot = transform.Find("UI-Finish-Score");
            Transform searchRoot = scoreRoot != null ? scoreRoot : transform;
            Transform result = searchRoot.Find("Result");
            if (result == null) return;

            TMP_Text[] texts = result.GetComponentsInChildren<TMP_Text>(true);
            if (texts.Length > 0 && totalLabelText == null) totalLabelText = texts[0];
            if (texts.Length > 1 && totalValueText == null) totalValueText = texts[1];
        }
    }

    private int GetVisibleStage()
    {
        int stage = currentStage;
        if (stage <= 0 && StageManager.Instance != null)
            stage = StageManager.Instance.CurrentStage;
        else if (stage <= 0 && FindStage3Manager() != null)
            stage = 3;
        else if (stage <= 0 && FindPurificationManager() != null)
            stage = 4;

        return Mathf.Clamp(stage, StageManager.FirstStage, StageManager.LastStage);
    }

    private float GetStagePurity(int stageNumber)
    {
        if (PurificationSystem.Instance != null)
        {
            float savedPurity = PurificationSystem.Instance.GetStagePurity(stageNumber);
            if (stageNumber == 3 && Mathf.Approximately(savedPurity, 0f) && FindStage3Manager() != null)
                return PurificationSystem.Instance.Purity;

            return savedPurity;
        }

        PurificationManager purificationManager = FindPurificationManager();
        if (stageNumber == 4 && purificationManager != null)
            return purificationManager.CurrentPurification;

        return 0f;
    }

    private static PurificationManager FindPurificationManager()
    {
        return PurificationManager.Instance != null
            ? PurificationManager.Instance
            : FindFirstObjectByType<PurificationManager>();
    }

    private static Stage3Manager FindStage3Manager()
    {
        return FindFirstObjectByType<Stage3Manager>();
    }
}
