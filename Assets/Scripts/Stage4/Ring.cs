using UnityEngine;

/// <summary>
/// 링 프리팹에 붙는 스크립트.
/// 수직으로 낙하하는 동안, 해당 타일에 플레이어가 있으면 매 프레임 시간에 비례해서 정화도 상승.
/// 바닥(Collider)에 닿으면:
/// 1. 타일 소등
/// 2. 이펙트/사운드 재생 후 Destroy
/// 
/// 세팅 방법:
/// 1. 링 프리팹에 Rigidbody(중력 사용 안 하면 Is Kinematic 체크, 이 스크립트가 직접 이동시킴) 추가
/// 2. Collider 추가 (Is Trigger 체크 - 바닥과 겹치는 순간을 OnTriggerEnter로 감지)
/// 3. 바닥 오브젝트 Tag를 "Floor"로 설정 (또는 아래 floorTag 필드 값 변경)
/// 4. 착지 이펙트 프리팹, 사운드가 있으면 인스펙터에 연결 (없으면 비워둬도 동작함)
/// 5. purificationRatePerSecond: 타일 위에 1초 서 있을 때 오르는 정화도 양 (인스펙터에서 조절)
/// </summary>
public class Ring : MonoBehaviour {
    [Header("낙하 설정")]
    [SerializeField] private float fallSpeed = 2f; // 초당 이동 거리 (m/s)

    [Header("정화도 설정")]
    [SerializeField] private float purificationRatePerSecond = 10f; // 타일 위에 1초 서 있을 때 오르는 정화도 양
    [SerializeField] private float purificationDecayPerSecond = 5f; // 타일 밖에 1초 있을 때 깎이는 정화도 양

    [Header("착지 판정")]
    [SerializeField] private string floorTag = "Floor";

    [Header("착지 연출 (선택)")]
    [SerializeField] private GameObject landEffectPrefab;
    [SerializeField] private AudioClip landSound;
    [SerializeField] private float destroyDelay = 0.3f; // 이펙트/사운드 재생 시간 확보용

    // RingSpawner가 스폰 시점에 세팅해줌
    private TileController _targetTile;

    private bool _hasLanded;

    public void Init(TileController targetTile) {
        _targetTile = targetTile;
    }

    private void Update() {
        if (_hasLanded) return;

        // 수직 낙하
        transform.position += Vector3.down * fallSpeed * Time.deltaTime;

        // 낙하 중, 타일 위에 있으면 정화도 상승, 타일 밖에 있으면 정화도 하락 (시간에 비례)
        if (_targetTile != null) {
            if (_targetTile.IsPlayerOnTile) {
                PurificationManager.Instance?.AddPurificationAmount(purificationRatePerSecond * Time.deltaTime);
            }
            else {
                PurificationManager.Instance?.DecreasePurificationAmount(purificationDecayPerSecond * Time.deltaTime);
            }
        }
    }

    private void OnTriggerEnter(Collider other) {
        if (_hasLanded) return;
        if (!other.CompareTag(floorTag)) return;

        _hasLanded = true;
        HandleLanding();
    }

    private void HandleLanding() {
        // 타일 소등 (정화도 상승은 이제 Update()에서 시간에 비례해 처리되므로 여기서는 안 함)
        if (_targetTile != null) {
            _targetTile.SetLit(false);
        }

        // 연출
        if (landEffectPrefab != null) {
            Instantiate(landEffectPrefab, transform.position, Quaternion.identity);
        }

        if (landSound != null) {
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