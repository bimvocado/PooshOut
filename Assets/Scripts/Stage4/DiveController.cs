using System;
using UnityEngine;

/// <summary>
/// 점프(머리 높이 급상승) 또는 컨트롤러 버튼 입력으로 최종 다이빙을 트리거.
/// Stage4Manager가 한강 진입 지점에서 ArmDive()를 호출해 다이빙을 활성화한다.
/// </summary>
public class DiveController : MonoBehaviour
{
    [SerializeField] private JumpDetector jumpDetector;
    [SerializeField] private XRInputHandler xrInput;
    [SerializeField] private bool allowButtonDive = true;

    public event Action OnDiveTriggered;
    public bool DiveArmed { get; private set; }
    public bool HasDived { get; private set; }

    private void OnEnable()
    {
        if (jumpDetector != null) jumpDetector.OnJumpDetected += HandleJump;
    }

    private void OnDisable()
    {
        if (jumpDetector != null) jumpDetector.OnJumpDetected -= HandleJump;
    }

    /// <summary>다이빙 판정 구간(한강 진입 지점)에 도달했을 때 호출.</summary>
    public void ArmDive()
    {
        DiveArmed = true;
        HasDived = false;
    }

    private void Update()
    {
        if (!DiveArmed || HasDived) return;

        if (allowButtonDive && xrInput != null && xrInput.GetTriggerPressed())
        {
            TriggerDive();
        }
    }

    private void HandleJump()
    {
        if (!DiveArmed || HasDived) return;
        TriggerDive();
    }

    private void TriggerDive()
    {
        HasDived = true;
        DiveArmed = false;
        Debug.Log("[DiveController] 다이빙 트리거됨");
        OnDiveTriggered?.Invoke();
    }
}
