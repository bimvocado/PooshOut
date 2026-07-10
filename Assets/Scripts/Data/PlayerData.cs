using System;

/// <summary>
/// 플레이어 한 명의 기록. 이름 + 최종 정화도 + 등급.
/// [Serializable] 이 붙어 있어야 JSON으로 저장/불러오기가 됨.
/// </summary>
[Serializable]
public class PlayerData
{
    public string playerName;   // 이니셜 3글자 (예: "KHW")
    public float purity;        // 최종 정화도 0~100
    public string grade;        // 등급 문자열 (예: "일급수 황금 물방울")

    public PlayerData() { }     // JSON 역직렬화용 기본 생성자

    public PlayerData(string playerName, float purity, string grade)
    {
        this.playerName = playerName;
        this.purity = purity;
        this.grade = grade;
    }
}
