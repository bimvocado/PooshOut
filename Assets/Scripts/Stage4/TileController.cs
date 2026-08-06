using UnityEngine;

/// <summary>
/// 16개 타일 각각에 붙이는 스크립트.
/// - 점등/소등 (머티리얼 교체 방식)
/// - 플레이어가 이 타일 위에 있는지 감지 (Collider Trigger)
/// 
/// 세팅 방법:
/// 1. 타일 오브젝트에 이 스크립트 추가
/// 2. 타일에 Collider를 하나 추가하고 Is Trigger 체크 (플레이어 감지용, 바닥보다 살짝 위로 볼륨 잡아주면 좋음)
/// 3. Renderer, Normal Material, Lit Material 인스펙터에서 연결
/// 4. Player 오브젝트의 Tag를 "Player"로 설정 (또는 아래 playerTag 필드에서 원하는 태그로 변경)
/// </summary>
[RequireComponent(typeof(Renderer))]
public class TileController : MonoBehaviour {
    [Header("점등 설정")]
    [SerializeField] private Material normalMaterial;
    [SerializeField] private Material litMaterial;
    [SerializeField] private int materialSlotIndex = 0; // 여러 머티리얼 중 바꿀 슬롯 번호 (Element 0 = 0)

    [Header("플레이어 감지 설정")]
    [SerializeField] private string playerTag = "Player";

    private Renderer _renderer;
    private Material[] _materials; // 인스턴스 머티리얼 배열 (런타임에 개별 수정)

    // 현재 이 타일 위에 플레이어가 있는지 여부. 외부(Ring)에서 참조함.
    public bool IsPlayerOnTile { get; private set; }

    // 타일의 인덱스(0~15). RingSpawner/TileManager에서 세팅해줌.
    public int TileIndex { get; set; }

    private void Awake() {
        _renderer = GetComponent<Renderer>();

        // .materials 접근 시 인스턴스가 생성되어 원본 에셋은 건드리지 않음
        _materials = _renderer.materials;

        if (normalMaterial != null && materialSlotIndex < _materials.Length) {
            _materials[materialSlotIndex] = normalMaterial;
            _renderer.materials = _materials;
        }
    }

    /// <summary>
    /// 타일 점등 상태를 켜고 끔. RingSpawner가 스폰 시 true, Ring이 착지 시 false로 호출.
    /// Element 0(materialSlotIndex)만 교체하고 나머지 슬롯은 그대로 유지.
    /// </summary>
    public void SetLit(bool lit) {
        if (materialSlotIndex >= _materials.Length) return;

        _materials[materialSlotIndex] = lit ? litMaterial : normalMaterial;
        _renderer.materials = _materials;
    }

    private void OnTriggerEnter(Collider other) {
        if (other.CompareTag(playerTag)) {
            IsPlayerOnTile = true;
        }
    }

    private void OnTriggerExit(Collider other) {
        if (other.CompareTag(playerTag)) {
            IsPlayerOnTile = false;
        }
    }
}