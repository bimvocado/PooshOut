using UnityEngine;

/// <summary>
/// 스테이지 1 전용 효과음 매니저. 5가지를 담당한다:
/// 이동(루프) / 가속(버블 획득 시 속도 상승) / 아이템(맑은 버블 획득) / 장애물(쓰레기 획득) / 벽면 충돌(하수관 벽).
/// BubbleItem 등 스테이지1 스크립트에서 Stage1SoundManager.Instance?.PlayXxx() 형태로 호출.
/// 씬에 하나만 배치하고 RailMover를 연결해주면 됨.
/// </summary>
public class Stage1SoundManager : Singleton<Stage1SoundManager>
{
    // Stage1 씬에만 존재하는 매니저라 다른 매니저들과 달리 씬 전환 시 유지할 필요가 없음.
    protected override bool Persistent => false;

    [Header("참조")]
    [SerializeField] private RailMover railMover;

    [Header("이동 (루프, RailMover가 움직이는 동안 재생)")]
    [SerializeField] private AudioClip moveLoopClip;
    [Range(0f, 1f)]
    [SerializeField] private float moveVolume = 0.6f;

    [Header("아이템 (맑은 버블 획득)")]
    [SerializeField] private AudioClip itemClip;

    [Header("장애물 (쓰레기 획득)")]
    [SerializeField] private AudioClip obstacleClip;

    [Header("벽면 충돌 (하수관 벽)")]
    [SerializeField] private AudioClip wallHitClip;
    [Tooltip("벽에 계속 밀착해서 밀고 있을 때 충돌음이 매 프레임 겹쳐 재생되는 걸 막기 위한 최소 재생 간격(초).")]
    [SerializeField] private float wallHitCooldown = 0.3f;

    private AudioSource _moveSource;
    private AudioSource _oneShotSource;
    private float _lastWallHitTime = -999f;

    protected override void Awake()
    {
        base.Awake();

        _moveSource = gameObject.AddComponent<AudioSource>();
        _moveSource.clip = moveLoopClip;
        _moveSource.loop = true;
        _moveSource.playOnAwake = false;
        _moveSource.volume = moveVolume;

        _oneShotSource = gameObject.AddComponent<AudioSource>();
        _oneShotSource.loop = false;
        _oneShotSource.playOnAwake = false;
    }

    private void OnEnable()
    {
        if (railMover != null) railMover.OnWallHit += HandleWallHit;
    }

    private void OnDisable()
    {
        if (railMover != null) railMover.OnWallHit -= HandleWallHit;
    }

    private void Update()
    {
        if (railMover == null || _moveSource.clip == null) return;

        if (railMover.IsMoving && !_moveSource.isPlaying) _moveSource.Play();
        else if (!railMover.IsMoving && _moveSource.isPlaying) _moveSource.Stop();
    }

    private void HandleWallHit(Collider other)
    {
        if (Time.time - _lastWallHitTime < wallHitCooldown) return;
        _lastWallHitTime = Time.time;
        PlayWallHit();
    }

    public void PlayItemCollect() => PlayOneShot(itemClip);
    public void PlayObstacleHit() => PlayOneShot(obstacleClip);
    public void PlayWallHit() => PlayOneShot(wallHitClip);

    private void PlayOneShot(AudioClip clip)
    {
        if (clip == null) return;
        _oneShotSource.PlayOneShot(clip);
    }
}
