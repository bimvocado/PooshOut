using System;
using System.Collections;
using UnityEngine;

/// <summary>
/// 한강 다이빙 연출 후 최종 점수/등급을 계산하고 엔딩 상태로 전환.
/// </summary>
public class EndingSequence : MonoBehaviour
{
    [SerializeField] private Animator diveAnimator;
    [SerializeField] private float sequenceDuration = 3f;

    public event Action<int, string> OnEndingReady; // (최종 점수, 등급)

    public void Play()
    {
        StartCoroutine(PlayRoutine());
    }

    private IEnumerator PlayRoutine()
    {
        diveAnimator?.SetTrigger("Dive");
        yield return new WaitForSeconds(sequenceDuration);

        float purity = PurificationSystem.Instance != null ? PurificationSystem.Instance.Purity : 0f;
        int finalScore = ScoreManager.CalculateScore(purity);
        string grade = ScoreManager.GetGrade(purity);

        Debug.Log($"[EndingSequence] 최종 점수={finalScore}, 등급={grade}");

        OnEndingReady?.Invoke(finalScore, grade);
        GameManager.Instance?.GoToEnding();
    }
}
