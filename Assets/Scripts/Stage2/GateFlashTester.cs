using System.Collections;
using UnityEngine;

/// ⚠️ 임시 테스트용. 화면 색 플래시만 단독으로 확인하기 위한 스크립트.
///
/// Stage2Manager 없이 PoseGateSpawner의 autoStartOnPlay만으로 씬을 돌릴 때,
/// 게이트 판정 결과를 받아 StageFeedbackFlash를 호출한다.
///
/// 나중에 Stage2Manager를 정식으로 붙이면 이 스크립트는 지우면 된다.
/// (Stage2Manager가 같은 일을 이미 하고 있어서 중복 호출된다)
public class GateFlashTester : MonoBehaviour
{
    [SerializeField] private PoseGateSpawner gateSpawner;
    [SerializeField] private StageFeedbackFlash feedbackFlash;

    [Header("키보드 테스트")]
    [Tooltip("게이트를 기다리지 않고 즉시 색을 확인하고 싶을 때. G=성공(초록), R=실패(빨강)")]
    [SerializeField] private bool enableKeyboardTest = true;

    private readonly System.Collections.Generic.HashSet<PoseGate> _subscribed
        = new System.Collections.Generic.HashSet<PoseGate>();

    private void Start()
    {
        if (gateSpawner == null || feedbackFlash == null)
        {
            Debug.LogWarning("[GateFlashTester] gateSpawner 또는 feedbackFlash가 비어 있다.");
            return;
        }

        StartCoroutine(SubscribeLoop());
    }

    /// 게이트는 시간차를 두고 하나씩 생성되므로, 새로 생긴 것만 골라서 계속 구독한다.
    private IEnumerator SubscribeLoop()
    {
        var wait = new WaitForSeconds(0.2f);

        while (true)
        {
            foreach (PoseGate gate in gateSpawner.SpawnedGates)
            {
                if (gate == null || _subscribed.Contains(gate)) continue;

                _subscribed.Add(gate);
                gate.OnGateResolved += HandleGateResolved;
            }

            yield return wait;
        }
    }

    private void HandleGateResolved(bool success)
    {
        if (success) feedbackFlash.FlashSuccess();
        else feedbackFlash.FlashFail();
    }

    private void Update()
    {
        if (!enableKeyboardTest || feedbackFlash == null) return;

        if (Input.GetKeyDown(KeyCode.G)) feedbackFlash.FlashSuccess();
        if (Input.GetKeyDown(KeyCode.R)) feedbackFlash.FlashFail();
    }
}
