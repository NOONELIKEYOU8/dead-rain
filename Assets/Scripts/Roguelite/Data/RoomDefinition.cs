using UnityEngine;

public class RoomDefinition : MonoBehaviour
{
    [Header("Identity")]
    public string roomId;
    public RoomType roomType = RoomType.Combat;
    public string[] eraTags;

    [Header("Layout")]
    public int width = 32;
    public int height = 18;
    public RoomConnectionDirection entranceDirections = RoomConnectionDirection.Left;
    public RoomConnectionDirection exitDirections = RoomConnectionDirection.Right;
    public RoomConnector[] connectors;

    [Header("Spawn Points")]
    public Transform[] enemySpawnPoints;
    public Transform[] itemSpawnPoints;
    public Transform[] treasureSpawnPoints;
    public Transform playerStartPoint;
    public Transform bossSpawnPoint;
    public Transform exitPoint;

    [Header("Camera And Difficulty")]
    public Collider2D cameraBounds;
    public int difficultyWeight = 1;
    public bool allowEliteSpawns = true;
    public bool requiresKey;
    public string lockId;

    public bool HasEntrance(RoomConnectionDirection direction)
    {
        return (entranceDirections & direction) != 0;
    }

    public bool HasExit(RoomConnectionDirection direction)
    {
        return (exitDirections & direction) != 0;
    }
}
