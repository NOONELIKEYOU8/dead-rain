using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// 攻击判定盒（Hitbox）
/// 挂载于武器或手部骨骼节点，仅在攻击动画特定帧激活。
/// 激活时检测与 Hurtbox / BlockBox 的碰撞，并传递 DamageInfo。
/// </summary>
[RequireComponent(typeof(Collider2D))]
public class Hitbox : MonoBehaviour
{
    [Header("Hitbox 配置")]
    [Tooltip("Hitbox 所属对象（用于标识攻击来源）")]
    public GameObject owner;

    [Tooltip("默认伤害值（可被 AttackDataSO 覆盖）")]
    public float baseDamage = 10f;

    [Tooltip("默认击退力度")]
    public float baseKnockbackForce = 8f;

    [Tooltip("该攻击是否可被格挡")]
    public bool canBeBlocked = true;

    /// <summary>当前伤害值（运行时由 AttackDataSO 设置）</summary>
    private float _currentDamage;

    /// <summary>当前击退力度（运行时由 AttackDataSO 设置）</summary>
    private float _currentKnockbackForce;

    /// <summary>当前帧已命中的目标列表（防止同一攻击多次命中同一目标）</summary>
    private readonly HashSet<Collider2D> _hitTargets = new HashSet<Collider2D>();

    /// <summary>Hitbox 对应的 Collider2D</summary>
    private Collider2D _collider;

    private void Awake()
    {
        _collider = GetComponent<Collider2D>();
        _collider.isTrigger = true;
        // 初始状态为禁用，由动画事件或代码激活
        _collider.enabled = false;
    }

    /// <summary>
    /// 激活 Hitbox，开始进行攻击判定
    /// 通常由动画事件（Animation Event）调用。
    /// </summary>
    /// <param name="damage">本次攻击伤害值</param>
    /// <param name="knockbackForce">本次攻击击退力度</param>
    public void EnableHitbox(float damage, float knockbackForce)
    {
        _currentDamage = damage > 0 ? damage : baseDamage;
        _currentKnockbackForce = knockbackForce > 0 ? knockbackForce : baseKnockbackForce;
        _hitTargets.Clear();
        _collider.enabled = true;
    }

    /// <summary>
    /// 激活 Hitbox（使用默认数值）
    /// </summary>
    public void EnableHitbox()
    {
        EnableHitbox(baseDamage, baseKnockbackForce);
    }

    /// <summary>
    /// 禁用 Hitbox，停止攻击判定
    /// 通常由动画事件（Animation Event）调用。
    /// </summary>
    public void DisableHitbox()
    {
        _collider.enabled = false;
        _hitTargets.Clear();
    }

    /// <summary>
    /// 设置 Hitbox 的伤害和击退参数（由攻击状态在发动攻击前调用）
    /// </summary>
    public void SetDamageParams(float damage, float knockbackForce, bool blockable)
    {
        _currentDamage = damage;
        _currentKnockbackForce = knockbackForce;
        canBeBlocked = blockable;
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        // 防止同一帧重复命中同一目标
        if (_hitTargets.Contains(other)) return;
        _hitTargets.Add(other);

        // 跳过自身所属对象
        if (owner != null && other.gameObject == owner) return;

        // 优先检测 BlockBox（格挡判定）
        BlockBox blockBox = other.GetComponent<BlockBox>();
        if (blockBox != null)
        {
            Vector2 knockbackDir = (other.transform.position - transform.position).normalized;
            DamageInfo dmgInfo = new DamageInfo(_currentDamage, knockbackDir, _currentKnockbackForce, owner, canBeBlocked);
            blockBox.OnBlocked(dmgInfo);
            Debug.Log($"[LOG] [Hitbox] 攻击被 {other.gameObject.name} 的 BlockBox 格挡");
            return;
        }

        // 使用统一伤害解析器处理 Hurtbox / Damageable 双系统
        Vector2 knockbackDir2 = (other.transform.position - transform.position).normalized;
        DamageInfo hitDmgInfo = new DamageInfo(_currentDamage, knockbackDir2, _currentKnockbackForce, owner, canBeBlocked);
        HitResolver.TryDealDamage(other, hitDmgInfo, owner);
    }

    /// <summary>获取当前 Hitbox 是否处于激活状态</summary>
    public bool IsActive => _collider != null && _collider.enabled;
}
