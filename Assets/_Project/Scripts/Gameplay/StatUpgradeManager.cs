using System;
using UnityEngine;

public sealed class StatUpgradeManager : MonoBehaviour, ISaveable
{
    [Serializable]
    public sealed class StatUpgrade
    {
        public StatType statType;
        public string displayName = "Stat";
        public float baseValue;
        public float increasePerLevel = 1f;
        public int level;
        public int baseCost = 1;
        public int costIncreasePerLevel = 1;
        public int valueDecimals;
        public int costDigits = 3;
    }

    [SerializeField] private AutoBattleUnit player;
    [SerializeField] private StatUpgrade[] upgrades =
    {
        new() { statType = StatType.Attack, displayName = "Attack", baseValue = 1f, increasePerLevel = 1f },
        new() { statType = StatType.AttackSpeed, displayName = "Attack Speed", baseValue = 1f, increasePerLevel = 0.05f, valueDecimals = 2 },
        new() { statType = StatType.Health, displayName = "Health", baseValue = 10f, increasePerLevel = 5f },
        new() { statType = StatType.CritChance, displayName = "Crit Chance", baseValue = 0f, increasePerLevel = 1f },
        new() { statType = StatType.CritDamage, displayName = "Crit Damage", baseValue = 150f, increasePerLevel = 5f },
        new() { statType = StatType.GoldBonus, displayName = "Gold Bonus", baseValue = 0f, increasePerLevel = 5f },
        new() { statType = StatType.ExpBonus, displayName = "Exp Bonus", baseValue = 0f, increasePerLevel = 5f },
    };

    private GameManager gameManager;

    public event Action StatsChanged;

    private void Awake()
    {
        ResolveReferences();
        DIContainer.Global.Register(this);
        ApplyToPlayer();
    }

    private void Start()
    {
        ResolveReferences();
        ApplyToPlayer();
        StatsChanged?.Invoke();
    }

    public StatUpgrade GetUpgrade(StatType statType)
    {
        for (int i = 0; i < upgrades.Length; i++)
        {
            if (upgrades[i] != null && upgrades[i].statType == statType)
            {
                return upgrades[i];
            }
        }

        return null;
    }

    public float GetValue(StatType statType)
    {
        var upgrade = GetUpgrade(statType);
        return upgrade == null ? 0f : upgrade.baseValue + upgrade.increasePerLevel * upgrade.level;
    }

    public float GetNextValue(StatType statType)
    {
        var upgrade = GetUpgrade(statType);
        return upgrade == null ? 0f : upgrade.baseValue + upgrade.increasePerLevel * (upgrade.level + 1);
    }

    public int GetCost(StatType statType)
    {
        var upgrade = GetUpgrade(statType);
        if (upgrade == null)
        {
            return 0;
        }

        return Mathf.Max(0, upgrade.baseCost + upgrade.level * upgrade.costIncreasePerLevel);
    }

    public bool TryUpgrade(StatType statType)
    {
        var upgrade = GetUpgrade(statType);
        if (upgrade == null)
        {
            return false;
        }

        ResolveReferences();

        int cost = GetCost(statType);
        if (gameManager != null && !gameManager.TrySpendGold(cost))
        {
            StatsChanged?.Invoke();
            return false;
        }

        upgrade.level++;
        ApplyToPlayer();
        StatsChanged?.Invoke();
        SaveEvents.RequestSave();
        return true;
    }

    public void CaptureSaveData(SaveData saveData)
    {
        if (saveData == null || upgrades == null)
        {
            return;
        }

        saveData.statUpgrades = new StatUpgradeSaveData[upgrades.Length];
        for (int i = 0; i < upgrades.Length; i++)
        {
            saveData.statUpgrades[i] = new StatUpgradeSaveData
            {
                statType = upgrades[i].statType,
                level = upgrades[i].level
            };
        }
    }

    public void RestoreSaveData(SaveData saveData)
    {
        ResetUpgradeLevels();

        if (saveData == null || saveData.statUpgrades == null)
        {
            ApplyToPlayer();
            StatsChanged?.Invoke();
            return;
        }

        for (int i = 0; i < saveData.statUpgrades.Length; i++)
        {
            var savedUpgrade = saveData.statUpgrades[i];
            var upgrade = GetUpgrade(savedUpgrade.statType);
            if (upgrade != null)
            {
                upgrade.level = Mathf.Max(0, savedUpgrade.level);
            }
        }

        ApplyToPlayer();
        StatsChanged?.Invoke();
    }

    private void ResetUpgradeLevels()
    {
        if (upgrades == null)
        {
            return;
        }

        for (int i = 0; i < upgrades.Length; i++)
        {
            if (upgrades[i] != null)
            {
                upgrades[i].level = 0;
            }
        }
    }

    private void ApplyToPlayer()
    {
        ResolveReferences();
        if (player == null)
        {
            return;
        }

        player.ApplyStatUpgrades(
            GetValue(StatType.Attack),
            GetValue(StatType.AttackSpeed),
            GetValue(StatType.Health),
            GetValue(StatType.CritChance),
            GetValue(StatType.CritDamage),
            GetValue(StatType.GoldBonus),
            GetValue(StatType.ExpBonus));
    }

    private void ResolveReferences()
    {
        if (player == null)
        {
            player = DIContainer.Global.Resolve<AutoBattleUnit>();
        }

        if (player == null)
        {
            player = GameObject.FindWithTag("Player")?.GetComponent<AutoBattleUnit>();
        }

        if (gameManager == null)
        {
            gameManager = DIContainer.Global.Resolve<GameManager>() ?? GameManager.Instance;
        }
    }
}
