using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Stats : CoreComponent {
    public event Action OnHealthZero;
    public event Action<float, float> OnHealthChanged;
    
    [SerializeField] private float maxHealth;
    private float currentHealth;
    private bool isDead;

    public float MaxHealth => maxHealth;
    public float CurrentHealth => currentHealth;
    public float HealthPercent => maxHealth <= 0f ? 0f : currentHealth / maxHealth;
    public bool IsDead => isDead;

    protected override void Awake()
    {
        base.Awake();

        currentHealth = maxHealth;
        isDead = currentHealth <= 0f;
        OnHealthChanged?.Invoke(currentHealth, maxHealth);
    }

    public void DecreaseHealth(float amount)
    {
        if (amount <= 0f || isDead)
        {
            return;
        }

        currentHealth -= amount;

        if(currentHealth <= 0)
        {
            currentHealth = 0;
            isDead = true;
        }

        OnHealthChanged?.Invoke(currentHealth, maxHealth);

        if(isDead)
        {
            OnHealthZero?.Invoke();
        }
    }

    public void IncreaseHealth(float amount)
    {
        if (amount <= 0f || isDead)
        {
            return;
        }

        currentHealth = Mathf.Clamp(currentHealth + amount, 0, maxHealth);
        OnHealthChanged?.Invoke(currentHealth, maxHealth);
    }
}
