using UnityEngine;

[RequireComponent(typeof(Rigidbody2D))]
public class EnemyRuntime : MonoBehaviour, IDamageable, IKnockbackable
{
    public enum EnemyState
    {
        Idle,
        Patrol,
        Chase,
        Attack,
        Hurt,
        Dead
    }

    [SerializeField] private EnemyData data;
    [SerializeField] private Transform attackPoint;
    [SerializeField] private Transform projectileSpawn;
    [SerializeField] private Projectile projectilePrefab;
    [SerializeField] private LayerMask playerMask;
    [SerializeField] private LayerMask groundMask;
    [SerializeField] private float detectionRange = 7f;
    [SerializeField] private float attackRange = 1.1f;
    [SerializeField] private float rangedRange = 6f;
    [SerializeField] private float attackCooldown = 1.4f;
    [SerializeField] private float patrolEdgeDistance = 0.7f;
    [SerializeField] private Color eliteTint = new Color(1f, 0.65f, 0.25f, 1f);

    private Rigidbody2D rb;
    private Animator animator;
    private RuntimeSpriteAnimator spriteAnimator;
    private SpriteRenderer spriteRenderer;
    private Stats stats;
    private Transform player;
    private int facingDirection = 1;
    private float lastAttackTime = -999f;
    private float scaledDamage;
    private bool initialized;

    public EnemyData Data => data;
    public bool IsElite { get; private set; }
    public EnemyState State { get; private set; }

    public void Damage(float amount)
    {
        if (stats == null || stats.IsDead)
        {
            return;
        }

        State = EnemyState.Hurt;
        if (spriteAnimator != null)
        {
            spriteAnimator.PlayHurt();
        }
        stats.DecreaseHealth(amount);
    }

    public void Knockback(Vector2 angle, float strength, int direction)
    {
        if (rb == null || stats == null || stats.IsDead)
        {
            return;
        }

        angle.Normalize();
        rb.velocity = new Vector2(angle.x * strength * direction, angle.y * strength);
    }

    private void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        animator = GetComponent<Animator>();
        spriteAnimator = GetComponent<RuntimeSpriteAnimator>();
        spriteRenderer = GetComponentInChildren<SpriteRenderer>();
        stats = GetComponentInChildren<Stats>();
        if (attackPoint == null)
        {
            attackPoint = transform;
        }
        if (projectileSpawn == null)
        {
            projectileSpawn = attackPoint;
        }
    }

    private void OnEnable()
    {
        if (stats != null)
        {
            stats.OnHealthZero += HandleDeath;
        }
    }

    private void OnDisable()
    {
        if (stats != null)
        {
            stats.OnHealthZero -= HandleDeath;
        }
    }

    private void Start()
    {
        if (!initialized)
        {
            Initialize(data, GameRunManager.Instance != null ? GameRunManager.Instance.DifficultyMultiplier : 1f, false);
        }
    }

    private void Update()
    {
        if (stats != null && stats.IsDead)
        {
            State = EnemyState.Dead;
            return;
        }

        ResolvePlayer();
        TickState();
        UpdateAnimator();
    }

    public void Initialize(EnemyData enemyData, float difficultyMultiplier, bool elite)
    {
        data = enemyData;
        IsElite = elite;
        initialized = true;

        float baseHp = data != null ? data.hp : 30f;
        float eliteDamageMultiplier = data != null ? data.eliteDamageMultiplier : 1.35f;
        float eliteHealthMultiplier = data != null ? data.eliteHealthMultiplier : 1.6f;
        scaledDamage = (data != null ? data.damage : 8f) * difficultyMultiplier * (elite ? eliteDamageMultiplier : 1f);

        if (stats != null)
        {
            stats.SetMaxHealth(baseHp * difficultyMultiplier * (elite ? eliteHealthMultiplier : 1f), true);
        }

        if (spriteRenderer != null && elite)
        {
            spriteRenderer.color = eliteTint;
        }
    }

    private void TickState()
    {
        if (player == null)
        {
            Patrol();
            return;
        }

        float distance = Vector2.Distance(transform.position, player.position);
        FaceTarget(player.position);

        if (CanAttack(distance))
        {
            State = EnemyState.Attack;
            rb.velocity = new Vector2(0f, rb.velocity.y);
            Attack();
            return;
        }

        if (distance <= detectionRange)
        {
            Chase();
        }
        else
        {
            Patrol();
        }
    }

    private bool CanAttack(float distance)
    {
        if (data == null)
        {
            return distance <= attackRange;
        }

        return data.runtimeRole == RuntimeEnemyRole.Ranged
            ? distance <= rangedRange
            : distance <= attackRange;
    }

    private void Patrol()
    {
        State = EnemyState.Patrol;
        if (spriteAnimator != null)
        {
            spriteAnimator.ApplyMove();
        }
        float speed = data != null ? data.moveSpeed : 2f;
        rb.velocity = new Vector2(speed * facingDirection, rb.velocity.y);

        Vector2 origin = transform.position + Vector3.right * facingDirection * patrolEdgeDistance;
        bool hasGround = Physics2D.Raycast(origin, Vector2.down, 1.6f, groundMask);
        bool hitWall = Physics2D.Raycast(transform.position, Vector2.right * facingDirection, 0.55f, groundMask);
        if (!hasGround || hitWall)
        {
            Flip();
        }
    }

    private void Chase()
    {
        State = EnemyState.Chase;
        if (spriteAnimator != null)
        {
            spriteAnimator.ApplyMove();
        }
        float speed = data != null ? data.moveSpeed : 2f;
        if (data != null && data.runtimeRole == RuntimeEnemyRole.Charger)
        {
            speed *= 1.25f;
        }
        rb.velocity = new Vector2(speed * facingDirection, rb.velocity.y);
    }

    private void Attack()
    {
        if (Time.time < lastAttackTime + attackCooldown)
        {
            return;
        }

        lastAttackTime = Time.time;
        if (animator != null)
        {
            animator.SetTrigger("attack");
        }

        if (data != null && data.runtimeRole == RuntimeEnemyRole.Ranged)
        {
            if (spriteAnimator != null)
            {
                spriteAnimator.PlayCast();
            }
            FireProjectile();
            return;
        }

        if (data != null && data.runtimeRole == RuntimeEnemyRole.Charger)
        {
            if (spriteAnimator != null)
            {
                spriteAnimator.PlayCharge();
            }
            rb.velocity = new Vector2(facingDirection * Mathf.Max(7f, data.moveSpeed * 3f), rb.velocity.y);
        }
        else if (spriteAnimator != null)
        {
            spriteAnimator.PlayAttack();
        }

        Collider2D[] hits = Physics2D.OverlapCircleAll(attackPoint.position, attackRange, playerMask);
        foreach (Collider2D hit in hits)
        {
            if (hit.TryGetComponent(out IDamageable damageable))
            {
                damageable.Damage(scaledDamage);
            }

            if (hit.TryGetComponent(out IKnockbackable knockbackable))
            {
                knockbackable.Knockback(new Vector2(1f, 0.35f), 8f, facingDirection);
            }
        }
    }

    private void FireProjectile()
    {
        if (projectilePrefab == null)
        {
            return;
        }

        Quaternion rotation = facingDirection >= 0 ? Quaternion.identity : Quaternion.Euler(0f, 180f, 0f);
        Projectile projectile = Instantiate(projectilePrefab, projectileSpawn.position, rotation);
        projectile.FireProjectile(10f, 9f, scaledDamage, playerMask, transform.root);
    }

    private void ResolvePlayer()
    {
        if (player != null)
        {
            return;
        }

        GameObject playerObject = GameObject.FindGameObjectWithTag("Player");
        player = playerObject != null ? playerObject.transform : null;
    }

    private void FaceTarget(Vector3 target)
    {
        int desired = target.x >= transform.position.x ? 1 : -1;
        if (desired != facingDirection)
        {
            Flip();
        }
    }

    private void Flip()
    {
        facingDirection *= -1;
        Vector3 scale = transform.localScale;
        scale.x = Mathf.Abs(scale.x) * facingDirection;
        transform.localScale = scale;
    }

    private void UpdateAnimator()
    {
        if (animator == null)
        {
            return;
        }

        animator.SetFloat("xVelocity", Mathf.Abs(rb.velocity.x));
        animator.SetFloat("yVelocity", rb.velocity.y);
    }

    private void HandleDeath()
    {
        State = EnemyState.Dead;
        rb.velocity = Vector2.zero;
        foreach (Collider2D collider in GetComponentsInChildren<Collider2D>())
        {
            collider.enabled = false;
        }
        gameObject.SetActive(false);
    }

    private void OnDrawGizmosSelected()
    {
        Transform point = attackPoint != null ? attackPoint : transform;
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(point.position, attackRange);
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, detectionRange);
    }
}
