using UnityEngine;

/// <summary>
/// EnemyEquipment — Equipment data container for enemies.
/// 
/// Defines the enemy's weapon and armor stats.
/// These values are read by MinionEnemy / BossEnemy to modify:
///   - Damage dealt  (weapon.damageBonus added to contactDamage)
///   - Damage taken  (armor.damageReduction subtracted, floored at 1)
///   - Attack range  (weapon.attackRange used for melee/ranged overlap)
///   - Visual tinting of the weapon/shield child sprites
/// 
/// How to add gear:
///   1. Attach this component to a Minion or Boss prefab.
///   2. Fill in WeaponData and ArmorData in the Inspector.
///   3. Optionally assign weaponTransform and shieldTransform to color them.
/// </summary>
public class EnemyEquipment : MonoBehaviour
{
    // ════════════════════════════════════════════════════════════════════════
    //  Data Structs
    // ════════════════════════════════════════════════════════════════════════

    [System.Serializable]
    public struct WeaponData
    {
        [Tooltip("Name of this weapon type (for debug / UI)")]
        public string weaponName;

        [Tooltip("Extra damage added on top of the enemy's base contactDamage")]
        public int damageBonus;

        [Tooltip("Melee attack reach in world units (OverlapCircle radius)")]
        public float attackRange;

        [Tooltip("Minimum time between attacks (seconds); overrides EnemyBase.attackInterval if > 0")]
        public float cooldownOverride;

        [Tooltip("Color tint applied to the Weapon child sprite")]
        public Color weaponTint;
    }

    [System.Serializable]
    public struct ArmorData
    {
        [Tooltip("Name of this armor type (for debug / UI)")]
        public string armorName;

        [Tooltip("Flat damage reduction per hit (e.g. 1 means every hit deals 1 less damage; minimum 1 always applied)")]
        public int damageReduction;

        [Tooltip("Fraction (0–1) of the time that incoming hits are blocked outright (no damage taken)")]
        [Range(0f, 1f)]
        public float blockChance;

        [Tooltip("Color tint applied to the Shield child sprite")]
        public Color armorTint;
    }

    // ════════════════════════════════════════════════════════════════════════
    //  Inspector Fields
    // ════════════════════════════════════════════════════════════════════════

    [Header("Weapon")]
    public WeaponData weapon = new WeaponData
    {
        weaponName      = "Claws",
        damageBonus     = 0,
        attackRange     = 0.6f,
        cooldownOverride = 0f,
        weaponTint      = Color.white
    };

    [Header("Armor")]
    public ArmorData armor = new ArmorData
    {
        armorName       = "None",
        damageReduction = 0,
        blockChance     = 0f,
        armorTint       = Color.white
    };

    [Header("Visual Overrides")]
    [Tooltip("Optional: the Weapon child Transform — its SpriteRenderer will be tinted.")]
    public Transform weaponTransform;
    [Tooltip("Optional: the Shield child Transform — its SpriteRenderer will be tinted.")]
    public Transform shieldTransform;

    // ════════════════════════════════════════════════════════════════════════
    //  Runtime
    // ════════════════════════════════════════════════════════════════════════

    private void Start()
    {
        ApplyVisualTints();
    }

    // ─── Public API ──────────────────────────────────────────────────────

    /// <summary>
    /// Returns the final effective attack damage given a base damage value.
    /// Adds weapon.damageBonus.
    /// </summary>
    public int GetEffectiveDamage(int baseDamage)
    {
        return Mathf.Max(1, baseDamage + weapon.damageBonus);
    }

    /// <summary>
    /// Returns the final damage taken after armor reduction.
    /// If a block roll succeeds, returns 0 (blocked) and fires the block flag.
    /// </summary>
    /// <param name="incomingDamage">Raw incoming damage amount.</param>
    /// <param name="blocked">Out: true if the hit was fully blocked by shield.</param>
    public int GetDamageAfterArmor(int incomingDamage, out bool blocked)
    {
        // Roll for block
        if (armor.blockChance > 0f && Random.value < armor.blockChance)
        {
            blocked = true;
            return 0;
        }
        blocked = false;
        int reduced = incomingDamage - armor.damageReduction;
        return Mathf.Max(1, reduced);
    }

    /// <summary>
    /// Returns the effective attack interval.
    /// Uses cooldownOverride if it is positive, otherwise returns the fallback value.
    /// </summary>
    public float GetEffectiveCooldown(float fallbackCooldown)
    {
        return weapon.cooldownOverride > 0f ? weapon.cooldownOverride : fallbackCooldown;
    }

    // ─── Internal helpers ────────────────────────────────────────────────

    private void ApplyVisualTints()
    {
        if (weaponTransform != null)
        {
            var sr = weaponTransform.GetComponent<SpriteRenderer>();
            if (sr != null) sr.color = weapon.weaponTint;
        }
        if (shieldTransform != null)
        {
            var sr = shieldTransform.GetComponent<SpriteRenderer>();
            if (sr != null) sr.color = armor.armorTint;
        }
    }
}
