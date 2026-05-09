using UnityEngine;

[CreateAssetMenu(fileName = "NewWeaponCatalogData", menuName = "Dead Rain/Roguelite/Weapon Catalog Entry")]
public class WeaponCatalogData : ScriptableObject
{
    public string id;
    public string displayName;
    [TextArea] public string description;
    public EraId era;
    public float hp;
    public float damage;
    public float moveSpeed;
    public EnemyAttackPattern attackPattern;
    public Weapon prefab;
    public string prefabPathPlaceholder;
    public Sprite iconPlaceholder;
    public ContentTier tier = ContentTier.Common;
    [TextArea] public string specialMechanics;
}
