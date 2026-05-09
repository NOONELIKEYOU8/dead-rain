using System.Collections.Generic;
using UnityEngine;

public class EnemySpawnDirector : MonoBehaviour
{
    [SerializeField] private EraStageSystem eraStageSystem;
    [SerializeField] private BronzeMapGenerator mapGenerator;
    [SerializeField] private Transform[] spawnPoints;
    [SerializeField] private bool spawnAutomatically = true;
    [SerializeField] private int initialSpawnCount = 3;

    private readonly List<EnemyRuntime> aliveEnemies = new();
    private float nextSpawnTime;

    public int AliveCount => aliveEnemies.Count;

    private void Start()
    {
        if ((spawnPoints == null || spawnPoints.Length == 0) && mapGenerator != null)
        {
            spawnPoints = mapGenerator.GetSpawnPointArray();
        }

        for (int i = 0; i < initialSpawnCount; i++)
        {
            SpawnOne();
        }
    }

    private void Update()
    {
        CleanupDeadEnemies();

        if (!spawnAutomatically || GameRunManager.Instance == null || Time.time < nextSpawnTime)
        {
            return;
        }

        nextSpawnTime = Time.time + GameRunManager.Instance.SpawnInterval;
        if (aliveEnemies.Count < GameRunManager.Instance.MaxAliveEnemies)
        {
            SpawnOne();
        }
    }

    public EnemyRuntime SpawnOne()
    {
        EraStageData era = eraStageSystem != null ? eraStageSystem.CurrentEra : GameRunManager.Instance != null ? GameRunManager.Instance.CurrentEra : null;
        if (era == null || era.enemyPool == null || era.enemyPool.Length == 0)
        {
            return null;
        }

        EnemyData data = era.enemyPool[Random.Range(0, era.enemyPool.Length)];
        if (data == null || data.prefab == null)
        {
            return null;
        }

        Transform spawn = spawnPoints != null && spawnPoints.Length > 0
            ? spawnPoints[Random.Range(0, spawnPoints.Length)]
            : transform;

        GameObject enemyObject = Instantiate(data.prefab, spawn.position, Quaternion.identity);
        EnemyRuntime runtime = enemyObject.GetComponent<EnemyRuntime>();
        if (runtime != null)
        {
            bool elite = GameRunManager.Instance != null && Random.value <= GameRunManager.Instance.EliteChance;
            runtime.Initialize(data, GameRunManager.Instance != null ? GameRunManager.Instance.DifficultyMultiplier : 1f, elite);
            aliveEnemies.Add(runtime);
        }

        return runtime;
    }

    private void CleanupDeadEnemies()
    {
        for (int i = aliveEnemies.Count - 1; i >= 0; i--)
        {
            if (aliveEnemies[i] == null || !aliveEnemies[i].gameObject.activeInHierarchy)
            {
                aliveEnemies.RemoveAt(i);
            }
        }
    }
}
