using System;

[Serializable]
public sealed class SaveData
{
    public int gold;
    public int playerLevel = 1;
    public float playerExp;
    public long lastSaveUnixTime;
    public StatUpgradeSaveData[] statUpgrades = new StatUpgradeSaveData[0];
}

[Serializable]
public sealed class StatUpgradeSaveData
{
    public StatType statType;
    public int level;
}
