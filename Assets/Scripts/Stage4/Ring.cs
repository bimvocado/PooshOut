using UnityEngine;

/// <summary>
/// 링 프리팹에 붙는 스크립트.
/// 수직으로 낙하하다가 바닥(Collider)에 닿으면:
/// 1. 해당 타일에 플레이어가 있었는지 확인해서 정화도 상승
/// 2. 타일 소등
/// 3. 이펙트/사운드 재생 후 Destroy
/// 
/// 세팅 방법:
/// 1. 링 프리팹에 Rigidbody(중력 사용 안 하면 Is Kinematic 체크, 이 스크립트가 직접 이동시킴) 추가
/// 2. Collider 추가 (Is Trigger 체크 - 바닥과 겹치는 순간을 OnTriggerEnter로 감지)
/// 3. 바닥 오브젝트 Tag를 "Floor"로 설정 (또는 아래 floorTag 필드 값 변경)
/// 4. 착지 이펙트 프리팹, 사운드가 있으면 인스펙터에 연결 (없으면 비워둬도 동작함)
/// </summary>
public class Ring : MonoBehaviour
{
    [Header("낙하 설정")]
    [SerializeField] private float fallSpeed = 2f; // 초당 이동 거리 (m/s)

    [Header("착지 판정")]
    [SerializeField] private string floorTag = "Floor";

    [Header("착지 연출 (선택)")]
    [SerializeField] private GameObject landEffectPrefab;
    [SerializeField] private AudioClip landSound;
    [SerializeField] private float destroyDelay = 0.3f; // 이펙트/사운드 재생 시간 확보용

    // RingSpawner가 스폰 시점에 세팅해줌
    private TileController _targetTile;

    private bool _hasLanded;

    public void Init(TileController targetTile)
    {
        _targetTile = targetTile;
    }

    private void Update()
    {
        if (_hasLanded) return;

        // 수직 낙하
        transform.position += Vector3.down * fallSpeed * Time.deltaTime;
    }

    private void OnTriggerEnter(Collider other)
    {
        if (_hasLanded) return;
        if (!other.CompareTag(floorTag)) return;

        _hasLanded = true;
        HandleLanding();
    }

    private void HandleLanding()
    {
        // 판정: 착지 순간 해당 타일에 플레이어가 있었는가
        if (_targetTile != null)
        {
            if (_targetTile.IsPlayerOnTile)
            {
                PurificationManager.Instance?.AddPurification();
            }

            // 성공/실패와 상관없이 타일은 소등
            _targetTile.SetLit(false);
        }

        // 연출
        if (landEffectPrefab != null)
        {
            Instantiate(landEffectPrefab, transform.position, Quaternion.identity);
        }

        if (landSound != null)
        {
            AudioSource.PlayClipAtPoint(landSound, transform.position);
        }

        // 렌더러/콜라이더는 즉시 꺼서 안 보이게 하고, 실제 파괴는 살짝 딜레이
        var col = GetComponent<Collider>();
        if (col != null) col.enabled = false;

        var rend = GetComponent<Renderer>();
        if (rend != null) rend.enabled = false;

        Destroy(gameObject, destroyDelay);
    }
}
