using UnityEngine;

[CreateAssetMenu(fileName = "NewDifficultyScalingData", menuName = "Dead Rain/Roguelite/Difficulty Scaling")]
public class DifficultyScalingData : ScriptableObject
{
    [Header("Identity")]
    public string id = "default_difficulty";
    public string displayName = "Default Difficulty Scaling";
    [TextArea] public string description;
    public EraId era;

    [Header("Formula")]
    public float baseDifficulty = 1f;
    public float timeScale = 0.018f;
    public float eraScale = 0.65f;
    public float multiplierPerDifficulty = 0.12f;

    [Header("Spawn Pressure")]
    public float baseSpawnInterval = 5f;
    public float minSpawnInterval = 1.4f;
    public float spawnIntervalDifficultyFactor = 0.08f;
    public int maxAliveEnemies = 8;
    public int baseSpawnBudget = 3;
    public float spawnBudgetPerMinute = 0.75f;
    public int maxSpawnBudget = 18;

    [Header("Elite And Drops")]
    public float baseEliteChance = 0.02f;
    public float eliteChancePerMinute = 0.015f;
    public float maxEliteChance = 0.35f;
    public float baseDropChance = 0.12f;
    public float dropChanceDifficultyBonus = 0.01f;
    public float maxDropChance = 0.45f;

    [Header("Enemy Scaling")]
    public float enemyDamageMultiplierPerDifficulty = 0.08f;
    public float enemyHealthMultiplierPerDifficulty = 0.12f;

    [Header("Boss Trigger")]
    public BossTriggerCondition bossTriggerCondition = BossTriggerCondition.KillCount;
    public int bossKillCountThreshold = 6;
    public float bossElapsedTimeThreshold = 240f;

    [Header("Common Data Fields")]
    public float hp;
    public float damage;
    public float moveSpeed;
    public EnemyAttackPattern attackPattern;
    public string prefabPathPlaceholder;
    public Sprite iconPlaceholder;
    public ContentTier tier = ContentTier.Common;
    [TextArea] public string specialMechanics;
}
