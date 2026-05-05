using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public sealed class SkillManager : MonoBehaviour
{
    [SerializeField] private SkillData[] skills =
    {
        new SkillData { skillType = SkillType.SwordExplosion, effectType = SkillEffectType.Damage, displayName = "Sword Explosion", requiredLevel = 2, cooldown = 5f, damageMultiplier = 2.5f },
        new SkillData { skillType = SkillType.GoldBurst, effectType = SkillEffectType.GoldBuff, displayName = "Gold Burst", requiredLevel = 3, cooldown = 20f, buffPercent = 50f, buffDuration = 8f },
        new SkillData { skillType = SkillType.Heal, effectType = SkillEffectType.Heal, displayName = "Heal", requiredLevel = 4, cooldown = 12f, healPercent = 30f },
        new SkillData { skillType = SkillType.MeteorRain, effectType = SkillEffectType.MeteorRain, displayName = "Meteor Rain", requiredLevel = 5, cooldown = 25f, damageMultiplier = 4f },
    };

    private ISkillTargetProvider targetProvider;
    private ISkillVfxPlayer vfxPlayer;
    private IEnemyDefeatHandler enemyDefeatHandler;
    private IAttackHitNotifier attackHitNotifier;
    private Coroutine goldBuffRoutine;
    private float[] runtimeCooldownRemaining;
    private SkillData pendingSwordExplosion;
    private readonly List<AutoBattleUnit> meteorTargets = new List<AutoBattleUnit>();

    public event Action<SkillType> SkillStateChanged;

    public void Construct(ISkillTargetProvider targetProvider, ISkillVfxPlayer vfxPlayer)
    {
        if (attackHitNotifier != null)
        {
            attackHitNotifier.PlayerAttackResolved -= OnPlayerAttackResolved;
        }

        this.targetProvider = targetProvider;
        this.vfxPlayer = vfxPlayer;
        enemyDefeatHandler = targetProvider as IEnemyDefeatHandler;
        attackHitNotifier = targetProvider as IAttackHitNotifier;

        if (attackHitNotifier != null)
        {
            attackHitNotifier.PlayerAttackResolved += OnPlayerAttackResolved;
        }
    }

    private void OnDestroy()
    {
        if (attackHitNotifier != null)
        {
            attackHitNotifier.PlayerAttackResolved -= OnPlayerAttackResolved;
        }
    }

    private void Awake()
    {
        EnsureDefaultRequiredLevels();
        runtimeCooldownRemaining = new float[skills != null ? skills.Length : 0];
        DIContainer.Global.Register(this);
    }

    private void Update()
    {
        if (skills == null || runtimeCooldownRemaining == null)
        {
            return;
        }

        for (int i = 0; i < runtimeCooldownRemaining.Length; i++)
        {
            if (runtimeCooldownRemaining[i] <= 0f)
            {
                continue;
            }

            runtimeCooldownRemaining[i] = Mathf.Max(0f, runtimeCooldownRemaining[i] - Time.deltaTime);
            SkillStateChanged?.Invoke(skills[i].skillType);
        }
    }

    public bool TryUseSkill(SkillType skillType)
    {
        SkillData skill = GetSkill(skillType, out int index);
        if (skill == null || index < 0)
        {
            return false;
        }

        if (GetCooldownRemaining(skillType) > 0f || !IsSkillUnlocked(skillType) || !CanUseSkill(skill))
        {
            return false;
        }

        ExecuteSkill(skill);
        runtimeCooldownRemaining[index] = Mathf.Max(0f, skill.cooldown);
        SkillStateChanged?.Invoke(skillType);
        return true;
    }

    public float GetCooldownRemaining(SkillType skillType)
    {
        GetSkill(skillType, out int index);
        if (index < 0 || runtimeCooldownRemaining == null || index >= runtimeCooldownRemaining.Length)
        {
            return 0f;
        }

        return runtimeCooldownRemaining[index];
    }

    public float GetCooldownDuration(SkillType skillType)
    {
        SkillData skill = GetSkill(skillType, out _);
        return skill != null ? Mathf.Max(0f, skill.cooldown) : 0f;
    }

    public int GetRequiredLevel(SkillType skillType)
    {
        SkillData skill = GetSkill(skillType, out _);
        return skill != null ? Mathf.Max(1, skill.requiredLevel) : 1;
    }

    public int GetPlayerLevel()
    {
        return targetProvider != null && targetProvider.Player != null ? targetProvider.Player.Level : 1;
    }

    public bool IsSkillUnlocked(SkillType skillType)
    {
        return GetPlayerLevel() >= GetRequiredLevel(skillType);
    }

    private bool CanUseSkill(SkillData skill)
    {
        if (targetProvider == null)
        {
            return false;
        }

        switch (skill.effectType)
        {
            case SkillEffectType.Heal:
                return targetProvider.Player != null && !targetProvider.Player.IsDead;
            case SkillEffectType.GoldBuff:
                return targetProvider.Player != null && !targetProvider.Player.IsDead;
            default:
                if (skill.skillType == SkillType.SwordExplosion)
                {
                    return targetProvider.Player != null && !targetProvider.Player.IsDead;
                }

                return targetProvider.CurrentEnemy != null
                    && !targetProvider.CurrentEnemy.IsDead
                    && targetProvider.Player != null
                    && !targetProvider.Player.IsDead;
        }
    }

    private void ExecuteSkill(SkillData skill)
    {
        switch (skill.effectType)
        {
            case SkillEffectType.Heal:
                ExecuteHeal(skill);
                break;
            case SkillEffectType.GoldBuff:
                ExecuteGoldBuff(skill);
                break;
            case SkillEffectType.MeteorRain:
                ExecuteMeteorRain(skill);
                break;
            default:
                if (skill.skillType == SkillType.SwordExplosion)
                {
                    QueueSwordExplosion(skill);
                    return;
                }

                ExecuteDamage(skill);
                break;
        }
    }

    private void QueueSwordExplosion(SkillData skill)
    {
        pendingSwordExplosion = skill;
    }

    private void OnPlayerAttackResolved(AutoBattleUnit attacker, AutoBattleUnit enemy)
    {
        if (pendingSwordExplosion == null || attacker == null || enemy == null || enemy.IsDead)
        {
            return;
        }

        SkillData skill = pendingSwordExplosion;
        pendingSwordExplosion = null;
        ExecuteDamage(skill, enemy, attacker);
    }

    private void ExecuteDamage(SkillData skill)
    {
        AutoBattleUnit enemy = targetProvider.CurrentEnemy;
        AutoBattleUnit player = targetProvider.Player;
        if (enemy == null || player == null)
        {
            return;
        }

        ExecuteDamage(skill, enemy, player);
    }

    private void ExecuteDamage(SkillData skill, AutoBattleUnit enemy, AutoBattleUnit player)
    {
        if (skill == null || enemy == null || player == null)
        {
            return;
        }

        vfxPlayer?.Play(skill.vfxPrefab, enemy.transform.position, skill.vfxOffset, skill.vfxScale, skill.vfxReleaseDelay);
        enemy.TakeDamage(player.AttackPower * skill.damageMultiplier);
        NotifyEnemyDefeatedIfNeeded(enemy);
    }

    private void ExecuteHeal(SkillData skill)
    {
        AutoBattleUnit player = targetProvider.Player;
        if (player == null)
        {
            return;
        }

        float vfxDuration = Mathf.Max(skill.vfxReleaseDelay, 0.1f);
        vfxPlayer?.PlayRepeated(skill.vfxPrefab, player.transform, skill.vfxOffset, skill.vfxScale, vfxDuration, skill.vfxReleaseDelay);
        player.Heal(player.MaxHealth * Mathf.Max(0f, skill.healPercent) / 100f);
    }

    private void ExecuteGoldBuff(SkillData skill)
    {
        AutoBattleUnit player = targetProvider.Player;
        if (player == null)
        {
            return;
        }

        float duration = Mathf.Max(0f, skill.buffDuration);
        vfxPlayer?.PlayRepeated(skill.vfxPrefab, player.transform, skill.vfxOffset, skill.vfxScale, duration, skill.vfxReleaseDelay);

        if (goldBuffRoutine != null)
        {
            StopCoroutine(goldBuffRoutine);
        }

        goldBuffRoutine = StartCoroutine(GoldBuffRoutine(player, skill.buffPercent, duration));
    }

    private IEnumerator GoldBuffRoutine(AutoBattleUnit player, float bonusPercent, float duration)
    {
        player.SetSkillGoldBonus(bonusPercent);
        yield return new WaitForSeconds(Mathf.Max(0f, duration));
        player.SetSkillGoldBonus(0f);
        goldBuffRoutine = null;
    }

    private void ExecuteMeteorRain(SkillData skill)
    {
        AutoBattleUnit enemy = targetProvider.CurrentEnemy;
        AutoBattleUnit player = targetProvider.Player;
        if (enemy == null || player == null)
        {
            return;
        }

        Vector3 targetPosition = enemy.transform.position;
        float damagePerImpact = player.AttackPower * skill.damageMultiplier / Mathf.Max(1, skill.meteorCount);
        vfxPlayer?.PlayMeteorRain(
            skill.vfxPrefab,
            skill.secondaryVfxPrefab,
            targetPosition,
            skill,
            (impactPosition) =>
            {
                meteorTargets.Clear();
                targetProvider.GetEnemiesInRadius(impactPosition, skill.meteorRadius, meteorTargets);

                for (int i = 0; i < meteorTargets.Count; i++)
                {
                    AutoBattleUnit target = meteorTargets[i];
                    if (target == null || target.IsDead)
                    {
                        continue;
                    }

                    vfxPlayer?.Play(skill.secondaryVfxPrefab, target.transform.position, skill.vfxOffset, skill.vfxScale, skill.vfxReleaseDelay);
                    target.TakeDamage(damagePerImpact);
                    NotifyEnemyDefeatedIfNeeded(target);
                }
            });
    }

    private void NotifyEnemyDefeatedIfNeeded(AutoBattleUnit enemy)
    {
        if (enemy != null && enemy.IsDead)
        {
            enemyDefeatHandler?.HandleExternalEnemyDefeated(enemy);
        }
    }

    private SkillData GetSkill(SkillType skillType, out int index)
    {
        index = -1;
        if (skills == null)
        {
            return null;
        }

        for (int i = 0; i < skills.Length; i++)
        {
            if (skills[i] != null && skills[i].skillType == skillType)
            {
                index = i;
                return skills[i];
            }
        }

        return null;
    }

    private void EnsureDefaultRequiredLevels()
    {
        if (skills == null)
        {
            return;
        }

        for (int i = 0; i < skills.Length; i++)
        {
            if (skills[i] == null || skills[i].requiredLevel > 1)
            {
                continue;
            }

            skills[i].requiredLevel = GetDefaultRequiredLevel(skills[i].skillType);
        }
    }

    private static int GetDefaultRequiredLevel(SkillType skillType)
    {
        switch (skillType)
        {
            case SkillType.SwordExplosion:
                return 2;
            case SkillType.GoldBurst:
                return 3;
            case SkillType.Heal:
                return 4;
            case SkillType.MeteorRain:
                return 5;
            default:
                return 1;
        }
    }
}
