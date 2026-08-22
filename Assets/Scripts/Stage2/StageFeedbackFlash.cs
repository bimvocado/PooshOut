using System.Collections;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

/// 게이트 성공/실패 순간에 화면 전체를 초록/빨강으로 잠깐 물들였다가 되돌린다.
/// Global Volume의 Color Adjustments > Color Filter를 사용하므로
/// 씬에 Volume이 있고 Color Adjustments override가 추가돼 있어야 동작한다.
public class StageFeedbackFlash : MonoBehaviour
{
    [Header("연결")]
    [Tooltip("씬의 Global Volume. Color Adjustments override가 켜져 있어야 한다.")]
    [SerializeField] private Volume globalVolume;

    [Header("색상")]
    [SerializeField] private Color successColor = new Color(0.35f, 1f, 0.4f);
    [SerializeField] private Color failColor = new Color(1f, 0.35f, 0.35f);

    [Header("타이밍(초)")]
    [Tooltip("색이 들어오는 시간. 아이 대상이라 너무 짧으면 번쩍여서 부담된다.")]
    [SerializeField] private float flashInTime = 0.15f;

    [Tooltip("색이 빠지는 시간. 들어올 때보다 길어야 부드럽다.")]
    [SerializeField] private float flashOutTime = 0.35f;

    private ColorAdjustments _colorAdjustments;
    private Coroutine _running;

    private void Awake()
    {
        if (globalVolume == null)
        {
            Debug.LogWarning("[StageFeedbackFlash] Global Volume이 비어 있어 플래시가 동작하지 않는다.");
            return;
        }

        if (!globalVolume.profile.TryGet(out _colorAdjustments))
        {
            Debug.LogWarning("[StageFeedbackFlash] Volume Profile에 Color Adjustments override가 없다. Add Override로 추가할 것.");
        }
    }

    public void FlashSuccess() => Flash(successColor);

    public void FlashFail() => Flash(failColor);

    private void Flash(Color target)
    {
        if (_colorAdjustments == null) return;

        // 게이트가 연속으로 판정될 때 이전 플래시가 남아 색이 겹치지 않도록 끊고 다시 시작.
        if (_running != null) StopCoroutine(_running);
        _running = StartCoroutine(FlashRoutine(target));
    }

    private IEnumerator FlashRoutine(Color target)
    {
        yield return Tween(_colorAdjustments.colorFilter.value, target, flashInTime);
        yield return Tween(target, Color.white, flashOutTime);
        _running = null;
    }

    private IEnumerator Tween(Color from, Color to, float duration)
    {
        if (duration <= 0f)
        {
            _colorAdjustments.colorFilter.value = to;
            yield break;
        }

        float elapsed = 0f;
        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            _colorAdjustments.colorFilter.value = Color.Lerp(from, to, elapsed / duration);
            yield return null;
        }

        _colorAdjustments.colorFilter.value = to;
    }

    private void OnDisable()
    {
        // 씬 전환 중 색이 물든 채로 남는 것 방지.
        if (_colorAdjustments != null)
            _colorAdjustments.colorFilter.value = Color.white;
    }
}
