using System;
using UnityEngine;

public class GameRunManager : MonoBehaviour
{
    public static GameRunManager Instance { get; private set; }

    [SerializeField] private DifficultyScalingData difficultyScaling;
    [SerializeField] private EraStageData startingEra;

    private DifficultyService difficultyService;

    public event Action OnRunStateChanged;
    public event Action<EnemyRuntime> OnEnemyKilled;
    public event Action<BossBase> OnBossKilled;
    public event Action<EraStageData> OnEraChanged;

    public float ElapsedTime { get; private set; }
    public int KillCount { get; private set; }
    public int ResourceCount { get; private set; }
    public int ClearedEraCount { get; private set; }
    public EraStageData CurrentEra { get; private set; }

    public float DifficultyLevel => difficultyService.GetDifficultyLevel(ElapsedTime, ClearedEraCount);
    public float DifficultyMultiplier => difficultyService.GetDifficultyMultiplier(ElapsedTime, ClearedEraCount);
    public float EliteChance => difficultyService.GetEliteChance(ElapsedTime);
    public float DropChance => difficultyService.GetDropChance(ElapsedTime, ClearedEraCount);
    public float SpawnInterval => difficultyService.GetSpawnInterval(ElapsedTime, ClearedEraCount);
    public int MaxAliveEnemies => difficultyService.MaxAliveEnemies;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        difficultyService = new DifficultyService(difficultyScaling);
        CurrentEra = startingEra;
    }

    private void Update()
    {
        ElapsedTime += Time.deltaTime;
        OnRunStateChanged?.Invoke();
    }

    public void Configure(DifficultyScalingData scaling, EraStageData era)
    {
        difficultyScaling = scaling;
        difficultyService = new DifficultyService(difficultyScaling);
        SetCurrentEra(era);
    }

    public void SetCurrentEra(EraStageData era)
    {
        if (era == null)
        {
            return;
        }

        CurrentEra = era;
        OnEraChanged?.Invoke(CurrentEra);
        OnRunStateChanged?.Invoke();
    }

    public void RegisterEnemyKilled(EnemyRuntime enemy)
    {
        KillCount++;
        ResourceCount += enemy != null && enemy.IsElite ? 3 : 1;
        OnEnemyKilled?.Invoke(enemy);
        OnRunStateChanged?.Invoke();
    }

    public void RegisterBossKilled(BossBase boss)
    {
        ResourceCount += 10;
        ClearedEraCount++;
        OnBossKilled?.Invoke(boss);
        OnRunStateChanged?.Invoke();
    }

    public void SpendResource(int amount)
    {
        ResourceCount = Mathf.Max(0, ResourceCount - Mathf.Max(0, amount));
        OnRunStateChanged?.Invoke();
    }
}
