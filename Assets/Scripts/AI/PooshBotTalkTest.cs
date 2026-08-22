using UnityEngine;

public class PooshBotTalkTest : MonoBehaviour
{
    [SerializeField] private Animator animator;
    [SerializeField] private AudioSource voiceSource;
    [SerializeField] private AudioClip testClip;

    void Update()
    {
        animator.SetBool("IsTalking", voiceSource.isPlaying);

        if (Input.GetKeyDown(KeyCode.Space))
        {
            // PlayOneShot 대신 메인 클립으로 넣고 정식으로 Play() 호출
            voiceSource.clip = testClip;
            voiceSource.Play();
        }
    }
}