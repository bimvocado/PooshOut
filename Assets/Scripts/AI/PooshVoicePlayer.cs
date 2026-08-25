using System;
using System.Collections;
using UnityEngine;
using UnityEngine.Networking;

/// 정화봇 음성 재생기.
/// 서버(/chat, /feedback 등)가 돌려주는 audioUrl(wav)을 다운로드해서 AudioSource로 재생한다.
///
/// 사용법:
///   1. 씬의 정화봇 오브젝트(또는 _LLMTest)에 이 스크립트 추가
///   2. AudioSource가 없으면 자동으로 붙음
///   3. LLMConnector 응답을 받은 곳에서 PooshVoicePlayer.Instance.PlayFromUrl(audioUrl) 호출
///
/// 서버 응답에서 audioUrl이 null이면(TTS 실패/미설정) 조용히 무시하고 텍스트만 표시되게 둔다.
///
/// ※ 오디오 포맷: Typecast는 wav를 돌려주므로 AudioType.WAV로 요청한다.
///   TTS 서비스를 바꿔서 mp3를 받게 되면 Inspector에서 Audio Type을 MPEG로 바꾸면 된다.
[RequireComponent(typeof(AudioSource))]
public class PooshVoicePlayer : Singleton<PooshVoicePlayer>
{
    [Header("오디오 포맷")]
    [Tooltip("서버가 돌려주는 음성 파일 형식. Typecast=WAV, mp3를 주는 서비스로 바꾸면 MPEG.")]
    [SerializeField] private AudioType audioType = AudioType.WAV;

    [Tooltip("음성 다운로드 제한 시간(초).")]
    [SerializeField] private int downloadTimeout = 10;

    private AudioSource _audioSource;

    // PooshBotAnimator가 IsTalking을 판단할 때 이걸 봄
    public bool IsPlaying => _audioSource != null && _audioSource.isPlaying;

    protected override void Awake()
    {
        base.Awake();

        _audioSource = GetComponent<AudioSource>();
        if (_audioSource == null)
        {
            _audioSource = gameObject.AddComponent<AudioSource>();
        }
        _audioSource.playOnAwake = false;
        _audioSource.spatialBlend = 0f; // 2D 사운드 (정화봇 목소리는 어디서든 또렷하게)
    }

    /// audioUrl의 음성을 다운받아 재생. 이미 재생 중이면 끊고 새로 재생.
    /// url이 비어있으면 아무것도 안 함 (텍스트만 표시되는 폴백 상황).
    public void PlayFromUrl(string url)
    {
        if (string.IsNullOrEmpty(url)) return;
        StopAllCoroutines();
        StartCoroutine(DownloadAndPlay(url));
    }

    /// 사전 생성해서 Assets/Audio/Poosh/에 넣어둔 고정 멘트 clip을 바로 재생.
    /// 다운로드가 필요 없으니 지연이 0. Stage별 성공/실패 멘트는 전부 이걸로 재생한다.
    public void PlayClip(AudioClip clip)
    {
        if (clip == null) return;
        StopAllCoroutines();
        if (_audioSource.isPlaying) _audioSource.Stop();
        _audioSource.clip = clip;
        _audioSource.Play();
    }

    private IEnumerator DownloadAndPlay(string url)
    {
        using (UnityWebRequest request = UnityWebRequestMultimedia.GetAudioClip(url, audioType))
        {
            request.timeout = downloadTimeout;
            yield return request.SendWebRequest();

            if (request.result != UnityWebRequest.Result.Success)
            {
                Debug.LogWarning($"[PooshVoicePlayer] 음성 다운로드 실패 (텍스트만 표시됨): {request.error}\nURL: {url}");
                yield break;
            }

            AudioClip clip = DownloadHandlerAudioClip.GetContent(request);
            if (clip == null || clip.length <= 0f)
            {
                Debug.LogWarning($"[PooshVoicePlayer] 오디오 클립 변환 실패 (형식 불일치일 수 있음. 현재 설정: {audioType})");
                yield break;
            }

            if (_audioSource.isPlaying) _audioSource.Stop();
            _audioSource.clip = clip;
            _audioSource.Play();
        }
    }

    /// 재생 중이면 즉시 중단 (씬 전환 등에서 호출).
    public void StopSpeaking()
    {
        if (_audioSource != null && _audioSource.isPlaying)
        {
            _audioSource.Stop();
        }
    }
}