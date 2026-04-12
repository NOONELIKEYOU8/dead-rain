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
    public float invulnTime = 0.4f;

    protected SpriteRenderer spriteRenderer;
    private Coroutine invulnCoroutine;
    // 在对象死亡时触发（传递当前 Damageable 实例）
    public event Action<Damageable> OnDeath;

    protected virtual void Awake()
    {
        currentHealth = maxHealth;
        spriteRenderer = GetComponent<SpriteRenderer>();
    }

    public virtual void TakeDamage(int amount)
    {
        if (invulnerable) return;
        currentHealth -= amount;
        if (currentHealth <= 0)
        {
            Die();
        }
        else
        {
            if (invulnCoroutine != null) StopCoroutine(invulnCoroutine);
            invulnCoroutine = StartCoroutine(FlashRoutine());
        }
    }

    protected virtual IEnumerator FlashRoutine()
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
            yield return new WaitForSeconds(invulnTime / 8f);
            spriteRenderer.enabled = true;
            yield return new WaitForSeconds(invulnTime / 8f);
        }
        invulnerable = false;
    }

    protected virtual void Die()
    {
        OnDeath?.Invoke(this);
        Destroy(gameObject);
    }
}
