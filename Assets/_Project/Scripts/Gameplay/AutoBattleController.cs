using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Pool;

public sealed class AutoBattleController : MonoBehaviour
{
    [Header("Settings")]
    [SerializeField] private AutoBattleUnit enemyTemplate;
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
    [Range(0f, 0.5f)]
    [SerializeField] private float attackImpactDelay = 0.25f;
    [Range(0f, 2f)]
    [SerializeField] private float enemyDeathReleaseDelay = 1.25f;

    private AutoBattleUnit player;
    private BackgroundScroller backgroundScroller;
    [SerializeField] private ParallaxBackground2D parallaxBackground;
    private AutoBattleSensor2D playerSensor;
    private GameManager gameManager;

    private IObjectPool<AutoBattleUnit> enemyPool;
    private IObjectPool<GameObject> impactPool;
    private readonly Dictionary<ParticleSystem, float> originalImpactStartSizeMultipliers = new();
    private AutoBattleUnit currentEnemy;
    private float playerAttackTimer;
    private float enemyRespawnTimer;
    private bool isFighting;
    private bool isAttackResolving;
    private bool playerAttackEventReceived;

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
        enemyPool = new ObjectPool<AutoBattleUnit>(
            createFunc: () => Instantiate(enemyTemplate, transform),
            actionOnGet: (unit) => unit.gameObject.SetActive(true),
            actionOnRelease: (unit) => unit.gameObject.SetActive(false),
            actionOnDestroy: (unit) => Destroy(unit.gameObject),
            collectionCheck: false,
            defaultCapacity: 5,
            maxSize: 10
        );

        impactPool = new ObjectPool<GameObject>(
            createFunc: () => Instantiate(impactPrefab, transform),
            actionOnGet: (effect) => effect.SetActive(true),
            actionOnRelease: (effect) => effect.SetActive(false),
            actionOnDestroy: Destroy,
            collectionCheck: false,
            defaultCapacity: 5,
            maxSize: 10
        );
    }

    private void Start()
    {
        TryResolveDependencies();
        Debug.Log($"<color=yellow>[BattleCtrl] Start BEGIN - player={(player != null ? player.name : "NULL")}, sensor={(playerSensor != null ? playerSensor.name : "NULL")}, sensorTarget={(playerSensor?.CurrentTarget != null ? playerSensor.CurrentTarget.name : "NULL")}</color>");
        if (enemyTemplate != null)
        {
            enemyTemplate.gameObject.SetActive(false);
            Debug.Log($"<color=yellow>[BattleCtrl] enemyTemplate {enemyTemplate.name} SetActive(false)</color>");
        }
        if (player != null)
        {
            player.AttackHit -= OnPlayerAttackHit;
            player.AttackHit += OnPlayerAttackHit;
            player.ResetUnit();
            Debug.Log($"<color=yellow>[BattleCtrl] Player ResetUnit() done</color>");
        }
        SpawnEnemy();
        Debug.Log($"<color=yellow>[BattleCtrl] Start END - currentEnemy={(currentEnemy != null ? currentEnemy.name : "NULL")}, sensorTarget={(playerSensor?.CurrentTarget != null ? playerSensor.CurrentTarget.name : "NULL")}</color>");
    }

    private void TryResolveDependencies()
    {
        if (player == null) player = DIContainer.Global.Resolve<AutoBattleUnit>();
        if (backgroundScroller == null) backgroundScroller = DIContainer.Global.Resolve<BackgroundScroller>();
        if (parallaxBackground == null) parallaxBackground = FindFirstObjectByType<ParallaxBackground2D>();
        if (playerSensor == null) playerSensor = DIContainer.Global.Resolve<AutoBattleSensor2D>();
        if (gameManager == null) gameManager = DIContainer.Global.Resolve<GameManager>();
    }

    private void Update()
    {
        if (gameManager == null || gameManager.State != GameState.Playing) return;
        if (player == null || player.IsDead) return;

        // 1. 적이 없으면 다음 적 소환 대기 (이때만 달리기)
        if (currentEnemy == null || currentEnemy.IsDead)
        {
            if (isAttackResolving)
            {
                return;
            }

            if (isFighting) 
            {
                isFighting = false;
                Debug.Log("<color=orange>[BattleCtrl] Section1 - Enemy gone, isFighting=false, resuming scroll</color>");
                SetBackgroundScrolling(true);
            }

            player.PlayRun();
            
            enemyRespawnTimer -= Time.deltaTime;
            if (enemyRespawnTimer <= 0f)
            {
                Debug.Log($"<color=orange>[BattleCtrl] Section1 - Respawning enemy (timer={enemyRespawnTimer:F2})</color>");
                SpawnEnemy();
            }
            return;
        }

        // --- 여기서부터는 적이 있는 상태 ---

        // 2. 아직 전투 시작 전이면 적에게 접근
        if (!isFighting)
        {
            float distance = Vector3.Distance(player.transform.position, currentEnemy.transform.position);
            bool inRange = distance <= attackRange;

            if (inRange)
            {
                isFighting = true;
                SetBackgroundScrolling(false);
                playerAttackTimer = 0f;
                StartCoroutine(ResolvePlayerAttack());
                Debug.Log($"<color=orange>[BattleCtrl] Section2 - BATTLE START! distance={distance:F2}, range={attackRange}, enemy={currentEnemy.UnitName} HP={currentEnemy.CurrentHealth}</color>");
            }
            else
            {
                player.PlayRun();
                MoveEnemyTowardPlayer();
                SetBackgroundScrolling(true);
                return;
            }
        }

        // 3. 전투 중 - 공격 타이머만 틱하고, 타이밍에만 Attack 애니메이션 + 데미지 동시 처리
        if (isFighting)
        {
            if (isAttackResolving)
            {
                return;
            }

            playerAttackTimer += Time.deltaTime;
            
            if (playerAttackTimer >= player.AttackInterval)
            {
                playerAttackTimer = 0f;
                Debug.Log($"<color=orange>[BattleCtrl] Section3 - Attacking! timer reset, enemy HP={currentEnemy.CurrentHealth}</color>");
                StartCoroutine(ResolvePlayerAttack());
            }
            else
            {
                // 공격 타이밍이 아닐 때는 Idle 유지
                player.PlayAttack();
            }
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

        if (currentEnemy != null && player != null && !player.IsDead)
        {
            player.Attack(currentEnemy);
            PlayImpactEffect(currentEnemy.transform.position + impactOffset);

            if (currentEnemy.IsDead)
            {
                Debug.Log($"<color=cyan>[BattleCtrl] ENEMY DEFEATED! enemy HP=0, respawnTimer={enemyRespawnDelay}s</color>");
                player.GainExp(5f);
                StartCoroutine(ReleaseEnemyAfterDelay());
            }
        }
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

        var renderers = impact.GetComponentsInChildren<Renderer>(true);
        for (int i = 0; i < renderers.Length; i++)
        {
            renderers[i].sortingOrder = impactSortingOrder;
        }

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

        StartCoroutine(ReleaseImpactAfterDelay(impact));
    }

    private IEnumerator ReleaseImpactAfterDelay(GameObject impact)
    {
        yield return new WaitForSeconds(impactReleaseDelay);

        if (impact != null)
        {
            impactPool.Release(impact);
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
        var targetPosition = playerTransform.position;
        var direction = enemyTransform.position - targetPosition;
        if (direction.sqrMagnitude <= attackRange * attackRange)
        {
            return;
        }

        targetPosition += direction.normalized * attackRange;
        var nextPosition = Vector3.MoveTowards(
            enemyTransform.position,
            targetPosition,
            moveSpeed * Time.deltaTime);

        enemyTransform.position = nextPosition;
    }

    private void SpawnEnemy()
    {
        currentEnemy = enemyPool.Get();
        
        // enemySpawnOffsetX가 attackRange보다 작으면 적이 스폰되자마자 전투 시작됨
        float actualOffset = Mathf.Max(enemySpawnOffsetX, attackRange + 3f);
        if (enemySpawnOffsetX < attackRange + 1f)
        {
            Debug.LogWarning($"<color=red>[BattleCtrl] enemySpawnOffsetX({enemySpawnOffsetX}) is too small! Using {actualOffset} instead.</color>");
        }
        
        currentEnemy.transform.position = player.transform.position + Vector3.right * actualOffset;
        currentEnemy.name = "Enemy_" + Time.frameCount;
        currentEnemy.ResetUnit();
        currentEnemy.PlayIdle();
        isFighting = false;
        isAttackResolving = false;
        playerAttackTimer = 0f;
        Debug.Log($"<color=yellow>[BattleCtrl] Spawned {currentEnemy.name} at offset {actualOffset}, attackRange={attackRange}</color>");
    }

    private void ReleaseCurrentEnemy()
    {
        if (currentEnemy != null)
        {
            enemyPool.Release(currentEnemy);
            currentEnemy = null;
        }
        isAttackResolving = false;
        if (playerSensor != null) playerSensor.ClearTarget();
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
        if (player != null) player.ResetUnit();
        isFighting = false;
        SpawnEnemy();
    }
}
