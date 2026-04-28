using UnityEngine;

/// <summary>
/// 统一伤害解析器（HitResolver）
/// 封装"新系统优先，旧系统兼容"的伤害判定逻辑。
/// 所有攻击源（近战、投射物、Hitbox 等）统一调用此解析器，
/// 避免在每个攻击源中重复编写双系统分支代码。
/// 
/// 注意：此类不直接引用 EnemyBase，避免跨程序集依赖。
/// 格挡→架势条的逻辑通过字符串查找组件实现。
/// </summary>
public static class HitResolver
{
    /// <summary>
    /// 尝试对碰撞目标造成伤害。
    /// 优先检测 Hurtbox（新敌人系统），其次兼容 Damageable（旧系统）。
    /// </summary>
    /// <param name="hitCollider">碰撞到的 Collider2D</param>
    /// <param name="damageInfo">伤害信息（用于新系统）</param>
    /// <param name="owner">攻击来源对象</param>
    /// <param name="selfDamageable">攻击者自身的 Damageable（用于排除自伤），可为 null</param>
    /// <returns>是否成功命中了某个目标</returns>
    public static bool TryDealDamage(Collider2D hitCollider, DamageInfo damageInfo, GameObject owner, Damageable selfDamageable = null)
    {
        if (hitCollider == null) return false;

        // 跳过自身所属对象
        if (owner != null && hitCollider.gameObject == owner) return false;

        // 优先检测 Hurtbox（新敌人系统）
        var hurtbox = hitCollider.GetComponent<Hurtbox>();
        if (hurtbox != null && hurtbox.owner != null && hurtbox.owner != owner)
        {
            hurtbox.OnHit(damageInfo);
            Debug.Log($"[HitResolver] 命中 {hurtbox.owner.name} (Hurtbox)，伤害: {damageInfo.damage}");
            return true;
        }

        // 兼容旧系统：检测 Damageable 组件
        var damageable = hitCollider.GetComponentInParent<Damageable>();
        if (damageable != null && damageable != selfDamageable)
        {
            if (owner != null && damageable.gameObject == owner) return false;

            // 检测玩家是否正在格挡（Parry → 敌人架势条增加）
            // 通过字符串查找 StanceBar 组件，避免直接引用 EnemyBase 导致跨程序集依赖
            if (damageable.isParrying && owner != null)
            {
                TryAddBlockedAttackStance(owner);
            }

            damageable.TakeDamage(Mathf.RoundToInt(damageInfo.damage), owner);
            Debug.Log($"[HitResolver] 命中 {damageable.gameObject.name} (Damageable)，伤害: {damageInfo.damage}");
            return true;
        }

        return false;
    }

    /// <summary>
    /// 尝试对碰撞目标造成伤害（简化版，用于投射物等场景）。
    /// </summary>
    /// <param name="hitCollider">碰撞到的 Collider2D</param>
    /// <param name="damage">伤害值</param>
    /// <param name="knockbackForce">击退力度</param>
    /// <param name="owner">攻击来源对象</param>
    /// <param name="selfDamageable">攻击者自身的 Damageable，可为 null</param>
    /// <returns>是否成功命中了某个目标</returns>
    public static bool TryDealDamage(Collider2D hitCollider, float damage, float knockbackForce, GameObject owner, Damageable selfDamageable = null)
    {
        Vector2 knockbackDir = hitCollider != null
            ? (hitCollider.transform.position - (owner != null ? owner.transform.position : Vector3.zero)).normalized
            : Vector2.zero;

        DamageInfo dmgInfo = new DamageInfo(damage, knockbackDir, knockbackForce, owner, true);
        return TryDealDamage(hitCollider, dmgInfo, owner, selfDamageable);
    }

    /// <summary>
    /// 尝试在攻击者身上找到 StanceBar 组件并增加格挡架势。
    /// 使用 GetComponent 而非直接类型引用，避免跨程序集依赖。
    /// </summary>
    private static void TryAddBlockedAttackStance(GameObject attacker)
    {
        if (attacker == null) return;

        // 通过类型名称查找 StanceBar 组件（避免硬引用 EnemyBase）
        var stanceBar = attacker.GetComponentInChildren<StanceBar>();
        if (stanceBar != null)
        {
            stanceBar.AddBlockedAttackStance();
            Debug.Log($"[HitResolver] 攻击被格挡！攻击者 {attacker.name} 架势条大幅增加 -> {stanceBar.NormalizedValue:P0}");
        }
    }
}
