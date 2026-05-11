using UnityEngine;
using UnityEngine.Tilemaps;

[CreateAssetMenu(fileName = "NewEraStageData", menuName = "Dead Rain/Roguelite/Era Stage")]
public class EraStageData : ScriptableObject
{
    [Header("Identity")]
    public string id;
    public string eraName;
    public string displayName;
    [TextArea] public string description;
    public EraId era;
    public string visualTheme;

    [Header("Pools")]
    public EnemyData[] enemyPool;
    public BossData[] miniBossPool;
    public BossData[] bossPool;
    public BossData finalBoss;
    public ItemData[] itemDropPool;
    public RoomDefinition[] roomPrefabPool;

    [Header("Presentation")]
    public TileBase[] tileSet;
    public AudioClip backgroundMusic;
    public string prefabPathPlaceholder;
    public string tilemapPrefabPathPlaceholder;
    public string backgroundMusicPlaceholder;
    public Sprite iconPlaceholder;

    [Header("Progression")]
    public float difficultyMultiplier = 1f;
    public EraId nextEra;
    public string nextEraId;
    public string unlockCondition;
    public ContentTier tier = ContentTier.Common;
    [TextArea] public string specialMechanics;

    public BossData[] BossPool => bossPool != null && bossPool.Length > 0 ? bossPool : miniBossPool;
}
