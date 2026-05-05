using System;
using System.Collections;
using UnityEngine;

public sealed class SkillVfxPlayer : MonoBehaviour, ISkillVfxPlayer
{
    public void Play(GameObject prefab, Vector3 position, Vector3 offset, Vector3 scale, float releaseDelay)
    {
        if (prefab == null)
        {
            return;
        }

        var instance = Instantiate(prefab, position + offset, Quaternion.identity, transform);
        instance.transform.localScale = scale;
        Destroy(instance, Mathf.Max(0.1f, releaseDelay));
    }

    public void PlayRepeated(GameObject prefab, Transform target, Vector3 offset, Vector3 scale, float duration, float releaseDelay)
    {
        if (prefab == null || target == null)
        {
            return;
        }

        StartCoroutine(PlayRepeatedRoutine(prefab, target, offset, scale, duration, releaseDelay));
    }

    public void PlayMeteorRain(GameObject meteorPrefab, GameObject explosionPrefab, Vector3 targetPosition, SkillData skillData, Action<Vector3> onImpact)
    {
        if (skillData == null)
        {
            onImpact?.Invoke(targetPosition);
            return;
        }

        StartCoroutine(PlayMeteorRainRoutine(meteorPrefab, explosionPrefab, targetPosition, skillData, onImpact));
    }

    private IEnumerator PlayRepeatedRoutine(GameObject prefab, Transform target, Vector3 offset, Vector3 scale, float duration, float releaseDelay)
    {
        float safeDuration = Mathf.Max(0.1f, duration);
        float safeReleaseDelay = Mathf.Max(0.1f, releaseDelay);
        float interval = Mathf.Max(0.1f, safeReleaseDelay * 0.75f);
        float elapsed = 0f;

        while (elapsed < safeDuration && target != null)
        {
            Play(prefab, target.position, offset, scale, safeReleaseDelay);
            yield return new WaitForSeconds(interval);
            elapsed += interval;
        }
    }

    private IEnumerator PlayMeteorRainRoutine(GameObject meteorPrefab, GameObject explosionPrefab, Vector3 targetPosition, SkillData skillData, Action<Vector3> onImpact)
    {
        int meteorCount = Mathf.Max(1, skillData.meteorCount);
        for (int i = 0; i < meteorCount; i++)
        {
            Vector3 impactPosition = targetPosition + GetMeteorSpreadOffset(skillData, i);
            StartCoroutine(PlaySingleMeteorRoutine(meteorPrefab, explosionPrefab, impactPosition, skillData, onImpact));

            if (skillData.meteorInterval > 0f)
            {
                yield return new WaitForSeconds(skillData.meteorInterval);
            }
        }
    }

    private IEnumerator PlaySingleMeteorRoutine(GameObject meteorPrefab, GameObject explosionPrefab, Vector3 targetPosition, SkillData skillData, Action<Vector3> onImpact)
    {
        GameObject meteor = null;
        Vector3 startPosition = targetPosition + skillData.meteorStartOffset;

        if (meteorPrefab != null)
        {
            meteor = Instantiate(meteorPrefab, startPosition, Quaternion.identity, transform);
            meteor.transform.localScale = skillData.vfxScale;
        }

        float duration = Mathf.Max(0.01f, skillData.meteorFallDuration);
        float elapsed = 0f;
        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / duration);
            if (meteor != null)
            {
                meteor.transform.position = Vector3.Lerp(startPosition, targetPosition + skillData.vfxOffset, t);
            }

            yield return null;
        }

        if (meteor != null)
        {
            Destroy(meteor);
        }

        onImpact?.Invoke(targetPosition);
    }

    private static Vector3 GetMeteorSpreadOffset(SkillData skillData, int index)
    {
        if (index == 0 || skillData.meteorRadius <= 0f)
        {
            return Vector3.zero;
        }

        float angle = index * 137.5f * Mathf.Deg2Rad;
        float radius = skillData.meteorRadius * ((index % 3) + 1) / 3f;
        return new Vector3(Mathf.Cos(angle) * radius, Mathf.Sin(angle) * radius * 0.35f, 0f);
    }
}
