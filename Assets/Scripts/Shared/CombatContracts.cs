using System;
using UnityEngine;

public enum DamageType
{
    Melee,
    Projectile,
    Skill,
    Dot,
    Reflect
}

[Serializable]
public struct CombatContext
{
    public int attackerId;
    public int targetId;
    public float baseDamage;
    public DamageType damageType;
    public float critChance;
    public float critMultiplier;
    public Vector2 knockback;
    public Vector3 hitPoint;
    public string sourceTag;
    public float timestamp;

    // 可选引用，方便首版快速接入。
    public GameObject attackerObject;
    public GameObject targetObject;

    public static CombatContext Create(
        GameObject attacker,
        GameObject target,
        float damage,
        DamageType type,
        string tag = null)
    {
        return new CombatContext
        {
            attackerId = attacker != null ? attacker.GetInstanceID() : 0,
            targetId = target != null ? target.GetInstanceID() : 0,
            baseDamage = Mathf.Max(0f, damage),
            damageType = type,
            critChance = 0f,
            critMultiplier = 1.5f,
            knockback = Vector2.zero,
            hitPoint = target != null ? target.transform.position : Vector3.zero,
            sourceTag = string.IsNullOrEmpty(tag) ? (attacker != null ? attacker.tag : "Unknown") : tag,
            timestamp = Time.time,
            attackerObject = attacker,
            targetObject = target
        };
    }
}

[Serializable]
public struct HealthSnapshot
{
    public int currentHealth;
    public int maxHealth;
    public bool isInvulnerable;
    public bool isParrying;
}

[Serializable]
public struct DifficultySnapshot
{
    public float runTimeSeconds;
    public float threatLevel;
    public float enemyHpMultiplier;
    public float enemyDamageMultiplier;
    public float spawnWeightMultiplier;
}
