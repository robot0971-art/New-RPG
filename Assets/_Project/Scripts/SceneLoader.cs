using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class SceneLoader : MonoBehaviour
{
    [SerializeField] private string sceneName = "Main Game";
    [SerializeField] private Image fadeImage;
    [SerializeField, Min(0.01f)] private float fadeDuration = 0.75f;

    private bool isLoading;

    public void LoadScene()
    {
        if (string.IsNullOrWhiteSpace(sceneName))
        {
            Debug.LogWarning("Scene name is empty.", this);
            return;
        }

        if (isLoading)
        {
            return;
        }

        StartCoroutine(LoadSceneWithFade());
    }

    private IEnumerator LoadSceneWithFade()
    {
        isLoading = true;

        if (fadeImage != null)
        {
            fadeImage.raycastTarget = true;
            fadeImage.gameObject.SetActive(true);
            fadeImage.transform.SetAsLastSibling();

            Color color = fadeImage.color;
            float elapsed = 0f;

            while (elapsed < fadeDuration)
            {
                elapsed += Time.unscaledDeltaTime;
                color.a = Mathf.Clamp01(elapsed / fadeDuration);
                fadeImage.color = color;
                yield return null;
            }

            color.a = 1f;
            fadeImage.color = color;
        }

        SceneManager.LoadScene(sceneName);
    }
}
