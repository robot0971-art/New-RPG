using UnityEngine;

public interface ISkillVfxPlayer
{
    void Play(GameObject prefab, Vector3 position, Vector3 offset, Vector3 scale, float releaseDelay);
    void PlayRepeated(GameObject prefab, Transform target, Vector3 offset, Vector3 scale, float duration, float releaseDelay);
    void PlayMeteorRain(GameObject meteorPrefab, GameObject explosionPrefab, Vector3 targetPosition, SkillData skillData, System.Action<Vector3> onImpact);
}
