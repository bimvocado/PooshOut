using UnityEngine;

/// <summary>
/// 스테이지 3 흐름 제어 (StageManager 의존성 제거 버전).
/// 씬이 시작하면 바로 미생물 스폰을 켜고, 목표 분해 수를 채우면 로그만 남긴다.
/// (나중에 여러 스테이지를 연결할 StageManager를 다시 갖추면, 그 이벤트에 맞춰 StartStage/StopSpawning을
/// 호출하도록 되돌리면 된다.)
/// </summary>
public class Stage3Manager : MonoBehaviour {
    [SerializeField] private MicrobeSpawner microbeSpawner;
    [SerializeField] private int decomposeTarget = 15;

    private bool _cleared;

    private void Start() {
        StartStage();
    }

    private void StartStage() {
        Debug.Log("[Stage3Manager] 스테이지 3 시작");
        microbeSpawner?.StartSpawning();
    }

    private void Update() {
        if (_cleared || microbeSpawner == null) return;

        if (microbeSpawner.DecomposedCount >= decomposeTarget) {
            _cleared = true;
            microbeSpawner.StopSpawning();
            Debug.Log("[Stage3Manager] 목표 분해량 달성");
        }
    }
}