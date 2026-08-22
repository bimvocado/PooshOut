using UnityEngine;

public class EndSceneManager : MonoBehaviour {
    [Header("BGM")]
    [SerializeField] private AudioSource bgmSource;
    [SerializeField] private AudioClip bgmClip;

    [Header("기록 저장")]
    [Tooltip("타이틀에서 칭호를 선택하지 않은 채로 엔딩에 도달한 경우(테스트 플레이 등) 사용할 기본 이름.")]
    [SerializeField] private string fallbackPlayerName = "익명";

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
            Debug.LogWarning("[EndSceneManager] PurificationSystem이 없어서 새 기록 저장은 건너뛰고 서버 순위표만 조회합니다.");
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
}
