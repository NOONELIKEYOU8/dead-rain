using UnityEngine;

public class SimpleProjectile : MonoBehaviour
{
    public float lifeTime = 2f;

    private GameObject owner;
    private float baseDamage;
    private Vector2 direction;
    private float speed;
    private LayerMask targetLayers;
    private DamageType damageType;
    private string sourceTag;
    private bool initialized;

    private Rigidbody2D rb;

    private void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
    }

    public void Initialize(
        GameObject ownerObject,
        float damage,
        Vector2 moveDirection,
        float moveSpeed,
        LayerMask hitLayers,
        DamageType type,
        string tag)
    {
        owner = ownerObject;
        baseDamage = Mathf.Max(0f, damage);
        direction = moveDirection.normalized;
        speed = Mathf.Max(0f, moveSpeed);
        targetLayers = hitLayers;
        damageType = type;
        sourceTag = string.IsNullOrEmpty(tag) ? "Projectile" : tag;
        initialized = true;

        Destroy(gameObject, lifeTime);
    }

    private void Update()
    {
        if (!initialized) return;

        Vector2 delta = direction * speed * Time.deltaTime;
        if (rb != null)
        {
            rb.MovePosition(rb.position + delta);
        }
        else
        {
            transform.position += (Vector3)delta;
        }
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (!initialized) return;
        if (owner != null && other.gameObject == owner) return;

        if ((targetLayers.value & (1 << other.gameObject.layer)) == 0) return;

        var d = other.GetComponentInParent<Damageable>();
        if (d == null) return;

        var ctx = CombatContext.Create(owner, d.gameObject, baseDamage, damageType, sourceTag);
        ctx.hitPoint = other.ClosestPoint(transform.position);

        BattleEvents.RaiseAttackStarted(ctx);
        d.TakeDamage(ctx, owner);
        BattleEvents.RaiseAttackResolved(ctx);

        Destroy(gameObject);
    }
}
