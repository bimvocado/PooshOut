using UnityEngine;

/// <summary>
/// 플레이어 발밑 물보라 파티클. RailMover의 실시간 속도(CurrentSpeed)를 읽어서
/// 자식 ParticleSystem들의 방출량(rateOverTime)을 속도에 비례해 조절한다.
/// 속도가 minSpeedForEmission 미만이면(정지/벽에 막힘) 방출을 멈춘다.
/// PoopCharacter(레일 라이더) 하위, 발밑 위치에 배치하고 railMover를 연결해서 사용.
/// </summary>
public class WaterSplashVFX : MonoBehaviour
{
    [SerializeField] private RailMover railMover;

    [Tooltip("비워두면 이 오브젝트 하위의 모든 ParticleSystem을 자동으로 찾아서 제어한다.")]
    [SerializeField] private ParticleSystem[] splashSystems;

    [Header("속도 → 방출량 매핑")]
    [SerializeField] private float minSpeedForEmission = 0.15f;
    [SerializeField] private float speedForMaxEmission = 6f;
    [Tooltip("최소 방출 시 각 파티클시스템 기본 rateOverTime 대비 배율")]
    [SerializeField] private float minRateMultiplier = 0.2f;
    [Tooltip("최대 방출 시 각 파티클시스템 기본 rateOverTime 대비 배율")]
    [SerializeField] private float maxRateMultiplier = 1.6f;

    private float[] _baseRates;

    private void Awake()
    {
        // 프리팹을 레일 라이더(PoopCharacter) 하위에 배치하면 자동으로 찾아 연결한다. 직접 연결도 가능.
        if (railMover == null) railMover = GetComponentInParent<RailMover>();

        if (splashSystems == null || splashSystems.Length == 0)
            splashSystems = GetComponentsInChildren<ParticleSystem>(true);

        _baseRates = new float[splashSystems.Length];
        for (int i = 0; i < splashSystems.Length; i++)
        {
            if (splashSystems[i] == null) continue;
            _baseRates[i] = splashSystems[i].emission.rateOverTime.constant;
        }
    }

    private void OnEnable()
    {
        foreach (var ps in splashSystems)
        {
            if (ps != null && !ps.isPlaying) ps.Play();
        }
    }

    private void Update()
    {
        float speed = railMover != null ? railMover.CurrentSpeed : 0f;
        float t = speed < minSpeedForEmission
            ? 0f
            : Mathf.InverseLerp(minSpeedForEmission, speedForMaxEmission, speed);
        float multiplier = Mathf.Lerp(minRateMultiplier, maxRateMultiplier, t);
        if (speed < minSpeedForEmission) multiplier = 0f;

        for (int i = 0; i < splashSystems.Length; i++)
        {
            if (splashSystems[i] == null) continue;
            var emission = splashSystems[i].emission;
            emission.rateOverTime = _baseRates[i] * multiplier;
        }
    }
}
