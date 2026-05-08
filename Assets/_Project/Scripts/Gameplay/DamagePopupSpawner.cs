using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

public sealed class DamagePopupSpawner : MonoBehaviour
{
    [Header("Templates")]
    [SerializeField] private TextMeshProUGUI damageTextTemplate;
    [SerializeField] private TextMeshProUGUI critTextTemplate;

    [Header("Motion")]
    [SerializeField] private Vector3 worldOffset = new Vector3(0f, 1.35f, 0f);
    [SerializeField] private float duration = 0.65f;
    [SerializeField] private float riseDistance = 0.45f;
    [SerializeField] private float horizontalJitter = 0.2f;
    [SerializeField] private Vector3 normalScale = Vector3.one;
    [SerializeField] private Vector3 criticalScale = new Vector3(1.15f, 1.15f, 1f);

    private readonly Queue<TextMeshProUGUI> damagePool = new Queue<TextMeshProUGUI>();
    private readonly Queue<TextMeshProUGUI> critPool = new Queue<TextMeshProUGUI>();

    private void Awake()
    {
        HideTemplate(damageTextTemplate);
        HideTemplate(critTextTemplate);
    }

    public void ShowDamage(float amount, bool isCritical, Vector3 worldPosition)
    {
        TextMeshProUGUI template = isCritical ? critTextTemplate : damageTextTemplate;
        if (template == null)
        {
            return;
        }

        TextMeshProUGUI popup = GetPopup(template, isCritical);
        RectTransform popupTransform = popup.rectTransform;
        int displayDamage = Mathf.Max(0, Mathf.CeilToInt(amount));
        Vector3 startPosition = worldPosition + worldOffset;
        startPosition.x += Random.Range(-horizontalJitter, horizontalJitter);

        popup.text = displayDamage.ToString();
        popup.alpha = 1f;
        popupTransform.position = startPosition;
        popupTransform.localScale = isCritical ? criticalScale : normalScale;
        popup.gameObject.SetActive(true);

        StartCoroutine(AnimatePopup(popup, startPosition, isCritical));
    }

    private TextMeshProUGUI GetPopup(TextMeshProUGUI template, bool isCritical)
    {
        Queue<TextMeshProUGUI> pool = isCritical ? critPool : damagePool;
        while (pool.Count > 0)
        {
            TextMeshProUGUI pooled = pool.Dequeue();
            if (pooled != null)
            {
                return pooled;
            }
        }

        TextMeshProUGUI instance = Instantiate(template, template.transform.parent);
        instance.name = isCritical ? "Crit Damage Popup" : "Damage Popup";
        return instance;
    }

    private IEnumerator AnimatePopup(TextMeshProUGUI popup, Vector3 startPosition, bool isCritical)
    {
        float elapsed = 0f;
        float safeDuration = Mathf.Max(0.01f, duration);
        Vector3 endPosition = startPosition + Vector3.up * riseDistance;

        while (elapsed < safeDuration)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / safeDuration);
            float eased = 1f - Mathf.Pow(1f - t, 2f);

            if (popup == null)
            {
                yield break;
            }

            popup.rectTransform.position = Vector3.LerpUnclamped(startPosition, endPosition, eased);
            popup.alpha = 1f - t;
            yield return null;
        }

        ReleasePopup(popup, isCritical);
    }

    private void ReleasePopup(TextMeshProUGUI popup, bool isCritical)
    {
        if (popup == null)
        {
            return;
        }

        popup.gameObject.SetActive(false);
        popup.alpha = 1f;

        if (isCritical)
        {
            critPool.Enqueue(popup);
        }
        else
        {
            damagePool.Enqueue(popup);
        }
    }

    private static void HideTemplate(TextMeshProUGUI template)
    {
        if (template != null)
        {
            template.gameObject.SetActive(false);
        }
    }
}
