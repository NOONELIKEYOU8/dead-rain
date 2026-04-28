using UnityEngine;
using System;

/// <summary>
/// 受击判定盒（Hurtbox）
/// 挂载于敌人身体主干骨骼，用于接收伤害判定。
/// 当 Hitbox 进入此判定区域且未被 BlockBox 拦截时，触发扣血逻辑。
/// </summary>
[RequireComponent(typeof(Collider2D))]
public class Hurtbox : MonoBehaviour
{
    /// <summary>
    /// 受击事件委托
    /// 参数为 DamageInfo 结构体，包含伤害、击退等信息。
    /// </summary>
    public event Action<DamageInfo> OnDamageReceived;

    [Header("Hurtbox 配置")]
    [Tooltip("Hurtbox 所属的敌人根对象")]
    public GameObject owner;

    /// <summary>当前是否处于无敌状态</summary>
    private bool _isInvincible = false;

    /// <summary>Hurtbox 对应的 Collider2D</summary>
    private Collider2D _collider;

    private void Awake()
    {
        _collider = GetComponent<Collider2D>();
        _collider.isTrigger = true;
    }

    /// <summary>
    /// 当 Hitbox 碰撞到此 Hurtbox 时由 Hitbox 调用
    /// </summary>
    /// <param name="damageInfo">伤害信息</param>
    public void OnHit(DamageInfo damageInfo)
    {
        // 无敌状态下不受伤
        if (_isInvincible) return;

        // 触发受击事件，由 EnemyBase 等监听并处理
        OnDamageReceived?.Invoke(damageInfo);
    }

    /// <summary>
    /// 设置无敌状态
    /// </summary>
    /// <param name="invincible">是否无敌</param>
    public void SetInvincible(bool invincible)
    {
        _isInvincible = invincible;
    }

    /// <summary>获取当前是否处于无敌状态</summary>
    public bool IsInvincible => _isInvincible;
}
