using System.Collections;
using UnityEngine;

/// <summary>
/// 개별 미생물.
/// 대기 중(Waiting) 총알에 맞으면(Hit) 플레이어 쪽으로 서서히 다가가고(Approaching),
/// 플레이어한테 도달하면 분해 처리(정화도 증가)된다.
/// 맞지 않고 lifeTime이 지나면 패널티 없이 그냥 사라진다.
/// 한 번 맞아서 Approaching 상태가 되면 자신의 Collider를 꺼서,
/// 이후 날아오는 총알이 자신을 다시 맞히고 사라지는 일이 없도록 함.
/// </summary>
[RequireComponent(typeof(Collider))]
public class MicrobeTarget : MonoBehaviour {
    private enum State {
        Waiting,
        Approaching,
        Resolved
    }

    [Header("대기")]
    [SerializeField] private float lifeTime = 3f;

    [Header("피격 후 접근")]
    [SerializeField] private float approachSpeed = 5f;
    [SerializeField] private float arriveDistance = 1f;
    [SerializeField] private float purityRewardOnReach = 2f;

    [Header("사운드")]
    [SerializeField] private AudioSource audioSource;
    [SerializeField] private AudioClip[] hitSfx;
    [SerializeField] private AudioClip decomposeSfx;

    [Header("접근 중 크기 변화")]
    [SerializeField] private float minScaleRatio = 0.2f;
    [SerializeField]
    private AnimationCurve shrinkCurve =
        AnimationCurve.Linear(0f, 0f, 1f, 1f);

    [Header("피격 애니메이션")]
    [SerializeField] private Animator animator;
    [SerializeField] private string hitTriggerName = "Hit";

    [Header("Idle 애니메이션")]
    [SerializeField]
    private string[] idleStateNames =
        { "Idle1", "Idle2", "Idle3" };

    private MicrobeSpawner _owner;
    private Transform _playerTarget;
    private State _state = State.Waiting;

    private Vector3 _originalScale;
    private float _initialDistance;
    private Collider _collider;

    private void Reset() {
        GetComponent<Collider>().isTrigger = true;
    }

    private void Awake() {
        _originalScale = transform.localScale;
        _collider = GetComponent<Collider>();

        // AudioSource 자동 탐색
        if (audioSource == null) {
            audioSource = GetComponent<AudioSource>();

            if (audioSource == null) {
                audioSource = GetComponentInChildren<AudioSource>();
            }
        }

        // Animator 자동 탐색
        if (animator == null) {
            animator = GetComponent<Animator>();

            if (animator == null) {
                animator = GetComponentInChildren<Animator>();
            }
        }
    }

    public void Initialize(MicrobeSpawner owner, Transform playerTarget) {
        _owner = owner;
        _playerTarget = playerTarget;
        _state = State.Waiting;

        PlayRandomIdle();
        StartCoroutine(LifeRoutine());
    }

    /// <summary>
    /// Idle 애니메이션 중 하나를 랜덤으로 골라 재생.
    /// </summary>
    private void PlayRandomIdle() {
        if (animator == null ||
            idleStateNames == null ||
            idleStateNames.Length == 0)
            return;

        int randomIndex = Random.Range(0, idleStateNames.Length);

        animator.Play(idleStateNames[randomIndex]);
    }

    private IEnumerator LifeRoutine() {
        yield return new WaitForSeconds(lifeTime);

        if (_state == State.Waiting) {
            Retreat();
        }
    }

    private void Update() {
        if (_state != State.Approaching || _playerTarget == null)
            return;

        transform.position = Vector3.MoveTowards(
            transform.position,
            _playerTarget.position,
            approachSpeed * Time.deltaTime
        );

        float currentDistance =
            Vector3.Distance(
                transform.position,
                _playerTarget.position
            );

        // 진행률 계산
        // 0 = 처음 맞은 위치
        // 1 = 플레이어 도달
        float t = 0f;

        if (_initialDistance > arriveDistance) {
            t = 1f - Mathf.Clamp01(
                (currentDistance - arriveDistance) /
                (_initialDistance - arriveDistance)
            );
        }
        else {
            t = 1f;
        }

        float shrinkT = shrinkCurve.Evaluate(t);

        float scaleRatio =
            Mathf.Lerp(
                1f,
                minScaleRatio,
                shrinkT
            );

        transform.localScale =
            _originalScale * scaleRatio;

        if (currentDistance <= arriveDistance) {
            Reach();
        }
    }

    /// <summary>
    /// 총알이 미생물에 맞았을 때 호출.
    /// </summary>
    public void Hit() {
        Debug.Log(
            $"[MicrobeTarget] Hit() 호출됨, 현재 상태: {_state}"
        );

        if (_state != State.Waiting)
            return;

        _state = State.Approaching;

        // -------------------------
        // 피격 사운드 랜덤 재생
        // -------------------------
        if (audioSource != null &&
            hitSfx != null &&
            hitSfx.Length > 0) {
            AudioClip randomSfx =
                hitSfx[
                    Random.Range(0, hitSfx.Length)
                ];

            if (randomSfx != null) {
                audioSource.PlayOneShot(randomSfx);
            }
        }

        if (_playerTarget != null) {
            _initialDistance =
                Vector3.Distance(
                    transform.position,
                    _playerTarget.position
                );
        }

        // 접근 시작하면 떠다니는 움직임 중지
        GetComponent<MicrobeFloatMotion>()?.StopFloating();

        // 피격 애니메이션
        if (animator != null) {
            animator.SetTrigger(hitTriggerName);
        }

        // 한 번 맞은 후 콜라이더 비활성화
        if (_collider != null) {
            _collider.enabled = false;
        }

        Debug.Log(
            $"[MicrobeTarget] Approaching으로 전환, playerTarget = {_playerTarget}"
        );
    }

    /// <summary>
    /// 플레이어에게 도달했을 때.
    /// </summary>
    private void Reach() {
        if (_state == State.Resolved)
            return;

        _state = State.Resolved;

        PurificationSystem.Instance?
            .Increase(purityRewardOnReach);

        _owner?.NotifyDecomposed();

        // 분해 사운드
        if (audioSource != null &&
            decomposeSfx != null) {
            audioSource.PlayOneShot(decomposeSfx);
        }

        _owner?.NotifyDespawned();

        Destroy(gameObject);
    }

    /// <summary>
    /// 맞지 않은 채 수명이 끝났을 때.
    /// </summary>
    private void Retreat() {
        if (_state != State.Waiting)
            return;

        _state = State.Resolved;

        _owner?.NotifyDespawned();

        Destroy(gameObject);
    }
}