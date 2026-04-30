using System;
using System.Collections;
using UnityEngine;

public sealed class AutoBattleUnit : MonoBehaviour
{
    [SerializeField] private string unitName = "Unit";
    [SerializeField] private float maxHealth = 10f;
    [SerializeField] private float attackPower = 1f;
    [SerializeField] private float attackInterval = 1f;
    [Header("Animation")]
    [SerializeField] private Animator animator;
    [SerializeField] private string idleStateName = "Idle";
    [SerializeField] private string runStateName = "Run";
    [SerializeField] private string attackStateName = "Attack";
    [SerializeField] private string hitStateName = "Hit";
    [SerializeField] private string deathStateName = "Death";
    [SerializeField] private float hitLockDuration = 0.4f;

    public string UnitName => unitName;
    public float MaxHealth => maxHealth;
    public float CurrentHealth { get; private set; }
    public float AttackPower => attackPower;
    public float AttackInterval => attackInterval;
    public bool IsDead => CurrentHealth <= 0f;

    // 성장 데이터
    public int Level { get; private set; } = 1;
    public float CurrentExp { get; private set; }
    public float RequiredExp => Level * 10f; 

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
        ResetUnit();
    }

    private void Awake()
    {
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
        attackPower += 2f;
        maxHealth += 5f;
        CurrentHealth = maxHealth;
        LevelChanged?.Invoke(Level);
        Debug.Log($"<color=yellow>[Level Up]</color> {unitName} Level {Level}! ATK: {attackPower}");
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

    public void Attack(AutoBattleUnit target)
    {
        if (target == null || IsDead)
        {
            return;
        }

        PlayAttack();
        target.TakeDamage(attackPower);
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

        var stateName = StateHashToName(stateHash);
        var caller = new System.Diagnostics.StackTrace(1, false).GetFrame(0)?.GetMethod()?.Name ?? "?";
        Debug.Log($"[Anim] {gameObject.name} → {stateName} (caller={caller}, forceRestart={forceRestart})");
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
