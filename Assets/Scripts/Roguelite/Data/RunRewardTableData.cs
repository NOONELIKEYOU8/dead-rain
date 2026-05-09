using UnityEngine;

[CreateAssetMenu(fileName = "NewRunRewardTableData", menuName = "Dead Rain/Roguelite/Run Reward Table")]
public class RunRewardTableData : ScriptableObject
{
    public string id;
    public string displayName;
    [TextArea] public string description;
    public EraId era;
    public ItemData[] rewards;
    public int rewardChoiceCount = 3;
    public float hp;
    public float damage;
    public float moveSpeed;
    public EnemyAttackPattern attackPattern;
    public string prefabPathPlaceholder;
    public Sprite iconPlaceholder;
    public ContentTier tier = ContentTier.Common;
    [TextArea] public string specialMechanics;
}
