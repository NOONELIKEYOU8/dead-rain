using UnityEngine;
using System;

/// <summary>
/// 敌人基类（EnemyBase）
/// 所有敌人的核心组件，管理生命值、移动、碰撞检测、状态机等。
/// 派生类（NormalEnemy、BossEnemy）可覆盖特定行为。
///
/// 组件依赖：
/// - Rigidbody2D：物理移动
/// - Animator：动画控制
/// - EnemyStateMachine：状态管理
/// - EnemyAIController：AI 决策
/// - Hitbox / Hurtbox / BlockBox：碰撞判定
/// - StanceBar：架势条管理
/// - ItemSlot：道具槽
/// </summary>
[RequireComponent(typeof(Rigidbody2D))]
// [ANIM_DISABLED] [RequireComponent(typeof(Animator))]
[RequireComponent(typeof(EnemyStateMachine))]
[RequireComponent(typeof(EnemyAIController))]
[RequireComponent(typeof(StanceBar))]
public class EnemyBase : MonoBehaviour, IExecutable
{
    [Header("基础配置")]
    [Tooltip("敌人数据配置（ScriptableObject）")]
    public EnemyDataSO data;

    [Tooltip("架势条数据配置（ScriptableObject）")]
    public StanceBarDataSO stanceData;

    [Header("组件引用（自动获取）")]
    [Tooltip("Hitbox 组件（挂载在武器/手部骨骼子对象上）")]
    public Hitbox hitbox;

    [Tooltip("Hurtbox 组件（挂载在身体主干子对象上）")]
    public Hurtbox hurtbox;

    [Tooltip("BlockBox 组件（挂载在盾牌/格挡骨骼子对象上）")]
    public BlockBox blockBox;

    [Tooltip("ItemSlot 组件")]
    public ItemSlot itemSlot;

    [Header("巡逻配置（实例级，每个敌人独立配置）")]
    [Tooltip("巡逻路径点数组（世界坐标），在 Inspector 中为每个敌人实例单独配置")]
    public Vector2[] patrolPoints = new Vector2[0];

    [Header("检测配置")]
    [Tooltip("玩家 Tag（用于检测玩家）")]
    public string playerTag = "Player";

    [Tooltip("地面 LayerMask（用于地面检测）")]
    public LayerMask groundLayer;

    // ==================== 组件缓存 ====================

    protected Rigidbody2D _rb;
    protected Animator _animator;
    protected EnemyStateMachine _stateMachine;
    protected EnemyAIController _aiController;
    protected StanceBar _stanceBar;

    // ==================== 运行时状态 ====================

    /// <summary>当前生命值</summary>
    protected float _currentHealth;

    /// <summary>玩家引用缓存</summary>
    private GameObject _player;

    /// <summary>是否已死亡</summary>
    private bool _isDead;

    // ==================== 属性访问器 ====================

    /// <summary>敌人数据配置</summary>
    public EnemyDataSO Data => data;

    /// <summary>架势条数据配置</summary>
    public StanceBarDataSO StanceData => stanceData;

    /// <summary>Rigidbody2D 引用</summary>
    public Rigidbody2D Rigidbody => _rb;

    /// <summary>Animator 引用</summary>
    public Animator Animator => _animator;

    /// <summary>状态机引用</summary>
    public EnemyStateMachine StateMachine => _stateMachine;

    /// <summary>AI 控制器引用</summary>
    public EnemyAIController AIController => _aiController;

    /// <summary>Hitbox 引用</summary>
    public Hitbox Hitbox => hitbox;

    /// <summary>Hurtbox 引用</summary>
    public Hurtbox Hurtbox => hurtbox;

    /// <summary>BlockBox 引用</summary>
    public BlockBox BlockBox => blockBox;

    /// <summary>ItemSlot 引用</summary>
    public ItemSlot ItemSlot => itemSlot;

    /// <summary>巡逻路径点数组（实例级配置）</summary>
    public Vector2[] PatrolPoints => patrolPoints;

    /// <summary>架势条引用</summary>
    public StanceBar StanceBar => _stanceBar;

    /// <summary>敌人世界坐标位置</summary>
    public Vector2 Position => transform.position;

    /// <summary>是否已死亡</summary>
    public bool IsDead => _isDead;

    // ==================== 生命周期 ====================

    protected virtual void Awake()
    {
        // 获取组件引用
        _rb = GetComponent<Rigidbody2D>();
        // [ANIM_DISABLED] _animator = GetComponent<Animator>();
        _stateMachine = GetComponent<EnemyStateMachine>();
        _aiController = GetComponent<EnemyAIController>();
        _stanceBar = GetComponent<StanceBar>();

        // 配置架势条数据
        if (stanceData != null)
        {
            _stanceBar.SetStanceData(stanceData);
        }

        // 初始化生命值
        _currentHealth = data != null ? data.maxHealth : 100f;

        // 查找玩家
        _player = GameObject.FindGameObjectWithTag(playerTag);

        // 注册 Hurtbox 受击事件
        if (hurtbox != null)
        {
            hurtbox.owner = gameObject;
            hurtbox.OnDamageReceived += OnDamageReceived;
        }

        // 注册 BlockBox 格挡事件
        if (blockBox != null)
        {
            blockBox.owner = gameObject;
            blockBox.OnBlockedEvent += OnBlocked;
        }

        // 注册架势条失衡事件
        _stanceBar.OnStanceBroken += OnStanceBroken;

        // 配置 Hitbox
        if (hitbox != null)
        {
            hitbox.owner = gameObject;
        }
    }

    // ==================== 实时状态监控 ====================

    /// <summary>状态打印间隔计时器</summary>
    private float _statePrintTimer;

    /// <summary>状态打印间隔（秒）</summary>
    private const float STATE_PRINT_INTERVAL = 0.5f;

    protected virtual void Update()
    {
        if (_isDead) return;

        // 定时打印敌人实时状态
        _statePrintTimer += Time.deltaTime;
        if (_statePrintTimer >= STATE_PRINT_INTERVAL)
        {
            _statePrintTimer = 0f;
            string stateName = _stateMachine != null ? _stateMachine.CurrentState.ToString() : "Unknown";
            float stancePercent = _stanceBar != null ? _stanceBar.NormalizedValue * 100f : 0f;
            float distToPlayer = DistanceToPlayer;
            bool canExec = CanBeExecuted();
            Debug.Log($"[STATE] [{name}] 状态:{stateName} | HP:{_currentHealth:F0}/{(data != null ? data.maxHealth : 0):F0} | 架势:{stancePercent:F0}% | 玩家距离:{distToPlayer:F1} | 可处决:{canExec}");
        }
    }

    // ==================== 玩家检测 ====================

    /// <summary>
    /// 检测玩家是否在视野范围内
    /// </summary>
    /// <returns>true 表示玩家在视野内</returns>
    public virtual bool DetectPlayer()
    {
        if (_player == null)
        {
            _player = GameObject.FindGameObjectWithTag(playerTag);
            if (_player == null) return false;
        }

        //Debug.Log($"[LOG] [{name}] 检测玩家，距离: {Vector2.Distance(transform.position, _player.transform.position):F1}, 检测范围: {data.detectionRange}");
        float distance = Vector2.Distance(transform.position, _player.transform.position);
        return distance <= data.detectionRange;
    }

    /// <summary>
    /// 检查玩家是否存活
    /// </summary>
    public virtual bool IsPlayerAlive()
    {
        if (_player == null) return false;
        // 假设玩家死亡时会被标记为 inactive 或有特定组件
        return _player.activeInHierarchy;
    }

    /// <summary>获取与玩家的距离</summary>
    public float DistanceToPlayer
    {
        get
        {
            if (_player == null) return float.MaxValue;
            return Vector2.Distance(transform.position, _player.transform.position);
        }
    }

    /// <summary>获取朝向玩家的方向（-1 左，1 右）</summary>
    public float DirectionToPlayer
    {
        get
        {
            if (_player == null) return 0f;
            return _player.transform.position.x > transform.position.x ? 1f : -1f;
        }
    }

    // ==================== 移动控制 ====================

    /// <summary>
    /// 设置水平速度
    /// </summary>
    /// <param name="velocityX">水平速度值</param>
    public void SetVelocityX(float velocityX)
    {
        if (_rb != null)
        {
            _rb.velocity = new Vector2(velocityX, _rb.velocity.y);
        }
    }

    /// <summary>
    /// 翻转敌人朝向
    /// </summary>
    /// <param name="direction">方向（-1 左，1 右）</param>
    public void FlipDirection(float direction)
    {
        if (direction == 0) return;

        Vector3 scale = transform.localScale;
        scale.x = Mathf.Abs(scale.x) * Mathf.Sign(direction);
        transform.localScale = scale;
    }

    /// <summary>
    /// 朝目标点移动（通用方法，供各状态类调用）
    /// </summary>
    /// <param name="target">目标位置</param>
    /// <param name="speed">移动速度</param>
    public void MoveToward(Vector2 target, float speed)
    {
        float direction = target.x > transform.position.x ? 1f : -1f;
        SetVelocityX(direction * speed);
        FlipDirection(direction);
    }

    /// <summary>
    /// 朝玩家方向移动（便捷方法）
    /// </summary>
    /// <param name="speed">移动速度</param>
    public void MoveTowardPlayer(float speed)
    {
        float direction = DirectionToPlayer;
        SetVelocityX(direction * speed);
        FlipDirection(direction);
    }

    // ==================== 战斗系统 ====================

    /// <summary>
    /// 受击处理（由 Hurtbox 事件触发）
    /// </summary>
    /// <param name="damageInfo">伤害信息</param>
    protected virtual void OnDamageReceived(DamageInfo damageInfo)
    {
        if (_isDead) return;

        Debug.Log($"[LOG] [{name}] 受击！伤害: {damageInfo.damage}, 击退力: {damageInfo.knockbackForce}");

        // 扣血
        TakeDamage(damageInfo.damage);

        // 增加架势条
        _stanceBar.AddHitStance();

        // 击退效果
        ApplyKnockback(damageInfo.knockbackDirection, damageInfo.knockbackForce);

        // 无敌帧
        StartCoroutine(InvincibleCoroutine(data.invincibleDuration));
    }

    /// <summary>
    /// 格挡处理（由 BlockBox 事件触发）
    /// </summary>
    /// <param name="damageInfo">被格挡的攻击信息</param>
    protected virtual void OnBlocked(DamageInfo damageInfo)
    {
        if (_isDead) return;

        Debug.Log($"[LOG] [{name}] 格挡触发！架势条: {_stanceBar.NormalizedValue:P0} -> 增加 {stanceData.blockedAttackGain}");

        // 格挡被击时架势条大幅增加
        _stanceBar.AddBlockedAttackStance();

        Debug.Log($"[{name}] 格挡成功！架势条: {_stanceBar.NormalizedValue:P0}");
    }

    /// <summary>
    /// 架势条失衡处理（由 StanceBar 事件触发）
    /// </summary>
    protected virtual void OnStanceBroken()
    {
        if (_isDead) return;

        Debug.Log($"[LOG] [{name}] 架势条失衡！强制进入 Staggered 状态");

        // 强制打断当前动作，进入失衡状态
        _stateMachine.ForceChangeState(EnemyState.Staggered);
    }

    /// <summary>
    /// 扣除生命值
    /// </summary>
    /// <param name="amount">伤害值</param>
    protected virtual void TakeDamage(float amount)
    {
        _currentHealth = Mathf.Max(0f, _currentHealth - amount);
        Debug.Log($"[{name}] 受到 {amount} 点伤害，剩余生命值: {_currentHealth}/{data.maxHealth}");

        // 播放受击动画
        // [ANIM_DISABLED] _animator.SetTrigger("Hit");

        // 检查死亡
        if (_currentHealth <= 0f)
        {
            Die();
        }
    }

    /// <summary>
    /// 击退效果
    /// </summary>
    /// <param name="direction">击退方向</param>
    /// <param name="force">击退力度</param>
    protected virtual void ApplyKnockback(Vector2 direction, float force)
    {
        if (_rb != null)
        {
            _rb.velocity = direction * force;
        }
    }

    /// <summary>
    /// 无敌帧协程
    /// </summary>
    /// <param name="duration">无敌持续时间</param>
    private System.Collections.IEnumerator InvincibleCoroutine(float duration)
    {
        hurtbox.SetInvincible(true);
        yield return new WaitForSeconds(duration);
        hurtbox.SetInvincible(false);
    }

    // ==================== 攻击系统 ====================

    /// <summary>
    /// 获取当前攻击数据
    /// 派生类可覆盖以支持多段攻击或随机攻击。
    /// </summary>
    /// <returns>当前使用的攻击数据</returns>
    public virtual AttackDataSO GetCurrentAttackData()
    {
        // 基类返回 null，派生类需配置具体攻击数据
        Debug.LogWarning($"[{name}] 基类 EnemyBase 未配置攻击数据，请使用 NormalEnemy 或 BossEnemy。");
        return null;
    }

    /// <summary>
    /// 重置攻击冷却（委托给 AIController 统一管理）
    /// </summary>
    public void ResetAttackCooldown()
    {
        if (_aiController != null)
        {
            _aiController.ResetCooldown();
        }
    }

    // ==================== 死亡 ====================

    /// <summary>
    /// 敌人死亡
    /// </summary>
    public virtual void Die()
    {
        if (_isDead) return;
        _isDead = true;

        Debug.Log($"[LOG] [{name}] 死亡！生命值归零。");

        _stateMachine.ForceChangeState(EnemyState.Dead);
        Debug.Log($"[{name}] 已死亡。");
    }

    // ==================== IExecutable 实现 ====================

    /// <summary>
    /// 检查是否可被处决（处于失衡状态且未死亡）
    /// </summary>
    public virtual bool CanBeExecuted()
    {
        return !_isDead && _stateMachine.IsInStaggeredState();
    }

    /// <summary>
    /// 执行处决（由 ExecutionSystem 调用）
    /// </summary>
    public virtual void Execute()
    {
        if (!CanBeExecuted()) return;
        Debug.Log($"[LOG] [{name}] 被处决！进入 Executed 状态");
        _stateMachine.ForceChangeState(EnemyState.Executed);
    }

    // ==================== 调试 ====================

    private void OnDrawGizmosSelected()
    {
        if (data == null) return;

        // 绘制检测范围
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, data.detectionRange);

        // 绘制攻击范围
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, data.attackRange);

        // 绘制处决范围
        Gizmos.color = Color.magenta;
        Gizmos.DrawWireSphere(transform.position, data.executionRange);

        // 绘制巡逻路径
        if (patrolPoints != null && patrolPoints.Length > 0)
        {
            Gizmos.color = Color.green;
            for (int i = 0; i < patrolPoints.Length; i++)
            {
                Gizmos.DrawWireSphere(patrolPoints[i], 0.3f);
                if (i > 0)
                {
                    Gizmos.DrawLine(patrolPoints[i - 1], patrolPoints[i]);
                }
            }
            // 连接最后一个点到第一个点
            if (patrolPoints.Length > 1)
            {
                Gizmos.DrawLine(patrolPoints[patrolPoints.Length - 1], patrolPoints[0]);
            }
        }
    }
}
