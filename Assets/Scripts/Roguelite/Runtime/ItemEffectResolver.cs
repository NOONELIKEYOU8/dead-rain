using UnityEngine;

public static class ItemEffectResolver
{
    public static void Apply(ItemData item, int stackCount, PlayerRunStats target)
    {
        if (item == null || target == null || stackCount <= 0)
        {
            return;
        }

        float value = item.baseValue + item.stackValue * Mathf.Max(0, stackCount - 1);

        switch (item.effectType)
        {
            case ItemEffectType.MeleeDamagePercent:
                target.AddMeleeDamagePercent(value);
                break;
            case ItemEffectType.DashCooldownPercent:
                target.AddDashCooldownReduction(value);
                break;
            case ItemEffectType.HealOnKill:
                target.SetHealOnKill(value, item.triggerCooldown);
                break;
            case ItemEffectType.BonusProjectileChance:
                target.AddBonusProjectileChance(value, item.maxValue);
                break;
            case ItemEffectType.EraAdvanceGrowth:
                break;
        }
    }
}
