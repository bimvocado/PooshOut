using UnityEngine;

/// <summary>
/// 정화도 → 등급/점수 계산. (순수 계산기)
///
/// v2 변경: MonoBehaviour + 싱글톤 → static class 로 전환.
/// 이유: 씬에 오브젝트로 존재할 필요가 없는 단순 계산 로직이라,
///       static이 메모리도 아끼고 어디서든 ScoreManager.GetGrade(85f) 로 바로 호출 가능.
/// 사용법이 바뀜: ScoreManager.Instance.GetGrade(x) → ScoreManager.GetGrade(x)
/// </summary>
public static class ScoreManager
{
    public const float GoldThreshold = 90f;

    public const string GradeGold = "일급수 황금 물방울";
    public const string GradeArisu = "아리수 물방울";

    /// <summary>정화도로 등급 문자열 반환.</summary>
    public static string GetGrade(float purity)
    {
        return purity >= GoldThreshold ? GradeGold : GradeArisu;
    }

    /// <summary>최종 점수 계산. 지금은 정화도가 곧 점수(단일 기준).</summary>
    public static int CalculateScore(float purity)
    {
        return Mathf.RoundToInt(purity);
    }
}
