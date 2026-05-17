using UnityEngine;

public sealed class BattleEnemySpawnService
{
    private readonly AutoBattleUnit enemyTemplate;
    private readonly MonsterData[] monsters;
    private readonly float enemySpawnOffsetX;
    private readonly float attackRange;
    private readonly System.Action<string> log;
    private readonly System.Action<string> logWarning;

    public BattleEnemySpawnService(
        AutoBattleUnit enemyTemplate,
        MonsterData[] monsters,
        float enemySpawnOffsetX,
        float attackRange,
        System.Action<string> log,
        System.Action<string> logWarning)
    {
        this.enemyTemplate = enemyTemplate;
        this.monsters = monsters;
        this.enemySpawnOffsetX = enemySpawnOffsetX;
        this.attackRange = attackRange;
        this.log = log;
        this.logWarning = logWarning;
    }

    public MonsterData SelectMonster(StageManager stageManager)
    {
        if (monsters == null || monsters.Length == 0)
        {
            logWarning?.Invoke("SelectMonster - monsters array is NULL or empty.");
            return null;
        }

        int unlockedCount = GetUnlockedMonsterCount(stageManager);
        int totalWeight = CalculateTotalSpawnWeight(unlockedCount);
        if (totalWeight <= 0)
        {
            logWarning?.Invoke("SelectMonster - totalWeight is 0. Using first valid monster.");
            return GetFirstValidMonster(unlockedCount);
        }

        int roll = Random.Range(0, totalWeight);
        log?.Invoke($"SelectMonster roll={roll} (range: 0-{totalWeight - 1})");

        for (int i = 0; i < unlockedCount; i++)
        {
            var monster = monsters[i];
            if (!IsValidMonster(monster))
            {
                continue;
            }

            roll -= monster.SpawnWeight;
            if (roll < 0)
            {
                log?.Invoke($"Selected monster: {monster.MonsterName}");
                return monster;
            }
        }

        logWarning?.Invoke("SelectMonster - no monster selected.");
        return GetFirstValidMonster(unlockedCount);
    }

    public MonsterData SelectBossMonster(StageManager stageManager)
    {
        if (monsters == null || monsters.Length == 0)
        {
            return null;
        }

        int bossIndex = stageManager != null ? stageManager.GetBossMonsterIndex(monsters.Length) : 0;
        if (IsValidMonster(monsters[bossIndex]))
        {
            return monsters[bossIndex];
        }

        return GetFirstValidMonster(GetUnlockedMonsterCount(stageManager));
    }

    public void SetupSpawnedEnemy(
        AutoBattleUnit enemy,
        MonsterData monster,
        AutoBattleUnit player,
        StageManager stageManager,
        bool isBoss,
        int frameCount)
    {
        if (enemy == null)
        {
            return;
        }

        enemy.ApplyMonsterData(monster);
        Vector3 baseScale = GetMonsterBaseScale(monster);
        enemy.transform.localScale = baseScale;
        enemy.transform.position = CalculateEnemySpawnPosition(monster, player, isBoss);

        if (monster != null)
        {
            enemy.transform.rotation = Quaternion.Euler(monster.SpawnRotation);
        }

        if (isBoss && stageManager != null)
        {
            enemy.ApplyRuntimeBattleModifiers(
                "BOSS",
                stageManager.BossHealthMultiplier,
                stageManager.BossAttackMultiplier,
                stageManager.BossAttackIntervalMultiplier);
            enemy.transform.localScale = baseScale * stageManager.BossScaleMultiplier;
        }
        else
        {
            enemy.transform.localScale = baseScale;
        }

        enemy.name = (monster != null ? monster.MonsterName : "Enemy") + (isBoss ? "_Boss_" : "_") + frameCount;
        enemy.ResetUnit();
        enemy.PlayIdle();
    }

    public float GetEnemySpawnOffset()
    {
        float actualOffset = Mathf.Max(enemySpawnOffsetX, attackRange + 3f);
        if (enemySpawnOffsetX < attackRange + 1f)
        {
            logWarning?.Invoke($"enemySpawnOffsetX({enemySpawnOffsetX}) is too small. Using {actualOffset} instead.");
        }

        return actualOffset;
    }

    private Vector3 CalculateEnemySpawnPosition(MonsterData monster, AutoBattleUnit player, bool isBoss)
    {
        Vector3 playerPosition = player != null ? player.transform.position : Vector3.zero;
        Vector3 spawnPos = playerPosition + Vector3.right * GetEnemySpawnOffset();
        if (monster != null)
        {
            spawnPos.z += monster.SpawnZOffset;
            spawnPos.y += isBoss ? monster.BossSpawnYOffset : monster.SpawnYOffset;
        }

        return spawnPos;
    }

    private Vector3 GetMonsterBaseScale(MonsterData monster)
    {
        if (monster != null && monster.Prefab != null)
        {
            return monster.Prefab.transform.localScale;
        }

        if (enemyTemplate != null)
        {
            return enemyTemplate.transform.localScale;
        }

        return Vector3.one;
    }

    private MonsterData GetFirstValidMonster(int maxCount)
    {
        if (monsters == null)
        {
            return null;
        }

        int count = Mathf.Clamp(maxCount, 1, monsters.Length);
        for (int i = 0; i < count; i++)
        {
            if (IsValidMonster(monsters[i]))
            {
                return monsters[i];
            }
        }

        return null;
    }

    private int CalculateTotalSpawnWeight(int maxCount)
    {
        int totalWeight = 0;
        int count = Mathf.Clamp(maxCount, 1, monsters.Length);
        for (int i = 0; i < count; i++)
        {
            if (IsValidMonster(monsters[i]))
            {
                totalWeight += monsters[i].SpawnWeight;
                continue;
            }

            logWarning?.Invoke($"SelectMonster - monsters[{i}] invalid: name={monsters[i]?.MonsterName ?? "NULL"}, prefab={(monsters[i]?.Prefab != null ? "OK" : "NULL")}");
        }

        log?.Invoke($"SelectMonster totalWeight={totalWeight}");
        return totalWeight;
    }

    private int GetUnlockedMonsterCount(StageManager stageManager)
    {
        if (monsters == null || monsters.Length == 0)
        {
            return 0;
        }

        return stageManager != null ? stageManager.GetUnlockedMonsterCount(monsters.Length) : monsters.Length;
    }

    private static bool IsValidMonster(MonsterData monster)
    {
        return monster != null && monster.Prefab != null;
    }
}
