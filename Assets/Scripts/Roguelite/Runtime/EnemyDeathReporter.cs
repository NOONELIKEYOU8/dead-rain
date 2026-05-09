using UnityEngine;

public class EnemyDeathReporter : MonoBehaviour
{
    [SerializeField] private EnemyRuntime enemyRuntime;
    [SerializeField] private BossBase boss;

    private Stats stats;
    private bool reported;

    private void Awake()
    {
        if (enemyRuntime == null)
        {
            enemyRuntime = GetComponentInParent<EnemyRuntime>();
        }

        if (boss == null)
        {
            boss = GetComponentInParent<BossBase>();
        }

        stats = GetComponentInChildren<Stats>();
    }

    private void OnEnable()
    {
        reported = false;
        if (stats != null)
        {
            stats.OnHealthZero += ReportDeath;
        }
    }

    private void OnDisable()
    {
        if (stats != null)
        {
            stats.OnHealthZero -= ReportDeath;
        }
    }

    private void ReportDeath()
    {
        if (reported || GameRunManager.Instance == null)
        {
            return;
        }

        reported = true;
        if (boss != null)
        {
            GameRunManager.Instance.RegisterBossKilled(boss);
        }
        else
        {
            GameRunManager.Instance.RegisterEnemyKilled(enemyRuntime);
        }
    }
}
