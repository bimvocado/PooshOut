/// <summary>
/// FastAPI 서버 주소 한 곳에서만 관리. LLMConnector/SaveLoadManager가 여기를 참조해서
/// 서버 주소가 바뀌어도(로컬 개발 → 배포) 이 파일 한 줄만 고치면 됨.
/// </summary>
public static class ServerConfig
{
    public const string SERVER_URL = "https://pooshout.onrender.com";
}
