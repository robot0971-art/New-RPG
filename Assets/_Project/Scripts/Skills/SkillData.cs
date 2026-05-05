using System;
using UnityEngine;

[Serializable]
public sealed class SkillData
{
    public SkillType skillType;
    public SkillEffectType effectType;
    public string displayName = "Skill";
    public int requiredLevel = 1;
    public float cooldown = 5f;
    public float damageMultiplier = 2f;
    public float healPercent = 30f;
    public float buffPercent = 50f;
    public float buffDuration = 8f;
    public GameObject vfxPrefab;
    public GameObject secondaryVfxPrefab;
    public Vector3 vfxOffset;
    public Vector3 vfxScale = Vector3.one;
    public float vfxReleaseDelay = 2f;
    public Vector3 meteorStartOffset = new Vector3(0f, 5f, 0f);
    public float meteorFallDuration = 0.7f;
    public int meteorCount = 5;
    public float meteorInterval = 0.15f;
    public float meteorRadius = 1.2f;
}
