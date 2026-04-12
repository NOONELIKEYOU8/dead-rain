using System.Collections;
using UnityEngine;

/// <summary>
/// MinionEnemy — A lightweight, fast enemy that rushes the player.
/// 
/// Enhancements over the original stub:
///   1. LIMB ANIMATION  — Drives EnemyLimbs for procedural leg/arm swing.
///   2. EQUIPMENT       — Reads EnemyEquipment for weapon damage and armor reduction.
///   3. HEALTH BAR      — Upgraded health bar with smooth animated fill transition.
///   4. EFFECTS         — Triggers EnemyEffectSystem on attack, hit, and death.
///   5. MELEE ATTACK    — Uses an OverlapCircle in front of the minion (not just contact),
///                        providing a proper attack range separate from the body collider.
///   6. STUN / KNOCKBACK — Brief stun on hit prevents the minion from immediately
///                         retaliating after taking damage.
/// 
/// Prefab child hierarchy (optional — system degrades gracefully if absent):
///   MinionEnemy (root, this script + EnemyLimbs + EnemyEquipment + EnemyEffectSystem)
///   ├── Body          SpriteRenderer — main torso (cyan 20×20 px placeholder)
///   │   ├── Head      SpriteRenderer — head      (cyan 10×10 px placeholder)
///   │   ├── ArmL      SpriteRenderer — left arm  (8×4 px placeholder)
///   │   ├── ArmR      SpriteRenderer — right arm / weapon arm
///   │   └── Weapon    SpriteRenderer — weapon sprite (rusty dagger tint)
///   ├── LegL          SpriteRenderer — left leg
///   └── LegR          SpriteRenderer — right leg
/// 
/// Animator parameters (inherited from EnemyBase):
///   IsWalking (bool), Attack (trigger), Hit (trigger)
/// </summary>
[RequireComponent(typeof(EnemyEffectSystem))]
public class MinionEnemy : EnemyBase
{
    // ════════════════════════════════════════════════════════════════════════
    //  Inspector — Minion config
    // ════════════════════════════════════════════════════════════════════════

    [Header("Minion Config")]
    [Tooltip("Base patrol / chase speed (world units / s)")]
    public float minionPatrolSpeed = 2.2f;

    [Tooltip("OverlapCircle radius for the melee lunge attack (world units)")]
    public float meleeAttackRange = 0.7f;

    [Tooltip("Distance at which the minion switches from chase to melee attack")]
    public float meleeRange = 0.9f;

    [Tooltip("Duration (seconds) the minion is stunned after taking a hit")]
    public float hitStunDuration = 0.15f;

    [Tooltip("Knockback force applied to the minion on hit (world units / s)")]
    public float knockbackForce = 3.5f;

    // ─── Optional limb / equipment references ────────────────────────────
    [Header("Sub-Components (auto-detected)")]
    [Tooltip("Limb animation driver; auto-detected if null")]
    public EnemyLimbs limbs;

    [Tooltip("Weapon/armor stat provider; auto-detected if null")]
    public EnemyEquipment equipment;

    // ════════════════════════════════════════════════════════════════════════
    //  Private state
    // ════════════════════════════════════════════════════════════════════════

    private EnemyEffectSystem _fx;
    private bool  _isStunned;
    private float _stunTimer;
    private bool  _isAttacking;      // true while the melee coroutine runs

    // ════════════════════════════════════════════════════════════════════════
    //  Unity lifecycle
    // ════════════════════════════════════════════════════════════════════════

    protected override void Awake()
    {
        base.Awake();

        // Apply minion defaults (Inspector values take priority if already set)
        patrolSpeed   = minionPatrolSpeed;
        maxHealth     = Mathf.Max(maxHealth, 3);
        contactDamage = Mathf.Max(contactDamage, 1);

        // Auto-detect sub-components
        _fx       = GetComponent<EnemyEffectSystem>();
        limbs     = limbs     ?? GetComponent<EnemyLimbs>();
        equipment = equipment ?? GetComponent<EnemyEquipment>();
    }

    protected override void Start()
    {
        base.Start();
    }

    protected override void Update()
    {
        // Handle stun timer
        if (_isStunned)
        {
            _stunTimer -= Time.deltaTime;
            if (_stunTimer <= 0f) _isStunned = false;
            // Update limb state (stationary during stun)
            limbs?.SetWalking(false);
            return;
        }

        if (_isAttacking) return;

        // Distance-based state: if close enough, attempt melee instead of chasing
        bool inMeleeRange = player != null &&
            Vector2.Distance(transform.position, player.position) <= meleeRange;

        if (inMeleeRange && Time.time >= lastAttackTime + GetEffectiveCooldown())
        {
            StartCoroutine(MeleeAttackRoutine());
        }
        else
        {
            base.Update();  // handles ChasePlayer / Patrol
        }

        // Drive limb animation based on current velocity
        bool isMoving = rb != null && Mathf.Abs(rb.velocity.x) > 0.1f;
        limbs?.SetWalking(isMoving);
    }

    // ════════════════════════════════════════════════════════════════════════
    //  Melee attack coroutine
    // ════════════════════════════════════════════════════════════════════════

    /// <summary>
    /// Lunges forward, plays the attack animation and effect, then hits in a circle.
    /// The minion briefly slows during the windup and then dashes.
    /// </summary>
    private IEnumerator MeleeAttackRoutine()
    {
        _isAttacking   = true;
        lastAttackTime = Time.time;

        // Brief windup: slow to a stop
        if (rb != null) rb.velocity = new Vector2(0f, rb.velocity.y);

        // Trigger attack animation
        if (anim != null) anim.SetTrigger(ANIM_ATTACK);

        // Trigger limb attack swing
        limbs?.TriggerAttackSwing();

        // Wait a brief frame before the hit lands (windup)
        yield return new WaitForSeconds(0.08f);

        // Determine attack direction (facing)
        bool facingRight = transform.localScale.x > 0f;
        Vector3 fxPos    = transform.position + (facingRight ? Vector3.right : Vector3.left) * 0.4f;

        // Play attack visual effect
        _fx?.PlayAttack(fxPos, facingRight);

        // Deal damage via OverlapCircle in front of the minion
        int finalDamage = equipment != null
            ? equipment.GetEffectiveDamage(contactDamage)
            : contactDamage;

        float reach   = equipment != null ? equipment.weapon.attackRange : meleeAttackRange;
        Vector2 center = (Vector2)transform.position +
                         (facingRight ? Vector2.right : Vector2.left) * reach * 0.5f;

        Collider2D[] hits = Physics2D.OverlapCircleAll(center, reach, playerLayer);
        foreach (var col in hits)
        {
            if (col == null) continue;
            var d = col.GetComponent<Damageable>();
            if (d == null || d == (Damageable)this) continue;
            d.TakeDamage(finalDamage);
        }

        // Short recovery
        yield return new WaitForSeconds(0.12f);

        _isAttacking = false;
    }

    // ════════════════════════════════════════════════════════════════════════
    //  Damage / death overrides
    // ════════════════════════════════════════════════════════════════════════

    /// <summary>
    /// Apply armor reduction, optional block, trigger hit effects and stun.
    /// </summary>
    public override void TakeDamage(int amount)
    {
        int finalAmount = amount;
        bool blocked    = false;

        if (equipment != null)
            finalAmount = equipment.GetDamageAfterArmor(amount, out blocked);

        if (blocked)
        {
            // Full block: play block effect but take no damage
            _fx?.PlayBlock(transform.position + Vector3.up * 0.3f);
            limbs?.TriggerHitRecoil(); // shield absorbs the blow
            return;
        }

        // Apply base damage (handles invuln frames, health decrement, death)
        base.TakeDamage(finalAmount);

        if (invulnerable) return; // already dead or in invuln

        // Visual / audio feedback
        _fx?.PlayHit(transform.position + Vector3.up * 0.2f);
        limbs?.TriggerHitRecoil();

        // Apply knockback away from player
        if (rb != null && player != null)
        {
            float dir = Mathf.Sign(transform.position.x - player.position.x);
            rb.velocity = new Vector2(dir * knockbackForce, rb.velocity.y + 1f);
        }

        // Enter stun
        _isStunned = true;
        _stunTimer = hitStunDuration;
    }

    protected override void Die()
    {
        // Play death explosion before the GameObject is destroyed
        _fx?.PlayDeath(transform.position);
        base.Die();
    }

    // ════════════════════════════════════════════════════════════════════════
    //  Helpers
    // ════════════════════════════════════════════════════════════════════════

    private float GetEffectiveCooldown()
    {
        return equipment != null
            ? equipment.GetEffectiveCooldown(attackInterval)
            : attackInterval;
    }

    // ─── Gizmos ──────────────────────────────────────────────────────────
    protected override void OnDrawGizmosSelected()
    {
        base.OnDrawGizmosSelected();
        Gizmos.color = Color.cyan;
        Gizmos.DrawWireSphere(transform.position, meleeRange);
        Gizmos.color = new Color(0f, 1f, 1f, 0.4f);
        bool facingRight = transform.localScale.x >= 0f;
        float reach  = equipment != null ? equipment.weapon.attackRange : meleeAttackRange;
        Vector2 atkCenter = (Vector2)transform.position +
                            (facingRight ? Vector2.right : Vector2.left) * reach * 0.5f;
        Gizmos.DrawWireSphere(atkCenter, reach);
    }
}
