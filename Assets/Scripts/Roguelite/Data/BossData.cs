using UnityEngine;

[CreateAssetMenu(fileName = "NewBossData", menuName = "Dead Rain/Roguelite/Boss")]
public class BossData : ScriptableObject
{
    [Header("Identity")]
    public string id;
    public string displayName;
    [TextArea] public string description;
    public EraId era;
    public ContentTier tier = ContentTier.Boss;

    [Header("Stats")]
    public float hp = 300f;
    public float damage = 18f;
    public float moveSpeed = 3f;
    public EnemyAttackPattern attackPattern = EnemyAttackPattern.BossPhase;
    public int phaseCount = 2;
    public float[] phaseHealthPercentThresholds = { 0.5f };
    public float baseSkillCooldown = 2.5f;
    public float phaseTwoCooldownMultiplier = 0.65f;
    public BossAttackDefinition[] attacks;
    public EnemyData[] summonPool;
    public ItemData[] rewardPool;
    [TextArea] public string introText;

    [Header("Runtime")]
    public GameObject prefab;
    public string prefabPathPlaceholder;
    public Sprite iconPlaceholder;
    [TextArea] public string specialMechanics;
}

[System.Serializable]
public class BossAttackDefinition
{
    public string id;
    public string displayName;
    public BossAttackType attackType;
    public float damageMultiplier = 1f;
    public float cooldown = 2.5f;
    public float range = 2f;
    public GameObject prefab;
    public EnemyData[] summonPool;
    [TextArea] public string telegraphText;
}
