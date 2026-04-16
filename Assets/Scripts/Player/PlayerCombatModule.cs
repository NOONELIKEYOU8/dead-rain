using System.Collections;
using UnityEngine;

[RequireComponent(typeof(PlayerController))]
public class PlayerCombatModule : MonoBehaviour
{
    [Header("Melee")]
    public Transform meleePoint;
    public float meleeRange = 0.8f;
    public LayerMask meleeTargetLayers = ~0;
    public int[] comboDamages = { 1, 1, 2 };
    public float comboResetTime = 0.8f;
    public float meleeLockTime = 0.1f;

    [Header("Ranged")]
    public bool enableRanged = true;
    public SimpleProjectile projectilePrefab;
    public Vector3 projectileSpawnOffset = new Vector3(0.6f, 0.1f, 0f);
    public float projectileSpeed = 8f;
    public int projectileDamage = 1;
    public float projectileCooldown = 0.75f;

    private PlayerController controller;
    private Damageable selfDamageable;

    private int comboIndex;
    private float comboExpiresAt;
    private float nextProjectileTime;
    private bool inMeleeLock;

    private void Awake()
    {
        controller = GetComponent<PlayerController>();
        selfDamageable = GetComponent<Damageable>();

        if (meleePoint == null)
        {
            var go = new GameObject("MeleePoint");
            go.transform.SetParent(transform);
            go.transform.localPosition = new Vector3(0.6f, 0f, 0f);
            meleePoint = go.transform;
        }
    }

    private void OnEnable()
    {
        if (controller == null) return;
        controller.OnPrimaryAttackInput += HandlePrimaryAttack;
        controller.OnSkill1Input += HandleRangedAttack;
    }

    private void OnDisable()
    {
        if (controller == null) return;
        controller.OnPrimaryAttackInput -= HandlePrimaryAttack;
        controller.OnSkill1Input -= HandleRangedAttack;
    }

    private void HandlePrimaryAttack()
    {
        if (inMeleeLock) return;
        StartCoroutine(MeleeRoutine());
    }

    private IEnumerator MeleeRoutine()
    {
        inMeleeLock = true;

        if (Time.time > comboExpiresAt) comboIndex = 0;
        int idx = Mathf.Clamp(comboIndex, 0, comboDamages.Length - 1);
        int damage = comboDamages[idx];
        comboIndex = (comboIndex + 1) % comboDamages.Length;
        comboExpiresAt = Time.time + comboResetTime;

        PerformMeleeHit(damage);

        yield return new WaitForSeconds(meleeLockTime);
        inMeleeLock = false;
    }

    private void PerformMeleeHit(int damage)
    {
        if (meleePoint == null) return;

        Collider2D[] hits = Physics2D.OverlapCircleAll(meleePoint.position, meleeRange, meleeTargetLayers);

        var openCtx = CombatContext.Create(gameObject, null, damage, DamageType.Melee, "PlayerMelee");
        BattleEvents.RaiseAttackStarted(openCtx);

        for (int i = 0; i < hits.Length; i++)
        {
            var d = hits[i] != null ? hits[i].GetComponentInParent<Damageable>() : null;
            if (d == null || d == selfDamageable) continue;

            var ctx = openCtx;
            ctx.targetObject = d.gameObject;
            ctx.targetId = d.gameObject.GetInstanceID();
            ctx.hitPoint = hits[i].ClosestPoint(meleePoint.position);

            d.TakeDamage(ctx, gameObject);
            BattleEvents.RaiseAttackResolved(ctx);
        }
    }

    private void HandleRangedAttack()
    {
        if (!enableRanged) return;
        if (Time.time < nextProjectileTime) return;

        nextProjectileTime = Time.time + projectileCooldown;
        SpawnProjectile();
    }

    private void SpawnProjectile()
    {
        float dir = transform.localScale.x >= 0f ? 1f : -1f;
        Vector3 spawnPos = transform.position + new Vector3(projectileSpawnOffset.x * dir, projectileSpawnOffset.y, 0f);

        SimpleProjectile projectile;
        if (projectilePrefab != null)
        {
            projectile = Instantiate(projectilePrefab, spawnPos, Quaternion.identity);
        }
        else
        {
            projectile = CreateRuntimeProjectile(spawnPos);
        }

        if (projectile == null) return;

        projectile.Initialize(
            gameObject,
            projectileDamage,
            new Vector2(dir, 0f),
            projectileSpeed,
            meleeTargetLayers,
            DamageType.Projectile,
            "PlayerRanged");
    }

    private SimpleProjectile CreateRuntimeProjectile(Vector3 spawnPos)
    {
        var go = new GameObject("RuntimeProjectile");
        go.transform.position = spawnPos;

        var rb = go.AddComponent<Rigidbody2D>();
        rb.gravityScale = 0f;
        rb.isKinematic = true;

        var col = go.AddComponent<CircleCollider2D>();
        col.isTrigger = true;
        col.radius = 0.12f;

        return go.AddComponent<SimpleProjectile>();
    }

    private void OnDrawGizmosSelected()
    {
        if (meleePoint == null) return;
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(meleePoint.position, meleeRange);
    }
}
