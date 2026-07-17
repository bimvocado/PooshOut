using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 하수관 파이프 프리팹(직선/약곡선/강곡선/T자)에 붙는 컴포넌트.
/// 입구 → 출구 순서로 정렬된 웨이포인트를 보유한다.
/// T자의 경우 실제로 갈라지는 분기가 아니라 한쪽 팔은 막혀있는 구조이므로,
/// 이 컴포넌트도 다른 타입과 동일하게 "입구 → 출구" 단일 경로만 가지면 된다.
/// 어느 팔이 뚫려있는지는 웨이포인트를 배치하는 디자이너가 결정한다.
///
/// RailCourse가 씬에 놓인 PipeSegment들을 순서대로 이어붙여
/// RailMover가 따라갈 전체 경로를 만든다.
/// </summary>
public class PipeSegment : MonoBehaviour
{
    public enum SegmentType { Straight, SlightCurve, SharpCurve, TJunction }

    [SerializeField] private SegmentType type = SegmentType.Straight;

    [Header("입구 → 출구 순서로 배치 (직선 2개 / 곡선 4~5개)")]
    [SerializeField] private List<Transform> waypoints = new List<Transform>();

    public SegmentType Type => type;
    public IReadOnlyList<Transform> Waypoints => waypoints;

#if UNITY_EDITOR
    private void OnDrawGizmos()
    {
        if (waypoints == null || waypoints.Count == 0) return;

        Gizmos.color = Color.cyan;
        for (int i = 0; i < waypoints.Count; i++)
        {
            if (waypoints[i] == null) continue;
            Gizmos.DrawSphere(waypoints[i].position, 0.08f);
            if (i > 0 && waypoints[i - 1] != null)
                Gizmos.DrawLine(waypoints[i - 1].position, waypoints[i].position);
        }
    }
#endif
}
