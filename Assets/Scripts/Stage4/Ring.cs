using System;
using UnityEngine;

/// <summary>
/// 링 프리팹에 붙는 스크립트.
/// 생성되면 낙하 사운드를 재생하고,
/// 수직으로 낙하하다가 바닥(Collider)에 닿으면:
/// 1. 플레이어가 해당 타일 위에 있으면 정화도 상승
/// 2. 타일 소등
/// 3. 착지 이펙트/사운드 재생
/// 4. Destroy
/// </summary>
public class Ring : MonoBehaviour
{
    [Header("낙하 설정")]
    [SerializeField] private float fallSpeed = 2f;

    [Header("정화도 설정")]
    [SerializeField] private float purificationRewardOnLand = 7f;

    [Header("착지 판정")]
    [SerializeField] private string floorTag = "Floor";

    [Header("사운드")]
    [SerializeField] private AudioSource audioSource;

    // 생성되는 순간부터 재생되는 소리
    [SerializeField] private AudioClip fallSound;

    // 바닥에 착지했을 때 재생되는 소리
    [SerializeField] private AudioClip landSound;

    [Header("착지 연출")]
    [SerializeField] private GameObject landEffectPrefab;
    [SerializeField] private float destroyDelay = 0.3f;

    private TileController _targetTile;
    private bool _hasLanded;

    // 정화봇 반응용 static 이벤트. 링이 계속 새로 생성/파괴되므로 인스턴스별 구독 대신
    // 이 static 이벤트 하나만 구독하면 Stage4PooshTrigger가 모든 링의 결과를 다 받을 수 있음.
    // 원래 있던 "플레이어가 타일 위에 있었는지" 판정(IsPlayerOnTile)에 딱 이 알림 두 줄만 얹은 것 - 새 판정 로직 아님.
    public static event Action OnRingPassed;   // 착지 시점에 플레이어가 그 타일 위에 있었음(통과 성공)
    public static event Action OnRingMissed;   // 착지 시점에 플레이어가 그 타일 위에 없었음(놓침)

    private void Awake()
    {
        // Inspector에서 연결하지 않아도
        // 같은 오브젝트의 AudioSource를 자동으로 찾음
        if (audioSource == null)
        {
            audioSource = GetComponent<AudioSource>();
        }
    }

    private void Start()
    {
        // 링이 생성되는 순간 낙하 사운드 시작
        if (audioSource != null && fallSound != null)
        {
            audioSource.clip = fallSound;

            // 낙하하는 동안 반복 재생
            audioSource.loop = false;

            audioSource.Play();
        }
    }

    public void Init(TileController targetTile)
    {
        _targetTile = targetTile;
    }

    private void Update()
    {
        if (_hasLanded)
            return;

        // 수직 낙하
        transform.position +=
            Vector3.down * fallSpeed * Time.deltaTime;
    }

    private void OnTriggerEnter(Collider other)
    {
        if (_hasLanded)
            return;

        if (!other.CompareTag(floorTag))
            return;

        _hasLanded = true;

        HandleLanding();
    }

    private void HandleLanding()
    {
        // =========================
        // 낙하 사운드 정지
        // =========================
        if (audioSource != null)
        {
            audioSource.Stop();
            audioSource.loop = false;
        }

        // =========================
        // 정화도 처리 (기존 로직 그대로, 성공 시에만 이벤트 한 줄 추가)
        // =========================
        if (_targetTile != null)
        {
            if (_targetTile.IsPlayerOnTile)
            {
                PurificationManager.Instance?
                    .AddPurificationAmount(
                        purificationRewardOnLand
                    );

                OnRingPassed?.Invoke();
            }
            else
            {
                OnRingMissed?.Invoke();
            }

            _targetTile.SetLit(false);
        }

        // =========================
        // 착지 이펙트
        // =========================
        if (landEffectPrefab != null)
        {
            Instantiate(
                landEffectPrefab,
                transform.position,
                Quaternion.identity
            );
        }

        // =========================
        // 착지 사운드
        // =========================
        if (landSound != null)
        {
            AudioSource.PlayClipAtPoint(
                landSound,
                transform.position
            );
        }

        // =========================
        // 링 숨기기
        // =========================
        Collider col = GetComponent<Collider>();

        if (col != null)
        {
            col.enabled = false;
        }

        Renderer rend = GetComponent<Renderer>();

        if (rend != null)
        {
            rend.enabled = false;
        }

        Destroy(gameObject, destroyDelay);
    }
}