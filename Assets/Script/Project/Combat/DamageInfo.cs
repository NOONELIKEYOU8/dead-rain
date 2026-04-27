using UnityEngine;

/// <summary>
/// 伤害信息数据结构
/// 用于在 Hitbox 和 Hurtbox 之间传递伤害、击退等信息。
/// </summary>
public struct DamageInfo
{
    /// <summary>伤害值</summary>
    public float damage;

    /// <summary>击退方向（世界坐标）</summary>
    public Vector2 knockbackDirection;

    /// <summary>击退力度</summary>
    public float knockbackForce;

    /// <summary>攻击来源（GameObject）</summary>
    public GameObject source;

    /// <summary>该攻击是否可被格挡</summary>
    public bool canBeBlocked;

    /// <summary>
    /// 构造伤害信息
    /// </summary>
    /// <param name="damage">伤害值</param>
    /// <param name="knockbackDir">击退方向</param>
    /// <param name="knockbackForce">击退力度</param>
    /// <param name="source">攻击来源</param>
    /// <param name="canBeBlocked">是否可被格挡</param>
    public DamageInfo(float damage, Vector2 knockbackDir, float knockbackForce, GameObject source, bool canBeBlocked = true)
    {
        this.damage = damage;
        this.knockbackDirection = knockbackDir;
        this.knockbackForce = knockbackForce;
        this.source = source;
        this.canBeBlocked = canBeBlocked;
    }
}
