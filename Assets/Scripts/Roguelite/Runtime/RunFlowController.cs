using UnityEngine;

public class RunFlowController : MonoBehaviour
{
    [SerializeField] private EnemySpawnDirector spawnDirector;
    [SerializeField] private EraStageSystem eraStageSystem;
    [SerializeField] private BronzeMapGenerator mapGenerator;
    [SerializeField] private BossBase bronzeBossPrefab;
    [SerializeField] private Transform bossSpawnPoint;
    [SerializeField] private RunRewardTableData rewardTable;
    [SerializeField] private RunRewardChoiceUI rewardChoiceUI;
    [SerializeField] private int killsBeforeBoss = 6;
    [SerializeField] private GameObject nextEraPortal;

    private bool bossSpawned;
    private bool rewardsShown;
    private BossBase activeBoss;
    private bool subscribed;

    private void OnEnable()
    {
        Subscribe();
    }

    private void Start()
    {
        Subscribe();

        if (nextEraPortal != null)
        {
            nextEraPortal.SetActive(false);
        }

        if (bossSpawnPoint == null && mapGenerator != null)
        {
            bossSpawnPoint = mapGenerator.BossSpawnPoint;
        }

        if (nextEraPortal != null && mapGenerator != null && mapGenerator.PortalPoint != null)
        {
            nextEraPortal.transform.position = mapGenerator.PortalPoint.position;
        }
    }

    private void Update()
    {
        CheckProgress();
    }

    private void OnDisable()
    {
        if (GameRunManager.Instance != null && subscribed)
        {
            GameRunManager.Instance.OnRunStateChanged -= CheckProgress;
            GameRunManager.Instance.OnBossKilled -= HandleBossKilled;
            subscribed = false;
        }
    }

    private void Subscribe()
    {
        if (subscribed || GameRunManager.Instance == null)
        {
            return;
        }

        GameRunManager.Instance.OnRunStateChanged += CheckProgress;
        GameRunManager.Instance.OnBossKilled += HandleBossKilled;
        subscribed = true;
    }

    private void CheckProgress()
    {
        if (bossSpawned || GameRunManager.Instance == null || GameRunManager.Instance.KillCount < killsBeforeBoss)
        {
            return;
        }

        SpawnBoss();
    }

    private void SpawnBoss()
    {
        if (bronzeBossPrefab == null)
        {
            return;
        }

        bossSpawned = true;
        Transform spawn = bossSpawnPoint != null ? bossSpawnPoint : transform;
        activeBoss = Instantiate(bronzeBossPrefab, spawn.position, Quaternion.identity);
    }

    private void HandleBossKilled(BossBase boss)
    {
        if (rewardsShown)
        {
            return;
        }

        rewardsShown = true;
        if (rewardChoiceUI == null)
        {
            rewardChoiceUI = FindObjectOfType<RunRewardChoiceUI>(true);
        }

        if (rewardChoiceUI != null)
        {
            rewardChoiceUI.ShowRewards(rewardTable, HandleRewardChosen);
        }

        if (nextEraPortal != null)
        {
            nextEraPortal.SetActive(true);
        }
    }

    private void HandleRewardChosen(ItemData item)
    {
        GameObject player = GameObject.FindGameObjectWithTag("Player");
        RunInventorySystem inventory = player != null ? player.GetComponent<RunInventorySystem>() : null;
        inventory?.AddItem(item);
        eraStageSystem?.UnlockAndEnterNextEra();
    }
}
