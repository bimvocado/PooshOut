using System;
using UnityEngine;

/// <summary>
/// 정화도(0~100%)를 관리하는 핵심 시스템.
/// 값이 바뀔 때마다 OnPurityChanged 이벤트를 쏴서,
/// 게이지 UI / 캐릭터 외형 / (나중에) 정화봇이 알아서 반응하게 함.
///
/// 이 방식(이벤트) 덕분에 "매 프레임 정화도를 계속 확인"할 필요가 없음.
/// 나중에 GameState 로 확장할 때도 이 클래스만 손보면 되도록 정화도 로직을 여기 모아둠.
/// </summary>
public class PurificationSystem : MonoBehaviour
{
    public static PurificationSystem Instance { get; private set; }

    [SerializeField] private float startingPurity = 0f;
    public float Purity { get; private set; }
    // 정화도가 바뀔 때 발행. 인자는 (현재 정화도 0~100).
    public event Action<float> OnPurityChanged;

    private void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);

        Purity = Mathf.Clamp(startingPurity, 0f, 100f);
    }
    /// <summary>정화도 증가 (버블 획득, 미션 성공 등).</summary>
    public void Increase(float amount) => SetPurity(Purity + amount);
    /// <summary>정화도 감소 (오염물 접촉 등).</summary>
    public void Decrease(float amount) => SetPurity(Purity - amount);
    /// <summary>정화도를 특정 값으로 직접 설정.</summary>
    public void SetPurity(float value)
    {
        float clamped = Mathf.Clamp(value, 0f, 100f);
        if (Mathf.Approximately(clamped, Purity)) return;

        Purity = clamped;
        OnPurityChanged?.Invoke(Purity);
    }
    /// <summary>스테이지 클리어 기준 충족 여부 확인.</summary>
    public bool MeetsThreshold(float threshold) => Purity >= threshold;
    /// <summary>재시작 시 초기값으로 되돌림.</summary>
    public void ResetPurity() => SetPurity(startingPurity);
}
