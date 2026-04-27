using UnityEngine;
using System;

/// <summary>
/// 格挡判定盒（BlockBox）
/// 挂载于盾牌或特定格挡骨骼，用于检测玩家攻击。
/// 当 Player.Hitbox 进入此判定区域且敌人处于格挡状态时，触发格挡逻辑。
/// </summary>
[RequireComponent(typeof(Collider2D))]
public class BlockBox : MonoBehaviour
{
    /// <summary>
    /// 格挡成功事件委托
    /// 参数为 DamageInfo，包含被格挡攻击的信息。
    /// </summary>
    public event Action<DamageInfo> OnBlockedEvent;

    [Header("BlockBox 配置")]
    [Tooltip("BlockBox 所属的敌人根对象")]
    public GameObject owner;

    /// <summary>当前是否处于格挡激活状态（由 BlockState 控制）</summary>
    private bool _isBlocking = false;

    /// <summary>BlockBox 对应的 Collider2D</summary>
    private Collider2D _collider;

    private void Awake()
    {
        _collider = GetComponent<Collider2D>();
        _collider.isTrigger = true;
        // 初始状态为禁用，仅在格挡时激活
        _collider.enabled = false;
    }

    /// <summary>
    /// 激活格挡判定
    /// 由 BlockState 的 Enter 方法调用。
    /// </summary>
    public void EnableBlock()
    {
        _isBlocking = true;
        _collider.enabled = true;
    }

    /// <summary>
    /// 禁用格挡判定
    /// 由 BlockState 的 Exit 方法调用。
    /// </summary>
    public void DisableBlock()
    {
        _isBlocking = false;
        _collider.enabled = false;
    }

    /// <summary>
    /// 当 Hitbox 碰撞到此 BlockBox 时由 Hitbox 调用
    /// 仅在格挡激活状态下处理格挡逻辑。
    /// </summary>
    /// <param name="damageInfo">被格挡的攻击信息</param>
    public void OnBlocked(DamageInfo damageInfo)
    {
        if (!_isBlocking) return;

        // 触发格挡事件，由 EnemyBase 监听并增加架势条
        OnBlockedEvent?.Invoke(damageInfo);
    }

    /// <summary>获取当前是否处于格挡状态</summary>
    public bool IsBlocking => _isBlocking;
}
