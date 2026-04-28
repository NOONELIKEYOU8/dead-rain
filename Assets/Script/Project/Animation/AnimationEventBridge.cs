using UnityEngine;

/// <summary>
/// 动画事件桥接器（AnimationEventBridge）
/// 挂载到敌人预制体上，作为动画事件（Animation Event）的接收器。
/// 将动画事件转发给 EnemyBase 处理，避免动画系统直接依赖业务逻辑。
///
/// 使用方式：
/// 1. 在 Animator 的动画片段中添加 Animation Event
/// 2. 将 Function Name 设置为本类的方法名（如 "OnHitboxEnable"）
/// 3. 本类会将事件转发给 EnemyBase
/// </summary>
public class AnimationEventBridge : MonoBehaviour
{
    /// <summary>敌人基类引用</summary>
    private EnemyBase _enemy;

    private void Awake()
    {
        _enemy = GetComponent<EnemyBase>();
    }

    /// <summary>
    /// 动画事件：激活 Hitbox
    /// 在攻击动画的活跃帧调用。
    /// </summary>
    public void OnHitboxEnable()
    {
        _enemy?.Hitbox.EnableHitbox();
    }

    /// <summary>
    /// 动画事件：禁用 Hitbox
    /// 在攻击动画的活跃帧结束或后摇开始时调用。
    /// </summary>
    public void OnHitboxDisable()
    {
        _enemy?.Hitbox.DisableHitbox();
    }

    /// <summary>
    /// 动画事件：攻击命中（可选，用于音效、特效等）
    /// </summary>
    public void OnAttackHit()
    {
        // 预留：播放命中音效、粒子特效等
        Debug.Log($"[AnimationEvent] {_enemy?.name} 攻击命中！");
    }

    /// <summary>
    /// 动画事件：脚步声（可选，用于音效）
    /// </summary>
    public void OnFootstep()
    {
        // 预留：播放脚步声
    }

    /// <summary>
    /// 动画事件：处决动画结束
    /// </summary>
    public void OnExecutionEnd()
    {
        // 预留：处决动画结束后的回调
    }

    /// <summary>
    /// 动画事件：死亡动画结束
    /// </summary>
    public void OnDeathEnd()
    {
        // 预留：死亡动画结束后的回调（如销毁敌人）
    }
}
