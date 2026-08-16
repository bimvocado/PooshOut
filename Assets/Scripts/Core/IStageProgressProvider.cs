/// <summary>
/// 현재 스테이지의 진행도(0~1)를 제공하는 컴포넌트가 구현하는 인터페이스.
/// WristUIController처럼 스테이지에 상관없이 동작해야 하는 UI가 특정 스테이지의
/// 이동/진행 컴포넌트 타입을 직접 참조하지 않고도 진행도를 읽을 수 있게 해줌.
/// </summary>
public interface IStageProgressProvider
{
    float NormalizedProgress { get; }
}
