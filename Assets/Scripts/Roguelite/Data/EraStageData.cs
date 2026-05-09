using UnityEngine;

[CreateAssetMenu(fileName = "NewEraStageData", menuName = "Dead Rain/Roguelite/Era Stage")]
public class EraStageData : ScriptableObject
{
    [Header("Identity")]
    public string id;
    public string displayName;
    [TextArea] public string description;
    public EraId era;
    public string visualTheme;

    [Header("Pools")]
    public EnemyData[] enemyPool;
    public BossData[] miniBossPool;
    public BossData finalBoss;
    public ItemData[] itemDropPool;

    [Header("Placeholders")]
    public string prefabPathPlaceholder;
    public string tilemapPrefabPathPlaceholder;
    public string backgroundMusicPlaceholder;
    public Sprite iconPlaceholder;

    [Header("Progression")]
    public string unlockCondition;
    public ContentTier tier = ContentTier.Common;
    [TextArea] public string specialMechanics;
}
