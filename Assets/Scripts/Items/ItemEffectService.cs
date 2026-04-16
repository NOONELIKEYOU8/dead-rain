using UnityEngine;

public class ItemEffectService : MonoBehaviour, IItemEffectService
{
    public RunInventory inventory;
    public ItemData[] itemCatalog;

    private Damageable ownerDamageable;

    private void Awake()
    {
        if (inventory == null) inventory = GetComponent<RunInventory>();
        ownerDamageable = GetComponent<Damageable>();
    }

    private void OnEnable()
    {
        BattleEvents.OnBeforeDamageApplied += HandleBeforeDamageApplied;
        BattleEvents.OnEnemyDied += HandleEnemyDied;
    }

    private void OnDisable()
    {
        BattleEvents.OnBeforeDamageApplied -= HandleBeforeDamageApplied;
        BattleEvents.OnEnemyDied -= HandleEnemyDied;
    }

    public void ApplyItem(string itemId, int stackDelta)
    {
        if (inventory == null || stackDelta <= 0) return;
        ItemData item = FindItem(itemId);
        if (item == null) return;

        inventory.AddItem(item, stackDelta);
    }

    public void RemoveItem(string itemId, int stackDelta)
    {
        if (inventory == null || stackDelta <= 0) return;
        ItemData item = FindItem(itemId);
        if (item == null) return;

        inventory.RemoveItem(item, stackDelta);
    }

    public void EvaluateDamageModifiers(ref CombatContext ctx)
    {
        if (inventory == null) return;

        var entries = inventory.Entries;
        for (int i = 0; i < entries.Count; i++)
        {
            var entry = entries[i];
            if (entry == null || entry.item == null || entry.stack <= 0) continue;

            float totalValue = entry.item.effectValue * entry.stack;
            switch (entry.item.effectType)
            {
                case ItemEffectType.DamagePercent:
                    ctx.baseDamage *= 1f + totalValue;
                    break;
                case ItemEffectType.CritChanceFlat:
                    ctx.critChance += totalValue;
                    break;
                case ItemEffectType.CritMultiplierPercent:
                    ctx.critMultiplier *= 1f + totalValue;
                    break;
            }
        }
    }

    private void HandleBeforeDamageApplied(ref CombatContext ctx)
    {
        if (ctx.attackerObject != gameObject) return;
        EvaluateDamageModifiers(ref ctx);
    }

    private void HandleEnemyDied(GameObject enemy, CombatContext killingBlow)
    {
        if (ownerDamageable == null || inventory == null) return;
        if (killingBlow.attackerObject != gameObject) return;

        int totalHeal = 0;
        var entries = inventory.Entries;
        for (int i = 0; i < entries.Count; i++)
        {
            var entry = entries[i];
            if (entry == null || entry.item == null || entry.stack <= 0) continue;
            if (entry.item.effectType != ItemEffectType.OnKillHeal) continue;

            totalHeal += Mathf.RoundToInt(entry.item.effectValue * entry.stack);
        }

        if (totalHeal > 0) ownerDamageable.Heal(totalHeal);
    }

    private ItemData FindItem(string itemId)
    {
        if (string.IsNullOrEmpty(itemId) || itemCatalog == null) return null;
        for (int i = 0; i < itemCatalog.Length; i++)
        {
            if (itemCatalog[i] != null && itemCatalog[i].itemId == itemId)
            {
                return itemCatalog[i];
            }
        }
        return null;
    }
}
