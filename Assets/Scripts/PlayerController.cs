using UnityEngine;

[RequireComponent(typeof(Rigidbody2D))]
public class PlayerController : Damageable
{
    [Header("Movement")]
    public float moveSpeed = 5f;
    public float jumpForce = 12f;

    [Header("Checks")]
    public Transform groundCheck;
    public float groundCheckRadius = 0.1f;
    public LayerMask groundLayer;

    [Header("Attack")]
    public Transform attackPoint;
    public float attackRange = 0.5f;
    public LayerMask enemyLayers;
    public int attackDamage = 1;
    public float attackCooldown = 0.5f;
    public AudioClip attackSound;
    [Tooltip("攻击按键（可改为其他键）")]
    public KeyCode attackKey = KeyCode.J;
    [Tooltip("开启调试日志，按 J 时会在 Console 输出攻击信息")]
    public bool debugAttack = true;

    Rigidbody2D rb;
    float lastAttackTime = -999f;

    protected override void Awake()
    {
        base.Awake();
        rb = GetComponent<Rigidbody2D>();
        spriteRenderer = GetComponent<SpriteRenderer>();
        if (groundCheck == null)
        {
            var go = new GameObject("GroundCheck");
            go.transform.SetParent(transform);
            go.transform.localPosition = new Vector3(0, -0.5f, 0);
            groundCheck = go.transform;
        }
        if (attackPoint == null)
        {
            var go = new GameObject("AttackPoint");
            go.transform.SetParent(transform);
            go.transform.localPosition = new Vector3(0.6f, 0f, 0);
            attackPoint = go.transform;
        }
    }

    void Update()
    {
        HandleMovement();
        HandleAttack();
    }

    void HandleMovement()
    {
        float h = Input.GetAxisRaw("Horizontal");
        Vector2 vel = rb.velocity;
        vel.x = h * moveSpeed;
        rb.velocity = vel;

        // flip
        if (h > 0.1f) transform.localScale = new Vector3(1, 1, 1);
        else if (h < -0.1f) transform.localScale = new Vector3(-1, 1, 1);

        bool isGrounded = Physics2D.OverlapCircle(groundCheck.position, groundCheckRadius, groundLayer);
        if (Input.GetButtonDown("Jump") && isGrounded)
        {
            rb.velocity = new Vector2(rb.velocity.x, jumpForce);
        }
    }

    void HandleAttack()
    {
        if (Input.GetKeyDown(attackKey) && Time.time >= lastAttackTime + attackCooldown)
        {
            if (debugAttack) Debug.Log($"[{name}] Attack key {attackKey} pressed at {Time.time}");
            lastAttackTime = Time.time;
            if (attackSound != null) AudioSource.PlayClipAtPoint(attackSound, transform.position);
            if (attackPoint == null)
            {
                if (debugAttack) Debug.LogWarning("AttackPoint is null. Attack will use player's position.");
            }
            Vector2 center = attackPoint != null ? (Vector2)attackPoint.position : (Vector2)transform.position;
            Collider2D[] hits;
            if (enemyLayers.value == 0)
            {
                if (debugAttack) Debug.LogWarning("enemyLayers mask is empty; using All layers for attack check (for testing). Please set enemyLayers in the Inspector to the enemy Layer.");
                hits = Physics2D.OverlapCircleAll(center, attackRange);
            }
            else
            {
                hits = Physics2D.OverlapCircleAll(center, attackRange, enemyLayers);
            }
            if (debugAttack) Debug.Log($"Attack found {hits.Length} colliders.");
            foreach (var c in hits)
            {
                if (c == null) continue;
                // 跳过自己的碰撞体（避免自伤）
                if (c.gameObject == this.gameObject)
                {
                    if (debugAttack) Debug.Log($" - Skipped own collider: {c.gameObject.name}");
                    continue;
                }
                // 跳过属于玩家的子对象（例如攻击点上的碰撞体）
                if (c.transform.IsChildOf(transform))
                {
                    if (debugAttack) Debug.Log($" - Skipped child collider: {c.gameObject.name}");
                    continue;
                }
                if (debugAttack) Debug.Log($" - Hit: {c.gameObject.name} (layer {c.gameObject.layer})");
                var d = c.GetComponent<Damageable>();
                if (d == null) continue;
                // 额外保险：如果 Damageable 是自己也跳过
                if (d == (Damageable)this)
                {
                    if (debugAttack) Debug.Log($" - Skipped self Damageable: {d.name}");
                    continue;
                }
                d.TakeDamage(attackDamage);
                if (debugAttack) Debug.Log($" --> Damaged {d.name} for {attackDamage}");
            }
        }
    }

    protected override void Die()
    {
        base.Die();
        var gm = GameManager.Instance;
        if (gm != null) gm.OnPlayerDead();
    }

    void OnDrawGizmosSelected()
    {
        if (attackPoint != null)
        {
            Gizmos.color = Color.red;
            Gizmos.DrawWireSphere(attackPoint.position, attackRange);
        }
        if (groundCheck != null)
        {
            Gizmos.color = Color.yellow;
            Gizmos.DrawWireSphere(groundCheck.position, groundCheckRadius);
        }
    }
}
