using System;
using System.Collections;
using UnityEngine;
using UnityEngine.Networking;

/// <summary>
/// 정화봇 음성 재생기.
/// 서버(/chat 등)가 돌려주는 audioUrl(mp3)을 다운로드해서 AudioSource로 재생한다.
///
/// 사용법:
///   1. 씬의 정화봇 오브젝트(또는 _LLMTest)에 이 스크립트 추가
///   2. AudioSource가 없으면 자동으로 붙음
///   3. LLMConnector 응답을 받은 곳에서 PooshVoicePlayer.Instance.PlayFromUrl(audioUrl) 호출
///
/// 서버 응답에서 audioUrl이 null이면(TTS 실패/미설정) 조용히 무시하고 텍스트만 표시되게 둔다.
/// </summary>
[RequireComponent(typeof(AudioSource))]
public class PooshVoicePlayer : Singleton<PooshVoicePlayer>
{
    private AudioSource _audioSource;

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

    /// <summary>
    /// audioUrl의 음성을 다운받아 재생. 이미 재생 중이면 끊고 새로 재생.
    /// url이 비어있으면 아무것도 안 함 (텍스트만 표시되는 폴백 상황).
    /// </summary>
    public void PlayFromUrl(string url)
    {
        if (string.IsNullOrEmpty(url)) return;
        StopAllCoroutines();
        StartCoroutine(DownloadAndPlay(url));
    }

    private IEnumerator DownloadAndPlay(string url)
    {
        using (UnityWebRequest request = UnityWebRequestMultimedia.GetAudioClip(url, AudioType.MPEG))
        {
            request.timeout = 10;
            yield return request.SendWebRequest();

            if (request.result != UnityWebRequest.Result.Success)
            {
                Debug.LogWarning($"[PooshVoicePlayer] 음성 다운로드 실패 (텍스트만 표시됨): {request.error}");
                yield break;
            }

            AudioClip clip = DownloadHandlerAudioClip.GetContent(request);
            if (clip == null)
            {
                Debug.LogWarning("[PooshVoicePlayer] 오디오 클립 변환 실패");
                yield break;
            }

            if (_audioSource.isPlaying) _audioSource.Stop();
            _audioSource.clip = clip;
            _audioSource.Play();
        }
    }

    /// <summary>재생 중이면 즉시 중단 (씬 전환 등에서 호출).</summary>
    public void StopSpeaking()
    {
        if (_audioSource != null && _audioSource.isPlaying)
        {
            _audioSource.Stop();
        }
    }
}
