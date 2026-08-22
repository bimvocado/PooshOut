using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// 거름망 실루엣 하나. 플레이어가 트리거 영역에 들어오는 순간의 포즈를 판정해서
/// 요구 포즈(PoseDetector.PoseType)와 일치하면 통과, 아니면 오염물 접촉으로 처리.
[RequireComponent(typeof(Collider))]
[RequireComponent(typeof(AudioSource))]
public class PoseGate : MonoBehaviour
{
    [SerializeField] private PoseDetector poseDetector;
    [SerializeField] private PoseDetector.PoseType requiredPose = PoseDetector.PoseType.Normal;
    [SerializeField] private float purityRewardOnPass = 4f;
    [SerializeField] private float purityPenaltyOnFail = 4f;
    [SerializeField] private string playerTag = "Player";

    [Header("실패 연출")]
    [Tooltip("실패 판정 순간 재생할 VFX 프리팹 (스파크+먼지 묶음). 비워두면 연출 없이 판정만 처리됨.")]
    [SerializeField] private GameObject failVfxPrefab;

    [Tooltip("VFX가 자동으로 파괴되기까지의 시간(초). 파티클 재생 길이보다 넉넉하게.")]
    [SerializeField] private float failVfxLifetime = 2f;

    [Tooltip("실패 VFX가 생성될 때 게이트 위치에서 위로 얼마나 띄울지(m). 게이트 피벗이 바닥 쪽에 있어 그대로 두면 시야보다 낮게 나옴.")]
    [SerializeField] private float failVfxHeightOffset = 1.4f;

    [Tooltip("실패 VFX를 플레이어 쪽(이동 방향)으로 얼마나 당겨올지(m). 눈앞에 오도록 조정.")]
    [SerializeField] private float failVfxForwardOffset = 0f;

    [Tooltip("실패 시 게이트 네온이 물드는 색.")]
    [SerializeField] private Color failFlashColor = new Color(1f, 0.15f, 0.15f);

    [Tooltip("실패 시 흔들림 지속 시간(초).")]
    [SerializeField] private float shakeDuration = 0.25f;

    [Tooltip("실패 시 흔들림 각도 폭(도). Z축 회전으로 흔들어서 이동 경로에는 영향 없음. VR 멀미 방지를 위해 작게 유지.")]
    [SerializeField] private float shakeAngle = 2.5f;

    [Header("성공 연출")]
    [Tooltip("성공 판정 순간 재생할 VFX 프리팹 (빛 기둥+샤인). 비워두면 연출 없이 판정만 처리됨.")]
    [SerializeField] private GameObject successVfxPrefab;

    [Tooltip("VFX가 자동으로 파괴되기까지의 시간(초). 파티클 재생 길이보다 넉넉하게.")]
    [SerializeField] private float successVfxLifetime = 2f;

    [Tooltip("성공 VFX가 생성될 때 게이트 위치에서 위로 얼마나 띄울지(m). 게이트 피벗이 바닥 쪽에 있어 그대로 두면 시야보다 낮게 나옴.")]
    [SerializeField] private float successVfxHeightOffset = 1.4f;

    [Tooltip("성공 VFX를 플레이어 쪽(이동 방향)으로 얼마나 당겨올지(m). 눈앞에 오도록 조정.")]
    [SerializeField] private float successVfxForwardOffset = 0f;

    [Tooltip("성공 시 게이트 네온이 물드는 색.")]
    [SerializeField] private Color successFlashColor = new Color(0.3f, 1f, 0.4f);

    [Tooltip("성공 시 게이트 네온이 원래보다 몇 배 밝아지는지.")]
    [SerializeField] private float successFlashIntensity = 3f;

    [Header("발광 연출 타이밍(초)")]
    [Tooltip("판정 색/밝기가 확 바뀌는 데 걸리는 시간.")]
    [SerializeField] private float flashInTime = 0.1f;

    [Tooltip("판정 색/밝기가 원래대로 돌아오는 데 걸리는 시간.")]
    [SerializeField] private float flashOutTime = 0.4f;

    [Header("사운드")]
    [Tooltip("게이트가 스폰되어 다가오는 동안 계속 재생되는 접근음 (예: 시계 초침 소리). 3D Spatial로 재생됨.")]
    [SerializeField] private AudioClip approachSfx;

    [Tooltip("성공 판정 순간 재생할 효과음. 2D로 재생됨.")]
    [SerializeField] private AudioClip successSfx;

    [Tooltip("실패 판정 순간 재생할 효과음. 2D로 재생됨.")]
    [SerializeField] private AudioClip failSfx;

    [Range(0f, 1f)]
    [SerializeField] private float approachVolume = 0.6f;

    [Range(0f, 1f)]
    [SerializeField] private float judgementVolume = 0.8f;

    [Header("등장 연출")]
    [Tooltip("생성 직후 서서히 나타나는 시간(초). 스폰 지점이 시야에 있어도 '뿅' 하고 튀어나오지 않게 함.")]
    [SerializeField] private float fadeInDuration = 1.5f;

    [Header("이동 설정")]
    [SerializeField] private float approachSpeed = 1.5f;
    [SerializeField] private Vector3 moveDirection = Vector3.back;
    [SerializeField] private float destroyPastZ = -2f;

    public event Action<bool> OnGateResolved;

    private bool _resolved;
    private AudioSource _audioSource;

    // 네온 발광 머티리얼 캐시. FadeIn과 성공/실패 발광 연출이 공유해서 쓴다.
    private readonly List<Material> _emissiveMats = new List<Material>();
    private readonly List<Color> _emissiveOrigins = new List<Color>();

    private void Reset()
    {
        GetComponent<Collider>().isTrigger = true;
    }

    private void Awake()
    {
        _audioSource = GetComponent<AudioSource>();
        _audioSource.spatialBlend = 1f;   // 3D Spatial - 게이트 자신의 위치에서 나는 소리
        _audioSource.playOnAwake = false;
        _audioSource.loop = true;

        CacheEmissiveMaterials();
    }

    private void Start()
    {
        if (fadeInDuration > 0f) StartCoroutine(FadeIn());

        if (approachSfx != null)
        {
            _audioSource.clip = approachSfx;
            _audioSource.volume = approachVolume;
            _audioSource.Play();
        }
    }

    /// 이 게이트의 모든 렌더러에서 Emission을 지원하는 머티리얼과 그 원래 색을 한 번만 수집해둔다.
    /// .materials는 이 인스턴스 전용 복제본이라 프리팹 원본은 안 건드림.
    private void CacheEmissiveMaterials()
    {
        foreach (Renderer r in GetComponentsInChildren<Renderer>())
        {
            foreach (Material m in r.materials)
            {
                if (!m.HasProperty("_EmissionColor")) continue;
                _emissiveMats.Add(m);
                _emissiveOrigins.Add(m.GetColor("_EmissionColor"));
            }
        }
    }

    /// 생성 직후 네온 발광을 0에서 원래 밝기까지 서서히 올린다.
    /// 어두운 프레임은 Fog가 가려주고, 눈에 띄는 네온만 서서히 켜져도 충분히 자연스럽다.
    private IEnumerator FadeIn()
    {
        if (_emissiveMats.Count == 0) yield break;

        float t = 0f;
        while (t < fadeInDuration)
        {
            t += Time.deltaTime;
            float k = Mathf.Clamp01(t / fadeInDuration);
            SetEmission(k);
            yield return null;
        }

        SetEmission(1f);   // 오차 없이 원래 밝기로 확정
    }

    /// 캐시된 원래 Emission 색에 배수(k)를 곱해서 전부 적용한다.
    private void SetEmission(float multiplier)
    {
        for (int i = 0; i < _emissiveMats.Count; i++)
        {
            _emissiveMats[i].SetColor("_EmissionColor", _emissiveOrigins[i] * multiplier);
        }
    }

    /// 캐시된 원래 Emission 색을 targetColor로 덮어써서 적용한다. intensity는 밝기 배수(기본 1 = 원래 밝기).
    private void SetEmissionColor(Color targetColor, float intensity = 1f)
    {
        for (int i = 0; i < _emissiveMats.Count; i++)
        {
            Color original = _emissiveOrigins[i];
            float originalIntensity = Mathf.Max(original.r, original.g, original.b, 0.001f);
            _emissiveMats[i].SetColor("_EmissionColor", targetColor * originalIntensity * intensity);
        }
    }

    /// 성공 판정: 네온 색을 초록으로 물들이면서 밝기도 확 키웠다가, 원래 색/밝기로 되돌린다.
    private IEnumerator SuccessFlash()
    {
        float t = 0f;
        while (t < flashInTime)
        {
            t += Time.deltaTime;
            float k = t / flashInTime;
            SetEmissionColor(Color.Lerp(Color.white, successFlashColor, k), Mathf.Lerp(1f, successFlashIntensity, k));
            yield return null;
        }

        t = 0f;
        while (t < flashOutTime)
        {
            t += Time.deltaTime;
            float k = t / flashOutTime;
            SetEmissionColor(Color.Lerp(successFlashColor, Color.white, k), Mathf.Lerp(successFlashIntensity, 1f, k));
            yield return null;
        }

        SetEmission(1f);   // 원래 색/밝기로 완전히 복구
    }

    /// 실패 판정: 네온 색을 빨갛게 물들였다가 원래 색으로 되돌린다. 흔들림과 동시에 재생.
    private IEnumerator FailFlash()
    {
        float t = 0f;
        while (t < flashInTime)
        {
            t += Time.deltaTime;
            SetEmissionColor(Color.Lerp(Color.white, failFlashColor, t / flashInTime));
            yield return null;
        }

        t = 0f;
        while (t < flashOutTime)
        {
            t += Time.deltaTime;
            SetEmissionColor(Color.Lerp(failFlashColor, Color.white, t / flashOutTime));
            yield return null;
        }

        // 원래 색상표로 완전히 복구 (SetEmissionColor는 배수만 다루므로 여기서 원본 그대로 재적용)
        SetEmission(1f);
    }

    /// Z축 회전으로 짧게 흔든다. transform.position은 Update()가 계속 이동시키므로
    /// 회전만 건드려서 이동 경로에 영향이 없게 한다.
    private IEnumerator Shake()
    {
        Quaternion original = transform.localRotation;
        float t = 0f;
        while (t < shakeDuration)
        {
            t += Time.deltaTime;
            float z = UnityEngine.Random.Range(-shakeAngle, shakeAngle);
            transform.localRotation = original * Quaternion.Euler(0f, 0f, z);
            yield return null;
        }
        transform.localRotation = original;
    }

    public void SetRequiredPose(PoseDetector.PoseType pose) => requiredPose = pose;

    /// <summary>스포너가 런타임에 씬의 PoseDetector를 연결해줄 때 사용. 프리팹 애셋 자체는 씬 오브젝트를 참조 못 하므로 필수.</summary>
    public void SetPoseDetector(PoseDetector detector) => poseDetector = detector;

    private void Update()
    {
        transform.position += moveDirection.normalized * approachSpeed * Time.deltaTime;

        if (transform.position.z <= destroyPastZ)
        {
            if (!_resolved)
            {
                _resolved = true;
                StopApproachSfx();
                PurificationSystem.Instance?.Decrease(purityPenaltyOnFail);
                PlayVfx(failVfxPrefab, failVfxLifetime, failVfxHeightOffset, failVfxForwardOffset);
                PlayJudgementSfx(failSfx);
                StartCoroutine(FailFlash());
                StartCoroutine(Shake());
                OnGateResolved?.Invoke(false);
                Debug.Log("[PoseGate] 판정 없이 통과됨 - 실패 처리");
            }
            Destroy(gameObject);
        }
    }

    private void OnTriggerEnter(Collider other)
    {
        if (_resolved || !other.CompareTag(playerTag) || poseDetector == null) return;
        _resolved = true;
        StopApproachSfx();

        bool success = poseDetector.IsPose(requiredPose);
        if (success)
        {
            PurificationSystem.Instance?.Increase(purityRewardOnPass);
            PlayVfx(successVfxPrefab, successVfxLifetime, successVfxHeightOffset, successVfxForwardOffset);
            PlayJudgementSfx(successSfx);
            StartCoroutine(SuccessFlash());
        }
        else
        {
            PurificationSystem.Instance?.Decrease(purityPenaltyOnFail);
            PlayVfx(failVfxPrefab, failVfxLifetime, failVfxHeightOffset, failVfxForwardOffset);
            PlayJudgementSfx(failSfx);
            StartCoroutine(FailFlash());
            StartCoroutine(Shake());
        }

        OnGateResolved?.Invoke(success);
        Debug.Log($"[PoseGate] 요구 포즈={requiredPose}, 결과={(success ? "통과" : "실패")}");
    }

    /// 거름망 위치(+ 눈높이 오프셋 + 플레이어 쪽 오프셋)에 판정 VFX를 잠깐 생성했다가 자동 파괴한다.
    /// 게이트 피벗이 바닥 쪽에 있어서 heightOffset 없이 그대로 두면 시야보다 낮게 생성되고,
    /// forwardOffset 없이 그대로 두면 게이트 위치(플레이어보다 먼 곳)에 생성돼 눈앞이 아니게 보일 수 있다.
    /// 게이트 자신은 이동/파괴가 계속되므로, VFX는 별도 오브젝트로 스폰해 게이트 파괴와 무관하게 재생시킨다.
    private void PlayVfx(GameObject prefab, float lifetime, float heightOffset, float forwardOffset)
    {
        if (prefab == null) return;

        Vector3 spawnPos = transform.position
            + Vector3.up * heightOffset
            + moveDirection.normalized * forwardOffset;
        GameObject vfx = Instantiate(prefab, spawnPos, Quaternion.identity);
        Destroy(vfx, lifetime);
    }

    private void StopApproachSfx()
    {
        if (_audioSource.isPlaying) _audioSource.Stop();
    }

    /// 판정음은 2D로 재생 — 방향/거리 상관없이 또렷하게 들려야 하는 알림음이라
    /// 별도 임시 오브젝트로 재생해서 게이트가 파괴돼도 소리는 끝까지 재생되게 한다.
    private void PlayJudgementSfx(AudioClip clip)
    {
        if (clip == null) return;

        GameObject temp = new GameObject("TempJudgementSfx");
        AudioSource src = temp.AddComponent<AudioSource>();
        src.clip = clip;
        src.spatialBlend = 0f;   // 2D
        src.volume = judgementVolume;
        src.Play();
        Destroy(temp, clip.length + 0.1f);
    }
}