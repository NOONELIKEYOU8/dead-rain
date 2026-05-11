using UnityEngine;

public class DifficultyService
{
    private readonly DifficultyScalingData data;

    public DifficultyService(DifficultyScalingData data)
    {
        this.data = data;
    }

    public float GetDifficultyLevel(float elapsedSeconds, int clearedEraCount)
    {
        if (data == null)
        {
            return 1f;
        }

        return data.baseDifficulty + elapsedSeconds * data.timeScale + clearedEraCount * data.eraScale;
    }

    public float GetDifficultyMultiplier(float elapsedSeconds, int clearedEraCount)
    {
        if (data == null)
        {
            return 1f;
        }

        float level = GetDifficultyLevel(elapsedSeconds, clearedEraCount);
        return Mathf.Max(1f, 1f + (level - data.baseDifficulty) * data.multiplierPerDifficulty);
    }

    public int GetSpawnBudget(float elapsedSeconds, int clearedEraCount)
    {
        if (data == null)
        {
            return 3;
        }

        float minutes = elapsedSeconds / 60f;
        int budget = Mathf.RoundToInt(data.baseSpawnBudget + minutes * data.spawnBudgetPerMinute + clearedEraCount);
        return Mathf.Clamp(budget, 1, Mathf.Max(1, data.maxSpawnBudget));
    }

    public float GetEnemyDamageMultiplier(float elapsedSeconds, int clearedEraCount)
    {
        if (data == null)
        {
            return 1f;
        }

        float level = GetDifficultyLevel(elapsedSeconds, clearedEraCount);
        return Mathf.Max(1f, 1f + (level - data.baseDifficulty) * data.enemyDamageMultiplierPerDifficulty);
    }

    public float GetEnemyHealthMultiplier(float elapsedSeconds, int clearedEraCount)
    {
        if (data == null)
        {
            return 1f;
        }

        float level = GetDifficultyLevel(elapsedSeconds, clearedEraCount);
        return Mathf.Max(1f, 1f + (level - data.baseDifficulty) * data.enemyHealthMultiplierPerDifficulty);
    }

    public float GetEliteChance(float elapsedSeconds)
    {
        if (data == null)
        {
            return 0f;
        }

        float minutes = elapsedSeconds / 60f;
        return Mathf.Clamp(data.baseEliteChance + minutes * data.eliteChancePerMinute, 0f, data.maxEliteChance);
    }

    public float GetDropChance(float elapsedSeconds, int clearedEraCount)
    {
        if (data == null)
        {
            return 0f;
        }

        float level = GetDifficultyLevel(elapsedSeconds, clearedEraCount);
        return Mathf.Clamp(data.baseDropChance + level * data.dropChanceDifficultyBonus, 0f, data.maxDropChance);
    }

    public float GetSpawnInterval(float elapsedSeconds, int clearedEraCount)
    {
        if (data == null)
        {
            return 5f;
        }

        float multiplier = GetDifficultyMultiplier(elapsedSeconds, clearedEraCount);
        return Mathf.Max(data.minSpawnInterval, data.baseSpawnInterval / (1f + multiplier * data.spawnIntervalDifficultyFactor));
    }

    public int MaxAliveEnemies => data != null ? data.maxAliveEnemies : 8;

    public BossTriggerCondition BossTriggerCondition => data != null ? data.bossTriggerCondition : BossTriggerCondition.KillCount;
    public int BossKillCountThreshold => data != null ? data.bossKillCountThreshold : 6;
    public float BossElapsedTimeThreshold => data != null ? data.bossElapsedTimeThreshold : 240f;
}
