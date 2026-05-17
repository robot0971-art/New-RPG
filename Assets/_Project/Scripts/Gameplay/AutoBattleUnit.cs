using System;
using System.Collections;
using UnityEngine;

public sealed class AutoBattleUnit : MonoBehaviour, ISaveable
{
    [SerializeField] private string unitName = "Unit";
    [SerializeField] private float maxHealth = 10f;
    [SerializeField] private float attackPower = 1f;
    [SerializeField] private float attackInterval = 1f;
    [SerializeField] private LevelBalanceData levelBalance;
    [Header("Animation")]
    [SerializeField] private Animator animator;
    [SerializeField] private string idleStateName = "Idle";
    [SerializeField] private string runStateName = "Run";
    [SerializeField] private string attackStateName = "Attack";
    [SerializeField] private string hitStateName = "Hit";
    [SerializeField] private string deathStateName = "Death";
    [SerializeField] private float hitLockDuration = 0.4f;
    [SerializeField] private bool debugAnimationLogs;

    public string UnitName => unitName;
    public float MaxHealth => maxHealth;
    public float CurrentHealth { get; private set; }
    public float AttackPower => attackPower;
    public float AttackInterval => attackInterval;
    public float CritChancePercent { get; private set; }
    public float CritDamagePercent { get; private set; } = 150f;
    public float GoldBonusPercent { get; private set; }
    public float SkillGoldBonusPercent { get; private set; }
    public float TotalGoldBonusPercent => GoldBonusPercent + SkillGoldBonusPercent;
    public float ExpBonusPercent { get; private set; }
    public bool IsDead => CurrentHealth <= 0f;

    // 성장 데이터
    public int Level { get; private set; } = 1;
    public float CurrentExp { get; private set; }
    public float RequiredExp => GetRequiredExp(Level);

    public event Action<AutoBattleUnit> Damaged;
    public event Action<AutoBattleUnit> Died;
    public event Action<AutoBattleUnit> AttackHit;
    public event Action<AutoBattleUnit> ExperienceChanged;
    public event Action<int> LevelChanged;

    private int idleHash;
    private int runHash;
    private int attackHash;
    private int hitHash;
    private int deathHash;
    private int currentAnimationHash;
    private Coroutine hitLockRoutine;
    private Coroutine deathAfterHitRoutine;
    private bool isHitLocked;
    private float baseAttackPower;
    private float baseMaxHealth;
    private float baseAttackInterval;
    private float upgradedAttackPower;
    private float upgradedAttackSpeed = 1f;
    private float upgradedMaxHealth;
    private float levelAttackBonus;
    private float levelHealthBonus;
    private bool hasStatUpgrades;

    public void ApplyMonsterData(MonsterData data)
    {
        if (data == null)
        {
            return;
        }

        unitName = data.MonsterName;
        maxHealth = data.MaxHealth;
        attackPower = data.AttackPower;
        attackInterval = data.AttackInterval;
        baseAttackPower = attackPower;
        baseMaxHealth = maxHealth;
        baseAttackInterval = attackInterval;
        hasStatUpgrades = false;
        levelAttackBonus = 0f;
        levelHealthBonus = 0f;
        ResetUnit();
    }

    public void ApplyRuntimeBattleModifiers(string nameSuffix, float healthMultiplier, float attackMultiplier, float attackIntervalMultiplier)
    {
        if (!string.IsNullOrWhiteSpace(nameSuffix))
        {
            unitName = $"{unitName} {nameSuffix}";
        }

        maxHealth = Mathf.Max(1f, maxHealth * Mathf.Max(0.01f, healthMultiplier));
        attackPower = Mathf.Max(0f, attackPower * Mathf.Max(0f, attackMultiplier));
        attackInterval = Mathf.Max(0.1f, attackInterval * Mathf.Max(0.01f, attackIntervalMultiplier));
        baseAttackPower = attackPower;
        baseMaxHealth = maxHealth;
        baseAttackInterval = attackInterval;
        CurrentHealth = maxHealth;
    }

    public void ApplyStatUpgrades(
        float upgradedAttackPower,
        float upgradedAttackSpeed,
        float upgradedMaxHealth,
        float critChancePercent,
        float critDamagePercent,
        float goldBonusPercent,
        float expBonusPercent)
    {
        if (baseAttackInterval <= 0f)
        {
            baseAttackInterval = attackInterval;
        }

        this.upgradedAttackPower = Mathf.Max(0f, upgradedAttackPower);
        this.upgradedAttackSpeed = Mathf.Max(0.1f, upgradedAttackSpeed);
        this.upgradedMaxHealth = Mathf.Max(1f, upgradedMaxHealth);
        hasStatUpgrades = true;

        CritChancePercent = Mathf.Clamp(critChancePercent, 0f, 100f);
        CritDamagePercent = Mathf.Max(100f, critDamagePercent);
        GoldBonusPercent = Mathf.Max(0f, goldBonusPercent);
        ExpBonusPercent = Mathf.Max(0f, expBonusPercent);

        RecalculateCombatStats(true);
    }

    private void Awake()
    {
        baseAttackInterval = attackInterval;
        baseAttackPower = attackPower;
        baseMaxHealth = maxHealth;
        upgradedAttackPower = attackPower;
        upgradedMaxHealth = maxHealth;

        if (animator == null)
        {
            animator = GetComponent<Animator>();
        }

        // 해시값 미리 캐싱
        idleHash = Animator.StringToHash(idleStateName);
        runHash = Animator.StringToHash(runStateName);
        attackHash = Animator.StringToHash(attackStateName);
        hitHash = Animator.StringToHash(hitStateName);
        deathHash = Animator.StringToHash(deathStateName);

        ResetUnit();
    }

    public void ResetUnit()
    {
        isHitLocked = false;
        if (hitLockRoutine != null)
        {
            StopCoroutine(hitLockRoutine);
            hitLockRoutine = null;
        }

        if (deathAfterHitRoutine != null)
        {
            StopCoroutine(deathAfterHitRoutine);
            deathAfterHitRoutine = null;
        }

        CurrentHealth = maxHealth;
        PlayIdle();
    }

    public void GainExp(float amount)
    {
        if (IsDead) return;

        CurrentExp += amount;
        while (CurrentExp >= RequiredExp)
        {
            LevelUp();
        }

        ExperienceChanged?.Invoke(this);
    }

    private void LevelUp()
    {
        CurrentExp -= RequiredExp;
        Level++;
        levelAttackBonus += GetAttackBonusPerLevel();
        levelHealthBonus += GetHealthBonusPerLevel();
        RecalculateCombatStats(false);
        CurrentHealth = maxHealth;
        LevelChanged?.Invoke(Level);
        SaveEvents.RequestSave();
        Debug.Log($"<color=yellow>[Level Up]</color> {unitName} Level {Level}! ATK: {attackPower}");
    }

    public void LoadProgress(int level, float currentExp)
    {
        Level = Mathf.Max(1, level);
        CurrentExp = Mathf.Clamp(currentExp, 0f, RequiredExp);
        RecalculateLevelBonuses();
        RecalculateCombatStats(false);
        LevelChanged?.Invoke(Level);
        ExperienceChanged?.Invoke(this);
    }

    public void CaptureSaveData(SaveData saveData)
    {
        if (saveData == null)
        {
            return;
        }

        saveData.playerLevel = Level;
        saveData.playerExp = CurrentExp;
    }

    public void RestoreSaveData(SaveData saveData)
    {
        if (saveData == null)
        {
            return;
        }

        LoadProgress(saveData.playerLevel, saveData.playerExp);
    }

    private void RecalculateLevelBonuses()
    {
        int bonusLevels = Mathf.Max(0, Level - 1);
        levelAttackBonus = GetLevelAttackBonus(bonusLevels);
        levelHealthBonus = GetLevelHealthBonus(bonusLevels);
    }

    private float GetRequiredExp(int level)
    {
        return levelBalance != null ? levelBalance.GetRequiredExp(level) : Mathf.Max(1f, Mathf.Max(1, level) * 10f);
    }

    private float GetAttackBonusPerLevel()
    {
        return levelBalance != null ? levelBalance.AttackBonusPerLevel : 2f;
    }

    private float GetHealthBonusPerLevel()
    {
        return levelBalance != null ? levelBalance.HealthBonusPerLevel : 5f;
    }

    private float GetLevelAttackBonus(int bonusLevels)
    {
        return levelBalance != null ? levelBalance.GetAttackBonus(bonusLevels) : Mathf.Max(0, bonusLevels) * 2f;
    }

    private float GetLevelHealthBonus(int bonusLevels)
    {
        return levelBalance != null ? levelBalance.GetHealthBonus(bonusLevels) : Mathf.Max(0, bonusLevels) * 5f;
    }

    private void RecalculateCombatStats(bool preserveHealthRatio)
    {
        float previousMaxHealth = Mathf.Max(1f, maxHealth);
        float healthRatio = preserveHealthRatio ? Mathf.Clamp01(CurrentHealth / previousMaxHealth) : 1f;

        if (hasStatUpgrades)
        {
            attackPower = upgradedAttackPower + levelAttackBonus;
            maxHealth = upgradedMaxHealth + levelHealthBonus;
            attackInterval = Mathf.Max(0.1f, baseAttackInterval / upgradedAttackSpeed);
        }
        else
        {
            attackPower = baseAttackPower + levelAttackBonus;
            maxHealth = baseMaxHealth + levelHealthBonus;
            attackInterval = Mathf.Max(0.1f, baseAttackInterval);
        }

        maxHealth = Mathf.Max(1f, maxHealth);
        attackPower = Mathf.Max(0f, attackPower);
        CurrentHealth = Mathf.Clamp(maxHealth * healthRatio, 1f, maxHealth);
    }

    public void TakeDamage(float damage)
    {
        if (IsDead)
        {
            return;
        }

        CurrentHealth = Mathf.Max(0f, CurrentHealth - damage);
        Damaged?.Invoke(this);
        PlayHit();

        if (IsDead)
        {
            if (deathAfterHitRoutine != null)
            {
                StopCoroutine(deathAfterHitRoutine);
            }

            deathAfterHitRoutine = StartCoroutine(PlayDeathAfterHit());
            return;
        }
    }

    public void Heal(float amount)
    {
        if (IsDead || amount <= 0f)
        {
            return;
        }

        CurrentHealth = Mathf.Min(maxHealth, CurrentHealth + amount);
        Damaged?.Invoke(this);
    }

    public void SetSkillGoldBonus(float bonusPercent)
    {
        SkillGoldBonusPercent = Mathf.Max(0f, bonusPercent);
    }

    public void PlayIdle() => PlayState(idleHash);
    public void PlayRun() => PlayState(runHash);
    public void PlayAttack() => PlayState(attackHash);
    public void RestartAttack() => PlayState(attackHash, true);
    public void PlayDeath() => PlayState(deathHash, true);
    public void PlayHit()
    {
        PlayState(hitHash, true);

        if (hitLockRoutine != null)
        {
            StopCoroutine(hitLockRoutine);
        }

        hitLockRoutine = StartCoroutine(LockHitAnimation());
    }

    public DamageResult Attack(AutoBattleUnit target)
    {
        if (target == null || IsDead)
        {
            return DamageResult.None;
        }

        PlayAttack();
        DamageResult damageResult = GetAttackDamage();
        target.TakeDamage(damageResult.Amount);
        return damageResult;
    }

    private DamageResult GetAttackDamage()
    {
        float damage = attackPower;
        bool isCritical = false;
        if (CritChancePercent > 0f && UnityEngine.Random.value <= CritChancePercent / 100f)
        {
            damage *= CritDamagePercent / 100f;
            isCritical = true;
        }

        return new DamageResult(damage, isCritical);
    }

    public struct DamageResult
    {
        public static readonly DamageResult None = new DamageResult(0f, false);

        public DamageResult(float amount, bool isCritical)
        {
            Amount = amount;
            IsCritical = isCritical;
        }

        public float Amount { get; }
        public bool IsCritical { get; }
    }

    public void OnAttackHit()
    {
        AttackHit?.Invoke(this);
    }

    private bool PlayState(int stateHash, bool forceRestart = false)
    {
        if (animator == null || stateHash == 0)
        {
            return false;
        }

        if (isHitLocked && stateHash != hitHash && stateHash != deathHash)
        {
            return false;
        }

        if (!animator.HasState(0, stateHash))
        {
            Debug.LogWarning($"[Animation Error] State with hash {stateHash} (Name?) not found in {gameObject.name}'s Animator.");
            return false;
        }

        var currentState = animator.GetCurrentAnimatorStateInfo(0);
        if (!forceRestart && currentState.shortNameHash == stateHash)
        {
            return true;
        }

        if (debugAnimationLogs)
        {
            var stateName = StateHashToName(stateHash);
            var caller = new System.Diagnostics.StackTrace(1, false).GetFrame(0)?.GetMethod()?.Name ?? "?";
            Debug.Log($"[Anim] {gameObject.name} → {stateName} (caller={caller}, forceRestart={forceRestart})");
        }

        animator.Play(stateHash, 0, 0f);
        currentAnimationHash = stateHash;
        return true;
    }

    private IEnumerator LockHitAnimation()
    {
        isHitLocked = true;
        yield return new WaitForSeconds(hitLockDuration);
        isHitLocked = false;
        hitLockRoutine = null;
    }

    private IEnumerator PlayDeathAfterHit()
    {
        yield return new WaitForSeconds(hitLockDuration);
        isHitLocked = false;
        hitLockRoutine = null;
        PlayDeath();
        Died?.Invoke(this);
        deathAfterHitRoutine = null;
    }

    private string StateHashToName(int hash)
    {
        if (hash == idleHash) return idleStateName;
        if (hash == runHash) return runStateName;
        if (hash == attackHash) return attackStateName;
        if (hash == hitHash) return hitStateName;
        if (hash == deathHash) return deathStateName;
        return hash.ToString();
    }
}
