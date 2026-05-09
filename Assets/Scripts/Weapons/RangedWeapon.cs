using UnityEngine;

public class RangedWeapon : Weapon
{
    [SerializeField] private Projectile projectilePrefab;
    [SerializeField] private float projectileSpeed = 14f;
    [SerializeField] private float projectileTravelDistance = 8f;
    [SerializeField] private float projectileDamage = 10f;
    [SerializeField] private Vector2 projectileSpawnOffset = new Vector2(0.75f, 0f);
    [SerializeField] private LayerMask targetMask;

    private Movement Movement { get => movement ??= core.GetCoreComponent<Movement>(); }
    private Movement movement;

    public override void AnimationActionTrigger()
    {
        base.AnimationActionTrigger();

        if (projectilePrefab == null)
        {
            Debug.LogWarning($"{name} has no projectile prefab assigned.");
            return;
        }

        int facingDirection = Movement != null ? Movement.FacingDirection : 1;
        Vector3 offset = new Vector3(projectileSpawnOffset.x * facingDirection, projectileSpawnOffset.y, 0f);
        Quaternion rotation = facingDirection >= 0 ? Quaternion.identity : Quaternion.Euler(0f, 180f, 0f);
        Projectile projectile = Instantiate(projectilePrefab, transform.position + offset, rotation);
        projectile.FireProjectile(projectileSpeed, projectileTravelDistance, projectileDamage, targetMask, transform.root);

        PlayerRunStats runStats = core != null ? core.GetComponentInParent<PlayerRunStats>() : null;
        runStats?.TryFireBonusProjectile(transform, facingDirection);
    }
}
