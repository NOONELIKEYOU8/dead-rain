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

    [Header("Runtime")]
    public GameObject prefab;
    public string prefabPathPlaceholder;
    public Sprite iconPlaceholder;
    [TextArea] public string specialMechanics;
}
