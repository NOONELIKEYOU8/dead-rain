using UnityEngine;

[CreateAssetMenu(fileName = "NewDropTableData", menuName = "Dead Rain/Roguelite/Drop Table")]
public class DropTableData : ScriptableObject
{
    public string id;
    public string displayName;
    [TextArea] public string description;
    public EraId era;
    public ItemData[] possibleItems;
    public float[] weights;
    public float hp;
    public float damage;
    public float moveSpeed;
    public EnemyAttackPattern attackPattern;
    public string prefabPathPlaceholder;
    public Sprite iconPlaceholder;
    public ContentTier tier = ContentTier.Common;
    [TextArea] public string specialMechanics;
}
