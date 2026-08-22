using UnityEngine;

/// <summary>
/// 스테이지 1 전용 VFX 매니저. 속도선(버블 획득) / 벽 충돌 스파크를 담당한다.
/// BubbleItem 등 스테이지1 스크립트에서 Stage1VFXManager.Instance?.PlayXxx() 형태로 호출.
/// 물보라(WaterSplashVFX)는 항상 재생되는 이펙트라 별도 스크립트로 직접 RailMover를 참조한다.
/// 씬에 하나만 배치하고 RailMover / 프리팹 / 속도선 앵커를 연결해주면 됨.
/// </summary>
public class Stage1VFXManager : Singleton<Stage1VFXManager>
{
    // Stage1 씬에만 존재하는 매니저라 다른 매니저들과 달리 씬 전환 시 유지할 필요가 없음.
    protected override bool Persistent => false;

    [Header("참조")]
    [SerializeField] private RailMover railMover;
    [Tooltip("속도선 이펙트가 따라다닐 기준. 보통 VR 카메라(CenterEyeAnchor)를 연결 - 월드스페이스로 시야 주변에 고정된다.")]
    [SerializeField] private Transform speedLineAnchor;

    [Header("프리팹")]
    [SerializeField] private GameObject speedLinePrefab;
    [SerializeField] private GameObject wallSparkPrefab;

    [Tooltip("벽에 계속 밀착해서 밀고 있을 때 스파크가 매 프레임 겹쳐 생성되는 걸 막기 위한 최소 생성 간격(초).")]
    [SerializeField] private float wallSparkCooldown = 0.2f;

    [Header("벽 스파크 위치 보정")]
    [Tooltip("충돌 지점에서 벽 안쪽(normal 반대 = 플레이어가 있는 파이프 내부 공간 쪽)으로 밀어내는 거리. " +
             "0이면 스파크가 벽 표면에 딱 붙어서 생성되어 파이프 벽 메시 안쪽에 절반쯤 묻혀 안 보이는 문제가 있다.")]
    [SerializeField] private float wallSparkInwardOffset = 0.25f;
    [Tooltip("충돌 지점에서 플레이어 진행/시야 방향(railMover.transform.forward)으로 밀어내는 거리. " +
             "ControllerColliderHit.point가 물리 서브스텝상 실제 캐릭터 위치보다 살짝 뒤처진 지점을 가리켜 " +
             "스파크가 플레이어 뒤쪽에 스폰되는 것처럼 보이는 문제를 보정한다.")]
    [SerializeField] private float wallSparkForwardOffset = 0.3f;

    private float _lastWallSparkTime = -999f;
    private SpeedLineFlash _speedLineFlash;

    private void OnEnable()
    {
        if (railMover != null) railMover.OnWallHit += HandleWallHit;
    }

    private void OnDisable()
    {
        if (railMover != null) railMover.OnWallHit -= HandleWallHit;
    }

    private void HandleWallHit(ControllerColliderHit hit)
    {
        if (Time.time - _lastWallSparkTime < wallSparkCooldown) return;
        _lastWallSparkTime = Time.time;

        // 1) normal 반대 방향(벽 안쪽/플레이어가 있는 파이프 내부 공간)으로 밀어내서
        //    스파크가 벽 표면 메시에 절반쯤 묻혀 안 보이는 문제를 막는다.
        Vector3 spawnPos = hit.point - hit.normal * wallSparkInwardOffset;

        // 2) 플레이어 진행/시야 방향으로도 밀어내서, hit.point가 실제 캐릭터 위치보다
        //    뒤처져 스파크가 플레이어 뒤쪽에 스폰되는 것처럼 보이는 문제를 보정한다.
        if (railMover != null) spawnPos += railMover.transform.forward * wallSparkForwardOffset;

        Debug.Log($"[Stage1VFXManager] WallSpark 스폰 위치 계산 - hit.point={hit.point}, hit.normal={hit.normal}, " +
            $"inwardOffset={wallSparkInwardOffset}, forwardOffset={wallSparkForwardOffset}, " +
            $"railMover.forward={(railMover != null ? railMover.transform.forward.ToString() : "N/A")}, " +
            $"최종 spawnPos={spawnPos}");

        PlayWallSpark(spawnPos, hit.normal);
    }

    /// <summary>
    /// 버블 획득 시 호출. 속도선(화면 가장자리 방사형 집중선) 쿼드는 매번 새로 스폰하지 않고
    /// 카메라 앞에 딱 붙여 최초 1회만 생성해서 재사용하고, 부를 때마다 알파를 반짝여서 보여준다.
    /// </summary>
    public void PlaySpeedLineEffect()
    {
        if (speedLinePrefab == null) return;

        if (_speedLineFlash == null)
        {
            Transform anchor = speedLineAnchor != null ? speedLineAnchor : transform;
            // Instantiate(prefab, parent) 2-인자 오버로드는 instantiateInWorldSpace가 기본 true라
            // 프리팹에 미리 넣어둔 로컬 오프셋(카메라 앞 0.6m)을 "월드 좌표"로 그대로 써버린다.
            // 그 결과 anchor(카메라)가 월드 원점 근처가 아닌 한 카메라 앞이 아니라 엉뚱한 월드 위치에
            // 생성되고, 만약 그 위치가 카메라 바로 앞/내부에 걸리면 거대한 쿼드가 화면을 통째로
            // 가려버리는 문제가 생긴다. worldPositionStays를 false로 줘서 프리팹의 로컬 오프셋을
            // anchor 기준 로컬 좌표로 그대로 유지해야 한다.
            GameObject instance = Instantiate(speedLinePrefab, anchor, false);
            _speedLineFlash = instance.GetComponent<SpeedLineFlash>();
        }

        if (_speedLineFlash != null) _speedLineFlash.Flash();
    }

    /// <summary>벽 충돌 지점에 스파크 이펙트를 생성한다. normal 방향(벽 바깥쪽)을 바라보도록 회전시킨다.</summary>
    public void PlayWallSpark(Vector3 position, Vector3 normal)
    {
        if (wallSparkPrefab == null) return;
        Quaternion rotation = normal.sqrMagnitude > 0.0001f ? Quaternion.LookRotation(normal) : Quaternion.identity;
        GameObject instance = Instantiate(wallSparkPrefab, position, rotation);
        ForceReplay(instance);
    }

    /// <summary>
    /// playOnAwake만 믿으면 인스턴스가 생성 직후 재생을 안 하는 경우가 있어(파티클이 이전 상태를 이어받거나
    /// 초기화가 늦는 문제), 매 스폰마다 명시적으로 Clear 후 Play를 강제해서 항상 처음부터 새로 재생되게 한다.
    /// 다음 충돌/획득 때 다시 보이려면 이 인스턴스가 매번 새로 스폰되고(Instantiate) 확실히 재생까지 되어야 한다.
    /// </summary>
    private static void ForceReplay(GameObject instance)
    {
        var ps = instance.GetComponentInChildren<ParticleSystem>(true);
        if (ps == null) return;
        ps.Clear(true);
        ps.Play(true);
    }
}
