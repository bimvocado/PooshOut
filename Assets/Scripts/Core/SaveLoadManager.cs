using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Text;
using UnityEngine;
using UnityEngine.Networking;

/// <summary>
/// 순위표를 로컬 파일(JSON)로 저장/불러오기 + 서버(FastAPI) 동기화.
/// Application.persistentDataPath 는 어떤 플랫폼(PC/Quest)에서도
/// 안전하게 쓰기 가능한 경로라 여기에 저장함. 로컬 저장이 기본이고,
/// 서버 업로드/조회는 그 위에 추가로 붙인 것 — 서버가 죽어도 로컬 순위표는 항상 동작함.
/// 서버 스펙은 docs/server-api-spec.md 참고.
///
/// ★★★ VR 저장 타이밍 규칙 (중요) ★★★
/// 파일 저장(디스크 쓰기)은 순간적으로 프레임 드랍을 일으킬 수 있고,
/// VR에서 프레임 드랍 = 즉각적인 멀미.
/// 따라서 AddEntry/Save/UploadScore 는 게임 플레이 중(스테이지 진행 중)에 절대 호출 금지.
/// 오직 Stage 4 클리어 후 엔딩 화면으로 전환되는 타이밍(페이드아웃 중)에 딱 1회만 호출할 것.
/// 플레이 중 데이터는 PurificationSystem 등 메모리에만 들고 있기.
/// </summary>
public class SaveLoadManager : Singleton<SaveLoadManager>
{
    // 서버 주소는 ServerConfig 한 곳에서만 관리 (LLMConnector/SaveLoadManager 공용).
    private const string SERVER_URL = ServerConfig.SERVER_URL;
    private const string LeaderboardEndpoint = SERVER_URL + "/leaderboard";

    private const string FileName = "leaderboard.json";

    private string FilePath => Path.Combine(Application.persistentDataPath, FileName);

    private LeaderboardData _cached;

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

    // ---------- 서버 동기화 ----------
    // 기존 로컬 저장(AddEntry/Save)은 그대로 유지. 아래 두 메서드는 그 위에 추가로 붙인 서버 연동.

    /// <summary>
    /// 기록을 로컬에 저장(AddEntry, 기존 동작 그대로)하고 서버에도 업로드 시도.
    /// 서버가 죽어있어도 로컬 저장은 이미 끝난 뒤라 기록 자체는 안전함 — 업로드만 조용히 실패.
    /// </summary>
    public void UploadScore(PlayerData data)
    {
        AddEntry(data);
        StartCoroutine(UploadScoreRoutine(data));
    }

    private IEnumerator UploadScoreRoutine(PlayerData data)
    {
        string jsonBody = JsonUtility.ToJson(data);

        using (UnityWebRequest request = new UnityWebRequest(LeaderboardEndpoint, "POST"))
        {
            byte[] bodyBytes = Encoding.UTF8.GetBytes(jsonBody);
            request.uploadHandler = new UploadHandlerRaw(bodyBytes);
            request.downloadHandler = new DownloadHandlerBuffer();
            request.SetRequestHeader("Content-Type", "application/json");
            request.timeout = 5;

            yield return request.SendWebRequest();

            if (request.result != UnityWebRequest.Result.Success)
            {
                Debug.LogWarning($"[SaveLoadManager] 서버 업로드 실패 (로컬 저장은 완료됨): {request.error}");
                yield break;
            }

            Debug.Log($"[SaveLoadManager] 서버 업로드 완료: {request.downloadHandler.text}");
        }
    }

    /// <summary>
    /// 서버(GET /leaderboard)에서 순위표를 받아와 callback으로 전달.
    /// 서버 요청 실패 시 로컬에 저장된 순위표로 폴백.
    /// </summary>
    public void FetchLeaderboard(Action<List<PlayerData>> callback)
    {
        StartCoroutine(FetchLeaderboardRoutine(callback));
    }

    private IEnumerator FetchLeaderboardRoutine(Action<List<PlayerData>> callback)
    {
        using (UnityWebRequest request = UnityWebRequest.Get(LeaderboardEndpoint))
        {
            request.timeout = 5;
            yield return request.SendWebRequest();

            if (request.result != UnityWebRequest.Result.Success)
            {
                Debug.LogWarning($"[SaveLoadManager] 서버 순위표 조회 실패, 로컬 데이터로 폴백: {request.error}");
                callback?.Invoke(LoadLeaderboard().entries);
                yield break;
            }

            LeaderboardData serverBoard = JsonUtility.FromJson<LeaderboardData>(request.downloadHandler.text);
            if (serverBoard?.entries == null)
            {
                Debug.LogWarning("[SaveLoadManager] 서버 응답 파싱 실패, 로컬 데이터로 폴백");
                callback?.Invoke(LoadLeaderboard().entries);
                yield break;
            }

            callback?.Invoke(serverBoard.entries);
        }
    }
}
