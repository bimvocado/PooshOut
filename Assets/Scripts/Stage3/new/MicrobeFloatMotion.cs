using UnityEngine;

/// <summary>
/// 미생물이 공중에서 둥둥 떠다니는 느낌을 주는 상하 진동 스크립트.
/// 대기(Waiting) 상태에서만 흔들리고, MicrobeTarget이 Approaching으로 전환되면
/// StopFloating()을 호출해 자연스럽게 멈추도록 설계됨.
/// (계속 흔들리면 MicrobeTarget의 MoveTowards 이동과 겹쳐서 경로가 지저분해짐)
/// </summary>
public class MicrobeFloatMotion : MonoBehaviour {
    [Header("상하 진동")]
    [SerializeField] private float floatAmplitude = 0.15f; // 위아래로 움직이는 폭
    [SerializeField] private float floatSpeed = 1.5f;       // 진동 속도 (클수록 빨리 흔들림)

    [Header("개체마다 살짝 다르게 (여러 마리가 똑같이 안 움직이도록)")]
    [SerializeField] private bool randomizePhase = true;
    [SerializeField] private float amplitudeJitter = 0.05f; // 진폭에 랜덤 편차
    [SerializeField] private float speedJitter = 0.3f;       // 속도에 랜덤 편차

    private Vector3 _basePosition;
    private float _phaseOffset;
    private float _amplitude;
    private float _speed;
    private bool _isFloating = true;

    private void Awake() {
        _amplitude = floatAmplitude;
        _speed = floatSpeed;

        if (randomizePhase) {
            _phaseOffset = Random.Range(0f, Mathf.PI * 2f);
            _amplitude += Random.Range(-amplitudeJitter, amplitudeJitter);
            _speed += Random.Range(-speedJitter, speedJitter);
        }
    }

    private void Start() {
        _basePosition = transform.position;
    }

    private void Update() {
        if (!_isFloating) return;

        float yOffset = Mathf.Sin(Time.time * _speed + _phaseOffset) * _amplitude;
        transform.position = _basePosition + new Vector3(0f, yOffset, 0f);
    }

    /// <summary>기준 위치를 지금 위치로 다시 잡음. 스폰 직후나 위치가 바뀐 시점에 호출.</summary>
    public void ResetBasePosition() {
        _basePosition = transform.position;
    }

    /// <summary>총알 맞아서 플레이어 쪽으로 이동 시작할 때 호출해서 진동을 끔.</summary>
    public void StopFloating() {
        _isFloating = false;
    }
}