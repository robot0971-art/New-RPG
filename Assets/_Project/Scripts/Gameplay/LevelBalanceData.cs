using UnityEngine;

[CreateAssetMenu(fileName = "LevelBalanceData", menuName = "Game Data/Level Balance Data")]
public sealed class LevelBalanceData : ScriptableObject
{
    [SerializeField, Min(0.01f)] private float requiredExpPerLevel = 10f;
    [SerializeField, Min(0f)] private float attackBonusPerLevel = 2f;
    [SerializeField, Min(0f)] private float healthBonusPerLevel = 5f;

    public float GetRequiredExp(int level)
    {
        return Mathf.Max(1f, Mathf.Max(1, level) * requiredExpPerLevel);
    }

    public float GetAttackBonus(int bonusLevels)
    {
        return Mathf.Max(0, bonusLevels) * attackBonusPerLevel;
    }

    public float GetHealthBonus(int bonusLevels)
    {
        return Mathf.Max(0, bonusLevels) * healthBonusPerLevel;
    }

    public float AttackBonusPerLevel => attackBonusPerLevel;
    public float HealthBonusPerLevel => healthBonusPerLevel;
}
