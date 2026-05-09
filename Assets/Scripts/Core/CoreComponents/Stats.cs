using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Stats : CoreComponent {
    public event Action OnHealthZero;
    public event Action<float, float> OnHealthChanged;
    
    [SerializeField] private float maxHealth;
    private float currentHealth;

    public float MaxHealth => maxHealth;
    public float CurrentHealth => currentHealth;
    public float HealthPercent => maxHealth <= 0f ? 0f : currentHealth / maxHealth;

    protected override void Awake()
    {
        base.Awake();

        currentHealth = maxHealth;
        OnHealthChanged?.Invoke(currentHealth, maxHealth);
    }

    public void DecreaseHealth(float amount)
    {
        if (amount <= 0f || currentHealth <= 0f)
        {
            return;
        }

        currentHealth -= amount;

        if(currentHealth <= 0)
        {
            currentHealth = 0;
        }

        OnHealthChanged?.Invoke(currentHealth, maxHealth);

        if(currentHealth <= 0)
        {
            OnHealthZero?.Invoke();
            
            Debug.Log("Health is zero!!");
        }
    }

    public void IncreaseHealth(float amount)
    {
        if (amount <= 0f || currentHealth <= 0f)
        {
            return;
        }

        currentHealth = Mathf.Clamp(currentHealth + amount, 0, maxHealth);
        OnHealthChanged?.Invoke(currentHealth, maxHealth);
    }
}
