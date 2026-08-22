using System.Collections;
using UnityEngine;

/// <summary>
/// 카메라 앞에 고정된 방사형 스피드라인 쿼드(만화책 집중선 스타일)를 잠깐 반짝였다 지운다.
/// 화면 가장자리에만 선이 보이고 중앙이 비어있는 모양은 쿼드에 입힌 알파 텍스처 자체가
/// 보장하므로(Stage1VFXBuilder가 절차적으로 생성), 이 스크립트는 알파 페이드 타이밍만 담당한다.
/// 파티클처럼 매번 새로 스폰/파괴하지 않고 오브젝트 하나를 계속 재사용한다 - Stage1VFXManager가
/// 최초 1회만 Instantiate하고 이후로는 Flash()만 반복 호출.
/// </summary>
public class SpeedLineFlash : MonoBehaviour
{
    [SerializeField] private float fadeInTime = 0.1f;
    [SerializeField] private float holdTime = 0.4f;
    [SerializeField] private float fadeOutTime = 0.6f;
    [SerializeField] private float peakAlpha = 0.9f;
    [Tooltip("완전히 정적으로 보이지 않도록 반짝이는 동안 천천히 돌려주는 속도(도/초).")]
    [SerializeField] private float rotationSpeedDegPerSec = 25f;

    private static readonly int BaseColorId = Shader.PropertyToID("_BaseColor");

    private Material _material;
    private Coroutine _routine;

    private void Awake()
    {
        _material = GetComponent<MeshRenderer>().material; // 인스턴스화해서 알파를 이 오브젝트만 따로 제어
        SetAlpha(0f);
    }

    private void Update()
    {
        transform.Rotate(0f, 0f, rotationSpeedDegPerSec * Time.deltaTime);
    }

    /// <summary>버블 획득 등 트리거 시 호출. 재생 중에 다시 호출되면 처음부터 다시 반짝인다.</summary>
    public void Flash()
    {
        if (_routine != null) StopCoroutine(_routine);
        _routine = StartCoroutine(FlashRoutine());
    }

    private IEnumerator FlashRoutine()
    {
        yield return Fade(0f, peakAlpha, fadeInTime);
        yield return new WaitForSeconds(holdTime);
        yield return Fade(peakAlpha, 0f, fadeOutTime);
        _routine = null;
    }

    private IEnumerator Fade(float from, float to, float duration)
    {
        if (duration <= 0f)
        {
            SetAlpha(to);
            yield break;
        }

        float elapsed = 0f;
        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            SetAlpha(Mathf.Lerp(from, to, elapsed / duration));
            yield return null;
        }
        SetAlpha(to);
    }

    private void SetAlpha(float alpha)
    {
        Color c = _material.GetColor(BaseColorId);
        c.a = alpha;
        _material.SetColor(BaseColorId, c);
    }
}
