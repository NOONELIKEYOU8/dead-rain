using System.Collections;
using UnityEngine;


/// <summary>
/// 玩家战斗模块
/// 负责管理玩家的近战和远程攻击系统
/// 包含连击系统、攻击冷却、投射物生成等功能
/// </summary>
[RequireComponent(typeof(PlayerController))]
public class PlayerCombatModule : MonoBehaviour
{
    [Header("近战攻击配置")]
    [Tooltip("近战攻击检测点，用于确定攻击位置")]
    public Transform meleePoint;
    
    [Tooltip("近战攻击检测范围半径")]
    public float meleeRange = 0.8f;
    
    [Tooltip("近战攻击可命中的层级")]
    public LayerMask meleeTargetLayers = ~0;
    
    [Tooltip("连击伤害序列，按顺序循环使用")]
    public int[] comboDamages = { 1, 1, 2 };
    
    [Tooltip("连击重置时间，超过此时间未攻击则重置连击")]
    public float comboResetTime = 0.8f;
    
    [Tooltip("近战攻击锁定时间，防止连续攻击")]
    public float meleeLockTime = 0.1f;

    [Header("远程攻击配置")]
    [Tooltip("是否启用远程攻击功能")]
    public bool enableRanged = true;
    
    [Tooltip("投射物预制体，如果为空则运行时创建")]
    public SimpleProjectile projectilePrefab;
    
    [Tooltip("投射物生成位置相对于玩家的偏移")]
    public Vector3 projectileSpawnOffset = new Vector3(0.6f, 0.1f, 0f);
    
    [Tooltip("投射物飞行速度")]
    public float projectileSpeed = 8f;
    
    [Tooltip("投射物伤害值")]
    public int projectileDamage = 1;
    
    [Tooltip("投射物攻击冷却时间")]
    public float projectileCooldown = 0.75f;

    // 组件引用
    private PlayerController controller;     // 玩家控制器
    private Damageable selfDamageable;       // 玩家自身的Damageable组件

    // 近战状态
    private int comboIndex;                  // 当前连击索引
    private float comboExpiresAt;            // 连击过期时间
    private bool inMeleeLock;                // 是否处于近战锁定状态

    // 远程状态
    private float nextProjectileTime;        // 下次可发射投射物的时间

    /// <summary>
    /// 初始化组件引用，如果meleePoint为空则自动创建
    /// </summary>
    private void Awake()
    {
        // 获取必要的组件引用
        controller = GetComponent<PlayerController>();
        selfDamageable = GetComponent<Damageable>();

        // 如果近战攻击点未设置，自动创建一个
        if (meleePoint == null)
        {
            var go = new GameObject("MeleePoint");
            go.transform.SetParent(transform);
            go.transform.localPosition = new Vector3(0.6f, 0f, 0f);
            meleePoint = go.transform;
        }
    }

    /// <summary>
    /// 组件启用时注册输入事件
    /// </summary>
    private void OnEnable()
    {
        if (controller == null) return;
        // 注册主攻击输入事件
        controller.OnPrimaryAttackInput += HandlePrimaryAttack;
        // 注册技能1输入事件（远程攻击）
        controller.OnSkill1Input += HandleRangedAttack;
    }

    /// <summary>
    /// 组件禁用时取消注册输入事件
    /// </summary>
    private void OnDisable()
    {
        if (controller == null) return;
        // 取消注册事件，防止内存泄漏
        controller.OnPrimaryAttackInput -= HandlePrimaryAttack;
        controller.OnSkill1Input -= HandleRangedAttack;
    }

    /// <summary>
    /// 处理主攻击输入
    /// </summary>
    private void HandlePrimaryAttack()
    {
        // 如果处于近战锁定状态，忽略此次攻击
        if (inMeleeLock) return;
        
        // 启动近战攻击协程
        StartCoroutine(MeleeRoutine());
    }

    /// <summary>
    /// 近战攻击协程，处理连击逻辑和攻击锁定
    /// </summary>
    private IEnumerator MeleeRoutine()
    {
        // 进入近战锁定状态，防止连续攻击
        inMeleeLock = true;

        // 连击逻辑：如果超过重置时间，重置连击计数
        if (Time.time > comboExpiresAt) comboIndex = 0;
        
        // 获取当前连击的伤害值
        int idx = Mathf.Clamp(comboIndex, 0, comboDamages.Length - 1);
        int damage = comboDamages[idx];
        
        // 更新连击索引和过期时间
        comboIndex = (comboIndex + 1) % comboDamages.Length;
        comboExpiresAt = Time.time + comboResetTime;

        // 执行近战攻击
        PerformMeleeHit(damage);

        // 等待攻击锁定时间结束
        yield return new WaitForSeconds(meleeLockTime);
        
        // 解除近战锁定状态
        inMeleeLock = false;
    }

    /// <summary>
    /// 执行近战攻击检测和伤害计算
    /// 支持两套伤害系统：旧 Damageable 和新 Hurtbox
    /// </summary>
    /// <param name="damage">攻击伤害值</param>
    private void PerformMeleeHit(int damage)
    {
        if (meleePoint == null) return;

        // 在攻击点周围检测可命中的碰撞体
        Collider2D[] hits = Physics2D.OverlapCircleAll(meleePoint.position, meleeRange, meleeTargetLayers);

        // 创建战斗上下文并触发攻击开始事件
        var openCtx = CombatContext.Create(gameObject, null, damage, DamageType.Melee, "PlayerMelee");
        BattleEvents.RaiseAttackStarted(openCtx);

        // 遍历所有命中的碰撞体
        for (int i = 0; i < hits.Length; i++)
        {
            if (hits[i] == null) continue;

            // 优先检测 BlockBox（格挡判定）
            var blockBox = hits[i].GetComponent<BlockBox>();
            if (blockBox != null && blockBox.IsBlocking)
            {
                Vector2 knockbackDir = (hits[i].transform.position - meleePoint.position).normalized;
                DamageInfo blockInfo = new DamageInfo(damage, knockbackDir, 5f, gameObject, true);
                blockBox.OnBlocked(blockInfo);
                Debug.Log($"[LOG] [PlayerCombat] 攻击被格挡！");
                continue;
            }

            // 使用统一伤害解析器处理 Hurtbox / Damageable 双系统
            Vector2 knockbackDir2 = (hits[i].transform.position - meleePoint.position).normalized;
            DamageInfo dmgInfo = new DamageInfo(damage, knockbackDir2, 5f, gameObject, true);
            HitResolver.TryDealDamage(hits[i], dmgInfo, gameObject, selfDamageable);
        }
    }

    /// <summary>
    /// 处理远程攻击输入
    /// </summary>
    private void HandleRangedAttack()
    {
        // 检查远程攻击是否启用
        if (!enableRanged) return;
        
        // 检查冷却时间
        if (Time.time < nextProjectileTime) return;

        // 更新下次可攻击时间并生成投射物
        nextProjectileTime = Time.time + projectileCooldown;
        SpawnProjectile();
    }

    /// <summary>
    /// 生成并初始化投射物
    /// </summary>
    private void SpawnProjectile()
    {
        // 根据玩家朝向确定投射物方向
        float dir = transform.localScale.x >= 0f ? 1f : -1f;
        
        // 计算投射物生成位置（考虑玩家朝向和偏移）
        Vector3 spawnPos = transform.position + new Vector3(projectileSpawnOffset.x * dir, projectileSpawnOffset.y, 0f);

        SimpleProjectile projectile;
        
        // 如果有预制体则实例化，否则运行时创建
        if (projectilePrefab != null)
        {
            projectile = Instantiate(projectilePrefab, spawnPos, Quaternion.identity);
        }
        else
        {
            projectile = CreateRuntimeProjectile(spawnPos);
        }

        if (projectile == null) return;

        // 初始化投射物参数
        projectile.Initialize(
            gameObject,                    // 攻击者
            projectileDamage,              // 伤害值
            new Vector2(dir, 0f),          // 飞行方向
            projectileSpeed,               // 飞行速度
            meleeTargetLayers,             // 可命中层级
            DamageType.Projectile,         // 伤害类型
            "PlayerRanged");               // 攻击标识
    }

    /// <summary>
    /// 运行时创建投射物对象（当没有预制体时使用）
    /// </summary>
    /// <param name="spawnPos">生成位置</param>
    /// <returns>创建的投射物组件</returns>
    private SimpleProjectile CreateRuntimeProjectile(Vector3 spawnPos)
    {
        // 创建新的游戏对象
        var go = new GameObject("RuntimeProjectile");
        go.transform.position = spawnPos;

        // 添加刚体组件并配置物理属性
        var rb = go.AddComponent<Rigidbody2D>();
        rb.gravityScale = 0f;      // 无重力影响
        rb.isKinematic = true;     // 运动学模式

        // 添加碰撞体组件
        var col = go.AddComponent<CircleCollider2D>();
        col.isTrigger = true;      // 触发器模式
        col.radius = 0.12f;        // 碰撞半径

        // 添加投射物脚本并返回
        return go.AddComponent<SimpleProjectile>();
    }

    /// <summary>
    /// 在Scene视图中绘制近战攻击范围（仅在选中对象时显示）
    /// </summary>
    private void OnDrawGizmosSelected()
    {
        if (meleePoint == null) return;
        
        // 设置Gizmo颜色为红色
        Gizmos.color = Color.red;
        
        // 绘制近战攻击范围的线框球体
        Gizmos.DrawWireSphere(meleePoint.position, meleeRange);
    }
}