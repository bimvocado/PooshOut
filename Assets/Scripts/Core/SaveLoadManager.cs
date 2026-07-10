using System.IO;
using UnityEngine;

/// <summary>
/// 순위표를 로컬 파일(JSON)로 저장/불러오기.
/// Application.persistentDataPath 는 어떤 플랫폼(PC/Quest)에서도
/// 안전하게 쓰기 가능한 경로라 여기에 저장함.
///
/// ★★★ VR 저장 타이밍 규칙 (중요) ★★★
/// 파일 저장(디스크 쓰기)은 순간적으로 프레임 드랍을 일으킬 수 있고,
/// VR에서 프레임 드랍 = 즉각적인 멀미.
/// 따라서 AddEntry/Save 는 게임 플레이 중(스테이지 진행 중)에 절대 호출 금지.
/// 오직 Stage 4 클리어 후 엔딩 화면으로 전환되는 타이밍(페이드아웃 중)에 딱 1회만 호출할 것.
/// 플레이 중 데이터는 PurificationSystem 등 메모리에만 들고 있기.
/// </summary>
public class SaveLoadManager : MonoBehaviour
{
    public static SaveLoadManager Instance { get; private set; }

    private const string FileName = "leaderboard.json";
    private string FilePath => Path.Combine(Application.persistentDataPath, FileName);
    private LeaderboardData _cached;

    private void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);
    }
    /// <summary>순위표 불러오기 (파일 없으면 빈 순위표 생성).</summary>
    public LeaderboardData LoadLeaderboard()
    {
        if (_cached != null) return _cached;

        if (File.Exists(FilePath))
        {
            string json = File.ReadAllText(FilePath);
            _cached = JsonUtility.FromJson<LeaderboardData>(json);
            if (_cached == null) _cached = new LeaderboardData();
        }
        else
        {
            _cached = new LeaderboardData();
        }
        return _cached;
    }
    /// <summary>기록 하나 추가하고 저장. 추가 후 정화도 내림차순 정렬.</summary>
    public void AddEntry(PlayerData entry)
    {
        LeaderboardData board = LoadLeaderboard();
        board.entries.Add(entry);
        // 정화도 높은 순으로 정렬 (내림차순)
        board.entries.Sort((a, b) => b.purity.CompareTo(a.purity));
        Save();
    }

    /// <summary>현재 순위표를 파일에 기록.</summary>
    public void Save()
    {
        if (_cached == null) _cached = new LeaderboardData();
        string json = JsonUtility.ToJson(_cached, true); // true = 사람이 읽기 좋게 들여쓰기
        File.WriteAllText(FilePath, json);
        Debug.Log($"[SaveLoadManager] 저장 완료 → {FilePath}");
    }

    /// <summary>특정 정화도가 현재 순위표에서 몇 위인지 계산 (1-based).</summary>
    public int GetRank(float purity)
    {
        LeaderboardData board = LoadLeaderboard();
        int rank = 1;
        foreach (var e in board.entries)
        {
            if (e.purity > purity) rank++;
        }
        return rank;
    }

    /// <summary>순위표 전체 삭제 (테스트/부스 리셋용).</summary>
    public void ClearAll()
    {
        _cached = new LeaderboardData();
        if (File.Exists(FilePath)) File.Delete(FilePath);
        Debug.Log("[SaveLoadManager] 순위표 초기화됨");
    }
}