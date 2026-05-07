using System;

[Serializable]
public sealed class SaveData
{
    public int gold;
    public int playerLevel = 1;
    public float playerExp;
    public float playerMana;
    public int currentStage = 1;
    public int normalKillCount;
    public bool bossAvailable;
    public long lastSaveUnixTime;
    public StatUpgradeSaveData[] statUpgrades = new StatUpgradeSaveData[0];
    public int[] skillLoadout = new int[0];
    public EquipmentSlotSaveData[] equipmentSlots = new EquipmentSlotSaveData[0];
}

[Serializable]
public sealed class StatUpgradeSaveData
{
    public StatType statType;
    public int level;
}

[Serializable]
public sealed class EquipmentSlotSaveData
{
    public EquipmentSlotType slotType;
    public string equipmentId;
}
