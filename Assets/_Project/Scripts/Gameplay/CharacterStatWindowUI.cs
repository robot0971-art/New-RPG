using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public sealed class CharacterStatWindowUI : MonoBehaviour
{
    [Serializable]
    private sealed class StatLine
    {
        public StatType statType;
        public TMP_Text nameText;
        public TMP_Text valueText;
    }

    [Header("Window")]
    [SerializeField] private GameObject windowRoot;
    [SerializeField] private Button closeButton;
    [SerializeField] private GameObject[] hudObjectsToHideWhenOpen;

    [Header("Character")]
    [SerializeField] private AutoBattleUnit player;
    [SerializeField] private PlayerResources playerResources;
    [SerializeField] private TMP_Text titleText;
    [SerializeField] private TMP_Text levelText;
    [SerializeField] private TMP_Text classText;
    [SerializeField] private string characterClassName = "기사";
    [SerializeField] private Image portraitImage;
    [SerializeField] private Sprite portraitSprite;

    [Header("Vitals")]
    [SerializeField] private TMP_Text healthText;
    [SerializeField] private TMP_Text manaText;
    [SerializeField] private Image healthFillImage;
    [SerializeField] private Image manaFillImage;
    [SerializeField] private Slider healthSlider;
    [SerializeField] private Slider manaSlider;

    [Header("Stats")]
    [SerializeField] private TMP_Text combatPowerText;
    [SerializeField] private bool combatPowerValueOnly;
    [SerializeField] private StatLine[] statLines;

    public bool IsOpen => windowRoot != null && windowRoot.activeInHierarchy;

    private StatUpgradeManager statUpgradeManager;
    private EquipmentManager equipmentManager;
    private bool isSubscribed;

    private void Awake()
    {
        if (windowRoot == null)
        {
            windowRoot = gameObject;
        }

    }

    private void OnEnable()
    {
        ResolveReferences();
        Subscribe();

        if (closeButton != null)
        {
            closeButton.onClick.RemoveListener(Close);
            closeButton.onClick.AddListener(Close);
        }

        Refresh();
    }

    private void OnDisable()
    {
        Unsubscribe();
        SetHudObjectsVisible(true);

        if (closeButton != null)
        {
            closeButton.onClick.RemoveListener(Close);
        }
    }

    public void Open()
    {
        SetVisible(true);
        SetHudObjectsVisible(false);
        transform.SetAsLastSibling();
        Refresh();
    }

    public void Close()
    {
        SetHudObjectsVisible(true);
        SetVisible(false);
    }

    public void Toggle()
    {
        if (IsOpen)
        {
            Close();
            return;
        }

        Open();
    }

    public void Refresh()
    {
        ResolveReferences();

        if (titleText != null)
        {
            titleText.text = "캐릭터 스탯";
        }

        if (levelText != null)
        {
            levelText.text = player != null ? $"LV : {player.Level:00}" : "LV : 00";
        }

        if (classText != null)
        {
            classText.text = characterClassName;
        }

        if (portraitImage != null && portraitSprite != null)
        {
            portraitImage.sprite = portraitSprite;
            portraitImage.preserveAspect = true;
        }

        RefreshVitals();
        RefreshStats();
    }

    private void RefreshVitals()
    {
        if (player != null)
        {
            SetFill(healthFillImage, healthSlider, player.MaxHealth > 0f ? player.CurrentHealth / player.MaxHealth : 0f);
            if (healthText != null)
            {
                healthText.text = $"{Mathf.RoundToInt(player.CurrentHealth):N0} / {Mathf.RoundToInt(player.MaxHealth):N0}";
            }
        }

        if (playerResources != null)
        {
            SetFill(manaFillImage, manaSlider, playerResources.ManaNormalized);
            if (manaText != null)
            {
                manaText.text = $"{Mathf.RoundToInt(playerResources.CurrentMana):N0} / {Mathf.RoundToInt(playerResources.MaxMana):N0}";
            }
        }
    }

    private void RefreshStats()
    {
        if (player == null)
        {
            return;
        }

        if (combatPowerText != null)
        {
            int combatPower = CalculateCombatPower();
            combatPowerText.text = combatPowerValueOnly ? combatPower.ToString("N0") : $"전투력 : {combatPower:N0}";
        }

        if (statLines == null)
        {
            return;
        }

        for (int i = 0; i < statLines.Length; i++)
        {
            StatLine line = statLines[i];
            if (line == null)
            {
                continue;
            }

            if (line.nameText != null)
            {
                line.nameText.text = GetStatDisplayName(line.statType);
            }

            if (line.valueText != null)
            {
                line.valueText.text = FormatStatValue(line.statType);
            }
        }
    }

    private int CalculateCombatPower()
    {
        float attackPerSecond = player.AttackPower / Mathf.Max(0.1f, player.AttackInterval);
        float critMultiplier = 1f + (player.CritChancePercent / 100f) * ((player.CritDamagePercent / 100f) - 1f);
        float utilityMultiplier = 1f + (player.TotalGoldBonusPercent + player.ExpBonusPercent) * 0.0025f;
        float rawPower = (attackPerSecond * critMultiplier * 120f) + (player.MaxHealth * 8f);
        return Mathf.Max(0, Mathf.RoundToInt(rawPower * utilityMultiplier));
    }

    private string FormatStatValue(StatType statType)
    {
        return statType switch
        {
            StatType.Attack => Mathf.RoundToInt(player.AttackPower).ToString("N0"),
            StatType.AttackSpeed => (1f / Mathf.Max(0.1f, player.AttackInterval)).ToString("F2"),
            StatType.Health => Mathf.RoundToInt(player.MaxHealth).ToString("N0"),
            StatType.CritChance => $"{player.CritChancePercent:F1}%",
            StatType.CritDamage => $"{player.CritDamagePercent:F1}%",
            StatType.GoldBonus => $"+{player.TotalGoldBonusPercent:F1}%",
            StatType.ExpBonus => $"+{player.ExpBonusPercent:F1}%",
            _ => "0"
        };
    }

    private static string GetStatDisplayName(StatType statType)
    {
        return statType switch
        {
            StatType.Attack => "공격력",
            StatType.AttackSpeed => "공격속도",
            StatType.Health => "체력",
            StatType.CritChance => "치명타 확률",
            StatType.CritDamage => "치명타 데미지",
            StatType.GoldBonus => "골드 획득량",
            StatType.ExpBonus => "경험치 획득량",
            _ => statType.ToString()
        };
    }

    private void ResolveReferences()
    {
        if (player == null)
        {
            player = DIContainer.Global.Resolve<AutoBattleUnit>();
        }

        if (player == null)
        {
            player = GameObject.Find("Player")?.GetComponent<AutoBattleUnit>();
        }

        if (playerResources == null)
        {
            playerResources = DIContainer.Global.Resolve<PlayerResources>();
        }

        if (playerResources == null)
        {
            playerResources = GameObject.Find("PlayerResources")?.GetComponent<PlayerResources>();
        }

        if (statUpgradeManager == null)
        {
            statUpgradeManager = DIContainer.Global.Resolve<StatUpgradeManager>();
        }

        if (equipmentManager == null)
        {
            equipmentManager = DIContainer.Global.Resolve<EquipmentManager>();
        }
    }

    private void Subscribe()
    {
        if (isSubscribed)
        {
            return;
        }

        if (player != null)
        {
            player.Damaged += OnPlayerChanged;
            player.ExperienceChanged += OnPlayerChanged;
            player.LevelChanged += OnPlayerLevelChanged;
        }

        if (playerResources != null)
        {
            playerResources.ManaChanged += OnManaChanged;
        }

        if (statUpgradeManager != null)
        {
            statUpgradeManager.StatsChanged += Refresh;
        }

        if (equipmentManager != null)
        {
            equipmentManager.EquipmentChanged += Refresh;
        }

        isSubscribed = true;
    }

    private void Unsubscribe()
    {
        if (!isSubscribed)
        {
            return;
        }

        if (player != null)
        {
            player.Damaged -= OnPlayerChanged;
            player.ExperienceChanged -= OnPlayerChanged;
            player.LevelChanged -= OnPlayerLevelChanged;
        }

        if (playerResources != null)
        {
            playerResources.ManaChanged -= OnManaChanged;
        }

        if (statUpgradeManager != null)
        {
            statUpgradeManager.StatsChanged -= Refresh;
        }

        if (equipmentManager != null)
        {
            equipmentManager.EquipmentChanged -= Refresh;
        }

        isSubscribed = false;
    }

    private void OnPlayerChanged(AutoBattleUnit unit)
    {
        Refresh();
    }

    private void OnPlayerLevelChanged(int level)
    {
        Refresh();
    }

    private void OnManaChanged(PlayerResources resources)
    {
        RefreshVitals();
    }

    private void SetVisible(bool visible)
    {
        if (windowRoot != null)
        {
            windowRoot.SetActive(visible);
        }
    }

    private void SetHudObjectsVisible(bool visible)
    {
        if (hudObjectsToHideWhenOpen == null)
        {
            return;
        }

        for (int i = 0; i < hudObjectsToHideWhenOpen.Length; i++)
        {
            if (hudObjectsToHideWhenOpen[i] != null)
            {
                hudObjectsToHideWhenOpen[i].SetActive(visible);
            }
        }
    }

    private static void SetFill(Image image, Slider slider, float value)
    {
        float clampedValue = Mathf.Clamp01(value);

        if (image != null)
        {
            image.type = Image.Type.Filled;
            image.fillMethod = Image.FillMethod.Horizontal;
            image.fillOrigin = (int)Image.OriginHorizontal.Left;
            image.fillAmount = clampedValue;
        }

        if (slider != null)
        {
            slider.minValue = 0f;
            slider.maxValue = 1f;
            slider.value = clampedValue;
        }
    }
}
