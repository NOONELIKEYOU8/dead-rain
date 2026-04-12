using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 敌人基类：扩展自 Damageable，包含通用 AI 逻辑（巡逻/追踪/接触伤害）
/// 以及血条（World-Space Canvas）支持，供 MinionEnemy 和 BossEnemy 继承。
/// 
/// 血条定位逻辑：
///   运行时自动读取 SpriteRenderer.bounds 获取敌人实际包围盒，
///   血条显示在包围盒顶端上方 healthBarMargin 处，宽度与包围盒宽度一致。
///   血条为场景根节点对象（不是敌人子节点），避免跟随敌人 localScale 翻转/缩放。
/// </summary>
[RequireComponent(typeof(Rigidbody2D))]
public abstract class EnemyBase : Damageable
{
    // ─── 移动 ───────────────────────────────────────────────────────────
    [Header("Movement")]
    public float patrolSpeed = 1.5f;
    public Transform leftLimit;
    public Transform rightLimit;

    // ─── AI ─────────────────────────────────────────────────────────────
    [Header("AI")]
    public float chaseRange    = 4f;
    public LayerMask playerLayer;
    public int  contactDamage  = 1;
    public float attackInterval = 1f;

    // ─── 血条 ────────────────────────────────────────────────────────────
    [Header("Health Bar")]
    [Tooltip("World-Space Canvas 预制体（含 HealthBarCanvas 脚本）；留空则运行时自动创建")]
    public GameObject healthBarPrefab;

    [Tooltip("血条距离敌人包围盒顶端的额外偏移（世界单位）")]
    public float healthBarMargin = 0.15f;

    [Tooltip("血条高度（世界单位），0.12~0.18 较合适")]
    public float healthBarHeight = 0.14f;

    // ─── 动画 ────────────────────────────────────────────────────────────
    [Header("Animation")]
    protected Animator anim;
    protected const string ANIM_ATTACK = "Attack";
    protected const string ANIM_HIT    = "Hit";
    protected const string ANIM_WALK   = "IsWalking";

    // ─── 内部状态 ────────────────────────────────────────────────────────
    protected Rigidbody2D  rb;
    protected Transform    player;
    protected bool         movingRight    = true;
    protected float        lastAttackTime = -999f;

    // 血条引用
    protected HealthBarCanvas healthBar;

    // 缓存包围盒信息（在 Start 里读取，此时 localScale 已应用）
    private float _spriteHalfHeight = 0.5f;  // 敌人包围盒半高（世界单位）
    private float _spriteWidth      = 1.0f;  // 敌人包围盒宽度（世界单位）

    // ─── Unity 回调 ──────────────────────────────────────────────────────
    protected override void Awake()
    {
        base.Awake();
        rb   = GetComponent<Rigidbody2D>();
        anim = GetComponent<Animator>();
        // 注意：血条在 Start 里创建，此时 localScale 已由子类 Awake 设置完毕
    }

    protected virtual void Start()
    {
        // 查找玩家
        var pgo = GameObject.FindWithTag("Player");
        if (pgo != null) player = pgo.transform;

        // 读取实际包围盒尺寸（基于已应用 Scale 的 SpriteRenderer）
        RecalculateSpriteBounds();

        // 创建血条（此时尺寸已知）
        CreateHealthBar();
    }

    protected virtual void Update()
    {
        bool inRange = player != null &&
                       Vector2.Distance(transform.position, player.position) <= chaseRange;
        if (inRange)
            ChasePlayer();
        else
            Patrol();

        // 每帧同步血条位置（敌人移动后跟随，且始终保持朝上）
        SyncHealthBarPosition();
    }

    // ─── 包围盒计算 ──────────────────────────────────────────────────────
    /// <summary>
    /// 从 SpriteRenderer 读取世界空间包围盒，缓存半高和宽度。
    /// 若没有 SpriteRenderer，则用 Collider2D 包围盒作为备选。
    /// </summary>
    private void RecalculateSpriteBounds()
    {
        var sr = GetComponentInChildren<SpriteRenderer>();
        if (sr != null)
        {
            // bounds 已是世界空间（包含 localScale）
            _spriteHalfHeight = sr.bounds.extents.y;
            _spriteWidth      = sr.bounds.size.x;
            return;
        }

        // 备选：Collider2D
        var col = GetComponent<Collider2D>();
        if (col != null)
        {
            _spriteHalfHeight = col.bounds.extents.y;
            _spriteWidth      = col.bounds.size.x;
            return;
        }

        // 兜底：localScale
        _spriteHalfHeight = Mathf.Abs(transform.localScale.y) * 0.5f;
        _spriteWidth      = Mathf.Abs(transform.localScale.x);
    }

    // ─── 血条位置同步 ────────────────────────────────────────────────────
    private void SyncHealthBarPosition()
    {
        if (healthBar == null) return;

        // 血条中心 Y = 敌人枢轴 Y + 半高 + margin + 血条自身高度/2
        float topY = transform.position.y
                     + _spriteHalfHeight
                     + healthBarMargin
                     + healthBarHeight * 0.5f;

        healthBar.transform.position = new Vector3(
            transform.position.x,
            topY,
            transform.position.z);

        // 始终保持正方向朝上（防止敌人翻转时血条颠倒）
        healthBar.transform.rotation = Quaternion.identity;
    }

    // ─── AI 行为 ─────────────────────────────────────────────────────────
    protected virtual void ChasePlayer()
    {
        float dir = Mathf.Sign(player.position.x - transform.position.x);
        rb.velocity = new Vector2(dir * patrolSpeed, rb.velocity.y);
        // 翻转 X 缩放实现左右朝向（Y 保持绝对值不变）
        transform.localScale = new Vector3(
            Mathf.Sign(dir) * Mathf.Abs(transform.localScale.x),
            transform.localScale.y, 1f);

        if (anim != null) anim.SetBool(ANIM_WALK, Mathf.Abs(rb.velocity.x) > 0.1f);
    }

    protected virtual void Patrol()
    {
        if (leftLimit == null || rightLimit == null)
        {
            rb.velocity = new Vector2((movingRight ? 1 : -1) * patrolSpeed, rb.velocity.y);
            if (anim != null) anim.SetBool(ANIM_WALK, true);
            return;
        }

        float dir = movingRight ? 1f : -1f;
        rb.velocity = new Vector2(dir * patrolSpeed, rb.velocity.y);
        transform.localScale = new Vector3(
            (movingRight ? 1f : -1f) * Mathf.Abs(transform.localScale.x),
            transform.localScale.y, 1f);

        if (movingRight  && transform.position.x >= rightLimit.position.x) movingRight = false;
        else if (!movingRight && transform.position.x <= leftLimit.position.x)  movingRight = true;

        if (anim != null) anim.SetBool(ANIM_WALK, Mathf.Abs(rb.velocity.x) > 0.1f);
    }

    protected virtual void OnCollisionEnter2D(Collision2D col)
    {
        if ((playerLayer.value & (1 << col.gameObject.layer)) != 0)
        {
            if (Time.time >= lastAttackTime + attackInterval)
            {
                if (anim != null) anim.SetTrigger(ANIM_ATTACK);
                var d = col.gameObject.GetComponent<Damageable>();
                if (d != null) d.TakeDamage(contactDamage);
                lastAttackTime = Time.time;
            }
        }
        else
        {
            movingRight = !movingRight;
        }
    }

    // ─── 受伤覆写：更新血条 ──────────────────────────────────────────────
    public override void TakeDamage(int amount)
    {
        base.TakeDamage(amount);
        if (anim != null && !invulnerable) anim.SetTrigger(ANIM_HIT);
        if (healthBar != null) healthBar.UpdateBar(currentHealth, maxHealth);
    }

    // ─── 死亡时销毁血条 ──────────────────────────────────────────────────
    protected override void Die()
    {
        if (healthBar != null) Destroy(healthBar.gameObject);
        base.Die();
    }

    // ─── 血条创建 ────────────────────────────────────────────────────────
    protected void CreateHealthBar()
    {
        // 计算初始位置
        float topY = transform.position.y
                     + _spriteHalfHeight
                     + healthBarMargin
                     + healthBarHeight * 0.5f;
        Vector3 barPos = new Vector3(transform.position.x, topY, transform.position.z);

        if (healthBarPrefab != null)
        {
            var go = Instantiate(healthBarPrefab, barPos, Quaternion.identity);
            healthBar = go.GetComponent<HealthBarCanvas>();
        }
        else
        {
            // 血条宽度 = 精灵宽度，最小 0.6 世界单位
            float barWidth = Mathf.Max(_spriteWidth, 0.6f);
            healthBar = HealthBarCanvas.CreateDefault(barPos, barWidth, healthBarHeight);
        }

        if (healthBar != null)
        {
            healthBar.UpdateBar(maxHealth, maxHealth);
        }
    }

    // ─── Gizmos ──────────────────────────────────────────────────────────
    protected virtual void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, chaseRange);
        if (leftLimit  != null) { Gizmos.color = Color.yellow; Gizmos.DrawWireSphere(leftLimit.position,  0.2f); }
        if (rightLimit != null) { Gizmos.color = Color.yellow; Gizmos.DrawWireSphere(rightLimit.position, 0.2f); }

        // 血条预览线（Editor 中可视化）
        float topY = transform.position.y + _spriteHalfHeight + healthBarMargin;
        Gizmos.color = Color.cyan;
        Gizmos.DrawLine(
            new Vector3(transform.position.x - _spriteWidth * 0.5f, topY, 0),
            new Vector3(transform.position.x + _spriteWidth * 0.5f, topY, 0));
    }
}
