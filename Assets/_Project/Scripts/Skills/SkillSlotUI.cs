using UnityEngine;
using UnityEngine.UI;

public sealed class SkillSlotUI : MonoBehaviour
{
    [SerializeField] private Button button;
    [SerializeField] private Image skillIconImage;
    [SerializeField] private GameObject emptyStateRoot;
    [SerializeField] private SkillType[] verticallyFlippedSkillIcons = System.Array.Empty<SkillType>();

    private SkillLoadoutUI loadout;
    private SkillManager skillManager;
    private Image buttonImage;
    private Sprite emptySprite;
    private SkillType assignedSkillType;
    private bool hasAssignedSkill;

    public bool HasAssignedSkill => hasAssignedSkill;
    public SkillType AssignedSkillType => assignedSkillType;

    private void Awake()
    {
        if (button == null)
        {
            button = GetComponent<Button>();
        }

        buttonImage = GetComponent<Image>();
        if (buttonImage != null)
        {
            emptySprite = buttonImage.sprite;
            NormalizeImageTransform(buttonImage, false);
        }

        if (skillIconImage != null)
        {
            NormalizeImageTransform(skillIconImage, false);
        }
    }

    private void OnEnable()
    {
        ResolveReferences();

        if (button != null)
        {
            button.onClick.AddListener(OnClicked);
        }

        Refresh();
    }

    private void OnDisable()
    {
        if (button != null)
        {
            button.onClick.RemoveListener(OnClicked);
        }
    }

    public void Construct(SkillLoadoutUI loadout, SkillManager skillManager)
    {
        this.loadout = loadout;
        this.skillManager = skillManager;
        Refresh();
    }

    public void AssignSkill(SkillType skillType)
    {
        ResolveReferences();
        assignedSkillType = skillType;
        hasAssignedSkill = true;
        Refresh();
    }

    public void ClearSkill()
    {
        ResolveReferences();
        hasAssignedSkill = false;
        Refresh();
    }

    private void ResolveReferences()
    {
        if (SkillLoadoutUI.ActiveLoadout != null)
        {
            loadout = SkillLoadoutUI.ActiveLoadout;
        }
        else if (loadout == null)
        {
            loadout = GetComponentInParent<SkillLoadoutUI>();
        }

        if (skillManager == null)
        {
            skillManager = DIContainer.Global.Resolve<SkillManager>();
        }
    }

    private void OnClicked()
    {
        ResolveReferences();

        if (!hasAssignedSkill && loadout != null)
        {
            loadout.BeginAssign(this);
            return;
        }

        if (hasAssignedSkill)
        {
            skillManager?.TryUseSkill(assignedSkillType);
        }
    }

    private void Refresh()
    {
        ResolveReferences();
        Sprite icon = null;
        if (hasAssignedSkill && skillManager != null)
        {
            SkillData skill = skillManager.GetSkillData(assignedSkillType);
            icon = skill != null ? skill.icon : null;
        }

        if (buttonImage != null)
        {
            buttonImage.sprite = icon != null ? icon : emptySprite;
            NormalizeImageTransform(buttonImage, false);
        }

        if (skillIconImage != null)
        {
            skillIconImage.sprite = icon;
            skillIconImage.enabled = icon != null;
            skillIconImage.raycastTarget = false;
            skillIconImage.preserveAspect = true;
            NormalizeImageTransform(skillIconImage, hasAssignedSkill && ShouldFlipIconVertically(assignedSkillType));
        }

        if (emptyStateRoot != null)
        {
            bool shouldShowEmptyState = !hasAssignedSkill || icon == null;
            if (emptyStateRoot != gameObject)
            {
                emptyStateRoot.SetActive(shouldShowEmptyState);
            }
        }
    }

    private bool ShouldFlipIconVertically(SkillType skillType)
    {
        if (verticallyFlippedSkillIcons == null)
        {
            return false;
        }

        for (int i = 0; i < verticallyFlippedSkillIcons.Length; i++)
        {
            if (verticallyFlippedSkillIcons[i] == skillType)
            {
                return true;
            }
        }

        return false;
    }

    private static void NormalizeImageTransform(Image image, bool flipVertically)
    {
        RectTransform rectTransform = image.rectTransform;
        rectTransform.localRotation = Quaternion.identity;
        rectTransform.localScale = new Vector3(1f, flipVertically ? -1f : 1f, 1f);
    }
}
