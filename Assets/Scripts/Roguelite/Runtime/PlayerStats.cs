using UnityEngine;

[RequireComponent(typeof(RunInventorySystem))]
public class PlayerStats : MonoBehaviour
{
    [SerializeField] private RunInventorySystem inventory;
    [SerializeField] private PlayerRunStats runStats;
    [SerializeField] private float baseAttack = 1f;
    [SerializeField] private float baseDefense;
    [SerializeField] private float baseMoveSpeed = 1f;
    [SerializeField] private float baseCritChance;
    [SerializeField] private float baseSkillCooldownMultiplier = 1f;

    public float AttackMultiplier { get; private set; } = 1f;
    public float DefenseMultiplier { get; private set; } = 1f;
    public float MoveSpeedMultiplier { get; private set; } = 1f;
    public float CritChance { get; private set; }
    public float SkillCooldownMultiplier { get; private set; } = 1f;

    public float FinalAttack => baseAttack * AttackMultiplier;
    public float FinalDefense => baseDefense * DefenseMultiplier;
    public float FinalMoveSpeed => baseMoveSpeed * MoveSpeedMultiplier;

    private void Awake()
    {
        if (inventory == null)
        {
            inventory = GetComponent<RunInventorySystem>();
        }

        if (runStats == null)
        {
            runStats = GetComponent<PlayerRunStats>();
        }
    }

    private void OnEnable()
    {
        if (inventory != null)
        {
            inventory.OnItemStackChanged += Recalculate;
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
    }

    private void Recalculate(ItemData changedItem, int count)
    {
        AttackMultiplier = runStats != null ? runStats.MeleeDamageMultiplier : 1f;
        DefenseMultiplier = 1f;
        MoveSpeedMultiplier = 1f;
        CritChance = baseCritChance;
        SkillCooldownMultiplier = baseSkillCooldownMultiplier;

        if (inventory == null)
        {
            return;
        }

        foreach (RunInventorySystem.ItemStack stack in inventory.Stacks)
        {
            ApplyItem(stack.item, stack.count);
        }
    }

    private void ApplyItem(ItemData item, int stackCount)
    {
        if (item == null || stackCount <= 0)
        {
            return;
        }

        float value = item.baseValue + item.stackValue * Mathf.Max(0, stackCount - 1);
        switch (item.effectType)
        {
            case ItemEffectType.DefensePercent:
                DefenseMultiplier += value;
                break;
            case ItemEffectType.MoveSpeedPercent:
                MoveSpeedMultiplier += value;
                break;
            case ItemEffectType.CritChancePercent:
                CritChance = Mathf.Clamp01(CritChance + value);
                break;
            case ItemEffectType.SkillCooldownPercent:
                SkillCooldownMultiplier = Mathf.Clamp(1f - value, 0.25f, 1f);
                break;
        }
    }
}
