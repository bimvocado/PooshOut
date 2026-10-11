using UnityEngine;

public class EndSceneManager : MonoBehaviour {
    [Header("BGM")]
    [SerializeField] private AudioSource bgmSource;
    [SerializeField] private AudioClip bgmClip;

    [Header("기록 저장")]
    [Tooltip("타이틀에서 칭호를 선택하지 않은 채로 엔딩에 도달한 경우(테스트 플레이 등) 사용할 기본 이름.")]
    [SerializeField] private string fallbackPlayerName = "익명";

    [Tooltip("PurificationSystem이 없는 상태로(예: 이 씬만 단독 Play) 엔딩에 도달했을 때 '내 결과' UI에 대신 보여줄 더미 이름. DebugLeaderboard.json의 7등 항목과 이름·정화도를 맞춰서, 순위표에서도 내 순위가 실제로 채워지는 것처럼 보이게 한다.")]
    [SerializeField] private string fallbackMyResultName = "하수맨";

    [Tooltip("위 더미 이름과 짝을 맞출 더미 정화도 (DebugLeaderboard.json 7등 값과 동일하게).")]
    [SerializeField] private float fallbackPurity = 454f;

    [Tooltip("위 더미가 DebugLeaderboard.json에서 몇 등인지 (순위 표시용).")]
    [SerializeField] private int fallbackRank = 7;

    [Tooltip("서버가 중복 닉네임을 구분할 때 붙이는 '#번호' 표시용. DebugLeaderboard.json 7등 항목의 번호(#1)와 동일하게.")]
    [SerializeField] private int fallbackMyResultNumber = 1;

    [Header("결과 UI")]
    [SerializeField] private EndLeaderboardUI leaderboardUI;

    private bool _scoreSubmitted;

    private void Start() {
        PlayBGM();
        leaderboardUI?.Refresh();
        SubmitScore();
    }

    private void PlayBGM() {
        if (bgmSource == null || bgmClip == null) {
            Debug.LogWarning("BGM AudioSource 또는 AudioClip이 설정되지 않았습니다.");
            return;
        }

        bgmSource.clip = bgmClip;
        bgmSource.loop = true;
        bgmSource.Play();
    }

    /// <summary>
    /// Stage4 클리어 → 엔딩 씬 진입 시 딱 1회 호출.
    /// GameManager에 저장된 칭호(StartSceneManager에서 선택) + PurificationSystem의 최종 정화도로
    /// PlayerData를 만들어 SaveLoadManager.UploadScore(로컬 저장 + 서버 업로드)에 넘긴다.
    /// SaveLoadManager의 "VR 저장 타이밍 규칙"에 따라 여기(엔딩 화면 전환 시점)에서만 호출해야 함.
    /// </summary>
    private void SubmitScore() {
        if (_scoreSubmitted) return;
        _scoreSubmitted = true;

        if (SaveLoadManager.Instance == null) {
            Debug.LogWarning("[EndSceneManager] SaveLoadManager가 없어서 기록을 저장/조회하지 못했습니다.");
            return;
        }
        if (PurificationSystem.Instance == null) {
            Debug.LogWarning("[EndSceneManager] PurificationSystem이 없어서 새 기록 저장은 건너뛰고, '내 결과'는 더미 데이터로 대신 보여줍니다.");
            ShowFallbackMyResult();
            leaderboardUI?.Refresh();
            return;
        }

        string playerName = GameManager.Instance != null && GameManager.Instance.HasPlayerName
            ? GameManager.Instance.PlayerName
            : fallbackPlayerName;

        float purity = PurificationSystem.Instance.Purity;
        string grade = ScoreManager.GetGrade(purity);

        var data = new PlayerData(playerName, purity, grade);
        int localRank = SaveLoadManager.Instance.GetRank(purity);
        leaderboardUI?.ShowMyResult(data, localRank);

        SaveLoadManager.Instance.UploadScore(data, result =>
        {
            if (result != null)
            {
                data.displayName = result.displayName;
                leaderboardUI?.ShowMyResult(data, result.rank);
            }

            leaderboardUI?.Refresh();
        });

        Debug.Log($"[EndSceneManager] 최종 기록 저장 - 이름={playerName}, 정화도={purity}, 등급={grade}");
    }

    /// <summary>
    /// PurificationSystem이 없어서 실제 정화도를 알 수 없을 때(이 씬만 단독 Play 등)
    /// '내 결과' UI가 비어 보이지 않도록 더미 값으로 채운다.
    /// DebugLeaderboard.json의 fallbackRank등 항목(fallbackMyResultName)과 이름·정화도를 맞춰서,
    /// 순위표 목록 + 내 결과가 서로 어긋나지 않고 실제로 순위가 채워진 것처럼 보이게 한다.
    /// </summary>
    private void ShowFallbackMyResult() {
        bool hasRealName = GameManager.Instance != null && GameManager.Instance.HasPlayerName;
        string playerName = hasRealName ? GameManager.Instance.PlayerName : fallbackMyResultName;

        var dummy = new PlayerData(playerName, fallbackPurity, ScoreManager.GetGrade(fallbackPurity));

        // 실제 선택된 칭호가 아니라 더미일 때만 DebugLeaderboard.json과 맞춘 "#번호"를 붙인다.
        // 순위표 쪽 번호 표기(3자리, 앞에 0 채움)와 형식을 맞춘다.
        if (!hasRealName)
            dummy.displayName = $"{playerName}#{fallbackMyResultNumber:D3}";

        leaderboardUI?.ShowMyResult(dummy, fallbackRank);
    }
}
