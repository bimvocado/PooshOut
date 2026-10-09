using UnityEngine;
using UnityEngine.SceneManagement;

// 레버를 당기면 Stage1 씬으로 전환.
// LeverController.cs(회전 애니메이션 담당)는 건드리지 않고, 이 함수만
// XR Simple Interactable의 Interactable Events(Select Entered 또는 Activated)에 추가로 연결하면 됨.
//
// StageManager.CurrentStage는 기본값이 이미 1(FirstStage)이라 여기서 AdvanceStage()를 부르면
// "1번을 방금 클리어했다"고 착각해서 곧바로 2번으로 넘어가버린다. 그래서 StageManager는 거치지 않고
// 씬만 직접 로드한다 (이후 Stage1→2→3→4 전환은 각 스테이지가 끝날 때 AdvanceStage()를 부르고,
// StageSceneLoader.cs가 그 이벤트를 받아 씬을 로드하는 방식으로 처리됨).
public class LeverStageStarter : MonoBehaviour
{
    [SerializeField] private string firstStageSceneName = "Stage1";

    public void StartFirstStage()
    {
        Debug.Log($"[LeverStageStarter] '{firstStageSceneName}' 씬으로 전환");
        SceneManager.LoadScene(firstStageSceneName);
    }
}
