using UnityEngine;
using UnityEngine.UI;

public class BlinkingUIImage : MonoBehaviour
{
    [SerializeField] private float blinkInterval = 0.7f;
    [SerializeField] private Image targetImage;

    private float timer;
    private bool isVisible = true;

    private void Awake()
    {
        if (targetImage == null)
            targetImage = GetComponent<Image>();
    }

    private void Update()
    {
        timer += Time.unscaledDeltaTime;

        if (timer >= blinkInterval)
        {
            timer = 0f;
            isVisible = !isVisible;

            Color c = targetImage.color;
            c.a = isVisible ? 1f : 0f;
            targetImage.color = c;
        }
    }
}
