using UnityEngine;

[RequireComponent(typeof(Rigidbody2D))]
public class SimpleEnemy : Damageable
{
    [Header("Movement")]
    public float patrolSpeed = 1.5f;
    public Transform leftLimit;
    public Transform rightLimit;

    [Header("AI")]
    public float chaseRange = 4f;
    public LayerMask playerLayer;
    public int contactDamage = 1;
    public float attackInterval = 1f;

    Rigidbody2D rb;
    Transform player;
    bool movingRight = true;
    float lastAttackTime = -999f;

    protected override void Awake()
    {
        base.Awake();
        rb = GetComponent<Rigidbody2D>();
    }

    void Start()
    {
        var pgo = GameObject.FindWithTag("Player");
        if (pgo != null) player = pgo.transform;
    }

    void Update()
    {
        if (player != null && Vector2.Distance(transform.position, player.position) <= chaseRange)
        {
            float dir = Mathf.Sign(player.position.x - transform.position.x);
            rb.velocity = new Vector2(dir * patrolSpeed, rb.velocity.y);
            transform.localScale = new Vector3(Mathf.Sign(dir), 1, 1);
        }
        else
        {
            Patrol();
        }
    }

    void Patrol()
    {
        if (leftLimit == null || rightLimit == null)
        {
            rb.velocity = new Vector2((movingRight ? 1 : -1) * patrolSpeed, rb.velocity.y);
            return;
        }
        float dir = movingRight ? 1f : -1f;
        rb.velocity = new Vector2(dir * patrolSpeed, rb.velocity.y);
        transform.localScale = new Vector3(movingRight ? 1f : -1f, 1f, 1f);

        if (movingRight && transform.position.x >= rightLimit.position.x) movingRight = false;
        else if (!movingRight && transform.position.x <= leftLimit.position.x) movingRight = true;
    }

    void OnCollisionEnter2D(Collision2D col)
    {
        if ((playerLayer.value & (1 << col.gameObject.layer)) != 0)
        {
            if (Time.time >= lastAttackTime + attackInterval)
            {
                var d = col.gameObject.GetComponent<Damageable>();
                if (d != null) d.TakeDamage(contactDamage);
                lastAttackTime = Time.time;
            }
        }
        else
        {
            movingRight = !movingRight;
        }
    }
}
