using UnityEngine;
using UnityEngine.UI;

public sealed class ExpBarUI : MonoBehaviour
{
    [SerializeField] private AutoBattleUnit player;
    [SerializeField] private Slider expSlider;
    [SerializeField] private Image expFillImage;
    [SerializeField] private RectTransform backgroundRect;
    [SerializeField] private RectTransform fillAreaRect;
    [SerializeField] private bool applyFillAreaLayout;
    [SerializeField] private Vector4 fillAreaPadding;
    [SerializeField] private Vector2 fillAreaOffset;
    [SerializeField] private float debugFillAmount = -1f;

    private void Awake()
    {
        ResolveReferences();
    }

    private void Update()
    {
        if (player == null)
        {
            player = DIContainer.Global.Resolve<AutoBattleUnit>();
        }

        if (player != null)
        {
            UpdateExpBar(player);
        }
    }

    private void ResolveReferences()
    {
        if (expSlider == null)
        {
            expSlider = GetComponentInChildren<Slider>(true);
        }

        if (expFillImage == null)
        {
            var images = GetComponentsInChildren<Image>(true);
            for (int i = 0; i < images.Length; i++)
            {
                if (images[i].name == "Fill")
                {
                    expFillImage = images[i];
                    break;
                }
            }
        }

        if (fillAreaRect == null && expFillImage != null)
        {
            fillAreaRect = expFillImage.rectTransform.parent as RectTransform;
        }

        if (backgroundRect == null)
        {
            var child = transform.Find("Background");
            if (child != null)
            {
                backgroundRect = child as RectTransform;
            }
        }

        if (applyFillAreaLayout)
        {
            ApplyFillAreaLayout();
        }

        if (expSlider != null && expFillImage != null)
        {
            expSlider.fillRect = expFillImage.rectTransform;
        }
    }

    private void ApplyFillAreaLayout()
    {
        if (fillAreaRect == null || backgroundRect == null)
        {
            return;
        }

        fillAreaRect.anchorMin = backgroundRect.anchorMin;
        fillAreaRect.anchorMax = backgroundRect.anchorMax;
        fillAreaRect.offsetMin = backgroundRect.offsetMin + new Vector2(fillAreaPadding.x + fillAreaOffset.x, fillAreaPadding.y + fillAreaOffset.y);
        fillAreaRect.offsetMax = backgroundRect.offsetMax + new Vector2(-fillAreaPadding.z + fillAreaOffset.x, -fillAreaPadding.w + fillAreaOffset.y);
    }

    private void OnValidate()
    {
        ResolveReferences();
    }

    private void OnEnable()
    {
        ResolveReferences();

        if (player == null)
        {
            player = DIContainer.Global.Resolve<AutoBattleUnit>();
        }

        if (player != null)
        {
            player.ExperienceChanged += UpdateExpBar;
            UpdateExpBar(player);
        }
    }

    private void OnDisable()
    {
        if (player != null)
        {
            player.ExperienceChanged -= UpdateExpBar;
        }
    }

    private void UpdateExpBar(AutoBattleUnit unit)
    {
        float requiredExp = Mathf.Max(1f, unit.RequiredExp);
        float normalizedExp = Mathf.Clamp01(unit.CurrentExp / requiredExp);
        if (debugFillAmount >= 0f)
        {
            normalizedExp = Mathf.Clamp01(debugFillAmount);
        }

        if (expSlider != null)
        {
            expSlider.minValue = 0f;
            expSlider.maxValue = 1f;
            expSlider.value = normalizedExp;
        }

        if (expFillImage != null)
        {
            expFillImage.enabled = true;
            expFillImage.type = Image.Type.Simple;

            var color = expFillImage.color;
            color.a = Mathf.Max(color.a, 0.8f);
            expFillImage.color = color;
        }
    }
}
