using UnityEngine;

public class DamageInterfaceExample : MonoBehaviour, IDamageable
{
    [SerializeField] private int maxHealth = 10;

    private int currentHealth;

    private void Awake()
    {
        currentHealth = maxHealth;
    }

    public void TakeDamage(int damage)
    {
        currentHealth -= damage;

        Debug.Log($"{name} 受到 {damage} 点伤害，剩余血量: {currentHealth}");

        if (currentHealth <= 0)
        {
            Die();
        }
    }

    private void Die()
    {
        Debug.Log($"{name} 死亡");
        Destroy(gameObject);
    }
}