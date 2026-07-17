using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 씬에 순서대로 배치한 PipeSegment(직선/곡선/T자)들을 이어붙여
/// RailMover가 따라갈 하나의 웨이포인트 리스트를 만든다.
///
/// 이어붙이는 두 세그먼트는 맞닿는 지점의 웨이포인트가 같은 위치를 가리켜야 한다
/// (이전 세그먼트의 마지막 웨이포인트 = 다음 세그먼트의 첫 웨이포인트).
/// 겹치는 지점은 자동으로 하나만 남긴다.
/// </summary>
[RequireComponent(typeof(RailMover))]
public class RailCourse : MonoBehaviour
{
    [Header("코스 순서대로 등록 (입구 → 출구 방향)")]
    [SerializeField] private List<PipeSegment> segments = new List<PipeSegment>();
    [SerializeField] private RailMover railMover;

    private void Awake()
    {
        var combined = new List<Transform>();

        foreach (PipeSegment segment in segments)
        {
            if (segment == null || segment.Waypoints.Count == 0) continue;

            int startIndex = combined.Count > 0 ? 1 : 0; // 이전 세그먼트와 겹치는 접점 제거
            for (int i = startIndex; i < segment.Waypoints.Count; i++)
            {
                if (segment.Waypoints[i] != null)
                    combined.Add(segment.Waypoints[i]);
            }
        }

        if (combined.Count < 2)
        {
            Debug.LogWarning("[RailCourse] 세그먼트를 연결해서 만든 웨이포인트가 2개 미만입니다. segments를 확인하세요.");
            return;
        }

        if (railMover != null) railMover.SetWaypoints(combined);
    }
}
