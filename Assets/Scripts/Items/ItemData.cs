using UnityEngine;

public enum ItemEffectType
{
    DamagePercent,
    CritChanceFlat,
    CritMultiplierPercent,
    OnKillHeal
}

[CreateAssetMenu(fileName = "ItemData", menuName = "DeadRain/Items/Item Data")]
public class ItemData : ScriptableObject
{
    public string itemId = "item_001";
    public string displayName = "New Item";
    [TextArea]
    public string description = "Item description";

    public ItemEffectType effectType = ItemEffectType.DamagePercent;
    public float effectValue = 0.1f;
    public int maxStack = 10;
}
