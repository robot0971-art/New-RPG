using TMPro;
using UnityEngine;
using UnityEngine.UI;

public sealed class StatUpgradePanelUI : MonoBehaviour
{
    [Header("Texts")]
    [SerializeField] private TMP_Text nameTmpText;
    [SerializeField] private TMP_Text valueTmpText;
    [SerializeField] private TMP_Text costTmpText;
    [SerializeField] private Text nameText;
    [SerializeField] private Text valueText;
    [SerializeField] private Text costText;

    [Header("Button")]
    [SerializeField] private Button upgradeButton;

    [Header("Stat")]
    [SerializeField] private StatType statType;

    private StatUpgradeManager upgradeManager;

    private void Awake()
    {
        AutoAssignReferences();
    }

    private void OnEnable()
    {
        ResolveManager();

        if (upgradeButton != null)
        {
            upgradeButton.onClick.AddListener(TryUpgrade);
        }

        if (upgradeManager != null)
        {
            upgradeManager.StatsChanged += Refresh;
        }

        Refresh();
    }

    private void OnDisable()
    {
        if (upgradeButton != null)
        {
            upgradeButton.onClick.RemoveListener(TryUpgrade);
        }

        if (upgradeManager != null)
        {
            upgradeManager.StatsChanged -= Refresh;
        }
    }

    public void Refresh()
    {
        ResolveManager();

        if (upgradeManager == null)
        {
            return;
        }

        var upgrade = upgradeManager.GetUpgrade(statType);
        if (upgrade == null)
        {
            return;
        }

        SetText(nameTmpText, nameText, upgrade.displayName);
        SetText(valueTmpText, valueText, $"{FormatValue(upgradeManager.GetValue(statType), upgrade.valueDecimals, statType)} -> {FormatValue(upgradeManager.GetNextValue(statType), upgrade.valueDecimals, statType)}");
        SetText(costTmpText, costText, FormatCost(upgradeManager.GetCost(statType), upgrade.costDigits));
    }

    private void TryUpgrade()
    {
        ResolveManager();
        upgradeManager?.TryUpgrade(statType);
    }

    private void ResolveManager()
    {
        if (upgradeManager == null)
        {
            upgradeManager = DIContainer.Global.Resolve<StatUpgradeManager>();
        }

        if (upgradeManager == null)
        {
            upgradeManager = FindFirstObjectByType<StatUpgradeManager>();
        }
    }

    private void AutoAssignReferences()
    {
        if (upgradeButton == null)
        {
            upgradeButton = GetComponentInChildren<Button>(true);
        }
    }

    private static string FormatValue(float value, int decimals, StatType statType)
    {
        string suffix = IsPercentStat(statType) ? "%" : string.Empty;

        if (decimals <= 0)
        {
            return Mathf.RoundToInt(value).ToString("D3") + suffix;
        }

        return value.ToString($"F{decimals}") + suffix;
    }

    private static bool IsPercentStat(StatType statType)
    {
        return statType == StatType.CritChance
            || statType == StatType.CritDamage
            || statType == StatType.GoldBonus
            || statType == StatType.ExpBonus;
    }

    private static string FormatCost(int cost, int digits)
    {
        return digits > 0 ? cost.ToString($"D{digits}") : cost.ToString();
    }

    private static void SetText(TMP_Text tmpText, Text legacyText, string value)
    {
        if (tmpText != null)
        {
            tmpText.text = value;
        }

        if (legacyText != null)
        {
            legacyText.text = value;
        }
    }
}
