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

    private float _lastWallSparkTime = -999f;

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
        PlayWallSpark(hit.point, hit.normal);
    }

    /// <summary>버블 획득 시 호출. 속도선 이펙트를 앵커 위치에 생성한다(자동 소멸은 프리팹의 TimedVFX가 처리).</summary>
    public void PlaySpeedLineEffect()
    {
        if (speedLinePrefab == null) return;
        Transform anchor = speedLineAnchor != null ? speedLineAnchor : transform;
        Instantiate(speedLinePrefab, anchor.position, anchor.rotation, anchor);
    }

    /// <summary>벽 충돌 지점에 스파크 이펙트를 생성한다. normal 방향(벽 바깥쪽)을 바라보도록 회전시킨다.</summary>
    public void PlayWallSpark(Vector3 position, Vector3 normal)
    {
        if (wallSparkPrefab == null) return;
        Quaternion rotation = normal.sqrMagnitude > 0.0001f ? Quaternion.LookRotation(normal) : Quaternion.identity;
        Instantiate(wallSparkPrefab, position, rotation);
    }
}
