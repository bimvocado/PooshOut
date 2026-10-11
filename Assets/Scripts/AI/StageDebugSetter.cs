using UnityEngine;

// 단독 씬 테스트/촬영용: 씬 시작 시 StageManager.CurrentStage를 강제로 지정된 값으로 맞춰준다.
// StageManager.CurrentStage는 private set이라 Inspector에서 직접 못 바꾸기 때문에 필요함.
//
// 사용법: 이 스크립트를 씬의 아무 오브젝트(StageManager 자신도 OK)에 붙이고
// targetStage를 이 씬 번호로 맞추면 됨. Stage2 씬이면 2, Stage3이면 3.
//
// ⚠️ 정식으로 StartScene부터 이어서 플레이할 때는 이 오브젝트를 씬에서 빼거나
// 비활성화해둘 것 - 안 그러면 이미 3으로 진행 중인데 다시 2로 강제로 되돌리는 등
// 실제 진행 흐름과 충돌할 수 있음.
public class StageDebugSetter : MonoBehaviour
{
    [SerializeField] private int targetStage = 2;

    private void Start()
    {
        if (StageManager.Instance != null)
        {
            StageManager.Instance.SetStage(targetStage);
        }
    }
}
