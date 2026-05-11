using UnityEngine;

public class DifficultyDirector : MonoBehaviour
{
    [SerializeField] private DifficultyScalingData difficultyScaling;
    [SerializeField] private GameRunManager runManager;

    private DifficultyService service;

    public float RunTimeSeconds => runManager != null ? runManager.ElapsedTime : Time.timeSinceLevelLoad;
    public float DifficultyLevel => service.GetDifficultyLevel(RunTimeSeconds, ClearedEraCount);
    public int SpawnBudget => service.GetSpawnBudget(RunTimeSeconds, ClearedEraCount);
    public float EliteChance => service.GetEliteChance(RunTimeSeconds);
    public float EnemyDamageMultiplier => service.GetEnemyDamageMultiplier(RunTimeSeconds, ClearedEraCount);
    public float EnemyHealthMultiplier => service.GetEnemyHealthMultiplier(RunTimeSeconds, ClearedEraCount);
    public BossTriggerCondition BossTriggerCondition => service.BossTriggerCondition;
    public int BossKillCountThreshold => service.BossKillCountThreshold;
    public float BossElapsedTimeThreshold => service.BossElapsedTimeThreshold;

    private int ClearedEraCount => runManager != null ? runManager.ClearedEraCount : 0;

    private void Awake()
    {
        if (runManager == null)
        {
            runManager = GameRunManager.Instance;
        }

        service = new DifficultyService(difficultyScaling);
    }

    public void Configure(DifficultyScalingData scaling, GameRunManager manager = null)
    {
        difficultyScaling = scaling;
        if (manager != null)
        {
            runManager = manager;
        }

        service = new DifficultyService(difficultyScaling);
    }

    public bool ShouldTriggerBoss(int killCount, bool keyCollected = false)
    {
        switch (BossTriggerCondition)
        {
            case BossTriggerCondition.RoomEntered:
            case BossTriggerCondition.Manual:
                return false;
            case BossTriggerCondition.ElapsedTime:
                return RunTimeSeconds >= BossElapsedTimeThreshold;
            case BossTriggerCondition.KeyCollected:
                return keyCollected;
            default:
                return killCount >= BossKillCountThreshold;
        }
    }
}
