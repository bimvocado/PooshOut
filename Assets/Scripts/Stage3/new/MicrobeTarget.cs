using System.Collections;
using UnityEngine;

/// <summary>
/// 개별 미생물.
/// 대기 중(Waiting) 총알에 맞으면(Hit) 플레이어 쪽으로 서서히 다가가고(Approaching),
/// 플레이어한테 도달하면 분해 처리(정화도 증가)된다.
/// 맞지 않고 lifeTime이 지나면 패널티 없이 그냥 사라진다.
/// </summary>
[RequireComponent(typeof(Collider))]
public class MicrobeTarget : MonoBehaviour {
    private enum State { Waiting, Approaching, Resolved }

    [Header("대기")]
    [SerializeField] private float lifeTime = 2f; // 안 맞고 버티는 시간

    [Header("피격 후 접근")]
    [SerializeField] private float approachSpeed = 5f;    // 플레이어에게 다가가는 속도
    [SerializeField] private float arriveDistance = 1f; // 이 거리 이하면 '도달'로 처리
    [SerializeField] private float purityRewardOnReach = 2f;
    [SerializeField] private AudioClip decomposeSfx;

    [Header("접근 중 크기 변화")]
    [SerializeField] private float minScaleRatio = 0.2f; // 도달 직전 최소 크기(원래 크기 대비 비율)
    [SerializeField] private AnimationCurve shrinkCurve = AnimationCurve.Linear(0f, 0f, 1f, 1f); // t(진행률) -> 축소 정도

    private MicrobeSpawner _owner;
    private Transform _playerTarget;
    private State _state = State.Waiting;

    private Vector3 _originalScale;
    private float _initialDistance;

    private void Reset() {
        GetComponent<Collider>().isTrigger = true;
    }

    private void Awake() {
        _originalScale = transform.localScale;
    }

    public void Initialize(MicrobeSpawner owner, Transform playerTarget) {
        _owner = owner;
        _playerTarget = playerTarget;
        _state = State.Waiting;
        StartCoroutine(LifeRoutine());
    }

    private IEnumerator LifeRoutine() {
        yield return new WaitForSeconds(lifeTime);
        if (_state == State.Waiting) {
            Retreat();
        }
    }

    private void Update() {
        if (_state != State.Approaching || _playerTarget == null) return;

        transform.position = Vector3.MoveTowards(
            transform.position, _playerTarget.position, approachSpeed * Time.deltaTime);

        float currentDistance = Vector3.Distance(transform.position, _playerTarget.position);

        // 진행률 계산 (0: 처음 맞은 위치, 1: 도달 지점)
        float t = 0f;
        if (_initialDistance > arriveDistance) {
            t = 1f - Mathf.Clamp01((currentDistance - arriveDistance) / (_initialDistance - arriveDistance));
        }
        else {
            t = 1f;
        }

        float shrinkT = shrinkCurve.Evaluate(t);
        float scaleRatio = Mathf.Lerp(1f, minScaleRatio, shrinkT);
        transform.localScale = _originalScale * scaleRatio;

        if (currentDistance <= arriveDistance) {
            Reach();
        }
    }

    /// <summary>총알(Bullet)이 맞았을 때 호출.</summary>
    public void Hit() {
        Debug.Log($"[MicrobeTarget] Hit() 호출됨, 현재 상태: {_state}");
        if (_state != State.Waiting) return;
        _state = State.Approaching;

        if (_playerTarget != null) {
            _initialDistance = Vector3.Distance(transform.position, _playerTarget.position);
        }

        GetComponent<MicrobeFloatMotion>()?.StopFloating(); // 접근 시작하면 진동 멈춤

        Debug.Log($"[MicrobeTarget] Approaching으로 전환, playerTarget = {_playerTarget}");
    }

    private void Reach() {
        if (_state == State.Resolved) return;
        _state = State.Resolved;

        PurificationSystem.Instance?.Increase(purityRewardOnReach);
        _owner?.NotifyDecomposed();
        if (decomposeSfx != null) AudioManager.Instance?.PlaySfx(decomposeSfx);

        _owner?.NotifyDespawned();
        Destroy(gameObject);
    }

    private void Retreat() {
        if (_state != State.Waiting) return;
        _state = State.Resolved;

        _owner?.NotifyDespawned();
        Destroy(gameObject);
    }
}