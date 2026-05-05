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

    [Header("Rewards")]
    [SerializeField] private GameObject coinDropPrefab;
    [SerializeField] private RectTransform coinUITarget;
    [SerializeField] private Vector3 coinDropOffset = new Vector3(0f, 0.8f, 0f);
    [SerializeField] private float coinDropReleaseDelay = 0.8f;

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

    private readonly Dictionary<MonsterData, IObjectPool<AutoBattleUnit>> enemyPools = new Dictionary<MonsterData, IObjectPool<AutoBattleUnit>>();
    private readonly Dictionary<ParticleSystem, float> originalImpactStartSizeMultipliers = new Dictionary<ParticleSystem, float>();
    private IObjectPool<AutoBattleUnit> fallbackEnemyPool;
    private IObjectPool<GameObject> impactPool;
    private IObjectPool<GameObject> coinDropPool;
    private AutoBattleUnit currentEnemy;
    private MonsterData currentMonsterData;
    private float playerAttackTimer;
    private float enemyRespawnTimer;
    private bool isFighting;
    private bool isAttackResolving;
    private bool playerAttackEventReceived;
    private bool isEnemyDefeatHandled;

    public AutoBattleUnit Player => player;
    public AutoBattleUnit CurrentEnemy => currentEnemy;
    public event System.Action<AutoBattleUnit, AutoBattleUnit> PlayerAttackResolved;

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
        this.player = player;
        this.backgroundScroller = backgroundScroller;
        this.playerSensor = playerSensor;
        this.gameManager = gameManager;
    }

    private void Awake()
    {
        BuildEnemyPools();
        BuildEffectPools();
    }

    private void Start()
    {
        TryResolveDependencies();
        LogStartupState();

        if (enemyTemplate != null)
        {
            enemyTemplate.gameObject.SetActive(false);
            Log($"enemyTemplate {enemyTemplate.name} SetActive(false)");
        }

        if (player != null)
        {
            player.AttackHit -= OnPlayerAttackHit;
            player.AttackHit += OnPlayerAttackHit;
            player.ResetUnit();
            Log("Player ResetUnit() done");
        }

        SpawnEnemy();
        Log($"Start complete. currentEnemy={(currentEnemy != null ? currentEnemy.name : "NULL")}");
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

    private void BuildEffectPools()
    {
        impactPool = new ObjectPool<GameObject>(
            createFunc: () => Instantiate(impactPrefab, transform),
            actionOnGet: (effect) => effect.SetActive(true),
            actionOnRelease: (effect) => effect.SetActive(false),
            actionOnDestroy: Destroy,
            collectionCheck: false,
            defaultCapacity: 5,
            maxSize: 10
        );

        coinDropPool = new ObjectPool<GameObject>(
            createFunc: () => Instantiate(coinDropPrefab, transform),
            actionOnGet: (coin) => coin.SetActive(true),
            actionOnRelease: (coin) => coin.SetActive(false),
            actionOnDestroy: Destroy,
            collectionCheck: false,
            defaultCapacity: 5,
            maxSize: 20
        );
    }

    private void TryResolveDependencies()
    {
        if (player == null)
        {
            player = DIContainer.Global.Resolve<AutoBattleUnit>();
        }

        if (backgroundScroller == null)
        {
            backgroundScroller = DIContainer.Global.Resolve<BackgroundScroller>();
        }

        if (playerSensor == null)
        {
            playerSensor = DIContainer.Global.Resolve<AutoBattleSensor2D>();
        }

        if (gameManager == null)
        {
            gameManager = DIContainer.Global.Resolve<GameManager>();
        }

        if (parallaxBackground == null)
        {
            parallaxBackground = FindFirstObjectByType<ParallaxBackground2D>();
        }
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

        player.Attack(currentEnemy);
        PlayImpactEffect(currentEnemy.transform.position + impactOffset);

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
        PlayCoinDrop(currentEnemy.transform.position + coinDropOffset);
        player.GainExp(GetExpReward());
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

    private void PlayImpactEffect(Vector3 position)
    {
        if (impactPrefab == null || impactPool == null)
        {
            return;
        }

        var impact = impactPool.Get();
        impact.transform.SetPositionAndRotation(position, Quaternion.Euler(impactRotation));
        impact.transform.localScale = impactScale;
        ApplyImpactSortingOrder(impact);
        RestartImpactParticles(impact);
        StartCoroutine(ReleaseImpactAfterDelay(impact));
    }

    private void ApplyImpactSortingOrder(GameObject impact)
    {
        var renderers = impact.GetComponentsInChildren<Renderer>(true);
        for (int i = 0; i < renderers.Length; i++)
        {
            renderers[i].sortingOrder = impactSortingOrder;
        }
    }

    private void RestartImpactParticles(GameObject impact)
    {
        var particleSystems = impact.GetComponentsInChildren<ParticleSystem>(true);
        for (int i = 0; i < particleSystems.Length; i++)
        {
            var main = particleSystems[i].main;
            if (!originalImpactStartSizeMultipliers.TryGetValue(particleSystems[i], out float originalStartSizeMultiplier))
            {
                originalStartSizeMultiplier = main.startSizeMultiplier;
                originalImpactStartSizeMultipliers.Add(particleSystems[i], originalStartSizeMultiplier);
            }

            main.startSizeMultiplier = originalStartSizeMultiplier * impactStartSizeMultiplier;
            particleSystems[i].Clear(true);
            particleSystems[i].Play(true);
        }
    }

    private IEnumerator ReleaseImpactAfterDelay(GameObject impact)
    {
        yield return new WaitForSeconds(impactReleaseDelay);

        if (impact != null)
        {
            impactPool.Release(impact);
        }
    }

    private void PlayCoinDrop(Vector3 position)
    {
        if (coinDropPrefab == null || coinDropPool == null)
        {
            return;
        }

        var coin = coinDropPool.Get();
        coin.transform.rotation = Quaternion.identity;

        var coinDrop = coin.GetComponent<CoinDrop>();
        if (coinDrop == null)
        {
            coin.transform.position = position;
            StartCoroutine(ReleaseCoinDropAfterDelay(coin));
            return;
        }

        SetupCoinDrop(coinDrop);

        float groundY = player != null ? player.transform.position.y : 0f;
        coinDrop.Play(position, groundY);
    }

    private void SetupCoinDrop(CoinDrop coinDrop)
    {
        coinDrop.CoinUITarget = coinUITarget;
        coinDrop.GoldAmount = GetGoldReward();
        coinDrop.Pool = coinDropPool;
    }

    private int GetGoldReward()
    {
        int baseGoldReward = currentMonsterData != null ? currentMonsterData.GoldReward : goldReward;
        float bonusPercent = player != null ? player.TotalGoldBonusPercent : 0f;
        return Mathf.CeilToInt(GetRewardWithBonus(baseGoldReward, bonusPercent));
    }

    private float GetExpReward()
    {
        float baseExpReward = currentMonsterData != null ? currentMonsterData.ExpReward : 5f;
        float bonusPercent = player != null ? player.ExpBonusPercent : 0f;
        return GetRewardWithBonus(baseExpReward, bonusPercent);
    }

    private float GetRewardWithBonus(float baseReward, float bonusPercent)
    {
        return Mathf.Max(0f, baseReward * (1f + bonusPercent / 100f));
    }

    private IEnumerator ReleaseCoinDropAfterDelay(GameObject coin)
    {
        yield return new WaitForSeconds(coinDropReleaseDelay);

        if (coin != null)
        {
            coinDropPool.Release(coin);
        }
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
        currentMonsterData = SelectMonster();
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

        SetupSpawnedEnemy(currentEnemy, currentMonsterData);
        ResetFightState();
        Log($"Spawned {currentEnemy.name} at offset {GetEnemySpawnOffset()}, attackRange={attackRange}");
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

    private void SetupSpawnedEnemy(AutoBattleUnit enemy, MonsterData monster)
    {
        enemy.ApplyMonsterData(monster);
        enemy.transform.position = CalculateEnemySpawnPosition(monster);

        if (monster != null)
        {
            enemy.transform.rotation = Quaternion.Euler(monster.SpawnRotation);
        }

        enemy.name = (monster != null ? monster.MonsterName : "Enemy") + "_" + Time.frameCount;
        enemy.ResetUnit();
        enemy.PlayIdle();
    }

    private Vector3 CalculateEnemySpawnPosition(MonsterData monster)
    {
        Vector3 spawnPos = player.transform.position + Vector3.right * GetEnemySpawnOffset();
        if (monster != null)
        {
            spawnPos += new Vector3(0f, monster.SpawnYOffset, monster.SpawnZOffset);
        }

        return spawnPos;
    }

    private float GetEnemySpawnOffset()
    {
        float actualOffset = Mathf.Max(enemySpawnOffsetX, attackRange + 3f);
        if (enemySpawnOffsetX < attackRange + 1f)
        {
            LogWarning($"enemySpawnOffsetX({enemySpawnOffsetX}) is too small. Using {actualOffset} instead.");
        }

        return actualOffset;
    }

    private void ResetFightState()
    {
        isFighting = false;
        isAttackResolving = false;
        playerAttackTimer = 0f;
        isEnemyDefeatHandled = false;
    }

    private MonsterData SelectMonster()
    {
        if (monsters == null || monsters.Length == 0)
        {
            LogWarning("SelectMonster - monsters array is NULL or empty.");
            return null;
        }

        int totalWeight = CalculateTotalSpawnWeight();
        if (totalWeight <= 0)
        {
            LogWarning("SelectMonster - totalWeight is 0. Using first valid monster.");
            return GetFirstValidMonster();
        }

        int roll = Random.Range(0, totalWeight);
        Log($"SelectMonster roll={roll} (range: 0-{totalWeight - 1})");

        for (int i = 0; i < monsters.Length; i++)
        {
            var monster = monsters[i];
            if (monster == null || monster.Prefab == null)
            {
                continue;
            }

            roll -= monster.SpawnWeight;
            if (roll < 0)
            {
                Log($"Selected monster: {monster.MonsterName}");
                return monster;
            }
        }

        LogWarning("SelectMonster - no monster selected.");
        return GetFirstValidMonster();
    }

    private MonsterData GetFirstValidMonster()
    {
        if (monsters == null)
        {
            return null;
        }

        for (int i = 0; i < monsters.Length; i++)
        {
            if (monsters[i] != null && monsters[i].Prefab != null)
            {
                return monsters[i];
            }
        }

        return null;
    }

    private int CalculateTotalSpawnWeight()
    {
        int totalWeight = 0;
        for (int i = 0; i < monsters.Length; i++)
        {
            if (monsters[i] != null && monsters[i].Prefab != null)
            {
                totalWeight += monsters[i].SpawnWeight;
                continue;
            }

            LogWarning($"SelectMonster - monsters[{i}] invalid: name={monsters[i]?.MonsterName ?? "NULL"}, prefab={(monsters[i]?.Prefab != null ? "OK" : "NULL")}");
        }

        Log($"SelectMonster totalWeight={totalWeight}");
        return totalWeight;
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
