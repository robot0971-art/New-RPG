using UnityEngine;

[CreateAssetMenu(fileName = "MonsterData", menuName = "Game Data/Monster Data")]
public sealed class MonsterData : ScriptableObject
{
    [SerializeField] private string monsterName = "Monster";
    [SerializeField] private AutoBattleUnit prefab;
    [SerializeField] private float maxHealth = 10f;
    [SerializeField] private float attackPower = 1f;
    [SerializeField] private float attackInterval = 1f;
    [SerializeField] private float moveSpeed = 1.2f;
    [SerializeField] private float expReward = 5f;
    [SerializeField] private int goldReward = 10;
    [SerializeField] private int spawnWeight = 1;

    public string MonsterName => monsterName;
    public AutoBattleUnit Prefab => prefab;
    public float MaxHealth => maxHealth;
    public float AttackPower => attackPower;
    public float AttackInterval => attackInterval;
    public float MoveSpeed => moveSpeed;
    public float ExpReward => expReward;
    public int GoldReward => goldReward;
    public int SpawnWeight => Mathf.Max(0, spawnWeight);
}
