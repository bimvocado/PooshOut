using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

public class StartLeaderboardUI : MonoBehaviour
{
    [SerializeField] private TMP_Text[] rankTexts;
    [SerializeField] private TMP_Text[] nameTexts;
    [SerializeField] private TMP_Text[] purityTexts;
    [SerializeField] private TMP_Text statusText;
    [SerializeField] private int maxEntries = 6;
    [SerializeField] private bool refreshOnEnable = true;
    [SerializeField] private string loadingText = "\uBD88\uB7EC\uC624\uB294 \uC911...";
    [SerializeField] private string emptyText = "\uC544\uC9C1 \uAE30\uB85D\uC774 \uC5C6\uC2B5\uB2C8\uB2E4.";
    [SerializeField] private bool useDebugLeaderboardWhenEmpty = true;
    [SerializeField] private TextAsset debugLeaderboardJson;

    private Coroutine refreshRoutine;

    private void Awake()
    {
        ClearRows();
        SetStatus("");
    }

    private void OnEnable()
    {
        if (refreshOnEnable)
            QueueRefresh();
    }

    private void Start()
    {
        if (refreshOnEnable)
            QueueRefresh();
    }

    public void Refresh()
    {
        QueueRefresh();
    }

    private void QueueRefresh()
    {
        if (!isActiveAndEnabled)
            return;

        if (refreshRoutine != null)
            StopCoroutine(refreshRoutine);

        refreshRoutine = StartCoroutine(RefreshWhenReady());
    }

    private IEnumerator RefreshWhenReady()
    {
        SetStatus(loadingText);

        const float timeoutSeconds = 5f;
        float deadline = Time.unscaledTime + timeoutSeconds;
        while (SaveLoadManager.Instance == null && Time.unscaledTime < deadline)
            yield return null;

        if (SaveLoadManager.Instance == null)
        {
            SetStatus("SaveLoadManager missing");
            refreshRoutine = null;
            yield break;
        }

        SaveLoadManager.Instance.FetchLeaderboard(ApplyLeaderboard);
        refreshRoutine = null;
    }

    private void ApplyLeaderboard(List<PlayerData> entries)
    {
        if ((entries == null || entries.Count == 0) && useDebugLeaderboardWhenEmpty)
            entries = LoadDebugLeaderboard();

        int availableCount = entries?.Count ?? 0;
        int displayCount = Mathf.Min(availableCount, Mathf.Max(0, maxEntries));
        int rowCount = Mathf.Min(GetRowCount(), Mathf.Max(0, maxEntries));

        for (int i = 0; i < rowCount; i++)
        {
            PlayerData entry = i < displayCount ? entries[i] : null;
            string displayName = GetDisplayName(entry);

            SetText(rankTexts, i, entry == null ? "" : FormatRank(i + 1));
            SetText(nameTexts, i, displayName);
            SetText(purityTexts, i, entry == null ? "" : FormatPurity(entry.purity));
        }

        SetStatus(displayCount == 0 ? emptyText : "");
    }

    private List<PlayerData> LoadDebugLeaderboard()
    {
        TextAsset source = debugLeaderboardJson != null
            ? debugLeaderboardJson
            : Resources.Load<TextAsset>("DebugLeaderboard");

        if (source == null)
            return null;

        LeaderboardData data = JsonUtility.FromJson<LeaderboardData>(source.text);
        return data?.entries;
    }

    private void ClearRows()
    {
        int rowCount = GetRowCount();
        for (int i = 0; i < rowCount; i++)
        {
            SetText(rankTexts, i, "");
            SetText(nameTexts, i, "");
            SetText(purityTexts, i, "");
        }
    }

    private int GetRowCount()
    {
        return Mathf.Max(rankTexts?.Length ?? 0, nameTexts?.Length ?? 0, purityTexts?.Length ?? 0);
    }

    private static string GetDisplayName(PlayerData entry)
    {
        if (entry == null)
            return "";

        return string.IsNullOrEmpty(entry.displayName) ? entry.playerName : entry.displayName;
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
        if (statusText != null)
            statusText.text = text;
    }

    private static void SetText(TMP_Text[] texts, int index, string value)
    {
        if (texts == null || index < 0 || index >= texts.Length || texts[index] == null)
            return;

        texts[index].text = value;
    }
}
