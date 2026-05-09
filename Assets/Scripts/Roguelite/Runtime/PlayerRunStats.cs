using UnityEngine;

public class PlayerRunStats : MonoBehaviour
{
    [SerializeField] private RunInventorySystem inventory;
    [SerializeField] private GameObject bonusProjectilePrefab;
    [SerializeField] private Transform bonusProjectileSpawn;
    [SerializeField] private LayerMask bonusProjectileTargetMask;

    private float lastHealOnKillTime = -999f;

    public float MeleeDamageMultiplier { get; private set; } = 1f;
    public float DashCooldownMultiplier { get; private set; } = 1f;
    public float HealOnKillAmount { get; private set; }
    public float HealOnKillCooldown { get; private set; }
    public float BonusProjectileChance { get; private set; }
    public float EraGrowthDamageMultiplier { get; private set; } = 1f;
    public int EraGrowthStacks { get; private set; }

    private void Awake()
    {
        if (inventory == null)
        {
            inventory = GetComponent<RunInventorySystem>();
        }
    }

    private void OnEnable()
    {
        if (inventory != null)
        {
            inventory.OnItemStackChanged += Recalculate;
        }

        if (GameRunManager.Instance != null)
        {
            GameRunManager.Instance.OnEnemyKilled += HandleEnemyKilled;
            GameRunManager.Instance.OnEraChanged += HandleEraChanged;
        }
    }

    private void Start()
    {
        Recalculate(null, 0);
    }

    private void OnDisable()
    {
        if (inventory != null)
        {
            inventory.OnItemStackChanged -= Recalculate;
        }

        if (GameRunManager.Instance != null)
        {
            GameRunManager.Instance.OnEnemyKilled -= HandleEnemyKilled;
            GameRunManager.Instance.OnEraChanged -= HandleEraChanged;
        }
    }

    public float ModifyMeleeDamage(float baseDamage)
    {
        return baseDamage * MeleeDamageMultiplier * EraGrowthDamageMultiplier;
    }

    public void TryFireBonusProjectile(Transform origin, int facingDirection)
    {
        if (bonusProjectilePrefab == null || Random.value > BonusProjectileChance)
        {
            return;
        }

        Transform spawn = bonusProjectileSpawn != null ? bonusProjectileSpawn : origin;
        GameObject projectileObject = Instantiate(bonusProjectilePrefab, spawn.position, Quaternion.identity);
        projectileObject.transform.right = Vector3.right * Mathf.Sign(facingDirection == 0 ? 1 : facingDirection);

        Projectile projectile = projectileObject.GetComponent<Projectile>();
        if (projectile != null)
        {
            projectile.FireProjectile(14f, 8f, 8f * EraGrowthDamageMultiplier, bonusProjectileTargetMask, transform.root);
        }
    }

    private void Recalculate(ItemData changedItem, int count)
    {
        MeleeDamageMultiplier = 1f;
        DashCooldownMultiplier = 1f;
        HealOnKillAmount = 0f;
        HealOnKillCooldown = 0f;
        BonusProjectileChance = 0f;

        if (inventory == null)
        {
            return;
        }

        foreach (RunInventorySystem.ItemStack stack in inventory.Stacks)
        {
            ItemEffectResolver.Apply(stack.item, stack.count, this);
        }
    }

    public void AddMeleeDamagePercent(float value)
    {
        MeleeDamageMultiplier += value;
    }

    public void AddDashCooldownReduction(float value)
    {
        DashCooldownMultiplier = Mathf.Clamp(1f - value, 0.35f, 1f);
    }

    public void SetHealOnKill(float amount, float cooldown)
    {
        HealOnKillAmount += amount;
        HealOnKillCooldown = Mathf.Max(HealOnKillCooldown, cooldown);
    }

    public void AddBonusProjectileChance(float value, float maxValue)
    {
        BonusProjectileChance = Mathf.Min(maxValue <= 0f ? 1f : maxValue, BonusProjectileChance + value);
    }

    public void AddEraGrowth(float value)
    {
        EraGrowthDamageMultiplier += value;
    }

    private void HandleEnemyKilled(EnemyRuntime enemy)
    {
        if (HealOnKillAmount <= 0f || Time.time < lastHealOnKillTime + HealOnKillCooldown)
        {
            return;
        }

        Stats stats = GetComponentInChildren<Stats>();
        if (stats != null)
        {
            stats.IncreaseHealth(HealOnKillAmount);
            lastHealOnKillTime = Time.time;
        }
    }

    private void HandleEraChanged(EraStageData era)
    {
        if (inventory == null)
        {
            return;
        }

        foreach (RunInventorySystem.ItemStack stack in inventory.Stacks)
        {
            if (stack.item != null && stack.item.effectType == ItemEffectType.EraAdvanceGrowth)
            {
                EraGrowthStacks += stack.count;
                AddEraGrowth(stack.item.baseValue + stack.item.stackValue * Mathf.Max(0, stack.count - 1));
            }
        }
    }
}
