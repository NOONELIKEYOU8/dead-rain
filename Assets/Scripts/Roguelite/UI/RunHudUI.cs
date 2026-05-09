using System.Text;
using UnityEngine;
using UnityEngine.UI;

public class RunHudUI : MonoBehaviour
{
    [SerializeField] private Text eraText;
    [SerializeField] private Text timeText;
    [SerializeField] private Text difficultyText;
    [SerializeField] private Text killsText;
    [SerializeField] private Text itemsText;
    [SerializeField] private UIHealthBar bossHealthBar;
    [SerializeField] private Text bossNameText;

    private RunInventorySystem inventory;
    private BossBase boundBoss;
    private readonly StringBuilder builder = new();

    private void Start()
    {
        GameObject player = GameObject.FindGameObjectWithTag("Player");
        inventory = player != null ? player.GetComponent<RunInventorySystem>() : null;

        if (inventory != null)
        {
            inventory.OnItemStackChanged += HandleInventoryChanged;
        }

        if (GameRunManager.Instance != null)
        {
            GameRunManager.Instance.OnRunStateChanged += Refresh;
            GameRunManager.Instance.OnBossKilled += HandleBossKilled;
        }

        HideBossBar();
        Refresh();
    }

    private void Update()
    {
        Refresh();
        TryBindBoss();
    }

    private void OnDestroy()
    {
        if (inventory != null)
        {
            inventory.OnItemStackChanged -= HandleInventoryChanged;
        }

        if (GameRunManager.Instance != null)
        {
            GameRunManager.Instance.OnRunStateChanged -= Refresh;
            GameRunManager.Instance.OnBossKilled -= HandleBossKilled;
        }

        if (boundBoss != null)
        {
            boundBoss.OnBossHealthChanged -= HandleBossHealthChanged;
        }
    }

    public void Configure(Text era, Text time, Text difficulty, Text kills, Text items, UIHealthBar bossBar, Text bossName)
    {
        eraText = era;
        timeText = time;
        difficultyText = difficulty;
        killsText = kills;
        itemsText = items;
        bossHealthBar = bossBar;
        bossNameText = bossName;
    }

    private void Refresh()
    {
        GameRunManager run = GameRunManager.Instance;
        if (run == null)
        {
            return;
        }

        if (eraText != null)
        {
            eraText.text = run.CurrentEra != null ? run.CurrentEra.displayName : "时代裂隙";
        }

        if (timeText != null)
        {
            int seconds = Mathf.FloorToInt(run.ElapsedTime);
            timeText.text = $"{seconds / 60:00}:{seconds % 60:00}";
        }

        if (difficultyText != null)
        {
            difficultyText.text = $"难度 {run.DifficultyLevel:0.0}  x{run.DifficultyMultiplier:0.00}";
        }

        if (killsText != null)
        {
            killsText.text = $"击杀 {run.KillCount}  资源 {run.ResourceCount}";
        }

        RefreshItems();
    }

    private void RefreshItems()
    {
        if (itemsText == null)
        {
            return;
        }

        builder.Clear();
        builder.AppendLine("道具");

        if (inventory == null || inventory.Stacks.Count == 0)
        {
            builder.Append("无");
        }
        else
        {
            foreach (RunInventorySystem.ItemStack stack in inventory.Stacks)
            {
                if (stack.item != null)
                {
                    builder.AppendLine($"{stack.item.displayName} x{stack.count}");
                }
            }
        }

        itemsText.text = builder.ToString();
    }

    private void TryBindBoss()
    {
        if (boundBoss != null)
        {
            return;
        }

        BossBase boss = FindObjectOfType<BossBase>();
        if (boss == null)
        {
            return;
        }

        boundBoss = boss;
        boundBoss.OnBossHealthChanged += HandleBossHealthChanged;
        if (bossNameText != null)
        {
            bossNameText.text = boundBoss.DisplayName;
        }
        if (bossHealthBar != null)
        {
            bossHealthBar.gameObject.SetActive(true);
        }
    }

    private void HandleBossHealthChanged(BossBase boss, float percent)
    {
        if (bossHealthBar != null)
        {
            bossHealthBar.SetPercent(percent);
        }
    }

    private void HandleBossKilled(BossBase boss)
    {
        HideBossBar();
    }

    private void HideBossBar()
    {
        if (bossHealthBar != null)
        {
            bossHealthBar.gameObject.SetActive(false);
        }
    }

    private void HandleInventoryChanged(ItemData item, int count)
    {
        RefreshItems();
    }
}
