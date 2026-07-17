using UnityEngine;

/// <summary>
/// 매니저들이 잘 도는지 키보드로 빠르게 확인하는 디버그용 스크립트.
/// 빈 GameObject 에 붙이고 Play 누른 뒤 키를 눌러보면 됨.
/// (실제 게임엔 안 들어감. 최종 빌드 전에는 지울 것)
///
///   1 : 정화도 +10
///   2 : 정화도 -10
///   S : 현재 스코어/등급 출력
///   N : 다음 스테이지로
///   A : 가짜 기록 하나 추가 후 순위 출력
///   C : 순위표 초기화
///
/// v2 수정: ScoreManager 가 static class 로 바뀌면서 .Instance 호출을 전부 제거.
/// </summary>
public class SmokeTest : MonoBehaviour
{
    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.Alpha1))
        {
            PurificationSystem.Instance.Increase(10f);
            Debug.Log($"정화도 = {PurificationSystem.Instance.Purity}");
        }

        if (Input.GetKeyDown(KeyCode.Alpha2))
        {
            PurificationSystem.Instance.Decrease(10f);
            Debug.Log($"정화도 = {PurificationSystem.Instance.Purity}");
        }

        if (Input.GetKeyDown(KeyCode.S))
        {
            float p = PurificationSystem.Instance.Purity;
            string grade = ScoreManager.GetGrade(p);          // ← .Instance 제거
            int score = ScoreManager.CalculateScore(p);        // ← .Instance 제거
            Debug.Log($"점수 = {score}, 등급 = {grade}");
        }

        if (Input.GetKeyDown(KeyCode.N))
        {
            bool isEnd = StageManager.Instance.AdvanceStage();
            if (isEnd) GameManager.Instance.GoToEnding();
        }

        if (Input.GetKeyDown(KeyCode.A))
        {
            float p = PurificationSystem.Instance.Purity;
            string grade = ScoreManager.GetGrade(p);            // ← .Instance 제거
            var entry = new PlayerData("KHW", p, grade);
            SaveLoadManager.Instance.AddEntry(entry);
            int rank = SaveLoadManager.Instance.GetRank(p);
            Debug.Log($"기록 추가됨. 현재 정화도 {p} → {rank}위");
        }

        if (Input.GetKeyDown(KeyCode.C))
        {
            SaveLoadManager.Instance.ClearAll();
        }
    }
}
