using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.XR.Interaction.Toolkit.UI;

public class StartSceneManager : MonoBehaviour {
    [Header("12개 칭호 오브젝트")]
    [SerializeField] private List<GameObject> titleObjects = new();

    [Header("상단 선택된 칭호")]
    [SerializeField] private TMP_Text selectedTitleText;

    [Header("Hover 설정")]
    [SerializeField] private float hoverScale = 1.2f;
    [SerializeField] private float hoverSpeed = 10f;

    private string selectedTitle;

    // GameObject -> 그 안의 TMP
    private readonly Dictionary<GameObject, TMP_Text> objectTexts = new();

    // 각 TMP 원래 크기
    private readonly Dictionary<TMP_Text, Vector3> originalScales = new();

    // 각 TMP 애니메이션
    private readonly Dictionary<TMP_Text, Coroutine> scaleCoroutines = new();


    private void Awake() {
        SetupTitleObjects();
    }


    private void SetupTitleObjects() {
        foreach (GameObject obj in titleObjects) {
            if (obj == null)
                continue;

            // 중요:
            // titleObjects에는 반드시 각각의 NameButton을 넣어야 함.
            TMP_Text titleText = obj.GetComponentInChildren<TMP_Text>(true);

            if (titleText == null) {
                Debug.LogWarning($"{obj.name} 안에 TMP_Text가 없습니다.");
                continue;
            }

            objectTexts[obj] = titleText;
            originalScales[titleText] = titleText.rectTransform.localScale;

            Debug.Log(
                $"[칭호 연결] GameObject = {obj.name} / TMP = {titleText.name} / 내용 = {titleText.text}"
            );

            SetupPointerEvents(obj);
        }
    }


    // ==================================================
    // Pointer Event
    // ==================================================

    private void SetupPointerEvents(GameObject obj)
{
    GameObject currentObj = obj;

    EventTrigger trigger = currentObj.GetComponent<EventTrigger>();

    if (trigger == null)
        trigger = currentObj.AddComponent<EventTrigger>();

    if (trigger.triggers == null)
        trigger.triggers = new List<EventTrigger.Entry>();


    // VR Ray Hover Enter
    EventTrigger.Entry enterEntry = new EventTrigger.Entry
    {
        eventID = EventTriggerType.PointerEnter
    };

    enterEntry.callback.AddListener((data) =>
    {
        // 마우스 무시하고 XR Ray만 받기
        if (data is not TrackedDeviceEventData)
            return;

        OnHoverEnter(currentObj);
    });

    trigger.triggers.Add(enterEntry);


    // VR Ray Hover Exit
    EventTrigger.Entry exitEntry = new EventTrigger.Entry
    {
        eventID = EventTriggerType.PointerExit
    };

    exitEntry.callback.AddListener((data) =>
    {
        if (data is not TrackedDeviceEventData)
            return;

        OnHoverExit(currentObj);
    });

    trigger.triggers.Add(exitEntry);


    // VR Ray Click
    EventTrigger.Entry clickEntry = new EventTrigger.Entry
    {
        eventID = EventTriggerType.PointerClick
    };

    clickEntry.callback.AddListener((data) =>
    {
        if (data is not TrackedDeviceEventData)
            return;

        SelectTitle(currentObj);
    });

    trigger.triggers.Add(clickEntry);
}


    // ==================================================
    // Hover
    // ==================================================

    private void OnHoverEnter(GameObject obj) {
        if (!objectTexts.TryGetValue(obj, out TMP_Text titleText))
            return;


        Vector3 originalScale = originalScales[titleText];

        StartScaleAnimation(
            titleText,
            originalScale * hoverScale
        );
    }


    private void OnHoverExit(GameObject obj) {
        if (!objectTexts.TryGetValue(obj, out TMP_Text titleText))
            return;

        Vector3 originalScale = originalScales[titleText];

        StartScaleAnimation(
            titleText,
            originalScale
        );
    }


    // ==================================================
    // 선택
    // ==================================================

    private void SelectTitle(GameObject obj) {
        if (!objectTexts.TryGetValue(obj, out TMP_Text titleText))
            return;

        selectedTitle = titleText.text;

        if (selectedTitleText != null)
            selectedTitleText.text = selectedTitle;

        Debug.Log($"선택된 칭호 : {selectedTitle}");

        // TODO: 서버에 selectedTitle 저장
    }


    // ==================================================
    // TMP Scale Animation
    // ==================================================

    private void StartScaleAnimation(
        TMP_Text text,
        Vector3 targetScale) {
        if (scaleCoroutines.TryGetValue(text, out Coroutine coroutine)) {
            if (coroutine != null)
                StopCoroutine(coroutine);
        }

        scaleCoroutines[text] =
            StartCoroutine(
                ScaleRoutine(text, targetScale)
            );
    }


    private IEnumerator ScaleRoutine(
        TMP_Text text,
        Vector3 targetScale) {
        RectTransform rect = text.rectTransform;

        while (
            Vector3.Distance(
                rect.localScale,
                targetScale
            ) > 0.01f) {
            rect.localScale = Vector3.Lerp(
                rect.localScale,
                targetScale,
                Time.unscaledDeltaTime * hoverSpeed
            );

            yield return null;
        }

        rect.localScale = targetScale;
        scaleCoroutines[text] = null;
    }
}