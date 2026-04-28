using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Linq;

public class AggressiveWeapon : Weapon
{
    private Movement Movement { get => movement ??= core.GetCoreComponent<Movement>(); }
    private Movement movement;

    protected SO_AggressiveWeaponData aggressiveWeaponData;

    private List<IDamageable> detectedDamageables = new();
    private List<IKnockbackable> detectedKnockbackables = new();

    protected override void Awake()
    {
        base.Awake();

        if(weaponData.GetType() == typeof(SO_AggressiveWeaponData))
        {
            aggressiveWeaponData = (SO_AggressiveWeaponData)weaponData;
        }
        else
        {
            Debug.LogError("Wrong data for the weapon");
        }
    }

    public override void AnimationActionTrigger()
    {
        base.AnimationActionTrigger();

        CheckMeleeAttack();
    }

    private void CheckMeleeAttack()
    {
        WeaponAttackDetails details = aggressiveWeaponData.AttackDetails[attackCounter];

        foreach (IDamageable item in detectedDamageables.ToList())
        {
            item.Damage(details.damageAmount);
        }

        foreach (IKnockbackable item in detectedKnockbackables.ToList())
        {
            item.Knockback(details.knockbackAngle, details.knockbackStrength, Movement.FacingDirection);
        }
    }

    public void AddToDetected(Collider2D collision)
    {
        if (collision.TryGetComponent(out IDamageable damageable))
        {
            detectedDamageables.Add(damageable);
        }

        if (collision.TryGetComponent(out IKnockbackable knockbackable ))
        {
            detectedKnockbackables.Add(knockbackable);
        }
    } 

    public void RemoveFromDetected(Collider2D collision)
    {
        if (collision.TryGetComponent(out IDamageable damageable))
        {
            detectedDamageables.Remove(damageable);
        }

        if (collision.TryGetComponent(out IKnockbackable knockbackable))
        {
            detectedKnockbackables.Remove(knockbackable);
        }
    }
}
