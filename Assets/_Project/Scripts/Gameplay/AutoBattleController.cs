using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Pool;

public sealed class AutoBattleController : MonoBehaviour, ISkillTargetProvider, IEnemyDefeatHandler, IAttackHitNotifier
{
    [Header("Settings")]
    [SerializeField] private AutoBattleUnit enemyTemplate;
    [SerializeField] private MonsterData[] monsters;
    [SerializeField] private int goldReward = 10;
    [SerializeField] private float enemyRespawnDelay = 1.0f;
    [SerializeField] private float enemySpawnOffsetX = 8f;
    [SerializeField] private float attackRange = 2.0f;
    [SerializeField] private float moveSpeed = 4.0f;
    [SerializeField] private GameObject impactPrefab;
    [SerializeField] private Vector3 impactOffset = new Vector3(0f, 0.5f, 0f);
    [SerializeField] private Vector3 impactRotation;
    [SerializeField] private Vector3 impactScale = Vector3.one;
    [SerializeField] private float impactStartSizeMultiplier = 1.0f;
    [SerializeField] private int impactSortingOrder = 10;
    [SerializeField] private float impactReleaseDelay = 1.0f;
    [SerializeField] private DamagePopupSpawner damagePopupSpawner;

    [Header("Rewards")]
    [SerializeField] private RewardBalanceData rewardBalance;
    [SerializeField] private CoinDropRewardSpawner coinRewardSpawner;
    [SerializeField] private Vector3 coinDropOffset = new Vector3(0f, 0.8f, 0f);

    [Header("Timing")]
    [Range(0f, 0.5f)]
    [SerializeField] private float attackImpactDelay = 0.25f;
    [Range(0f, 2f)]
    [SerializeField] private float enemyDeathReleaseDelay = 1.25f;

    [Header("Debug")]
    [SerializeField] private bool debugLogs;

    private AutoBattleUnit player;
    private BackgroundScroller backgroundScroller;
    [SerializeField] private ParallaxBackground2D parallaxBackground;
    private AutoBattleSensor2D playerSensor;
    private GameManager gameManager;
    private StageManager stageManager;

    private readonly Dictionary<MonsterData, IObjectPool<AutoBattleUnit>> enemyPools = new Dictionary<MonsterData, IObjectPool<AutoBattleUnit>>();
    private IObjectPool<AutoBattleUnit> fallbackEnemyPool;
    private BattleEffectService effectService;
    private BattleRewardService rewardService;
    private BattleEnemySpawnService enemySpawnService;
    private AutoBattleUnit currentEnemy;
    private MonsterData currentMonsterData;
    private AutoBattleUnit subscribedAttackHitPlayer;
    private float playerAttackTimer;
    private float enemyRespawnTimer;
    private bool isFighting;
    private bool isAttackResolving;
    private bool playerAttackEventReceived;
    private bool isEnemyDefeatHandled;
    private bool pendingBossSpawn;
    private bool currentEnemyIsBoss;

    public AutoBattleUnit Player => player;
    public AutoBattleUnit CurrentEnemy => currentEnemy;
    public event System.Action<AutoBattleUnit, AutoBattleUnit> PlayerAttackResolved;

    public bool StartBossBattle()
    {
        TryResolveDependencies();
        if (stageManager == null || !stageManager.BossAvailable || stageManager.BossBattleActive)
        {
            return false;
        }

        StopAllCoroutines();
        ReleaseCurrentEnemy();
        pendingBossSpawn = true;
        enemyRespawnTimer = 0f;
        SpawnEnemy();
        return currentEnemy != null && currentEnemyIsBoss;
    }

    public void SetPlayerIdle()
    {
        if (player == null)
        {
            TryResolveDependencies();
        }

        if (player != null && !player.IsDead)
        {
            player.PlayIdle();
        }
    }

    public void GetEnemiesInRadius(Vector3 position, float radius, List<AutoBattleUnit> results)
    {
        if (results == null)
        {
            return;
        }

        float safeRadius = Mathf.Max(0f, radius);
        float sqrRadius = safeRadius * safeRadius;
        var hits = Physics2D.OverlapCircleAll(position, safeRadius);
        for (int i = 0; i < hits.Length; i++)
        {
            AutoBattleUnit enemy = hits[i].GetComponentInParent<AutoBattleUnit>();
            if (enemy == null || enemy == player || enemy.IsDead || results.Contains(enemy))
            {
                continue;
            }

            results.Add(enemy);
        }

        if (currentEnemy != null
            && !currentEnemy.IsDead
            && !results.Contains(currentEnemy)
            && (currentEnemy.transform.position - position).sqrMagnitude <= sqrRadius)
        {
            results.Add(currentEnemy);
        }
    }

    public void Construct(
        AutoBattleUnit player,
        BackgroundScroller backgroundScroller,
        AutoBattleSensor2D playerSensor,
        GameManager gameManager)
    {
        if (this.player != player)
        {
            UnsubscribePlayerAttackHit();
        }

        this.player = player;
        this.backgroundScroller = backgroundScroller;
        this.playerSensor = playerSensor;
        this.gameManager = gameManager;
    }

    private void Awake()
    {
        BuildEnemyPools();
    }

    private void Start()
    {
        TryResolveDependencies();
        BuildServices();
        LogStartupState();
        PrepareEnemyTemplate();
        PreparePlayer();

        SpawnEnemy();
        Log($"Start complete. currentEnemy={(currentEnemy != null ? currentEnemy.name : "NULL")}");
    }

    private void PrepareEnemyTemplate()
    {
        if (enemyTemplate == null)
        {
            return;
        }

        enemyTemplate.gameObject.SetActive(false);
        Log($"enemyTemplate {enemyTemplate.name} SetActive(false)");
    }

    private void PreparePlayer()
    {
        if (player != null)
        {
            SubscribePlayerAttackHit();
            player.ResetUnit();
            Log("Player ResetUnit() done");
        }
    }

    private void OnDestroy()
    {
        UnsubscribePlayerAttackHit();
    }

    private void Update()
    {
        if (!CanUpdateBattle())
        {
            return;
        }

        if (currentEnemy == null || currentEnemy.IsDead)
        {
            HandleMissingEnemy();
            return;
        }

        if (!isFighting && !HandleApproachEnemy())
        {
            return;
        }

        HandleFight();
    }

    private bool CanUpdateBattle()
    {
        return gameManager != null
            && gameManager.State == GameState.Playing
            && (stageManager == null || !stageManager.IsTransitioning)
            && player != null
            && !player.IsDead;
    }

    private void HandleMissingEnemy()
    {
        if (isAttackResolving)
        {
            return;
        }

        if (isFighting)
        {
            isFighting = false;
            SetBackgroundScrolling(true);
            Log("Enemy gone, resuming scroll");
        }

        player.PlayRun();
        enemyRespawnTimer -= Time.deltaTime;
        if (enemyRespawnTimer <= 0f)
        {
            Log($"Respawn timer expired (timer={enemyRespawnTimer:F2})");
            SpawnEnemy();
        }
    }

    private bool HandleApproachEnemy()
    {
        float distance = Mathf.Abs(player.transform.position.x - currentEnemy.transform.position.x);
        if (distance > attackRange)
        {
            player.PlayRun();
            MoveEnemyTowardPlayer();
            SetBackgroundScrolling(true);
            return false;
        }

        StartFight(distance);
        return true;
    }

    private void StartFight(float distance)
    {
        isFighting = true;
        SetBackgroundScrolling(false);
        playerAttackTimer = 0f;
        StartCoroutine(ResolvePlayerAttack());
        Log($"Battle started. distance={distance:F2}, range={attackRange}, enemy={currentEnemy.UnitName}, hp={currentEnemy.CurrentHealth}");
    }

    private void HandleFight()
    {
        if (isAttackResolving)
        {
            return;
        }

        playerAttackTimer += Time.deltaTime;
        if (playerAttackTimer < player.AttackInterval)
        {
            player.PlayAttack();
            return;
        }

        playerAttackTimer = 0f;
        Log($"Attacking. enemy HP={currentEnemy.CurrentHealth}");
        StartCoroutine(ResolvePlayerAttack());
    }

    private void BuildEnemyPools()
    {
        enemyPools.Clear();
        Log($"BuildEnemyPools START - monsters.Length={monsters?.Length ?? 0}");

        if (monsters != null)
        {
            for (int i = 0; i < monsters.Length; i++)
            {
                var monster = monsters[i];
                if (!CanCreatePool(monster, i))
                {
                    continue;
                }

                enemyPools.Add(monster, CreateEnemyPool(monster));
                monster.Prefab.gameObject.SetActive(false);
                Log($"Added pool for {monster.MonsterName}");
            }
        }

        if (enemyTemplate != null)
        {
            fallbackEnemyPool = CreateEnemyPool(null);
            Log("Created fallback enemy pool");
        }

        Log($"BuildEnemyPools END - poolCount={enemyPools.Count}");
    }

    private bool CanCreatePool(MonsterData monster, int index)
    {
        if (monster != null && monster.Prefab != null && !enemyPools.ContainsKey(monster))
        {
            return true;
        }

        LogWarning($"Skipping monster[{index}]={monster?.MonsterName ?? "NULL"}, prefab={(monster?.Prefab != null ? "OK" : "NULL")}, contains={monster != null && enemyPools.ContainsKey(monster)}");
        return false;
    }

    private IObjectPool<AutoBattleUnit> CreateEnemyPool(MonsterData monster)
    {
        var prefab = monster != null ? monster.Prefab : enemyTemplate;
        return new ObjectPool<AutoBattleUnit>(
            createFunc: () => Instantiate(prefab, transform),
            actionOnGet: (unit) => unit.gameObject.SetActive(true),
            actionOnRelease: (unit) => unit.gameObject.SetActive(false),
            actionOnDestroy: (unit) => Destroy(unit.gameObject),
            collectionCheck: false,
            defaultCapacity: 3,
            maxSize: 10
        );
    }

    private void BuildServices()
    {
        effectService = new BattleEffectService(
            this,
            transform,
            impactPrefab,
            impactOffset,
            impactRotation,
            impactScale,
            impactStartSizeMultiplier,
            impactSortingOrder,
            impactReleaseDelay,
            damagePopupSpawner);

        rewardService = new BattleRewardService(
            goldReward,
            rewardBalance,
            coinDropOffset,
            LogWarning
        );

        enemySpawnService = new BattleEnemySpawnService(
            enemyTemplate,
            monsters,
            enemySpawnOffsetX,
            attackRange,
            Log,
            LogWarning);
    }

    private void EnsureServices()
    {
        if (effectService == null || rewardService == null || enemySpawnService == null)
        {
            BuildServices();
        }
    }

    private void TryResolveDependencies()
    {
        if (player == null)
        {
            DIContainer.Global.TryResolve(out player);
        }

        if (backgroundScroller == null)
        {
            DIContainer.Global.TryResolve(out backgroundScroller);
        }

        if (playerSensor == null)
        {
            DIContainer.Global.TryResolve(out playerSensor);
        }

        if (gameManager == null)
        {
            gameManager = DIContainer.Global.Resolve<GameManager>();
        }

        if (stageManager == null)
        {
            DIContainer.Global.TryResolve(out stageManager);
        }

        if (stageManager == null)
        {
            stageManager = FindFirstObjectByType<StageManager>();
        }

        if (coinRewardSpawner == null)
        {
            coinRewardSpawner = GetComponent<CoinDropRewardSpawner>();
        }

        if (coinRewardSpawner == null)
        {
            coinRewardSpawner = FindFirstObjectByType<CoinDropRewardSpawner>();
        }

        if (parallaxBackground == null)
        {
            parallaxBackground = FindFirstObjectByType<ParallaxBackground2D>();
        }

        if (damagePopupSpawner == null)
        {
            damagePopupSpawner = FindFirstObjectByType<DamagePopupSpawner>();
        }
    }

    private void SubscribePlayerAttackHit()
    {
        if (player == null || subscribedAttackHitPlayer == player)
        {
            return;
        }

        UnsubscribePlayerAttackHit();
        player.AttackHit += OnPlayerAttackHit;
        subscribedAttackHitPlayer = player;
    }

    private void UnsubscribePlayerAttackHit()
    {
        if (subscribedAttackHitPlayer == null)
        {
            return;
        }

        subscribedAttackHitPlayer.AttackHit -= OnPlayerAttackHit;
        subscribedAttackHitPlayer = null;
    }

    private IEnumerator ResolvePlayerAttack()
    {
        if (currentEnemy == null || player == null)
        {
            yield break;
        }

        isAttackResolving = true;
        playerAttackEventReceived = false;
        player.RestartAttack();

        if (attackImpactDelay > 0f)
        {
            yield return new WaitForSeconds(attackImpactDelay);
        }

        if (!playerAttackEventReceived)
        {
            ResolvePlayerAttackHit();
        }

        if (currentEnemy == null || !currentEnemy.IsDead)
        {
            isAttackResolving = false;
        }
    }

    private void OnPlayerAttackHit(AutoBattleUnit attacker)
    {
        if (attacker != player || !isAttackResolving || playerAttackEventReceived)
        {
            return;
        }

        ResolvePlayerAttackHit();

        if (currentEnemy == null || !currentEnemy.IsDead)
        {
            isAttackResolving = false;
        }
    }

    private void ResolvePlayerAttackHit()
    {
        playerAttackEventReceived = true;

        if (currentEnemy == null || player == null || player.IsDead)
        {
            return;
        }

        PlayerAttackResolved?.Invoke(player, currentEnemy);

        if (currentEnemy == null || currentEnemy.IsDead)
        {
            if (currentEnemy != null && currentEnemy.IsDead)
            {
                HandleEnemyDefeated();
            }

            return;
        }

        AutoBattleUnit.DamageResult damageResult = player.Attack(currentEnemy);
        effectService?.ShowDamage(currentEnemy, damageResult);
        effectService?.PlayImpactEffect(currentEnemy.transform.position);

        if (currentEnemy.IsDead)
        {
            HandleEnemyDefeated();
        }
    }

    private void HandleEnemyDefeated()
    {
        if (isEnemyDefeatHandled || currentEnemy == null)
        {
            return;
        }

        isEnemyDefeatHandled = true;
        Log($"Enemy defeated. respawnTimer={enemyRespawnDelay}s");
        PlayRewardCoins();
        player.GainExp(rewardService != null ? rewardService.GetExpReward(player, currentMonsterData, currentEnemyIsBoss) : 0f);
        stageManager?.NotifyEnemyDefeated(currentEnemyIsBoss);
        StartCoroutine(ReleaseEnemyAfterDelay());
    }

    public void HandleExternalEnemyDefeated(AutoBattleUnit enemy)
    {
        if (enemy == null || enemy != currentEnemy || !enemy.IsDead)
        {
            return;
        }

        if (isAttackResolving)
        {
            return;
        }

        isAttackResolving = true;
        HandleEnemyDefeated();
    }

    private void PlayRewardCoins()
    {
        if (coinRewardSpawner == null)
        {
            TryResolveDependencies();
        }

        if (coinRewardSpawner == null)
        {
            LogWarning("CoinRewardSpawner is missing. Gold reward was not dropped.");
            return;
        }

        rewardService?.PlayRewardCoins(player, currentEnemy, currentMonsterData, coinRewardSpawner, currentEnemyIsBoss);
    }

    private IEnumerator ReleaseEnemyAfterDelay()
    {
        if (enemyDeathReleaseDelay > 0f)
        {
            yield return new WaitForSeconds(enemyDeathReleaseDelay);
        }

        ReleaseCurrentEnemy();
        enemyRespawnTimer = enemyRespawnDelay;
        isAttackResolving = false;
    }

    private void MoveEnemyTowardPlayer()
    {
        if (player == null || currentEnemy == null)
        {
            return;
        }

        var playerTransform = player.transform;
        var enemyTransform = currentEnemy.transform;
        var targetPosition = new Vector3(playerTransform.position.x, enemyTransform.position.y, enemyTransform.position.z);
        var direction = enemyTransform.position - targetPosition;
        if (direction.sqrMagnitude <= attackRange * attackRange)
        {
            return;
        }

        targetPosition += direction.normalized * attackRange;
        enemyTransform.position = Vector3.MoveTowards(
            enemyTransform.position,
            targetPosition,
            GetCurrentMoveSpeed() * Time.deltaTime);
    }

    private float GetCurrentMoveSpeed()
    {
        return currentMonsterData != null ? currentMonsterData.MoveSpeed : moveSpeed;
    }

    private void SpawnEnemy()
    {
        EnsureServices();
        currentEnemyIsBoss = pendingBossSpawn;
        currentMonsterData = currentEnemyIsBoss
            ? enemySpawnService?.SelectBossMonster(stageManager)
            : enemySpawnService?.SelectMonster(stageManager);
        var selectedPool = GetSelectedEnemyPool(currentMonsterData);

        if (selectedPool == null)
        {
            Debug.LogError("[BattleCtrl] No enemy pool is available.");
            return;
        }

        currentEnemy = selectedPool.Get();
        if (currentEnemy == null)
        {
            Debug.LogError("[BattleCtrl] Failed to get enemy from pool.");
            return;
        }

        enemySpawnService?.SetupSpawnedEnemy(currentEnemy, currentMonsterData, player, stageManager, currentEnemyIsBoss, Time.frameCount);
        pendingBossSpawn = false;
        ResetFightState();
        Log($"Spawned {currentEnemy.name} at offset {enemySpawnService?.GetEnemySpawnOffset()}, attackRange={attackRange}");
    }

    private IObjectPool<AutoBattleUnit> GetSelectedEnemyPool(MonsterData monster)
    {
        if (monster != null && enemyPools.TryGetValue(monster, out var monsterPool))
        {
            Log($"Using pool: {monster.MonsterName}");
            return monsterPool;
        }

        if (fallbackEnemyPool != null)
        {
            Log("Using fallback pool");
            return fallbackEnemyPool;
        }

        foreach (var pool in enemyPools.Values)
        {
            Log("Using first available enemy pool");
            return pool;
        }

        return null;
    }

    private void ResetFightState()
    {
        isFighting = false;
        isAttackResolving = false;
        playerAttackTimer = 0f;
        isEnemyDefeatHandled = false;
    }

    private void ReleaseCurrentEnemy()
    {
        if (currentEnemy != null)
        {
            var selectedPool = GetSelectedEnemyPool(currentMonsterData);

            if (selectedPool != null)
            {
                selectedPool.Release(currentEnemy);
            }
            else
            {
                Destroy(currentEnemy.gameObject);
            }

            currentEnemy = null;
            currentMonsterData = null;
            currentEnemyIsBoss = false;
        }

        isAttackResolving = false;
        if (playerSensor != null)
        {
            playerSensor.ClearTarget();
        }
    }

    private void SetBackgroundScrolling(bool value)
    {
        if (backgroundScroller != null)
        {
            backgroundScroller.SetScrolling(value);
        }

        if (parallaxBackground != null)
        {
            parallaxBackground.SetScrolling(value);
        }
    }

    private void ResetBattle()
    {
        if (player != null)
        {
            player.ResetUnit();
        }

        isFighting = false;
        SpawnEnemy();
    }

    private void LogStartupState()
    {
        Log($"monsters.Length={monsters?.Length ?? 0}");
        for (int i = 0; i < (monsters?.Length ?? 0); i++)
        {
            var monster = monsters[i];
            Log($"monsters[{i}]={monster?.MonsterName ?? "NULL"}, prefab={(monster?.Prefab != null ? monster.Prefab.name : "NULL")}, weight={monster?.SpawnWeight}");
        }
    }

    private void Log(string message)
    {
        if (debugLogs)
        {
            Debug.Log($"[BattleCtrl] {message}");
        }
    }

    private void LogWarning(string message)
    {
        if (debugLogs)
        {
            Debug.LogWarning($"[BattleCtrl] {message}");
        }
    }
}
