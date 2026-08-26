using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;

/// 레버(LeverController)를 당기면 물 내려가는(flush) 소리를 재생하고,
/// 그 소리가 끝난 뒤 다음 씬(Stage1)으로 전환한다.
[RequireComponent(typeof(LeverController))]
public class LeverFlushToStage1 : MonoBehaviour {
    [SerializeField] private LeverController leverController;
    [SerializeField] private AudioSource audioSource;
    [SerializeField] private AudioClip flushClip;
    [SerializeField] private string nextSceneName = "Stage1";

    private bool _triggered;

    private void Reset() {
        leverController = GetComponent<LeverController>();
        audioSource = GetComponent<AudioSource>();
    }

    private void OnEnable() {
        if (leverController == null)
            leverController = GetComponent<LeverController>();

        if (leverController != null)
            leverController.OnLeverPulled.AddListener(HandleLeverPulled);
    }

    private void OnDisable() {
        if (leverController != null)
            leverController.OnLeverPulled.RemoveListener(HandleLeverPulled);
    }

    private void HandleLeverPulled() {
        if (_triggered)
            return;

        _triggered = true;
        StartCoroutine(FlushThenLoadRoutine());
    }

    private IEnumerator FlushThenLoadRoutine() {
        float waitTime = 0f;

        if (audioSource != null && flushClip != null) {
            audioSource.clip = flushClip;
            audioSource.Play();
            waitTime = flushClip.length;
        } else {
            Debug.LogWarning($"{nameof(LeverFlushToStage1)}: audioSource 또는 flushClip이 연결되지 않았습니다.");
        }

        yield return new WaitForSeconds(waitTime);

        if (string.IsNullOrEmpty(nextSceneName)) {
            Debug.LogWarning($"{nameof(LeverFlushToStage1)}: nextSceneName이 비어있어서 씬 전환 안 함.");
            yield break;
        }

        SceneManager.LoadScene(nextSceneName);
    }
}
