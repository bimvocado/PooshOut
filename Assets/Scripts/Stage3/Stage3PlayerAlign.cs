using System.Collections;
using Unity.XR.CoreUtils;
using UnityEngine;
using UnityEngine.XR;

/// <summary>
/// Stage3 진입 직전까지의 실제 트래킹 상태(이전 씬에서 이어진 위치)와 무관하게,
/// 카메라를 이 씬에 원래 의도된 스폰 위치(수평 X/Z)로 정렬한다.
///
/// 높이(Y)는 건드리지 않는다 - Device Tracking Origin Mode에서는 실제 트래킹/CameraYOffset으로
/// 계산되는 값이라, 여기서 고정값으로 덮어쓰면 "그냥 시작할 때"와 다른 높이가 되어버린다.
/// XR Origin의 Tracking Origin Mode(Device/Floor) 자체도 건드리지 않는다 — 씬마다 이미
/// 다르게 설정돼 있고 그대로 유지해야 함. XROrigin.MoveCameraToWorldLocation()으로
/// 원점(Origin)만 수평 이동시켜서, 카메라가 항상 spawnPoint의 X/Z에서 시작하게 만든다.
/// (회전도 건드리지 않음 - VR에서 시야 방향을 강제로 돌리면 어지러움/방향 혼란을 유발함.)
///
/// + 이전 씬이 다른 Tracking Origin Mode였을 경우, 씬 전환 직후 한두 프레임은
/// XR 서브시스템이 아직 이 씬의 모드로 다시 자리잡는 중이라 카메라 높이가 순간적으로
/// 비정상적으로 낮게 나올 수 있다. 그래서 CurrentTrackingOriginMode가 실제로 결정될 때까지
/// (최대 maxWaitFrames 프레임) 기다렸다가 정렬한다.
/// </summary>
public class Stage3PlayerAlign : MonoBehaviour {
    [SerializeField] private XROrigin xrOrigin;
    [SerializeField] private Transform spawnPoint;
    [SerializeField] private int maxWaitFrames = 10;

    private void Start() {
        StartCoroutine(AlignAfterTrackingReady());
    }

    private IEnumerator AlignAfterTrackingReady() {
        if (xrOrigin == null) {
            xrOrigin = FindFirstObjectByType<XROrigin>();
        }

        if (xrOrigin == null || spawnPoint == null) {
            Debug.LogWarning($"{nameof(Stage3PlayerAlign)}: xrOrigin 또는 spawnPoint가 연결되지 않았습니다.");
            yield break;
        }

        int waited = 0;
        while (xrOrigin.CurrentTrackingOriginMode == TrackingOriginModeFlags.Unknown && waited < maxWaitFrames) {
            waited++;
            yield return null;
        }

        // 높이는 지금 트래킹이 계산해준 값을 그대로 유지하고, 수평 위치만 맞춘다.
        Vector3 currentCameraPos = xrOrigin.Camera.transform.position;
        Vector3 target = new Vector3(
            spawnPoint.position.x,
            currentCameraPos.y,
            spawnPoint.position.z
        );

        xrOrigin.MoveCameraToWorldLocation(target);
    }
}
