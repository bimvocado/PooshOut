using UnityEngine;

public class Stage2CompleteUI : MonoBehaviour {
    [SerializeField] private Stage2Manager stage2Manager;
    [SerializeField] private GameObject completePanel;

    private void Start() {
        if (completePanel != null)
            completePanel.SetActive(false);
    }

    private void OnEnable() {
        if (stage2Manager != null)
            stage2Manager.OnStageCompleted += ShowCompleteUI;
    }

    private void OnDisable() {
        if (stage2Manager != null)
            stage2Manager.OnStageCompleted -= ShowCompleteUI;
    }

    private void ShowCompleteUI() {
        if (completePanel != null)
            completePanel.SetActive(true);

        Debug.Log("[Stage2CompleteUI] Stage2 완료 UI 표시");
    }
}