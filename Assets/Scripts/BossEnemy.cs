using System.Collections;
using UnityEngine;

/// <summary>
/// BossEnemy — A large, high-HP enemy with multiple attack phases.
/// 
/// Enhancements over the original stub:
///   1. LIMB ANIMATION  — Drives EnemyLimbs for procedural body/arm/leg swing.
///                        Boss limbs are weighted (slower, heavier feel).
///   2. EQUIPMENT       — Reads EnemyEquipment for weapon damage bonus, armor
///                        reduction and block chance (heavy armor preset).
///   3. HEALTH BAR      — Segmented health bar via HealthBarCanvas, shows phase
///                        thresholds (two colored bands at 50 % and 25 %).
///   4. EFFECTS         — Full EnemyEffectSystem: hit sparks, attack slash, death
///                        explosion, and block clang.
///   5. PHASE SYSTEM    — Three phases driven by HP percentage:
///                        Phase 1 (100–50 %): patrol/chase + charge attack.
///                        Phase 2 ( 50–25 %): speed boost, shorter charge cooldown.
///                        Phase 3 (  &lt;25 %): frenzy — rapid charge + ground slam AoE.
///   6. GROUND SLAM     — Phase-3 AoE that damages everything in a radius around
///                        the Boss when it lands after a short leap.
///   7. KNOCKBACK RESISTANCE — Boss is much harder to move on hit.
///   8. ENRAGE VFX      — Red tint + larger hit effect at phase transitions.
/// 
/// Prefab child hierarchy (optional — degrades gracefully if absent):
///   BossEnemy (root, this script + EnemyLimbs + EnemyEquipment + EnemyEffectSystem)
///   ├── Body          SpriteRenderer — torso  (purple 36×36 px placeholder)
///   │   ├── Head      SpriteRenderer — head
///   │   ├── ArmL      SpriteRenderer — left arm / shield arm
///   │   ├── ArmR      SpriteRenderer — right arm / weapon arm
///   │   ├── Weapon    SpriteRenderer — war-axe tint (dark orange)
///   │   └── Shield    SpriteRenderer — kite shield tint (steel blue)
///   ├── LegL          SpriteRenderer — left leg
///   └── LegR          SpriteRenderer — right leg
/// 
/// Animator parameters (inherited + Boss-exclusive):
///   IsWalking (bool), Attack (trigger), Hit (trigger), Charge (trigger), Slam (trigger)
/// </summary>
[RequireComponent(typeof(EnemyEffectSystem))]
public class BossEnemy : EnemyBase
{
    // ════════════════════════════════════════════════════════════════════════
    //  Inspector — Boss config
    // ════════════════════════════════════════════════════════════════════════

    [Header("Boss Config")]
    [Tooltip("Scale multiplier applied to the Boss sprite (relative to Prefab base scale)")]
    public float sizeMultiplier = 1.8f;

    [Tooltip("Chase speed in Phase 1")]
    public float bossChaseSpeed = 1.8f;

    [Header("Charge Attack")]
    [Tooltip("Distance at which the Boss triggers a charge dash")]
    public float chargeRange = 1.8f;

    [Tooltip("Speed multiplier during charge (relative to patrolSpeed)")]
    public float chargeSpeedMultiplier = 2.8f;

    [Tooltip("Duration of a single charge burst (seconds)")]
    public float chargeDuration = 0.4f;

    [Tooltip("Minimum time between charge attempts (seconds)")]
    public float chargeCooldown = 2.0f;

    [Header("Ground Slam (Phase 3)")]
    [Tooltip("Radius of the ground slam AoE")]
    public float slamRadius = 1.8f;

    [Tooltip("Damage dealt by the ground slam")]
    public int slamDamage = 4;

    [Tooltip("Cooldown between slam attempts (seconds)")]
    public float slamCooldown = 4f;

    [Header("Phase Thresholds")]
    [Tooltip("HP% at which Phase 2 begins (speed boost)")]
    [Range(0f, 1f)] public float phase2Threshold = 0.5f;

    [Tooltip("HP% at which Phase 3 (frenzy) begins")]
    [Range(0f, 1f)] public float phase3Threshold = 0.25f;

    [Header("Sub-Components (auto-detected)")]
    public EnemyLimbs    limbs;
    public EnemyEquipment equipment;

    // ─── Animator parameter names (Boss-exclusive) ───────────────────────
    private const string ANIM_CHARGE = "Charge";
    private const string ANIM_SLAM   = "Slam";

    // ════════════════════════════════════════════════════════════════════════
    //  Private state
    // ════════════════════════════════════════════════════════════════════════

    private EnemyEffectSystem _fx;

    private bool  _isCharging;
    private Coroutine _chargeCoroutine;

    private bool  _isSlamming;
    private float _lastChargeTime  = -999f;
    private float _lastSlamTime    = -999f;

    private int   _currentPhase    = 1;

    // Enrage tint overlaid on SpriteRenderer when entering phase 3
    private static readonly Color _enrageColor = new Color(1f, 0.3f, 0.3f, 1f);

    // ════════════════════════════════════════════════════════════════════════
    //  Unity lifecycle
    // ════════════════════════════════════════════════════════════════════════

    protected override void Awake()
    {
        base.Awake();

        // Scale up the Boss sprite
        Vector3 s = transform.localScale;
        transform.localScale = new Vector3(
            Mathf.Sign(s.x) * sizeMultiplier,
            sizeMultiplier,
            s.z);

        // Set Boss defaults (Inspector values override these if already changed)
        if (maxHealth     <= 5)  maxHealth     = 25;
        if (contactDamage <= 1)  contactDamage = 3;
        patrolSpeed = 1.2f;

        // Auto-detect sub-components
        _fx       = GetComponent<EnemyEffectSystem>();
        limbs     = limbs     ?? GetComponent<EnemyLimbs>();
        equipment = equipment ?? GetComponent<EnemyEquipment>();

        // Give Boss limbs a heavier, slower feel
        if (limbs != null)
        {
            limbs.walkFrequency  = 1.8f;
            limbs.legSwingAngle  = 22f;
            limbs.armSwingAngle  = 14f;
            limbs.bodyBobAmp     = 0.08f;
            limbs.hitRecoilAngle = 10f;
            limbs.attackSwingAngle = 75f;
            limbs.attackSwingDuration = 0.22f;
        }
    }

    protected override void Start()
    {
        base.Start();

        // Add phase threshold markers to the health bar so players can see
        // the phase boundaries visually (vertical dividers on the HP bar)
        if (healthBar != null)
        {
            healthBar.SetPhaseMarkers(new[] { phase3Threshold, phase2Threshold });
        }
    }

    protected override void Update()
    {
        if (_isCharging || _isSlamming) return;

        float hpRatio = maxHealth > 0 ? (float)currentHealth / maxHealth : 0f;
        UpdatePhase(hpRatio);

        bool closeEnough = player != null &&
            Vector2.Distance(transform.position, player.position) <= chargeRange;

        // Phase 3: try ground slam first (if in very close range)
        if (_currentPhase >= 3 &&
            player != null &&
            Vector2.Distance(transform.position, player.position) <= slamRadius * 0.8f &&
            Time.time >= _lastSlamTime + slamCooldown)
        {
            _chargeCoroutine = StartCoroutine(GroundSlamRoutine());
            return;
        }

        // All phases: try charge if in range and cooldown ready
        if (closeEnough && Time.time >= _lastChargeTime + chargeCooldown)
        {
            _chargeCoroutine = StartCoroutine(ChargeRoutine());
            return;
        }

        // Fall back to base (patrol / chase)
        base.Update();

        // Drive limb animation
        bool isMoving = rb != null && Mathf.Abs(rb.velocity.x) > 0.1f;
        limbs?.SetWalking(isMoving);
    }

    // ════════════════════════════════════════════════════════════════════════
    //  Phase management
    // ════════════════════════════════════════════════════════════════════════

    private void UpdatePhase(float hpRatio)
    {
        int newPhase = 1;
        if      (hpRatio <= phase3Threshold) newPhase = 3;
        else if (hpRatio <= phase2Threshold) newPhase = 2;

        if (newPhase != _currentPhase)
        {
            OnPhaseTransition(newPhase);
            _currentPhase = newPhase;
        }
    }

    private void OnPhaseTransition(int newPhase)
    {
        // Phase 2: speed boost
        if (newPhase == 2)
        {
            patrolSpeed   = bossChaseSpeed * 1.15f;
            chargeCooldown = Mathf.Max(0.8f, chargeCooldown - 0.5f);
            Debug.Log($"[BossEnemy] Phase 2 — speed increased, charge cooldown reduced.");
        }
        // Phase 3: frenzy enrage
        else if (newPhase == 3)
        {
            patrolSpeed    = bossChaseSpeed * 1.4f;
            chargeCooldown = Mathf.Max(0.5f, chargeCooldown - 0.4f);
            chargeDuration = Mathf.Max(0.25f, chargeDuration - 0.1f);

            // Apply enrage red tint to sprite
            if (spriteRenderer != null)
                spriteRenderer.color = _enrageColor;

            // Large hit spark to signal enrage
            _fx?.PlayHit(transform.position);
            Debug.Log($"[BossEnemy] Phase 3 — ENRAGE activated!");
        }
    }

    // ════════════════════════════════════════════════════════════════════════
    //  Charge attack coroutine
    // ════════════════════════════════════════════════════════════════════════

    private IEnumerator ChargeRoutine()
    {
        _isCharging    = true;
        _lastChargeTime = Time.time;
        lastAttackTime  = Time.time;

        // Trigger animations
        if (anim != null) { anim.SetTrigger(ANIM_CHARGE); anim.SetTrigger(ANIM_ATTACK); }
        limbs?.TriggerAttackSwing();

        float dir = player != null
            ? Mathf.Sign(player.position.x - transform.position.x)
            : (movingRight ? 1f : -1f);

        // Flash attack effect before dash
        Vector3 fxPos = transform.position + new Vector3(dir * 0.5f, 0.1f, 0f);
        _fx?.PlayAttack(fxPos, dir > 0f);

        float elapsed = 0f;
        float speed   = patrolSpeed * chargeSpeedMultiplier;

        while (elapsed < chargeDuration)
        {
            if (rb != null) rb.velocity = new Vector2(dir * speed, rb.velocity.y);
            elapsed += Time.deltaTime;
            yield return null;
        }

        // Slow down after charge
        if (rb != null) rb.velocity = new Vector2(0f, rb.velocity.y);

        _isCharging = false;
    }

    // ════════════════════════════════════════════════════════════════════════
    //  Ground slam (Phase 3) coroutine
    // ════════════════════════════════════════════════════════════════════════

    private IEnumerator GroundSlamRoutine()
    {
        _isSlamming  = true;
        _lastSlamTime = Time.time;
        lastAttackTime = Time.time;

        if (anim != null) anim.SetTrigger(ANIM_SLAM);

        // Brief hop: leap up
        if (rb != null) rb.velocity = new Vector2(rb.velocity.x, 6f);

        // Wait for peak of jump (~0.25 s)
        yield return new WaitForSeconds(0.25f);

        // Slam down fast
        if (rb != null) rb.velocity = new Vector2(0f, -18f);

        // Wait until landing (wait until velocity.y is near zero or negative close to ground)
        float waitMax = 0.5f;
        float waited  = 0f;
        while (waited < waitMax)
        {
            if (rb != null && rb.velocity.y >= -0.5f && rb.velocity.y <= 0.5f && waited > 0.1f) break;
            waited += Time.deltaTime;
            yield return null;
        }

        // On landing — AoE damage
        int finalDamage = equipment != null
            ? equipment.GetEffectiveDamage(slamDamage)
            : slamDamage;

        Collider2D[] hits = Physics2D.OverlapCircleAll(transform.position, slamRadius, playerLayer);
        foreach (var col in hits)
        {
            if (col == null) continue;
            var d = col.GetComponent<Damageable>();
            if (d == null || d == (Damageable)this) continue;
            d.TakeDamage(finalDamage);
        }

        // Large slam explosion effect
        _fx?.PlayDeath(transform.position); // reuse death effect for a big burst (won't destroy)
        // Note: _fx.PlayDeath detaches itself; we just use it visually

        _isSlamming = false;
    }

    // ════════════════════════════════════════════════════════════════════════
    //  Damage / death overrides
    // ════════════════════════════════════════════════════════════════════════

    public override void TakeDamage(int amount)
    {
        int finalAmount = amount;
        bool blocked    = false;

        if (equipment != null)
            finalAmount = equipment.GetDamageAfterArmor(amount, out blocked);

        if (blocked)
        {
            _fx?.PlayBlock(transform.position + Vector3.up * 0.4f);
            limbs?.TriggerHitRecoil();
            return;
        }

        base.TakeDamage(finalAmount);

        if (invulnerable) return;

        // Hit sparks
        _fx?.PlayHit(transform.position + Vector3.up * 0.3f);
        limbs?.TriggerHitRecoil();

        // Update health bar
        if (healthBar != null) healthBar.UpdateBar(currentHealth, maxHealth);

        // Knock Boss back only slightly (heavy)
        if (rb != null && player != null)
        {
            float dir = Mathf.Sign(transform.position.x - player.position.x);
            rb.velocity = new Vector2(dir * 1.5f, rb.velocity.y);   // minimal knockback
        }
    }

    protected override void Die()
    {
        // Stop any running coroutines
        if (_chargeCoroutine != null) StopCoroutine(_chargeCoroutine);

        // Death explosion (detaches from this GO automatically)
        _fx?.PlayDeath(transform.position);

        base.Die();
    }

    // ════════════════════════════════════════════════════════════════════════
    //  Gizmos
    // ════════════════════════════════════════════════════════════════════════

    protected override void OnDrawGizmosSelected()
    {
        base.OnDrawGizmosSelected();

        Gizmos.color = Color.magenta;
        Gizmos.DrawWireSphere(transform.position, chargeRange);

        Gizmos.color = new Color(1f, 0.5f, 0f, 0.5f);
        Gizmos.DrawWireSphere(transform.position, slamRadius);
    }
}
