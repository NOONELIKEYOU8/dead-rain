using UnityEngine;

[CreateAssetMenu(fileName = "NewEnemyData", menuName = "Dead Rain/Roguelite/Enemy")]
public class EnemyData : ScriptableObject
{
    [Header("Identity")]
    public string id;
    public string displayName;
    [TextArea] public string description;
    public EraId era;
    public ContentTier tier = ContentTier.Common;

    [Header("Stats")]
    public float hp = 30f;
    public float damage = 8f;
    public float moveSpeed = 2f;
    public EnemyAttackPattern attackPattern;
    public RuntimeEnemyRole runtimeRole;
    public int spawnCost = 1;
    public float spawnWeight = 1f;
    public float eliteHealthMultiplier = 1.6f;
    public float eliteDamageMultiplier = 1.35f;

    [Header("Runtime")]
    public GameObject prefab;
    public string prefabPathPlaceholder;
    public Sprite iconPlaceholder;
    public string[] eraTags;
    [TextArea] public string specialMechanics;
}
