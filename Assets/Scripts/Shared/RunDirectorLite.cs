using UnityEngine;

public class RunDirectorLite : MonoBehaviour, IDifficultyProvider
{
    [Header("Threat Curve")]
    public AnimationCurve threatCurve = AnimationCurve.Linear(0f, 1f, 600f, 3f);

    [Header("Base Multipliers")]
    public float enemyHpBase = 1f;
    public float enemyDamageBase = 1f;
    public float spawnWeightBase = 1f;

    private float runStartTime;

    private void Awake()
    {
        runStartTime = Time.time;
    }

    public DifficultySnapshot GetSnapshot()
    {
        float runTime = Mathf.Max(0f, Time.time - runStartTime);
        float threat = Mathf.Max(1f, threatCurve.Evaluate(runTime));

        return new DifficultySnapshot
        {
            runTimeSeconds = runTime,
            threatLevel = threat,
            enemyHpMultiplier = enemyHpBase * threat,
            enemyDamageMultiplier = enemyDamageBase * threat,
            spawnWeightMultiplier = spawnWeightBase * threat
        };
    }

    public float GetThreatLevel()
    {
        return GetSnapshot().threatLevel;
    }
}
