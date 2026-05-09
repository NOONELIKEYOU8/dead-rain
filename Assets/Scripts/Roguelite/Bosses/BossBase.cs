using System;
using System.Collections;
using UnityEngine;

[RequireComponent(typeof(Rigidbody2D))]
public class BossBase : MonoBehaviour, IDamageable, IKnockbackable
{
    [SerializeField] protected BossData bossData;
    [SerializeField] protected Transform attackPoint;
    [SerializeField] protected Transform summonPoint;
    [SerializeField] protected EnemyData summonEnemyData;
    [SerializeField] protected Projectile warningProjectilePrefab;
    [SerializeField] protected LayerMask playerMask;
    [SerializeField] protected float meleeRadius = 1.8f;
    [SerializeField] protected GameObject warningAreaPrefab;
    [SerializeField] protected GameObject summonEnemyPrefab;

    protected Rigidbody2D rb;
    protected Animator animator;
    protected RuntimeSpriteAnimator spriteAnimator;
    protected Stats stats;
    protected Transform player;
    protected int facingDirection = 1;
    protected int phase = 1;
    protected float lastSkillTime = -999f;
    protected bool casting;
    protected float scaledDamage;

    public event Action<BossBase, float> OnBossHealthChanged;
    public event Action<BossBase, int> OnPhaseChanged;

    public string DisplayName => bossData != null ? bossData.displayName : name;
    public int Phase => phase;
    public BossData Data => bossData;

    public void Damage(float amount)
    {
        if (stats == null || stats.IsDead)
        {
            return;
        }

        stats.DecreaseHealth(amount);
    }

    public void Knockback(Vector2 angle, float strength, int direction)
    {
        if (rb == null || phase >= 2)
        {
            return;
        }

        angle.Normalize();
        rb.velocity = new Vector2(angle.x * strength * 0.25f * direction, rb.velocity.y);
    }

    protected virtual void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        animator = GetComponent<Animator>();
        spriteAnimator = GetComponent<RuntimeSpriteAnimator>();
        stats = GetComponentInChildren<Stats>();
        if (attackPoint == null)
        {
            attackPoint = transform;
        }
        if (summonPoint == null)
        {
            summonPoint = transform;
        }
    }

    protected virtual void OnEnable()
    {
        if (stats != null)
        {
            stats.OnHealthChanged += HandleHealthChanged;
            stats.OnHealthZero += HandleDeath;
        }
    }

    protected virtual void Start()
    {
        Initialize(GameRunManager.Instance != null ? GameRunManager.Instance.DifficultyMultiplier : 1f);
    }

    protected virtual void OnDisable()
    {
        if (stats != null)
        {
            stats.OnHealthChanged -= HandleHealthChanged;
            stats.OnHealthZero -= HandleDeath;
        }
    }

    protected virtual void Update()
    {
        if (stats == null || stats.IsDead)
        {
            return;
        }

        ResolvePlayer();
        if (player == null || casting)
        {
            return;
        }

        FaceTarget(player.position);
        float cooldown = GetCurrentCooldown();
        if (Time.time >= lastSkillTime + cooldown)
        {
            StartCoroutine(CastNextSkill());
        }
    }

    public virtual void Initialize(float difficultyMultiplier)
    {
        scaledDamage = (bossData != null ? bossData.damage : 16f) * difficultyMultiplier;
        if (stats != null)
        {
            stats.SetMaxHealth((bossData != null ? bossData.hp : 260f) * difficultyMultiplier, true);
        }
    }

    protected virtual IEnumerator CastNextSkill()
    {
        casting = true;
        lastSkillTime = Time.time;
        int skill = UnityEngine.Random.Range(0, 4);

        switch (skill)
        {
            case 0:
                yield return BronzeSweep();
                break;
            case 1:
                yield return BeastCharge();
                break;
            case 2:
                yield return RitualShockwave();
                break;
            default:
                yield return SummonMinions();
                break;
        }

        casting = false;
    }

    protected virtual IEnumerator BronzeSweep()
    {
        if (animator != null)
        {
            animator.SetTrigger("attack");
        }
        if (spriteAnimator != null)
        {
            spriteAnimator.PlayAttack();
        }
        yield return new WaitForSeconds(0.25f);

        float radius = phase >= 2 ? meleeRadius * 1.25f : meleeRadius;
        Collider2D[] hits = Physics2D.OverlapCircleAll(attackPoint.position, radius, playerMask);
        foreach (Collider2D hit in hits)
        {
            if (hit.TryGetComponent(out IDamageable damageable))
            {
                damageable.Damage(scaledDamage);
            }

            if (hit.TryGetComponent(out IKnockbackable knockbackable))
            {
                knockbackable.Knockback(new Vector2(1f, 0.45f), 12f, facingDirection);
            }
        }
    }

    protected virtual IEnumerator BeastCharge()
    {
        if (animator != null)
        {
            animator.SetTrigger("charge");
        }
        if (spriteAnimator != null)
        {
            spriteAnimator.PlayCharge();
        }
        yield return new WaitForSeconds(0.45f);
        rb.velocity = new Vector2(facingDirection * (phase >= 2 ? 13f : 10f), rb.velocity.y);
        yield return new WaitForSeconds(0.45f);
        rb.velocity = new Vector2(0f, rb.velocity.y);
        BronzeSweep();
    }

    protected virtual IEnumerator RitualShockwave()
    {
        if (animator != null)
        {
            animator.SetTrigger("cast");
        }
        if (spriteAnimator != null)
        {
            spriteAnimator.PlayCast();
        }
        Vector3 target = player != null ? player.position : transform.position + Vector3.right * facingDirection * 2f;
        GameObject warning = null;
        if (warningAreaPrefab != null)
        {
            warning = Instantiate(warningAreaPrefab, target, Quaternion.identity);
            warning.transform.localScale = Vector3.one * (phase >= 2 ? 2.6f : 1.8f);
        }

        yield return new WaitForSeconds(0.8f);

        float radius = phase >= 2 ? 2.4f : 1.6f;
        Collider2D[] hits = Physics2D.OverlapCircleAll(target, radius, playerMask);
        foreach (Collider2D hit in hits)
        {
            if (hit.TryGetComponent(out IDamageable damageable))
            {
                damageable.Damage(scaledDamage * 1.15f);
            }
        }

        if (warning != null)
        {
            Destroy(warning);
        }
    }

    protected virtual IEnumerator SummonMinions()
    {
        if (animator != null)
        {
            animator.SetTrigger("cast");
        }
        if (spriteAnimator != null)
        {
            spriteAnimator.PlayCast();
        }
        yield return new WaitForSeconds(0.4f);

        if (summonEnemyPrefab == null)
        {
            yield break;
        }

        for (int i = 0; i < 2; i++)
        {
            Vector3 offset = new Vector3((i == 0 ? -1.5f : 1.5f), 0f, 0f);
            GameObject enemyObject = Instantiate(summonEnemyPrefab, summonPoint.position + offset, Quaternion.identity);
            EnemyRuntime runtime = enemyObject.GetComponent<EnemyRuntime>();
            runtime?.Initialize(summonEnemyData, GameRunManager.Instance != null ? GameRunManager.Instance.DifficultyMultiplier : 1f, false);
        }
    }

    protected virtual float GetCurrentCooldown()
    {
        float cooldown = bossData != null ? bossData.baseSkillCooldown : 2.5f;
        if (phase >= 2)
        {
            cooldown *= bossData != null ? bossData.phaseTwoCooldownMultiplier : 0.65f;
        }

        return cooldown;
    }

    protected virtual void HandleHealthChanged(float current, float max)
    {
        float percent = max <= 0f ? 0f : current / max;
        OnBossHealthChanged?.Invoke(this, percent);

        if (phase == 1 && percent <= 0.5f)
        {
            phase = 2;
            OnPhaseChanged?.Invoke(this, phase);
        }
    }

    protected virtual void HandleDeath()
    {
        StopAllCoroutines();
        rb.velocity = Vector2.zero;
        foreach (Collider2D collider in GetComponentsInChildren<Collider2D>())
        {
            collider.enabled = false;
        }
        gameObject.SetActive(false);
    }

    protected void ResolvePlayer()
    {
        if (player != null)
        {
            return;
        }

        GameObject playerObject = GameObject.FindGameObjectWithTag("Player");
        player = playerObject != null ? playerObject.transform : null;
    }

    protected void FaceTarget(Vector3 target)
    {
        int desired = target.x >= transform.position.x ? 1 : -1;
        if (desired == facingDirection)
        {
            return;
        }

        facingDirection = desired;
        Vector3 scale = transform.localScale;
        scale.x = Mathf.Abs(scale.x) * facingDirection;
        transform.localScale = scale;
    }
}
