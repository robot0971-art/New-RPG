using UnityEngine;

[CreateAssetMenu(fileName = "StageBalanceData", menuName = "Game Data/Stage Balance Data")]
public sealed class StageBalanceData : ScriptableObject
{
    [SerializeField, Min(0)] private int normalKillsRequiredForBoss = 10;
    [SerializeField, Min(0.01f)] private float bossHealthMultiplier = 12f;
    [SerializeField, Min(0f)] private float bossAttackMultiplier = 2f;
    [SerializeField, Min(0.01f)] private float bossAttackIntervalMultiplier = 1f;
    [SerializeField, Min(0.01f)] private float bossScaleMultiplier = 1.8f;
    [SerializeField, Min(0f)] private float bossClearRewardDelay = 1.25f;

    public int NormalKillsRequiredForBoss => normalKillsRequiredForBoss;
    public float BossHealthMultiplier => bossHealthMultiplier;
    public float BossAttackMultiplier => bossAttackMultiplier;
    public float BossAttackIntervalMultiplier => bossAttackIntervalMultiplier;
    public float BossScaleMultiplier => bossScaleMultiplier;
    public float BossClearRewardDelay => bossClearRewardDelay;
}
