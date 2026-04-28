using UnityEngine;

/// <summary>
/// Boss 敌人（BossEnemy）
/// 继承 EnemyBase，配置 Boss 特有的多阶段攻击数据和行为。
/// Boss 可在失衡后自动恢复（由 StanceBarDataSO 配置）。
/// Boss 拥有更高的生命值和更多样的攻击模式。
/// </summary>
public class BossEnemy : EnemyBase
{
    [Header("Boss 攻击配置")]
    [Tooltip("Boss 第一阶段攻击数据")]
    public AttackDataSO[] phaseOneAttacks;

    [Tooltip("Boss 第二阶段攻击数据（生命值低于阈值时启用）")]
    public AttackDataSO[] phaseTwoAttacks;

    [Header("Boss 阶段切换")]
    [Tooltip("第二阶段触发阈值（生命值比例 0~1）")]
    [Range(0.1f, 0.9f)]
    public float phaseTwoThreshold = 0.4f;

    /// <summary>当前是否处于第二阶段</summary>
    private bool _isPhaseTwo;

    /// <summary>当前攻击索引</summary>
    private int _currentAttackIndex;

    /// <summary>当前是否处于第二阶段</summary>
    public bool IsPhaseTwo => _isPhaseTwo;

    protected override void Awake()
    {
        base.Awake();
        _isPhaseTwo = false;
        _currentAttackIndex = 0;
    }

    protected override void Update()
    {
        base.Update();

        // 检查阶段切换
        if (!_isPhaseTwo && data != null)
        {
            float healthRatio = _currentHealth / data.maxHealth;
            if (healthRatio <= phaseTwoThreshold)
            {
                EnterPhaseTwo();
            }
        }
    }

    /// <summary>
    /// 获取当前攻击数据
    /// 根据当前阶段从对应的攻击数据数组中选取。
    /// </summary>
    /// <returns>当前攻击数据</returns>
    public override AttackDataSO GetCurrentAttackData()
    {
        AttackDataSO[] currentAttacks = _isPhaseTwo ? phaseTwoAttacks : phaseOneAttacks;

        if (currentAttacks == null || currentAttacks.Length == 0)
        {
            Debug.LogError($"[{name}] 当前阶段未配置任何 AttackDataSO！");
            return null;
        }

        AttackDataSO attackData = currentAttacks[_currentAttackIndex];
        _currentAttackIndex = (_currentAttackIndex + 1) % currentAttacks.Length;
        return attackData;
    }

    /// <summary>
    /// 进入第二阶段
    /// </summary>
    private void EnterPhaseTwo()
    {
        _isPhaseTwo = true;
        _currentAttackIndex = 0;

        // 播放阶段切换动画
        // [ANIM_DISABLED] _animator.SetTrigger("PhaseTwo");

        // 重置架势条
        _stanceBar.ResetStance();

        // 增加攻击速度（减少冷却时间）
        // 可通过修改 data.attackCooldown 实现

        Debug.Log($"[{name}] 进入第二阶段！");
    }

    /// <summary>
    /// Boss 死亡时不自动销毁（可能有特殊死亡动画/掉落物逻辑）
    /// </summary>
    public override void Die()
    {
        base.Die();

        // Boss 死亡后不自动销毁，由关卡管理器控制
        Debug.Log($"[{name}] Boss 已被击败！");
    }

    /// <summary>
    /// Boss 的可被处决判断
    /// Boss 在第二阶段不可被处决（或根据设计调整）
    /// </summary>
    public override bool CanBeExecuted()
    {
        // Boss 在第二阶段不可被处决
        if (_isPhaseTwo) return false;
        return base.CanBeExecuted();
    }
}
