using System;
using System.Collections.Generic;

/// <summary>
/// 순위표 전체. PlayerData 여러 개를 리스트로 담음.
/// JsonUtility 는 리스트를 최상위에서 바로 저장 못 하기 때문에,
/// 이렇게 클래스로 한 번 감싸야 JSON 저장이 됨.
/// </summary>
[Serializable]
public class LeaderboardData
{
    public List<PlayerData> entries = new List<PlayerData>();
}
