using System;
using System.Collections;
using UnityEngine;

public class Damageable : MonoBehaviour
{
    [Header("Health")]
    public int maxHealth = 5;
    [HideInInspector]
    public int currentHealth;
    [HideInInspector]
    public bool invulnerable = false;
    [HideInInspector]
    public bool isParrying = false;
    public float invulnTime = 0.4f;

    protected SpriteRenderer spriteRenderer;
    private Coroutine invulnCoroutine;
    // 在对象死亡时触发（传递当前 Damageable 实例）
    public event Action<Damageable> OnDeath;
    // 触发招架时抛出（传递攻击来源和尝试造成的伤害量）
    public event Action<GameObject, int> OnParrySuccess;

    protected virtual void Awake()
    {
        currentHealth = maxHealth;
        spriteRenderer = GetComponent<SpriteRenderer>();
    }

    // 兼容旧调用：默认构建一个最小 CombatContext。
    public virtual void TakeDamage(int amount, GameObject attacker = null)
    {
        var ctx = CombatContext.Create(attacker, gameObject, amount, DamageType.Melee);
        TakeDamage(ctx, attacker);
    }

    // 新调用：统一从 CombatContext 进入结算链，供道具与难度系统改写。
    public virtual void TakeDamage(CombatContext ctx, GameObject attacker = null)
    {
        if (invulnerable) return;

        if (ctx.targetObject == null) ctx.targetObject = gameObject;
        if (ctx.targetId == 0) ctx.targetId = gameObject.GetInstanceID();
        if (attacker != null && ctx.attackerObject == null)
        {
            ctx.attackerObject = attacker;
            ctx.attackerId = attacker.GetInstanceID();
        }

        BattleEvents.RaiseBeforeDamageApplied(ref ctx);
        int finalDamage = Mathf.Max(0, Mathf.RoundToInt(ctx.baseDamage));

        if (isParrying)
        {
            // 触发招架事件，不扣血，且进入短时间无敌以防连续帧受到多次判定
            OnParrySuccess?.Invoke(attacker, finalDamage);
            BattleEvents.RaiseParrySuccess(gameObject, attacker, ctx);
            if (invulnCoroutine != null) StopCoroutine(invulnCoroutine);
            invulnCoroutine = StartCoroutine(FlashRoutine(0.2f)); // 短暂无敌
            return;
        }

        currentHealth -= finalDamage;
        if (currentHealth < 0) currentHealth = 0;

        BattleEvents.RaiseAfterDamageApplied(ctx, GetHealthSnapshot());

        if (currentHealth <= 0)
        {
            Die();
        }
        else
        {
            if (invulnCoroutine != null) StopCoroutine(invulnCoroutine);
            invulnCoroutine = StartCoroutine(FlashRoutine(invulnTime));
        }
    }

    public virtual void Heal(int amount)
    {
        if (amount <= 0) return;
        currentHealth = Mathf.Min(maxHealth, currentHealth + amount);
    }

    public HealthSnapshot GetHealthSnapshot()
    {
        return new HealthSnapshot
        {
            currentHealth = currentHealth,
            maxHealth = maxHealth,
            isInvulnerable = invulnerable,
            isParrying = isParrying
        };
    }

    protected virtual IEnumerator FlashRoutine(float overrideTime)
    {
        invulnerable = true;
        if (spriteRenderer == null)
        {
            invulnerable = false;
            yield break;
        }
        for (int i = 0; i < 4; i++)
        {
            spriteRenderer.enabled = false;
            yield return new WaitForSeconds(overrideTime / 8f);
            spriteRenderer.enabled = true;
            yield return new WaitForSeconds(overrideTime / 8f);
        }
        invulnerable = false;
    }

    protected virtual void Die()
    {
        OnDeath?.Invoke(this);
        Destroy(gameObject);
    }
}
