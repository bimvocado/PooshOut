using System.Collections.Generic;
using TMPro;
using UnityEngine;

public class EndLeaderboardUI : MonoBehaviour
{
    [Header("My Result")]
    [SerializeField] private TMP_Text myNameText;
    [SerializeField] private TMP_Text myPurityText;
    [SerializeField] private TMP_Text myGradeText;
    [SerializeField] private TMP_Text myRankText;

    [Header("Leaderboard")]
    [SerializeField] private TMP_Text[] rankTexts;
    [SerializeField] private TMP_Text[] nameTexts;
    [SerializeField] private TMP_Text[] purityTexts;
    [SerializeField] private TMP_Text[] gradeTexts;

    [Header("Status")]
    [SerializeField] private TMP_Text statusText;
    [SerializeField] private bool refreshOnEnable = true;
    [SerializeField] private string loadingText = "\uBD88\uB7EC\uC624\uB294 \uC911...";
    [SerializeField] private string emptyText = "\uC544\uC9C1 \uAE30\uB85D\uC774 \uC5C6\uC2B5\uB2C8\uB2E4.";

    private void Awake()
    {
        ClearMyResult();
        ClearLeaderboardRows();
        SetStatus("");
    }

    private void OnEnable()
    {
        if (refreshOnEnable)
            Refresh();
    }

    public void Refresh()
    {
        if (SaveLoadManager.Instance == null)
        {
            SetStatus("SaveLoadManager missing");
            return;
        }

        SetStatus(loadingText);
        SaveLoadManager.Instance.FetchLeaderboard(ApplyLeaderboard);
    }

    public void ShowMyResult(PlayerData data, int rank)
    {
        if (data == null)
            return;

        string displayName = string.IsNullOrEmpty(data.displayName)
            ? data.playerName
            : data.displayName;

        if (myNameText != null) myNameText.text = displayName;
        if (myPurityText != null) myPurityText.text = FormatPurity(data.purity);
        if (myGradeText != null) myGradeText.text = data.grade;
        if (myRankText != null) myRankText.text = rank > 0 ? FormatRank(rank) : "-";
    }

    private void ApplyLeaderboard(List<PlayerData> entries)
    {
        int count = entries?.Count ?? 0;
        int rowCount = Mathf.Max(
            rankTexts?.Length ?? 0,
            nameTexts?.Length ?? 0,
            purityTexts?.Length ?? 0,
            gradeTexts?.Length ?? 0
        );

        for (int i = 0; i < rowCount; i++)
        {
            PlayerData entry = i < count ? entries[i] : null;
            string displayName = entry == null
                ? ""
                : string.IsNullOrEmpty(entry.displayName) ? entry.playerName : entry.displayName;

            SetText(rankTexts, i, entry == null ? "" : FormatRank(i + 1));
            SetText(nameTexts, i, displayName);
            SetText(purityTexts, i, entry == null ? "" : FormatPurity(entry.purity));
            SetText(gradeTexts, i, entry == null ? "" : entry.grade);
        }

        SetStatus(count == 0 ? emptyText : "");
    }

    private void ClearMyResult()
    {
        if (myNameText != null) myNameText.text = "";
        if (myPurityText != null) myPurityText.text = "";
        if (myGradeText != null) myGradeText.text = "";
        if (myRankText != null) myRankText.text = "";
    }

    private void ClearLeaderboardRows()
    {
        int rowCount = Mathf.Max(
            rankTexts?.Length ?? 0,
            nameTexts?.Length ?? 0,
            purityTexts?.Length ?? 0,
            gradeTexts?.Length ?? 0
        );

        for (int i = 0; i < rowCount; i++)
        {
            SetText(rankTexts, i, "");
            SetText(nameTexts, i, "");
            SetText(purityTexts, i, "");
            SetText(gradeTexts, i, "");
        }
    }

    private static string FormatRank(int rank)
    {
        return $"{rank}\uC704";
    }

    private static string FormatPurity(float purity)
    {
        return $"\uC815\uD654\uB3C4 {purity:F0}%";
    }

    private void SetStatus(string text)
    {
        if (statusText != null) statusText.text = text;
    }

    private static void SetText(TMP_Text[] texts, int index, string value)
    {
        if (texts == null || index < 0 || index >= texts.Length || texts[index] == null)
            return;

        texts[index].text = value;
    }
}
