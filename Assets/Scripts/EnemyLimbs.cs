using UnityEngine;

/// <summary>
/// EnemyLimbs — Lightweight procedural limb animation system for 2D enemies.
/// 
/// Design:
///   Each enemy prefab can have child GameObjects named:
///     "Body", "Head", "ArmL", "ArmR", "LegL", "LegR", "Weapon", "Shield"
///   This component locates those children and drives simple
///   procedural animations (bob, swing, recoil) WITHOUT requiring
///   a separate AnimationClip per limb.
/// 
/// If a named child does not exist, that limb slot is simply skipped —
/// the system degrades gracefully.
/// 
/// Limb Hierarchy (expected transform tree under the enemy root):
///   EnemyRoot
///   ├── Body          (torso pivot)
///   │   ├── Head      (head bob)
///   │   ├── ArmL      (left arm swing)
///   │   ├── ArmR      (right arm swing / weapon arm)
///   │   ├── Weapon    (sword / club parented to ArmR or Body)
///   │   └── Shield    (parented to ArmL or Body)
///   ├── LegL          (left leg stride)
///   └── LegR          (right leg stride)
/// </summary>
public class EnemyLimbs : MonoBehaviour
{
    // ─── Limb Transform references ───────────────────────────────────────
    [Header("Limb Transforms (auto-detected if left empty)")]
    public Transform body;
    public Transform head;
    public Transform armL;
    public Transform armR;
    public Transform legL;
    public Transform legR;
    [Tooltip("Weapon sprite transform (child of ArmR or Body)")]
    public Transform weapon;
    [Tooltip("Shield sprite transform (child of ArmL or Body)")]
    public Transform shield;

    // ─── Walk animation settings ─────────────────────────────────────────
    [Header("Walk Cycle")]
    [Tooltip("Leg swing angle (degrees) during walking")]
    public float legSwingAngle = 18f;
    [Tooltip("Arm swing angle (degrees) during walking")]
    public float armSwingAngle = 12f;
    [Tooltip("Body vertical bob amplitude (world units)")]
    public float bodyBobAmp = 0.05f;
    [Tooltip("Walk animation frequency (cycles per second)")]
    public float walkFrequency = 3f;

    // ─── Recoil/hit settings ─────────────────────────────────────────────
    [Header("Hit Recoil")]
    [Tooltip("Amount of rotation (degrees) applied to the body on hit")]
    public float hitRecoilAngle = 15f;
    [Tooltip("Duration (seconds) of the hit recoil spring-back")]
    public float hitRecoilDuration = 0.2f;

    // ─── Attack settings ─────────────────────────────────────────────────
    [Header("Attack Swing")]
    [Tooltip("Maximum weapon arm swing angle during attack")]
    public float attackSwingAngle = 60f;
    [Tooltip("Duration (seconds) of the weapon arm swing")]
    public float attackSwingDuration = 0.18f;

    // ─── Internal state ───────────────────────────────────────────────────
    private bool   _isWalking;
    private float  _walkPhase;
    private float  _hitRecoilTimer;
    private bool   _isAttacking;
    private float  _attackTimer;

    // Cached baseline local positions / rotations
    private Vector3    _bodyBasePos;
    private Quaternion _bodyBaseRot;
    private Quaternion _armRBaseRot;
    private Quaternion _armLBaseRot;
    private Quaternion _legLBaseRot;
    private Quaternion _legRBaseRot;

    // ─── Unity lifecycle ──────────────────────────────────────────────────
    private void Awake()
    {
        AutoFindLimbs();
        CacheBaselines();
    }

    private void LateUpdate()
    {
        // Update walk phase
        if (_isWalking)
            _walkPhase += Time.deltaTime * walkFrequency * Mathf.PI * 2f;

        // Update timers
        if (_hitRecoilTimer > 0f) _hitRecoilTimer -= Time.deltaTime;
        if (_attackTimer    > 0f) { _attackTimer  -= Time.deltaTime; if (_attackTimer <= 0f) _isAttacking = false; }

        // Apply all animations
        ApplyBodyBob();
        ApplyLegSwing();
        ApplyArmSwing();
        ApplyHitRecoil();
        ApplyAttackSwing();
    }

    // ─── Public API ───────────────────────────────────────────────────────

    /// <summary>Call every frame with current movement state.</summary>
    public void SetWalking(bool walking)
    {
        _isWalking = walking;
        if (!walking) _walkPhase = 0f;
    }

    /// <summary>Trigger a brief body recoil when the enemy is hit.</summary>
    public void TriggerHitRecoil()
    {
        _hitRecoilTimer = hitRecoilDuration;
    }

    /// <summary>Trigger the weapon-arm attack swing animation.</summary>
    public void TriggerAttackSwing()
    {
        _isAttacking = true;
        _attackTimer = attackSwingDuration;
    }

    // ─── Animation helpers ────────────────────────────────────────────────

    private void ApplyBodyBob()
    {
        if (body == null) return;
        float bob = _isWalking
            ? Mathf.Sin(_walkPhase) * bodyBobAmp
            : 0f;
        body.localPosition = _bodyBasePos + new Vector3(0f, bob, 0f);
    }

    private void ApplyLegSwing()
    {
        if (!_isWalking) return;
        float swing = Mathf.Sin(_walkPhase) * legSwingAngle;
        if (legL != null) legL.localRotation = _legLBaseRot * Quaternion.Euler(0f, 0f,  swing);
        if (legR != null) legR.localRotation = _legRBaseRot * Quaternion.Euler(0f, 0f, -swing);
    }

    private void ApplyArmSwing()
    {
        if (_isAttacking) return; // attack animation overrides arm swing
        float swing = _isWalking
            ? Mathf.Sin(_walkPhase) * armSwingAngle
            : 0f;
        // Opposite phase to legs for natural counter-swing
        if (armL != null) armL.localRotation = _armLBaseRot * Quaternion.Euler(0f, 0f, -swing);
        if (armR != null) armR.localRotation = _armRBaseRot * Quaternion.Euler(0f, 0f,  swing);
    }

    private void ApplyHitRecoil()
    {
        if (body == null || _hitRecoilTimer <= 0f) return;
        float t = _hitRecoilTimer / hitRecoilDuration;   // 1→0
        float angle = Mathf.Sin(t * Mathf.PI) * hitRecoilAngle;
        body.localRotation = _bodyBaseRot * Quaternion.Euler(0f, 0f, angle);
    }

    private void ApplyAttackSwing()
    {
        if (!_isAttacking || armR == null) return;
        float t       = 1f - (_attackTimer / attackSwingDuration); // 0→1 progress
        float angle   = Mathf.Sin(t * Mathf.PI) * attackSwingAngle;
        armR.localRotation = _armRBaseRot * Quaternion.Euler(0f, 0f, -angle);
        // Also swing the weapon if it exists
        if (weapon != null)
            weapon.localRotation = Quaternion.Euler(0f, 0f, -angle * 0.5f);
    }

    // ─── Setup helpers ────────────────────────────────────────────────────

    /// <summary>
    /// Auto-detect limb transforms by name among immediate children.
    /// Only fills slots that are currently null.
    /// </summary>
    private void AutoFindLimbs()
    {
        body   = body   ?? FindChildByName("Body");
        head   = head   ?? FindChildByName("Head");
        armL   = armL   ?? FindChildByName("ArmL");
        armR   = armR   ?? FindChildByName("ArmR");
        legL   = legL   ?? FindChildByName("LegL");
        legR   = legR   ?? FindChildByName("LegR");
        weapon = weapon ?? FindChildByName("Weapon");
        shield = shield ?? FindChildByName("Shield");
    }

    private Transform FindChildByName(string childName)
    {
        // Deep search (including nested children)
        return transform.Find(childName) ?? DeepFind(transform, childName);
    }

    private Transform DeepFind(Transform root, string name)
    {
        foreach (Transform child in root)
        {
            if (child.name == name) return child;
            var found = DeepFind(child, name);
            if (found != null) return found;
        }
        return null;
    }

    private void CacheBaselines()
    {
        if (body   != null) { _bodyBasePos = body.localPosition; _bodyBaseRot = body.localRotation; }
        if (armL   != null) _armLBaseRot = armL.localRotation;
        if (armR   != null) _armRBaseRot = armR.localRotation;
        if (legL   != null) _legLBaseRot = legL.localRotation;
        if (legR   != null) _legRBaseRot = legR.localRotation;
    }
}
