using System;
using UnityEngine;

/// 현재 정화도와 각 스테이지 종료 시 정화도 결과를 관리.
/// 현재는:
/// - 현재 정화도 관리 (하한 0, 상한 없음 - 완주 보너스 등으로 100을 넘을 수 있게 의도적으로 열어둠)
/// - 오염물 접촉 처리
/// - 정화도 기준 클리어 판정
/// - Stage 1~4 종료 시 정화도 저장 (역시 상한 없음. 4개 합산 시 400을 넘는 것도 정상 - 실제로
///   198%처럼 표시되는 게 의도된 디자인)
///
/// ※ 스테이지별 저장값 합산/전체 진행도 계산은 추후 구현 예정.
public class PurificationSystem : Singleton<PurificationSystem>
{
    [Header("정화도")]
    [SerializeField] private float startingPurity = 0f;

    /// 현재 스테이지에서 사용하는 정화도.
    /// 0 이상. 보너스 설계에 따라 100을 넘을 수 있다.
    public float Purity { get; private set; }

    /// Stage 1 ~ Stage 4 종료 시 저장되는 정화도.
    /// 각각 0 이상, 상한 없음 (완주 보너스 등으로 100을 넘는 게 정상).
    private readonly float[] stagePurities = new float[4];

    /// LLM 마무리 피드백용 스테이지별 플레이 로그.
    /// 각 스테이지 매니저가 스테이지 종료 시 RecordStageLog()로 채워 넣는다.
    /// "잘했다/못했다"는 판단은 여기 안 담고, 순수 숫자(성공/실패 횟수 등)와
    /// 그 숫자를 설명하는 객관적 사실(note)만 담는다 - 해석/표현은 LLM에게 맡긴다.
    private readonly StagePlayLog[] stageLogs = new StagePlayLog[4];

    /// 현재 정화도가 변경될 때 호출.
    /// 인자: 현재 정화도 (0 이상, 상한 없음)
    public event Action<float> OnPurityChanged;

    /// 스테이지 종료 정화도가 저장될 때 호출.
    /// 인자: (스테이지 번호, 저장된 정화도)
    public event Action<int, float> OnStagePuritySaved;

    /// 오염물 접촉 시 호출.
    /// 인자: 오염물 라벨
    public event Action<string> OnPollutantContact;

    protected override void Awake()
    {
        base.Awake();

        Purity = Mathf.Max(0f, startingPurity);
    }

    // ==================================================
    // 현재 정화도
    // ==================================================

    /// 현재 정화도 증가.
    public void Increase(float amount)
    {
        SetPurity(Purity + amount);
    }

    /// 현재 정화도 감소.
    public void Decrease(float amount)
    {
        SetPurity(Purity - amount);
    }

    /// 오염물 접촉 처리.
    /// 접촉 이벤트 발생 후 정화도를 감소시킨다.
    public void ReportPollutantContact(
        string pollutantLabel,
        float penalty)
    {
        OnPollutantContact?.Invoke(pollutantLabel);

        Decrease(penalty);
    }

    /// 현재 정화도를 직접 설정.
    /// 항상 0 이상으로 제한한다.
    public void SetPurity(float value)
    {
        float clamped = Mathf.Max(0f, value);

        if (Mathf.Approximately(clamped, Purity))
            return;

        Purity = clamped;

        OnPurityChanged?.Invoke(Purity);
    }

    /// 현재 정화도가 클리어 기준 이상인지 확인.
    /// Stage1Manager 등 기존 코드에서 사용.
    public bool MeetsThreshold(float threshold)
    {
        return Purity >= threshold;
    }

    /// 현재 정화도를 초기 정화도로 되돌린다.
    /// 저장된 Stage 1~4 결과는 건드리지 않는다.
    public void ResetPurity()
    {
        SetPurity(startingPurity);
    }

    // ==================================================
    // 스테이지별 최종 정화도 저장
    // ==================================================

    /// <summary>
    /// 해당 스테이지 종료 시 현재 정화도(Purity)를 저장한다.
    /// stageNumber는 1 ~ 4.
    /// </summary>
    public void SaveStagePurity(int stageNumber)
    {
        SaveStagePurity(stageNumber, Purity);
    }

    /// <summary>
    /// 해당 스테이지 종료 시 정화도를 직접 지정해서 저장한다.
    /// 스테이지가 자체적인 진행도 값(예: 손목 UI에 표시되는 값)을 따로 갖고 있어서
    /// 그 값을 그대로 저장해야 할 때 사용. (예: Stage3Manager)
    /// stageNumber는 1 ~ 4.
    /// </summary>
    public void SaveStagePurity(int stageNumber, float value)
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
            value,
            0f,
            float.MaxValue
        );

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

    /// 저장된 스테이지 정화도를 반환한다.
    /// stageNumber는 1 ~ 4.
    /// 유효하지 않은 번호면 0을 반환.
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
    // LLM 마무리 피드백용 스테이지 플레이 로그
    // ==================================================

    /// 각 스테이지가 끝날 때 호출. 성공/실패 등 순수 숫자와 그걸 설명하는 객관적
    /// 사실(note)만 기록한다. "잘함/못함" 같은 평가 문구는 여기 담지 말 것 -
    /// 그 판단은 서버의 LLM이 숫자를 보고 내리도록 설계되어 있음.
    /// 정화도(purity)는 이 함수를 부르기 직전에 이미 SaveStagePurity()로 저장해둔
    /// stagePurities 배열 값을 그대로 가져와서 자동으로 같이 기록한다 - 각 스테이지
    /// 매니저는 전부 SaveStagePurity → RecordStageLog 순서로 호출하고 있으므로,
    /// 이 함수 시그니처를 안 바꿔도(호출부 수정 없이) 정화도가 같이 담긴다.
    /// stageNumber는 1 ~ 4.
    public void RecordStageLog(int stageNumber, int success, int fail, string note)
    {
        int index = stageNumber - 1;

        if (index < 0 || index >= stageLogs.Length)
        {
            Debug.LogWarning($"[PurificationSystem] 잘못된 스테이지 번호: {stageNumber}", this);
            return;
        }

        float purity = stagePurities[index];

        stageLogs[index] = new StagePlayLog
        {
            stage = stageNumber,
            success = success,
            fail = fail,
            note = note,
            purity = purity,
        };

        Debug.Log($"[PurificationSystem] Stage {stageNumber} 플레이 로그 기록: 정화도 {purity}%, 성공 {success}, 실패 {fail}, note=\"{note}\"", this);
    }

    /// 엔딩씬1에서 LLM 마무리 피드백 요청 직전에 호출. 지금까지 기록된 스테이지
    /// 로그를 전부 모아서 반환한다 (기록 안 된 스테이지는 제외).
    public System.Collections.Generic.List<StagePlayLog> GetAllStageLogs()
    {
        var result = new System.Collections.Generic.List<StagePlayLog>();
        foreach (var log in stageLogs)
        {
            if (log != null) result.Add(log);
        }
        return result;
    }

    // ==================================================
    // TODO / 완료 현황
    // ==================================================

    /*
     * 1. Stage 1~4 종료 시 각각 SaveStagePurity(stageNumber) 호출 → 완료 (각 스테이지 매니저에서 호출 중)
     *
     * 2. 저장값: Stage1~4 각각 0 이상, 상한 없음 (보너스 설계상 100을 넘는 게 정상 동작)
     *
     * 3. 전체 진행도(스테이지 합산) → 완료
     *    GetStagePurity(1) + GetStagePurity(2) + GetStagePurity(3) + GetStagePurity(4)
     *    각 스테이지가 100을 넘을 수 있으므로 합산값도 이론상 상한 없음
     *
     * 4. 씬 전환 시 저장값 유지 구조 확인
     *    - Singleton<T>의 DontDestroyOnLoad 여부 확인
     *
     * 5. 필요하다면 게임 종료 후에도 유지하도록
     *    JSON / PlayerPrefs SaveData 구현
     */
}

/// 스테이지 하나의 플레이 기록. LLM 마무리 피드백 서버(/feedback)로 그대로 넘어갈 데이터 형태.
/// success/fail은 스테이지마다 무엇을 세는지 다르다 (예: Stage1=버블 획득/오염물 충돌,
/// Stage3=미생물 명중/빗나감). note에는 success/fail만으로는 못 담는 추가 수치(최대 연속 기록,
/// 평균 반응시간 등)를 "판단 없이 사실만" 문자열로 적는다.
/// 예: "최대 연속 명중 5회" (O), "명사수처럼 잘함" (X - 이건 LLM이 판단할 몫)
[Serializable]
public class StagePlayLog
{
    public int stage;
    public int success;
    public int fail;
    public string note;
    public float purity; // 이 스테이지의 최종 정화도(%). 서버에서 상위 2개 스테이지를 고를 때 씀.
}