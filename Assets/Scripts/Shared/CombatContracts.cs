using System;
using UnityEngine;

/// <summary>
/// 战斗系统数据契约定义
/// 包含伤害类型枚举和战斗相关的数据结构
/// </summary>

/// <summary>
/// 伤害类型枚举
/// 定义游戏中不同类型的伤害来源
/// </summary>
public enum DamageType
{
    /// <summary>近战攻击伤害</summary>
    Melee,
    /// <summary>投射物伤害（子弹、飞镖等）</summary>
    Projectile,
    /// <summary>技能伤害（特殊能力）</summary>
    Skill,
    /// <summary>持续伤害（中毒、燃烧等）</summary>
    Dot,
    /// <summary>反弹伤害（盾牌反弹等）</summary>
    Reflect
}

/// <summary>
/// 战斗上下文结构体
/// 包含一次攻击的所有相关信息，用于在战斗系统中传递数据
/// </summary>
[Serializable]
public struct CombatContext
{
    /// <summary>攻击者的实例ID</summary>
    public int attackerId;
    
    /// <summary>目标的实例ID</summary>
    public int targetId;
    
    /// <summary>基础伤害值</summary>
    public float baseDamage;
    
    /// <summary>伤害类型</summary>
    public DamageType damageType;
    
    /// <summary>暴击几率（0-1）</summary>
    public float critChance;
    
    /// <summary>暴击倍率</summary>
    public float critMultiplier;
    
    /// <summary>击退向量</summary>
    public Vector2 knockback;
    
    /// <summary>命中点世界坐标</summary>
    public Vector3 hitPoint;
    
    /// <summary>攻击来源标签</summary>
    public string sourceTag;
    
    /// <summary>攻击时间戳</summary>
    public float timestamp;

    // 可选引用，方便首版快速接入。
    /// <summary>攻击者游戏对象引用（可选）</summary>
    public GameObject attackerObject;
    
    /// <summary>目标游戏对象引用（可选）</summary>
    public GameObject targetObject;

    /// <summary>
    /// 创建战斗上下文的便捷方法
    /// </summary>
    /// <param name="attacker">攻击者游戏对象</param>
    /// <param name="target">目标游戏对象</param>
    /// <param name="damage">基础伤害值</param>
    /// <param name="type">伤害类型</param>
    /// <param name="tag">攻击来源标签（可选）</param>
    /// <returns>配置好的战斗上下文</returns>
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
            baseDamage = Mathf.Max(0f, damage),  // 确保伤害值非负
            damageType = type,
            critChance = 0f,                      // 默认无暴击
            critMultiplier = 1.5f,                // 默认暴击倍率
            knockback = Vector2.zero,             // 默认无击退
            hitPoint = target != null ? target.transform.position : Vector3.zero,
            sourceTag = string.IsNullOrEmpty(tag) ? (attacker != null ? attacker.tag : "Unknown") : tag,
            timestamp = Time.time,                // 记录当前时间
            attackerObject = attacker,
            targetObject = target
        };
    }
}

/// <summary>
/// 生命值快照结构体
/// 记录实体的当前生命状态，用于状态同步和事件传递
/// </summary>
[Serializable]
public struct HealthSnapshot
{
    /// <summary>当前生命值</summary>
    public int currentHealth;
    
    /// <summary>最大生命值</summary>
    public int maxHealth;
    
    /// <summary>是否处于无敌状态</summary>
    public bool isInvulnerable;
    
    /// <summary>是否正在格挡</summary>
    public bool isParrying;
}

/// <summary>
/// 难度快照结构体
/// 记录游戏当前的难度相关参数，用于动态调整游戏难度
/// </summary>
[Serializable]
public struct DifficultySnapshot
{
    /// <summary>游戏运行时间（秒）</summary>
    public float runTimeSeconds;
    
    /// <summary>威胁等级（0-1或更高，表示当前难度强度）</summary>
    public float threatLevel;
    
    /// <summary>敌人生命值倍率</summary>
    public float enemyHpMultiplier;
    
    /// <summary>敌人伤害倍率</summary>
    public float enemyDamageMultiplier;
    
    /// <summary>敌人生成权重倍率</summary>
    public float spawnWeightMultiplier;
}