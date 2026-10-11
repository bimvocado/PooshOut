using UnityEngine;
using UnityEngine.SceneManagement;

// StageManager.OnStageChanged가 발행될 때 그에 맞는 씬을 실제로 로드.
// StageManager.cs 주석에 있던 "실제 씬 로딩은 나중에 붙일 예정"이라던 그 부분을 담당.
// StartScene에 배치해서 Singleton으로 DontDestroyOnLoad 유지 (PooshBot 등과 같은 방식) -
// Stage1의 Stage1Manager가 AdvanceStage()를 부르면 여기서 자동으로 "Stage2" 씬을 로드해준다.
public class StageSceneLoader : Singleton<StageSceneLoader>
{
    [Tooltip("씬 이름 규칙이 Stage1, Stage2... 형태라는 전제. 실제 씬 이름이 다르면 여기서 접두어만 바꾸면 됨.")]
    [SerializeField] private string sceneNamePrefix = "Stage";

    private void OnEnable()
    {
        if (StageManager.Instance != null)
            StageManager.Instance.OnStageChanged += HandleStageChanged;
    }

    private void OnDisable()
    {
        if (StageManager.Instance != null)
            StageManager.Instance.OnStageChanged -= HandleStageChanged;
    }

    private void HandleStageChanged(int stage)
    {
        string sceneName = $"{sceneNamePrefix}{stage}";
        Debug.Log($"[StageSceneLoader] 스테이지 {stage} → '{sceneName}' 씬 로드");
        SceneManager.LoadScene(sceneName);
    }
}
