using UnityEngine;

/// <summary>
/// 16개 타일 각각에 붙이는 스크립트.
/// - 점등/소등 (머티리얼 교체 방식)
/// - 점등 상태에 따라 VFX ON/OFF
/// - 플레이어가 이 타일 위에 있는지 감지
/// </summary>
[RequireComponent(typeof(Renderer))]
public class TileController : MonoBehaviour {
    [Header("점등 설정")]
    [SerializeField] private Material normalMaterial;
    [SerializeField] private Material litMaterial;
    [SerializeField] private int materialSlotIndex = 0;

    [Header("점등 VFX")]
    [SerializeField] private GameObject litVfx;

    [Header("플레이어 감지 설정")]
    [SerializeField] private string playerTag = "Player";

    private Renderer _renderer;
    private Material[] _materials;

    public bool IsPlayerOnTile { get; private set; }

    public int TileIndex { get; set; }

    private void Awake() {
        _renderer = GetComponent<Renderer>();
        _materials = _renderer.materials;

        // 처음에는 기본 머티리얼
        if (normalMaterial != null &&
            materialSlotIndex < _materials.Length) {
            _materials[materialSlotIndex] = normalMaterial;
            _renderer.materials = _materials;
        }

        // 처음에는 VFX OFF
        if (litVfx != null) {
            litVfx.SetActive(false);
        }
    }

    /// <summary>
    /// true  = 타일 점등 + VFX ON
    /// false = 타일 소등 + VFX OFF
    /// </summary>
    public void SetLit(bool lit) {
        if (materialSlotIndex < _materials.Length) {
            _materials[materialSlotIndex] =
                lit ? litMaterial : normalMaterial;

            _renderer.materials = _materials;
        }

        // VFX 같이 켜고 끄기
        if (litVfx != null) {
            litVfx.SetActive(lit);
        }
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