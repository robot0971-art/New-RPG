using UnityEngine;

public sealed class BattleRewardService
{
    private readonly int defaultGoldReward;
    private readonly RewardBalanceData rewardBalance;
    private readonly Vector3 coinDropOffset;
    private readonly System.Action<string> logWarning;

    public BattleRewardService(
        int defaultGoldReward,
        RewardBalanceData rewardBalance,
        Vector3 coinDropOffset,
        System.Action<string> logWarning)
    {
        this.defaultGoldReward = defaultGoldReward;
        this.rewardBalance = rewardBalance;
        this.coinDropOffset = coinDropOffset;
        this.logWarning = logWarning;
    }

    public void PlayRewardCoins(
        AutoBattleUnit player,
        AutoBattleUnit enemy,
        MonsterData monsterData,
        CoinDropRewardSpawner coinRewardSpawner,
        bool isBoss)
    {
        if (coinRewardSpawner == null)
        {
            logWarning?.Invoke("CoinRewardSpawner is missing. Gold reward was not dropped.");
            return;
        }

        Vector3 position = enemy != null ? enemy.transform.position + coinDropOffset : coinDropOffset;
        float groundY = player != null ? player.transform.position.y : 0f;
        coinRewardSpawner.PlayReward(position, groundY, GetGoldReward(player, monsterData, isBoss), isBoss);
    }

    public int GetGoldReward(AutoBattleUnit player, MonsterData monsterData, bool isBoss)
    {
        float baseGoldReward = monsterData != null ? monsterData.GoldReward : GetFallbackGoldReward();
        if (isBoss)
        {
            baseGoldReward *= GetBossGoldMultiplier();
        }

        float bonusPercent = player != null ? player.TotalGoldBonusPercent : 0f;
        return Mathf.CeilToInt(GetRewardWithBonus(baseGoldReward, bonusPercent));
    }

    public float GetExpReward(AutoBattleUnit player, MonsterData monsterData, bool isBoss)
    {
        float baseExpReward = monsterData != null ? monsterData.ExpReward : GetFallbackExpReward();
        if (isBoss)
        {
            baseExpReward *= GetBossExpMultiplier();
        }

        float bonusPercent = player != null ? player.ExpBonusPercent : 0f;
        return GetRewardWithBonus(baseExpReward, bonusPercent);
    }

    private int GetFallbackGoldReward()
    {
        return rewardBalance != null ? rewardBalance.FallbackGoldReward : defaultGoldReward;
    }

    private float GetFallbackExpReward()
    {
        return rewardBalance != null ? rewardBalance.FallbackExpReward : 5f;
    }

    private float GetBossGoldMultiplier()
    {
        return rewardBalance != null ? rewardBalance.BossGoldMultiplier : 5f;
    }

    private float GetBossExpMultiplier()
    {
        return rewardBalance != null ? rewardBalance.BossExpMultiplier : 5f;
    }

    private static float GetRewardWithBonus(float baseReward, float bonusPercent)
    {
        return Mathf.Max(0f, baseReward * (1f + bonusPercent / 100f));
    }
}
