using UnityEngine;

// Stage3 씬에 빈 오브젝트 만들어서 이 스크립트 붙이고, 정화봇을 놓고 싶은 위치로 옮겨두면 됨.
// 이 오브젝트가 켜지는 순간(씬 진입) 정화봇이 여기로 순간이동해서 고정되고,
// 꺼지는 순간(씬 이탈) 다시 오른쪽 위 따라다니기로 복귀한다.
public class Stage3PooshAnchor : MonoBehaviour
{
    private void OnEnable()
    {
        PooshBotDirector.Instance?.EnterStage3(transform);
    }

    private void OnDisable()
    {
        PooshBotDirector.Instance?.ExitStage3();
    }
}
