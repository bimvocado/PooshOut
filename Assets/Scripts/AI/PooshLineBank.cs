using System;
using System.Collections.Generic;
using UnityEngine;

// generate_lines.py로 뽑은 고정 멘트 wav들을 key로 관리하는 창고.
// 같은 key에 여러 clip이 있으면(s2_pass_01~05처럼) 랜덤으로 하나 골라 재생한다.
//
// 사용법:
//   1. 정화봇 오브젝트(또는 별도 매니저 오브젝트)에 이 스크립트 추가
//   2. Inspector에서 Line Sets 배열 채우기 (key: "s2_pass", Clips: s2_pass_01~05 드래그)
//   3. 다른 스크립트에서 PooshLineBank.Instance.PlayLine("s2_pass") 호출
public class PooshLineBank : Singleton<PooshLineBank>
{
    [Serializable]
    public class LineSet
    {
        [Tooltip("generate_lines.py의 key와 동일하게. 예: s2_pass, s2_fail_tpose")]
        public string key;
        [Tooltip("이 key에 해당하는 wav들. 여러 개면 재생할 때마다 랜덤으로 하나 선택됨.")]
        public AudioClip[] clips;
    }

    [SerializeField] private LineSet[] lineSets;

    private Dictionary<string, AudioClip[]> _map;

    protected override void Awake()
    {
        base.Awake();
        _map = new Dictionary<string, AudioClip[]>();
        foreach (LineSet set in lineSets)
        {
            if (string.IsNullOrEmpty(set.key) || set.clips == null || set.clips.Length == 0) continue;
            _map[set.key] = set.clips;
        }
    }

    // key에 해당하는 멘트 중 하나를 랜덤으로 재생. 애니메이션(IsTalking)은 PooshBotAnimator가 자동으로 켜줌.
    public void PlayLine(string key)
    {
        if (!_map.TryGetValue(key, out AudioClip[] clips))
        {
            Debug.LogWarning($"[PooshLineBank] '{key}' 멘트를 찾을 수 없음 (Inspector 등록 확인)");
            return;
        }

        AudioClip clip = clips[UnityEngine.Random.Range(0, clips.Length)];
        PooshVoicePlayer.Instance?.PlayClip(clip);
    }
}
