using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// 거름망 실루엣 하나. 플레이어가 트리거 영역에 들어오는 순간의 포즈를 판정해서
/// 요구 포즈(PoseDetector.PoseType)와 일치하면 통과, 아니면 오염물 접촉으로 처리.
[RequireComponent(typeof(Collider))]
public class PoseGate : MonoBehaviour
{
    [SerializeField] private PoseDetector poseDetector;
    [SerializeField] private PoseDetector.PoseType requiredPose = PoseDetector.PoseType.Normal;
    [SerializeField] private float purityRewardOnPass = 4f;
    [SerializeField] private float purityPenaltyOnFail = 4f;
    [SerializeField] private string playerTag = "Player";

    [Header("등장 연출")]
    [Tooltip("생성 직후 서서히 나타나는 시간(초). 스폰 지점이 시야에 있어도 '뿅' 하고 튀어나오지 않게 함.")]
    [SerializeField] private float fadeInDuration = 1.5f;

    [Header("이동 설정")]
    [SerializeField] private float approachSpeed = 1.5f;
    [SerializeField] private Vector3 moveDirection = Vector3.back;
    [SerializeField] private float destroyPastZ = -2f;

    public event Action<bool> OnGateResolved;

    private bool _resolved;

    private void Reset()
    {
        GetComponent<Collider>().isTrigger = true;
    }

    private void Start()
    {
        if (fadeInDuration > 0f) StartCoroutine(FadeIn());
    }

    /// 생성 직후 네온 발광을 0에서 원래 밝기까지 서서히 올린다.
    /// 머티리얼이 Opaque면 알파는 먹지 않으므로 Emission 위주로 처리 —
    /// 어두운 프레임은 Fog가 가려주고, 눈에 띄는 네온만 서서히 켜져도 충분히 자연스럽다.
    private IEnumerator FadeIn()
    {
        var mats = new List<Material>();
        var origins = new List<Color>();

        foreach (Renderer r in GetComponentsInChildren<Renderer>())
        {
            foreach (Material m in r.materials)   // .materials = 이 인스턴스 전용 복제본이라 프리팹 원본은 안 건드림
            {
                if (!m.HasProperty("_EmissionColor")) continue;
                mats.Add(m);
                origins.Add(m.GetColor("_EmissionColor"));
            }
        }

        if (mats.Count == 0) yield break;

        float t = 0f;
        while (t < fadeInDuration)
        {
            t += Time.deltaTime;
            float k = Mathf.Clamp01(t / fadeInDuration);
            for (int i = 0; i < mats.Count; i++)
            {
                mats[i].SetColor("_EmissionColor", origins[i] * k);
            }
            yield return null;
        }

        // 오차 없이 원래 밝기로 확정
        for (int i = 0; i < mats.Count; i++)
        {
            mats[i].SetColor("_EmissionColor", origins[i]);
        }
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
                PurificationSystem.Instance?.Decrease(purityPenaltyOnFail);
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

        bool success = poseDetector.IsPose(requiredPose);
        if (success)
        {
            PurificationSystem.Instance?.Increase(purityRewardOnPass);
        }
        else
        {
            PurificationSystem.Instance?.Decrease(purityPenaltyOnFail);
        }

        OnGateResolved?.Invoke(success);
        Debug.Log($"[PoseGate] 요구 포즈={requiredPose}, 결과={(success ? "통과" : "실패")}");
    }
}