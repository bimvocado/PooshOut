using UnityEngine;

// Drives the microbe's Animator into the (looping) Wave state automatically,
// for the front-facing cutscene camera shot. Nothing else triggers "Wave",
// so without this the animation never plays.
public class MicrobeWaveAutoPlay : MonoBehaviour {
    [SerializeField] private Animator animator;
    [SerializeField] private string waveTrigger = "Hit";

    private void Reset() {
        animator = GetComponent<Animator>();
    }

    private void Start() {
        if (animator == null) {
            animator = GetComponent<Animator>();
        }

        if (animator == null) {
            Debug.LogWarning($"{nameof(MicrobeWaveAutoPlay)}: no Animator found on {name}.");
            return;
        }

        animator.SetTrigger(waveTrigger);
    }
}
