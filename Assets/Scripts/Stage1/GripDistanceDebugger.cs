using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit.Interactors.Casters;

/// <summary>
/// [디버그 전용] Grip 판정이 안 될 때, "손이 실제로 그립 콜라이더 범위 안에 들어오긴 하는지"를
/// 콘솔 로그로 확인하기 위한 스크립트. Near-Far Interactor의 실제 판정 지점(컨트롤러 트랜스폼)과
/// Grip_L/Grip_R 사이의 거리를 주기적으로 찍는다.
///
/// 허용 반경은 인스펙터에 값을 따로 적어두지 않고, 매 프레임 실제 컴포넌트에서 직접 읽어온다
/// (Grip L/R의 SphereCollider.radius, Controller의 SphereInteractionCaster.castRadius).
/// 그래야 씬에서 반경을 바꿔도 이 로그가 항상 실제 판정 기준과 같은 값을 보여준다.
/// 해당 컴포넌트가 안 붙어있으면 Fallback 필드값을 대신 쓴다.
///
/// 사용법:
///  1. 아무 오브젝트(예: HandleController가 있는 오브젝트)에 이 스크립트를 추가
///  2. Left Controller / Right Controller에 XR Origin 하위의 실제 컨트롤러(Interactor) 트랜스폼을 드래그
///     (Poo-Hand 비주얼 오브젝트가 아니라, Near-Far Interactor가 붙어있는 그 컨트롤러 오브젝트)
///  3. Grip L / Grip R에 Cart의 Grip_L, Grip_R 트랜스폼을 드래그
///  4. Play(또는 헤드셋 연결 후 빌드)해서 콘솔에서 거리 로그 확인
/// </summary>
public class GripDistanceDebugger : MonoBehaviour
{
    [Header("판정 지점 (Poo-Hand 비주얼이 아니라 실제 컨트롤러/Interactor 트랜스폼)")]
    [SerializeField] private Transform leftController;
    [SerializeField] private Transform rightController;

    [Header("그립 콜라이더")]
    [SerializeField] private Transform gripL;
    [SerializeField] private Transform gripR;

    [Header("Fallback 반경 (위 오브젝트에서 해당 컴포넌트를 못 찾았을 때만 사용, 월드 스케일 반영된 값으로 적을 것)")]
    [SerializeField] private float fallbackGripColliderRadius = 0.7f; // Grip 로컬 반경 1.0 x 부모(Cart 본체) 스케일 0.7
    [SerializeField] private float fallbackNearCasterRadius = 0.1f;

    [Header("로그 주기")]
    [SerializeField] private float logInterval = 0.5f;
    [SerializeField] private bool showOnScreenOverlay = true;

    private float _timer;

    private void Update()
    {
        _timer += Time.deltaTime;
        if (_timer < logInterval) return;
        _timer = 0f;

        LogPair("LEFT", leftController, gripL);
        LogPair("RIGHT", rightController, gripR);
    }

    private float GetCasterRadius(Transform controller)
    {
        if (controller != null && controller.TryGetComponent(out SphereInteractionCaster caster))
            return caster.castRadius * controller.lossyScale.x;
        return fallbackNearCasterRadius;
    }

    private float GetGripRadius(Transform grip)
    {
        if (grip != null && grip.TryGetComponent(out SphereCollider collider))
            return collider.radius * grip.lossyScale.x;
        return fallbackGripColliderRadius;
    }

    private void LogPair(string label, Transform controller, Transform grip)
    {
        if (controller == null || grip == null)
        {
            Debug.LogWarning($"[GripDistanceDebugger] {label}: 트랜스폼이 비어있음 (controller={(controller == null ? "null" : controller.name)}, grip={(grip == null ? "null" : grip.name)})");
            return;
        }

        float combinedRange = GetCasterRadius(controller) + GetGripRadius(grip);
        float distance = Vector3.Distance(controller.position, grip.position);
        bool inRange = distance <= combinedRange;

        Debug.Log($"[GripDistanceDebugger] {label} dist={distance:F3}m (허용 {combinedRange:F3}m 이내={inRange}) " +
                   $"controller={controller.name}@{controller.position:F3} grip={grip.name}@{grip.position:F3}");
    }

    private void OnGUI()
    {
        if (!showOnScreenOverlay) return;

        GUI.Label(new Rect(10, 110, 500, 20), "[GripDistanceDebugger]");
        DrawDistanceLine(130, "LEFT", leftController, gripL);
        DrawDistanceLine(150, "RIGHT", rightController, gripR);
    }

    private void DrawDistanceLine(int y, string label, Transform controller, Transform grip)
    {
        string text;
        if (controller == null || grip == null)
        {
            text = $"{label}: 트랜스폼 미할당";
        }
        else
        {
            float combinedRange = GetCasterRadius(controller) + GetGripRadius(grip);
            float distance = Vector3.Distance(controller.position, grip.position);
            text = $"{label}: {distance:F3}m (허용 {combinedRange:F3}m, in-range={distance <= combinedRange})";
        }
        GUI.Label(new Rect(10, y, 500, 20), text);
    }
}
