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

    [Header("Spawn Transform")]
    [SerializeField] private float spawnYOffset = 0f;
    [SerializeField] private float bossSpawnYOffset = 0f;
    [SerializeField] private float spawnZOffset = 0f;
    [SerializeField] private Vector3 spawnRotation = Vector3.zero;

    public string MonsterName => monsterName;
    public AutoBattleUnit Prefab => prefab;
    public float MaxHealth => maxHealth;
    public float AttackPower => attackPower;
    public float AttackInterval => attackInterval;
    public float MoveSpeed => moveSpeed;
    public float ExpReward => expReward;
    public int GoldReward => goldReward;
    public int SpawnWeight => Mathf.Max(0, spawnWeight);
    public float SpawnYOffset => spawnYOffset;
    public float BossSpawnYOffset => bossSpawnYOffset;
    public float SpawnZOffset => spawnZOffset;
    public Vector3 SpawnRotation => spawnRotation;
}
