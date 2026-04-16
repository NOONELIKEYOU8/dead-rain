using System.Collections;
using UnityEngine;

/// <summary>
/// Boss 敌人：继承自 EnemyBase。
/// 体型更大（Scale 约 1.8×），血量和伤害更高，速度较慢。
/// 额外特性：
///   - 近距离进入"冲刺攻击"模式（可扩展）
///   - 受伤时有屏幕震动预留接口
///   - 每次攻击冷却后有短暂嘲讽停顿（IdleAttack 动画）
/// </summary>
public class BossEnemy : EnemyBase
{
    [Header("Boss Config")]
    [Tooltip("Boss 尺寸系数（相对于 Prefab 原始 Scale 的乘数）")]
    public float sizeMultiplier = 1.8f;

    [Tooltip("Boss 追击时的速度（略低于小怪）")]
    public float bossChaseSpeed = 1.8f;

    [Tooltip("进入近战范围后执行冲刺攻击的距离阈值")]
    public float chargeRange = 1.5f;

    [Tooltip("冲刺攻击时的速度倍率")]
    public float chargeSpeedMultiplier = 2.5f;

    [Tooltip("冲刺攻击持续时间（秒）")]
    public float chargeDuration = 0.4f;

    // ─── 内部状态 ────────────────────────────────────────────────────────
    private bool isCharging = false;
    private Coroutine chargeCoroutine;

    // ─── Animator 参数（Boss 专属）────────────────────────────────────────
    private const string ANIM_CHARGE = "Charge";
    private const string ANIM_IDLE   = "IsIdle";

    protected override void Awake()
    {
        base.Awake();

        // 应用 Boss 专属体型（在 X/Y 方向等比放大）
        Vector3 s = transform.localScale;
        transform.localScale = new Vector3(
            Mathf.Sign(s.x) * sizeMultiplier,
            sizeMultiplier,
            s.z);

        // Boss 默认数值（比小怪高，Inspector 可覆盖）
        if (maxHealth     <= 5)  maxHealth     = 20;
        if (contactDamage <= 1)  contactDamage = 3;
        patrolSpeed = 1.2f;

        // 血条位置由 EnemyBase 在 Start() 根据 SpriteRenderer.bounds 自动计算，无需手动设置偏移
    }

    protected override void Start()
    {
        base.Start();
    }

    protected override void Update()
    {
        if (isCharging) return;   // 冲刺期间不走基类 Update

        // 检查是否进入冲刺范围
        if (player != null &&
            Vector2.Distance(transform.position, player.position) <= chargeRange &&
            Time.time >= lastAttackTime + attackInterval)
        {
            chargeCoroutine = StartCoroutine(ChargeAttack());
            return;
        }

        base.Update();
    }

    // ─── 冲刺攻击协程 ────────────────────────────────────────────────────
    private IEnumerator ChargeAttack()
    {
        isCharging = true;
        lastAttackTime = Time.time;

        // 触发冲刺动画
        if (anim != null) anim.SetTrigger(ANIM_CHARGE);
        if (anim != null) anim.SetTrigger(ANIM_ATTACK);

        float dir = player != null
            ? Mathf.Sign(player.position.x - transform.position.x)
            : (movingRight ? 1f : -1f);

        float elapsed = 0f;
        while (elapsed < chargeDuration)
        {
            if (rb != null)
                rb.velocity = new Vector2(dir * patrolSpeed * chargeSpeedMultiplier, rb.velocity.y);
            elapsed += Time.deltaTime;
            yield return null;
        }

        // 冲刺结束后减速
        if (rb != null) rb.velocity = new Vector2(0f, rb.velocity.y);
        isCharging = false;
    }

    // ─── 受伤：Boss 专属反馈 ────────────────────────────────────────────
    public override void TakeDamage(int amount, GameObject attacker = null)
    {
        base.TakeDamage(amount, attacker);
        // 后续可在此触发摄像机震动、全屏闪光等
    }

    public override void TakeDamage(CombatContext ctx, GameObject attacker = null)
    {
        base.TakeDamage(ctx, attacker);
        // 后续可在此触发摄像机震动、全屏闪光等
    }

    // ─── 死亡：Boss 专属效果 ─────────────────────────────────────────────
    protected override void Die()
    {
        // 后续可在此触发胜利事件、掉落 Boss 宝箱等
        if (chargeCoroutine != null) StopCoroutine(chargeCoroutine);
        base.Die();
    }

    // ─── Gizmos：额外显示冲刺范围 ─────────────────────────────────────────
    protected override void OnDrawGizmosSelected()
    {
        base.OnDrawGizmosSelected();
        Gizmos.color = Color.magenta;
        Gizmos.DrawWireSphere(transform.position, chargeRange);
    }
}
