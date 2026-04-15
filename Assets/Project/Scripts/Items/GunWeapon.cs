using UnityEngine;

/// <summary>
/// 手枪示例：使用 2D 射线进行瞬发命中判定。
/// </summary>
public class GunWeapon : Weapon
{
    [Header("Gun")]
    [SerializeField] private LayerMask targetLayers = ~0;
    [SerializeField] private float fireOffsetX = 0.6f;
    [SerializeField] private float fireOffsetY = 0.2f;
    [SerializeField] private GameObject hitEffectPrefab;
    [SerializeField] private bool drawDebugRay = true;
    [SerializeField] private float debugRayDuration = 0.08f;

    [Header("Lifecycle")]
    [SerializeField] private bool destroyAfterAttack = false;

    public override bool Attack(GameObject attacker)
    {
        if (attacker == null)
        {
            return false;
        }

        float facing = attacker.transform.localScale.x >= 0f ? 1f : -1f;
        Vector2 origin = (Vector2)attacker.transform.position + new Vector2(fireOffsetX * facing, fireOffsetY);
        Vector2 direction = new Vector2(facing, 0f);

        RaycastHit2D hit = Physics2D.Raycast(origin, direction, attackRange, targetLayers);

        if (drawDebugRay)
        {
            Vector2 end = hit.collider != null ? hit.point : origin + direction * attackRange;
            Debug.DrawLine(origin, end, Color.cyan, debugRayDuration);
        }

        if (hit.collider != null)
        {
            if (hitEffectPrefab != null)
            {
                Instantiate(hitEffectPrefab, hit.point, Quaternion.identity);
            }

            IDamageable idmg = hit.collider.GetComponentInParent<IDamageable>();
            if (idmg != null)
            {
                idmg.TakeDamage(damage);
            }
            else
            {
                Damageable legacyDamageable = hit.collider.GetComponentInParent<Damageable>();
                if (legacyDamageable != null)
                {
                    legacyDamageable.TakeDamage(damage, attacker);
                }
            }
        }

        if (destroyAfterAttack)
        {
            Destroy(gameObject);
        }

        return true;
    }

    protected override void OnDrawGizmosSelected()
    {
        base.OnDrawGizmosSelected();

        Gizmos.color = Color.cyan;
        Gizmos.DrawLine(transform.position, transform.position + Vector3.right * attackRange);
    }
}
