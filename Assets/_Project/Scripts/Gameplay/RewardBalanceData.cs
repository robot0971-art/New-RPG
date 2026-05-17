using UnityEngine;

[CreateAssetMenu(fileName = "RewardBalanceData", menuName = "Game Data/Reward Balance Data")]
public sealed class RewardBalanceData : ScriptableObject
{
    [SerializeField, Min(0)] private int fallbackGoldReward = 10;
    [SerializeField, Min(0f)] private float fallbackExpReward = 5f;
    [SerializeField, Min(0f)] private float bossGoldMultiplier = 5f;
    [SerializeField, Min(0f)] private float bossExpMultiplier = 5f;

    public int FallbackGoldReward => fallbackGoldReward;
    public float FallbackExpReward => fallbackExpReward;
    public float BossGoldMultiplier => bossGoldMultiplier;
    public float BossExpMultiplier => bossExpMultiplier;
}
