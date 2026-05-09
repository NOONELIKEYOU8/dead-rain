using UnityEngine;

[CreateAssetMenu(fileName = "NewBuffData", menuName = "Dead Rain/Roguelite/Buff Or Debuff")]
public class BuffData : ScriptableObject
{
    public string id;
    public string displayName;
    [TextArea] public string description;
    public EraId era;
    public float hp;
    public float damage;
    public float moveSpeed;
    public EnemyAttackPattern attackPattern;
    public string prefabPathPlaceholder;
    public Sprite iconPlaceholder;
    public ContentTier tier = ContentTier.Common;
    public float duration;
    public bool isDebuff;
    [TextArea] public string specialMechanics;
}
