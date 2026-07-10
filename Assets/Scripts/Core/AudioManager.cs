using UnityEngine;

/// <summary>
/// BGM / 효과음(SFX) 재생을 담당.
/// 지금은 사운드 파일이 없어도 구조만 잡아둠. (파일은 나중에 인스펙터에서 연결)
/// 다른 스크립트에서 AudioManager.Instance.PlaySfx(clip) 식으로 호출.
/// </summary>
public class AudioManager : MonoBehaviour
{
    public static AudioManager Instance { get; private set; }

    [SerializeField] private AudioSource bgmSource; // 배경음 재생용 (루프)
    [SerializeField] private AudioSource sfxSource; // 효과음 재생용 (원샷)

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
            return;
        }

        // AudioSource 가 인스펙터에서 연결 안 됐으면 자동 생성 (초보 안전장치)
        if (bgmSource == null)
        {
            bgmSource = gameObject.AddComponent<AudioSource>();
            bgmSource.loop = true;
            bgmSource.playOnAwake = false;
        }
        if (sfxSource == null)
        {
            sfxSource = gameObject.AddComponent<AudioSource>();
            sfxSource.loop = false;
            sfxSource.playOnAwake = false;
        }
    }

    /// <summary>배경음 재생 (기존 BGM은 멈추고 교체).</summary>
    public void PlayBgm(AudioClip clip)
    {
        if (clip == null) return;
        bgmSource.clip = clip;
        bgmSource.Play();
    }

    public void StopBgm() => bgmSource.Stop();

    /// <summary>효과음 한 번 재생 (여러 개 겹쳐도 됨).</summary>
    public void PlaySfx(AudioClip clip)
    {
        if (clip == null) return;
        sfxSource.PlayOneShot(clip);
    }

    /// <summary>전체 볼륨 조절 (0~1). 설정 메뉴용.</summary>
    public void SetVolume(float volume)
    {
        AudioListener.volume = Mathf.Clamp01(volume);
    }
}
