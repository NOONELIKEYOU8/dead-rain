using UnityEngine;

[CreateAssetMenu(fileName = "NewItemData", menuName = "Dead Rain/Roguelite/Item")]
public class ItemData : ScriptableObject
{
    [Header("Identity")]
    public string id;
    public string displayName;
    [TextArea] public string description;
    public EraId era;
    public ContentTier tier = ContentTier.Common;

    [Header("Effect")]
    public ItemEffectType effectType;
    public float baseValue;
    public float stackValue;
    public float maxValue;
    public float triggerCooldown;
    public ItemTriggerEvent triggerEvents = ItemTriggerEvent.Passive;
    public bool repeatable = true;

    [Header("Common Data Fields")]
    public float hp;
    public float damage;
    public float moveSpeed;
    public float defense;
    public float critChance;
    public float cooldownReduction;
    public EnemyAttackPattern attackPattern;
    public string prefabPathPlaceholder;
    public Sprite iconPlaceholder;
    public bool stackable = true;
    [TextArea] public string specialMechanics;
}
