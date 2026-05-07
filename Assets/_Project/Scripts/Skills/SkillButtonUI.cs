using TMPro;
using UnityEngine;
using UnityEngine.UI;

public sealed class SkillButtonUI : MonoBehaviour
{
    [SerializeField] private SkillType skillType;
    [SerializeField] private Button button;
    [SerializeField] private Image cooldownFillImage;
    [SerializeField] private TMP_Text cooldownText;
    [SerializeField] private GameObject lockOverlay;
    [SerializeField] private TMP_Text lockLevelText;
    [SerializeField] private Sprite lockSprite;
    [SerializeField] private Color lockTextColor = Color.white;
    [SerializeField] private int lockTextFontSize = 14;
    [SerializeField] private Vector2 lockOverlayAnchorPadding = new Vector2(6f, 6f);
    [SerializeField] private Vector2 lockTextOffset;
    [SerializeField] private float lockTextHeight = 24f;
    [SerializeField] private bool showLockLevelText = true;
    [SerializeField] private bool preserveLockSpriteAspect;
    [SerializeField] private bool createLockOverlayIfMissing = true;

    private SkillManager skillManager;
    private SkillLoadoutUI skillLoadout;

    private void Awake()
    {
        if (button == null)
        {
            button = GetComponent<Button>();
        }

        EnsureLockOverlay();
    }

    private void OnEnable()
    {
        ResolveManager();
        ResolveLoadout();
        EnsureLockOverlay();

        if (button != null)
        {
            button.onClick.AddListener(UseSkill);
        }

        if (skillManager != null)
        {
            skillManager.SkillStateChanged += OnSkillStateChanged;
        }

        if (skillLoadout != null)
        {
            skillLoadout.LoadoutChanged += Refresh;
        }

        Refresh();
    }

    private void OnDisable()
    {
        if (button != null)
        {
            button.onClick.RemoveListener(UseSkill);
        }

        if (skillManager != null)
        {
            skillManager.SkillStateChanged -= OnSkillStateChanged;
        }

        if (skillLoadout != null)
        {
            skillLoadout.LoadoutChanged -= Refresh;
        }
    }

    private void Update()
    {
        Refresh();
    }

    private void UseSkill()
    {
        ResolveManager();
        ResolveLoadout();

        SkillLoadoutUI assigningLoadout = SkillLoadoutUI.CurrentAssigningLoadout;
        if (assigningLoadout != null && assigningLoadout.IsAssigning)
        {
            assigningLoadout.TryAssignSkill(skillType);
            Refresh();
            return;
        }

        skillManager?.TryUseSkill(skillType);
        Refresh();
    }

    private void OnSkillStateChanged(SkillType changedSkillType)
    {
        if (changedSkillType == skillType)
        {
            Refresh();
        }
    }

    public void Refresh()
    {
        ResolveManager();
        ResolveLoadout();
        SkillLoadoutUI assigningLoadout = SkillLoadoutUI.CurrentAssigningLoadout;
        SkillLoadoutUI activeLoadout = assigningLoadout != null
            ? assigningLoadout
            : SkillLoadoutUI.ActiveLoadout != null
                ? SkillLoadoutUI.ActiveLoadout
                : skillLoadout;

        float remaining = skillManager != null ? skillManager.GetCooldownRemaining(skillType) : 0f;
        float duration = skillManager != null ? skillManager.GetCooldownDuration(skillType) : 0f;
        bool isCoolingDown = remaining > 0f;
        bool isUnlocked = skillManager != null && skillManager.IsSkillUnlocked(skillType);
        bool isAssigning = assigningLoadout != null && assigningLoadout.IsAssigning;
        bool isAlreadyAssigned = activeLoadout != null
            && (isAssigning
                ? activeLoadout.IsSkillAssignedToOtherSlot(skillType)
                : activeLoadout.IsSkillAssigned(skillType));

        if (button != null)
        {
            button.interactable = isAssigning
                ? isUnlocked && !isAlreadyAssigned
                : isUnlocked && !isCoolingDown && !isAlreadyAssigned;
        }

        if (lockOverlay != null)
        {
            lockOverlay.SetActive(!isUnlocked);
        }

        if (lockLevelText != null)
        {
            lockLevelText.gameObject.SetActive(showLockLevelText && !isUnlocked);
            lockLevelText.fontSize = lockTextFontSize;
            lockLevelText.color = lockTextColor;
            if (!isUnlocked && skillManager != null)
            {
                lockLevelText.text = $"Lv {skillManager.GetRequiredLevel(skillType)}";
            }
        }

        if (cooldownFillImage != null)
        {
            cooldownFillImage.fillAmount = duration > 0f ? Mathf.Clamp01(remaining / duration) : 0f;
            cooldownFillImage.enabled = isUnlocked && isCoolingDown;
        }

        if (cooldownText != null)
        {
            cooldownText.text = isUnlocked && isCoolingDown ? Mathf.CeilToInt(remaining).ToString() : string.Empty;
        }
    }

    private void ResolveManager()
    {
        if (skillManager == null)
        {
            skillManager = DIContainer.Global.Resolve<SkillManager>();
        }
    }

    private void ResolveLoadout()
    {
        if (skillLoadout == null)
        {
            skillLoadout = DIContainer.Global.Resolve<SkillLoadoutUI>();
        }
    }

    private void EnsureLockOverlay()
    {
        if (!createLockOverlayIfMissing || lockOverlay != null || lockSprite == null)
        {
            return;
        }

        if (!(transform is RectTransform))
        {
            return;
        }

        var overlayObject = new GameObject("Lock Overlay", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
        overlayObject.transform.SetParent(transform, false);
        overlayObject.transform.SetAsLastSibling();

        var overlayRect = overlayObject.GetComponent<RectTransform>();
        overlayRect.anchorMin = Vector2.zero;
        overlayRect.anchorMax = Vector2.one;
        overlayRect.offsetMin = lockOverlayAnchorPadding;
        overlayRect.offsetMax = -lockOverlayAnchorPadding;

        var overlayImage = overlayObject.GetComponent<Image>();
        overlayImage.sprite = lockSprite;
        overlayImage.raycastTarget = false;
        overlayImage.preserveAspect = preserveLockSpriteAspect;

        lockOverlay = overlayObject;

        if (lockLevelText == null)
        {
            CreateLockLevelText(transform);
        }
    }

    private void CreateLockLevelText(Transform parent)
    {
        var textObject = new GameObject("Lock Level Text", typeof(RectTransform), typeof(CanvasRenderer), typeof(TextMeshProUGUI));
        textObject.transform.SetParent(parent, false);
        textObject.transform.SetAsLastSibling();

        var textRect = textObject.GetComponent<RectTransform>();
        textRect.anchorMin = new Vector2(0f, 0f);
        textRect.anchorMax = new Vector2(1f, 1f);
        textRect.pivot = new Vector2(0.5f, 1f);
        textRect.localScale = Vector3.one;
        textRect.anchoredPosition = lockTextOffset;
        textRect.sizeDelta = new Vector2(0f, lockTextHeight);

        lockLevelText = textObject.GetComponent<TextMeshProUGUI>();
        lockLevelText.alignment = TextAlignmentOptions.CenterGeoAligned;
        lockLevelText.fontSize = lockTextFontSize;
        lockLevelText.color = lockTextColor;
        lockLevelText.raycastTarget = false;
        lockLevelText.enableWordWrapping = false;
        lockLevelText.overflowMode = TextOverflowModes.Overflow;
        lockLevelText.text = string.Empty;
    }
}
