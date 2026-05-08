using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public sealed class StageTransitionUI : MonoBehaviour
{
    [Header("Fade")]
    [SerializeField] private Image fadeImage;
    [SerializeField, Min(0.01f)] private float fadeOutDuration = 0.5f;
    [SerializeField, Min(0f)] private float holdBlackDuration = 0.2f;
    [SerializeField, Min(0.01f)] private float fadeInDuration = 0.7f;

    [Header("Intro")]
    [SerializeField] private TMP_Text stageText;
    [SerializeField] private TMP_Text battleStartText;
    [SerializeField, Min(0f)] private float stageTextDuration = 0.85f;
    [SerializeField, Min(0f)] private float battleStartDuration = 0.65f;
    [SerializeField, Min(0f)] private float battleStartDelay = 0.2f;

    private void Awake()
    {
        SetFadeAlpha(0f);
        SetTextAlpha(stageText, 0f);
        SetTextAlpha(battleStartText, 0f);

        if (fadeImage != null)
        {
            fadeImage.raycastTarget = false;
        }
    }

    public IEnumerator PlayStageTransition(int stage)
    {
        yield return Fade(0f, 1f, fadeOutDuration, true);

        if (holdBlackDuration > 0f)
        {
            yield return new WaitForSecondsRealtime(holdBlackDuration);
        }

        yield return Fade(1f, 0f, fadeInDuration, true);
        yield return PlayStageIntro(stage);
    }

    public IEnumerator PlayStageIntro(int stage)
    {
        if (stageText != null)
        {
            stageText.text = $"Stage {Mathf.Max(1, stage)}";
            yield return FlashText(stageText, stageTextDuration);
        }

        if (battleStartDelay > 0f)
        {
            yield return new WaitForSecondsRealtime(battleStartDelay);
        }

        if (battleStartText != null)
        {
            battleStartText.text = "Battle Start";
            yield return FlashText(battleStartText, battleStartDuration);
        }
    }

    private IEnumerator Fade(float from, float to, float duration, bool blockRaycasts)
    {
        if (fadeImage == null)
        {
            yield break;
        }

        fadeImage.gameObject.SetActive(true);
        fadeImage.transform.SetAsLastSibling();
        fadeImage.raycastTarget = blockRaycasts;

        float elapsed = 0f;
        while (elapsed < duration)
        {
            elapsed += Time.unscaledDeltaTime;
            float progress = Mathf.Clamp01(elapsed / duration);
            SetFadeAlpha(Mathf.Lerp(from, to, Mathf.SmoothStep(0f, 1f, progress)));
            yield return null;
        }

        SetFadeAlpha(to);
        fadeImage.raycastTarget = to > 0.01f && blockRaycasts;
    }

    private IEnumerator FlashText(TMP_Text text, float duration)
    {
        if (text == null)
        {
            yield break;
        }

        text.gameObject.SetActive(true);
        text.transform.SetAsLastSibling();
        SetTextAlpha(text, 1f);

        if (duration > 0f)
        {
            yield return new WaitForSecondsRealtime(duration);
        }

        float fadeDuration = 0.2f;
        float elapsed = 0f;
        while (elapsed < fadeDuration)
        {
            elapsed += Time.unscaledDeltaTime;
            SetTextAlpha(text, 1f - Mathf.Clamp01(elapsed / fadeDuration));
            yield return null;
        }

        SetTextAlpha(text, 0f);
        text.gameObject.SetActive(false);
    }

    private void SetFadeAlpha(float alpha)
    {
        if (fadeImage == null)
        {
            return;
        }

        Color color = fadeImage.color;
        color.a = Mathf.Clamp01(alpha);
        fadeImage.color = color;
    }

    private static void SetTextAlpha(TMP_Text text, float alpha)
    {
        if (text == null)
        {
            return;
        }

        Color color = text.color;
        color.a = Mathf.Clamp01(alpha);
        text.color = color;
    }
}
