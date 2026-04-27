using UnityEngine;

/// <summary>
/// 普通小怪（NormalEnemy）
/// 继承 EnemyBase，配置普通小怪特有的攻击数据和行为。
/// 小怪失衡后保持可被处决状态直到超时恢复或被处决。
/// </summary>
public class NormalEnemy : EnemyBase
{
    [Header("小怪攻击配置")]
    [Tooltip("小怪攻击数据（可配置多种攻击）")]
    public AttackDataSO[] attackDatas;

    /// <summary>当前攻击索引</summary>
    private int _currentAttackIndex;

    protected override void Awake()
    {
        base.Awake();
        _currentAttackIndex = 0;
    }

    /// <summary>
    /// 获取当前攻击数据
    /// 按顺序循环使用配置的攻击数据。
    /// </summary>
    /// <returns>当前攻击数据</returns>
    public override AttackDataSO GetCurrentAttackData()
    {
        if (attackDatas == null || attackDatas.Length == 0)
        {
            Debug.LogError($"[{name}] 未配置任何 AttackDataSO！");
            return null;
        }

        AttackDataSO attackData = attackDatas[_currentAttackIndex];
        _currentAttackIndex = (_currentAttackIndex + 1) % attackDatas.Length;
        return attackData;
    }

    /// <summary>
    /// 小怪死亡时直接销毁（或由对象池管理）
    /// </summary>
    public override void Die()
    {
        base.Die();

        // 3秒后销毁（等待死亡动画播放完毕）
        Destroy(gameObject, 3f);
    }
}
