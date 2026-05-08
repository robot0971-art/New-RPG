using System.Collections;
using UnityEngine;
using UnityEngine.UI;

public class SceneFadeIn : MonoBehaviour
{
    [SerializeField] private Image fadeImage;
    [SerializeField, Min(0f)] private float holdDuration = 0.15f;
    [SerializeField, Min(0.01f)] private float fadeDuration = 1.5f;

    private void Awake()
    {
        if (fadeImage == null)
        {
            fadeImage = GetComponent<Image>();
        }

        SetFadeAlpha(1f);
    }

    private void Start()
    {
        if (fadeImage == null)
        {
            Debug.LogWarning("Fade image is missing.", this);
            return;
        }

        StartCoroutine(FadeIn());
    }

    private IEnumerator FadeIn()
    {
        fadeImage.gameObject.SetActive(true);
        fadeImage.raycastTarget = true;

        SetFadeAlpha(1f);

        if (holdDuration > 0f)
        {
            yield return new WaitForSecondsRealtime(holdDuration);
        }

        float elapsed = 0f;
        while (elapsed < fadeDuration)
        {
            elapsed += Time.unscaledDeltaTime;
            float progress = Mathf.Clamp01(elapsed / fadeDuration);
            SetFadeAlpha(1f - Mathf.SmoothStep(0f, 1f, progress));
            yield return null;
        }

        SetFadeAlpha(0f);
        fadeImage.raycastTarget = false;
        fadeImage.gameObject.SetActive(false);
    }

    private void SetFadeAlpha(float alpha)
    {
        if (fadeImage == null)
        {
            return;
        }

        Color color = fadeImage.color;
        color.a = alpha;
        fadeImage.color = color;
    }
}
