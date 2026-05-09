using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Combat : CoreComponent, IDamageable,IKnockbackable
{
    [SerializeField] private GameObject damageParticles;
    [SerializeField] private float invulnerabilityTime = 0.15f;
    [SerializeField] private float hitFlashTime = 0.08f;
    [SerializeField] private Color hitFlashColor = Color.white;

    private Movement Movement { get => movement ??= core.GetCoreComponent<Movement>(); }
    private CollisionSenses CollisionSenses { get => collisionSenses ??= core.GetCoreComponent<CollisionSenses>(); }
    private Stats Stats { get => stats ??= core.GetCoreComponent<Stats>(); }
    private ParticleManager ParticleManager => particleManager ??= core.GetCoreComponent<ParticleManager>();

    private Movement movement;
    private CollisionSenses collisionSenses;
    private Stats stats;
    private ParticleManager particleManager;

    [SerializeField] private float maxKnockbackTime = 0.2f;

    private bool isKnockbackActive;
    private float knockbackStartTime;
    private float lastDamageTime = -999f;
    private SpriteRenderer[] spriteRenderers;
    private Color[] originalColors;
    private Coroutine hitFlashRoutine;

    protected override void Awake()
    {
        base.Awake();

        spriteRenderers = core.transform.parent.GetComponentsInChildren<SpriteRenderer>(true);
        originalColors = new Color[spriteRenderers.Length];

        for (int i = 0; i < spriteRenderers.Length; i++)
        {
            originalColors[i] = spriteRenderers[i].color;
        }
    }

    public override void LogicUpdate()
    {
        CheckKnockback();
    }

    public void Damage(float amount)
    {
        if (!isActiveAndEnabled || !gameObject.activeInHierarchy || Stats == null || Stats.IsDead || Time.time < lastDamageTime + invulnerabilityTime)
        {
            return;
        }

        lastDamageTime = Time.time;
        Stats?.DecreaseHealth(amount);
        ParticleManager?.StartParticlesWithRandomRotation(damageParticles);
        PlayHitFlash();
    }

    public void Knockback(Vector2 angle, float strength, int direction)
    {
        if (Stats != null && Stats.IsDead)
        {
            return;
        }

        Movement?.SetVelocity(strength, angle, direction);
        Movement.CanSetVelocity = false;
        isKnockbackActive = true;
        knockbackStartTime = Time.time;
    }

    private void CheckKnockback()
    {
        if (isKnockbackActive && ((Movement?.CurrentVelocity.y <= 0.01f && CollisionSenses.Ground) || Time.time >= knockbackStartTime + maxKnockbackTime))
        {
            isKnockbackActive = false;
            Movement.CanSetVelocity = true;
        }
    }

    private void PlayHitFlash()
    {
        if (!isActiveAndEnabled || !gameObject.activeInHierarchy || spriteRenderers == null || spriteRenderers.Length == 0)
        {
            return;
        }

        if (hitFlashRoutine != null)
        {
            StopCoroutine(hitFlashRoutine);
        }

        hitFlashRoutine = StartCoroutine(HitFlashRoutine());
    }

    private IEnumerator HitFlashRoutine()
    {
        for (int i = 0; i < spriteRenderers.Length; i++)
        {
            if (spriteRenderers[i] != null)
            {
                spriteRenderers[i].color = hitFlashColor;
            }
        }

        yield return new WaitForSeconds(hitFlashTime);

        for (int i = 0; i < spriteRenderers.Length; i++)
        {
            if (spriteRenderers[i] != null)
            {
                spriteRenderers[i].color = originalColors[i];
            }
        }

        hitFlashRoutine = null;
    }
}
