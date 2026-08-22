using UnityEngine;

/// <summary>
/// 오염물/버블 아이템 풀링 공용 유틸.
/// Bubble.prefab처럼 아이템 스크립트가 풀링 루트에 바로 붙어있는 경우와, Trash-*_glow 프리팹처럼
/// 글로우 파티클 루트 밑에 실제 아이템(BubbleItem/PollutantObject)이 중첩 프리팹 자식으로 들어있는
/// 경우를 구분하지 않고, PollutantSpawner가 실제로 풀링 중인 오브젝트를 찾아 비활성화하기 위함.
/// </summary>
public static class PooledItemUtil
{
    /// <summary>
    /// item의 조상 중 "부모가 PollutantSpawner인 오브젝트"(= 스포너가 직접 Instantiate한 풀링 루트)를
    /// 찾아 비활성화한다. item 자신이 이미 풀링 루트면(Bubble처럼) 자기 자신이 꺼진다.
    /// 이렇게 하지 않고 item 자신만 끄면, 글로우 파티클(루트) 밑에 실제 아이템(자식)이 들어있는
    /// 구조에서 자식만 꺼지고 루트의 파티클은 계속 살아남아 "쓰레기 없이 파티클만 남는" 문제가 생긴다.
    /// </summary>
    public static void DeactivatePooledRoot(Transform item)
    {
        Transform t = item;
        while (t.parent != null && t.parent.GetComponent<PollutantSpawner>() == null)
            t = t.parent;
        t.gameObject.SetActive(false);
    }
}
