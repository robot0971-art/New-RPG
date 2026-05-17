using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Pool;

public sealed class BattleEffectService
{
    private readonly MonoBehaviour coroutineRunner;
    private readonly Transform parent;
    private readonly GameObject impactPrefab;
    private readonly Vector3 impactOffset;
    private readonly Vector3 impactRotation;
    private readonly Vector3 impactScale;
    private readonly float impactStartSizeMultiplier;
    private readonly int impactSortingOrder;
    private readonly float impactReleaseDelay;
    private readonly DamagePopupSpawner damagePopupSpawner;

    private readonly Dictionary<ParticleSystem, float> originalImpactStartSizeMultipliers = new Dictionary<ParticleSystem, float>();
    private IObjectPool<GameObject> impactPool;

    public BattleEffectService(
        MonoBehaviour coroutineRunner,
        Transform parent,
        GameObject impactPrefab,
        Vector3 impactOffset,
        Vector3 impactRotation,
        Vector3 impactScale,
        float impactStartSizeMultiplier,
        int impactSortingOrder,
        float impactReleaseDelay,
        DamagePopupSpawner damagePopupSpawner)
    {
        this.coroutineRunner = coroutineRunner;
        this.parent = parent;
        this.impactPrefab = impactPrefab;
        this.impactOffset = impactOffset;
        this.impactRotation = impactRotation;
        this.impactScale = impactScale;
        this.impactStartSizeMultiplier = impactStartSizeMultiplier;
        this.impactSortingOrder = impactSortingOrder;
        this.impactReleaseDelay = impactReleaseDelay;
        this.damagePopupSpawner = damagePopupSpawner;

        BuildImpactPool();
    }

    public void ShowDamage(AutoBattleUnit target, AutoBattleUnit.DamageResult damageResult)
    {
        if (damagePopupSpawner == null || target == null || damageResult.Amount <= 0f)
        {
            return;
        }

        damagePopupSpawner.ShowDamage(damageResult.Amount, damageResult.IsCritical, target.transform.position);
    }

    public void PlayImpactEffect(Vector3 position)
    {
        if (impactPrefab == null || impactPool == null || coroutineRunner == null)
        {
            return;
        }

        var impact = impactPool.Get();
        impact.transform.SetPositionAndRotation(position + impactOffset, Quaternion.Euler(impactRotation));
        impact.transform.localScale = impactScale;
        ApplyImpactSortingOrder(impact);
        RestartImpactParticles(impact);
        coroutineRunner.StartCoroutine(ReleaseImpactAfterDelay(impact));
    }

    private void BuildImpactPool()
    {
        if (impactPrefab == null)
        {
            return;
        }

        impactPool = new ObjectPool<GameObject>(
            createFunc: () => Object.Instantiate(impactPrefab, parent),
            actionOnGet: (effect) => effect.SetActive(true),
            actionOnRelease: (effect) => effect.SetActive(false),
            actionOnDestroy: (effect) => Object.Destroy(effect),
            collectionCheck: false,
            defaultCapacity: 5,
            maxSize: 10
        );
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
}
