using System;
using UnityEngine;

/// <summary>
/// 현재 정화도와 각 스테이지 종료 시 정화도 결과를 관리.
/// 현재는:
/// - 현재 정화도 0~100 관리
/// - 오염물 접촉 처리
/// - 정화도 기준 클리어 판정
/// - Stage 1~4 종료 시 정화도 저장
///
/// ※ 스테이지별 저장값 합산/전체 진행도 계산은 추후 구현 예정.
/// </summary>
public class PurificationSystem : Singleton<PurificationSystem>
{
    // ─────────────────────────────────────────────
    // 씬별 개별 빌드 촬영용 - 기기 저장소(PlayerPrefs)에 값을 남겨서, 앱을 완전히 새로
    // 빌드/재설치해도(같은 패키지 이름 전제) 이전 실행에서 쌓인 정화도를 이어받게 한다.
    // GameManager.cs의 캘리브레이션/닉네임 저장 방식과 동일한 패턴.
    // ─────────────────────────────────────────────
    private const string PrefKeyPurity = "PooshOut_Purity";
    private const string PrefKeyStagePurityPrefix = "PooshOut_StagePurity_";

    [Header("정화도")]
    [SerializeField] private float startingPurity = 0f;

    /// <summary>
    /// 현재 스테이지에서 사용하는 정화도.
    /// 0 이상. 보너스 설계에 따라 100을 넘을 수 있다.
    /// </summary>
    public float Purity { get; private set; }

    /// <summary>
    /// Stage 1 ~ Stage 4 종료 시 저장되는 정화도.
    /// 각각 0 ~ 100.
    /// </summary>
    private readonly float[] stagePurities = new float[4];

    /// <summary>
    /// 현재 정화도가 변경될 때 호출.
    /// 인자: 현재 정화도 0~100
    /// </summary>
    public event Action<float> OnPurityChanged;

    /// <summary>
    /// 스테이지 종료 정화도가 저장될 때 호출.
    /// 인자: (스테이지 번호, 저장된 정화도)
    /// </summary>
    public event Action<int, float> OnStagePuritySaved;

    /// <summary>
    /// 오염물 접촉 시 호출.
    /// 인자: 오염물 라벨
    /// </summary>
    public event Action<string> OnPollutantContact;

    protected override void Awake()
    {
        base.Awake();

        Purity = Mathf.Max(0f, startingPurity);
        LoadPersistedValuesIfAny();
    }

    private void LoadPersistedValuesIfAny()
    {
        // [이 부분 삭제 또는 주석 처리]
        // if (PlayerPrefs.HasKey(PrefKeyPurity))
        // {
        //     Purity = PlayerPrefs.GetFloat(PrefKeyPurity);
        // }

        // 스테이지별 개별 저장값은 엔딩 합산을 위해 계속 불러와야 함
        for (int i = 0; i < stagePurities.Length; i++)
        {
            string key = PrefKeyStagePurityPrefix + (i + 1);
            if (PlayerPrefs.HasKey(key))
            {
                stagePurities[i] = PlayerPrefs.GetFloat(key);
            }
        }
    }

    // ==================================================
    // 현재 정화도
    // ==================================================

    /// <summary>
    /// 현재 정화도 증가.
    /// </summary>
    public void Increase(float amount)
    {
        SetPurity(Purity + amount);
    }

    /// <summary>
    /// 현재 정화도 감소.
    /// </summary>
    public void Decrease(float amount)
    {
        SetPurity(Purity - amount);
    }

    /// <summary>
    /// 오염물 접촉 처리.
    /// 접촉 이벤트 발생 후 정화도를 감소시킨다.
    /// </summary>
    public void ReportPollutantContact(
        string pollutantLabel,
        float penalty)
    {
        OnPollutantContact?.Invoke(pollutantLabel);

        Decrease(penalty);
    }

    /// <summary>
    /// 현재 정화도를 직접 설정.
    /// 항상 0 이상으로 제한한다.
    /// </summary>
    public void SetPurity(float value)
    {
        float clamped = Mathf.Max(0f, value);

        if (Mathf.Approximately(clamped, Purity))
            return;

        Purity = clamped;

        PlayerPrefs.SetFloat(PrefKeyPurity, Purity);
        PlayerPrefs.Save();

        OnPurityChanged?.Invoke(Purity);
    }

    /// <summary>
    /// 현재 정화도가 클리어 기준 이상인지 확인.
    /// Stage1Manager 등 기존 코드에서 사용.
    /// </summary>
    public bool MeetsThreshold(float threshold)
    {
        return Purity >= threshold;
    }

    /// <summary>
    /// 현재 정화도를 초기 정화도로 되돌린다.
    /// 저장된 Stage 1~4 결과는 건드리지 않는다.
    /// </summary>
    public void ResetPurity()
    {
        SetPurity(startingPurity);
    }

    /// 실제 부스 운영 시 "다음 플레이어" 시작 전에 호출하면, 이번 플레이 세션 기록을
    /// 완전히 지운다 (기기 저장소까지 포함). 지금은 촬영 편의를 위해 값이 자동 이어지게
    /// 해뒀지만, 실제 서비스 흐름에서 이걸 호출하는 지점이 필요하면 여기에 연결하면 됨.
    public void ClearPersistedData()
    {
        PlayerPrefs.DeleteKey(PrefKeyPurity);
        for (int i = 1; i <= stagePurities.Length; i++)
        {
            PlayerPrefs.DeleteKey(PrefKeyStagePurityPrefix + i);
        }
        System.Array.Clear(stagePurities, 0, stagePurities.Length);
        ResetPurity();
    }

    // ==================================================
    // 스테이지별 최종 정화도 저장
    // ==================================================

    /// <summary>
    /// 해당 스테이지 종료 시 현재 정화도를 저장한다.
    /// stageNumber는 1 ~ 4.
    /// </summary>
    public void SaveStagePurity(int stageNumber)
    {
        int index = stageNumber - 1;

        if (index < 0 || index >= stagePurities.Length)
        {
            Debug.LogWarning(
                $"[PurificationSystem] 잘못된 스테이지 번호: {stageNumber}",
                this
            );

            return;
        }

        stagePurities[index] = Mathf.Clamp(
            Purity,
            0f,
            float.MaxValue
        );

        PlayerPrefs.SetFloat(PrefKeyStagePurityPrefix + stageNumber, stagePurities[index]);
        PlayerPrefs.Save();

        Debug.Log(
            $"[PurificationSystem] Stage {stageNumber} 정화도 저장: " +
            $"{stagePurities[index]}%",
            this
        );

        OnStagePuritySaved?.Invoke(
            stageNumber,
            stagePurities[index]
        );
    }

    /// <summary>
    /// 저장된 스테이지 정화도를 반환한다.
    /// stageNumber는 1 ~ 4.
    /// 유효하지 않은 번호면 0을 반환.
    /// </summary>
    public float GetStagePurity(int stageNumber)
    {
        int index = stageNumber - 1;

        if (index < 0 || index >= stagePurities.Length)
        {
            Debug.LogWarning(
                $"[PurificationSystem] 잘못된 스테이지 번호: {stageNumber}",
                this
            );

            return 0f;
        }

        return stagePurities[index];
    }

    // ==================================================
    // TODO
    // ==================================================

    /*
     * TODO:
     *
     * 1. Stage 1~4 종료 시 각각 SaveStagePurity(stageNumber) 호출
     *
     * 2. 저장값:
     *    Stage1 = 0~100
     *    Stage2 = 0~100
     *    Stage3 = 0~100
     *    Stage4 = 0~100
     *
     * 3. 추후 전체 진행도 구현:
     *    GetStagePurity(1)
     *    + GetStagePurity(2)
     *    + GetStagePurity(3)
     *    + GetStagePurity(4)
     *
     *    전체 진행도 범위 = 0~400
     *
     * 4. 씬 전환 시 저장값 유지 구조 확인
     *    - Singleton<T>의 DontDestroyOnLoad 여부 확인
     *
     * 5. 필요하다면 게임 종료 후에도 유지하도록
     *    JSON / PlayerPrefs SaveData 구현
     */
}