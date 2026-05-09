using System.Collections.Generic;
using UnityEngine;

public class BronzeMapGenerator : MonoBehaviour
{
    [SerializeField] private int seed;
    [SerializeField] private bool randomizeSeed = true;
    [SerializeField] private int mainRoomCount = 8;
    [SerializeField] private int branchRoomCount = 3;
    [SerializeField] private Vector2 roomSize = new Vector2(14f, 7f);
    [SerializeField] private float corridorWidth = 4f;
    [SerializeField] private Sprite platformSprite;
    [SerializeField] private Sprite backgroundSprite;
    [SerializeField] private Transform player;
    [SerializeField] private Camera targetCamera;

    private readonly List<Transform> enemySpawnPoints = new();
    private Transform bossSpawnPoint;
    private Transform portalPoint;

    public IReadOnlyList<Transform> EnemySpawnPoints => enemySpawnPoints;
    public Transform BossSpawnPoint => bossSpawnPoint;
    public Transform PortalPoint => portalPoint;

    private void Awake()
    {
        Generate();
    }

    public void Generate()
    {
        if (randomizeSeed)
        {
            seed = Random.Range(1, int.MaxValue);
        }

        Random.InitState(seed);
        enemySpawnPoints.Clear();

        GameObject root = new GameObject("Generated Bronze Map");
        root.transform.SetParent(transform, false);

        List<Vector2> rooms = BuildRoomGraph();
        for (int i = 0; i < rooms.Count; i++)
        {
            CreateRoom(root.transform, rooms[i], i);
        }

        for (int i = 0; i < mainRoomCount - 1; i++)
        {
            ConnectRooms(root.transform, rooms[i], rooms[i + 1], i);
        }

        for (int i = mainRoomCount; i < rooms.Count; i++)
        {
            int anchor = Mathf.Clamp(i - mainRoomCount + 1, 1, mainRoomCount - 2);
            ConnectRooms(root.transform, rooms[anchor], rooms[i], i);
        }

        Vector2 start = rooms[0];
        Vector2 boss = rooms[mainRoomCount - 1];
        PlacePlayer(start + new Vector2(-roomSize.x * 0.35f, 0.5f));
        bossSpawnPoint = CreateMarker(root.transform, "Boss Spawn Point", boss + new Vector2(0f, 0.7f));
        portalPoint = CreateMarker(root.transform, "Portal Point", boss + new Vector2(roomSize.x * 0.35f, 1.1f));

        if (targetCamera == null)
        {
            targetCamera = Camera.main;
        }

        if (targetCamera != null)
        {
            targetCamera.transform.position = new Vector3(start.x + 2f, start.y + 3f, -10f);
        }
    }

    public Transform[] GetSpawnPointArray()
    {
        return enemySpawnPoints.ToArray();
    }

    private List<Vector2> BuildRoomGraph()
    {
        List<Vector2> rooms = new();
        for (int i = 0; i < mainRoomCount; i++)
        {
            float y = Mathf.Sin(i * 0.9f) * 2f + Random.Range(-0.7f, 0.7f);
            rooms.Add(new Vector2(i * (roomSize.x + corridorWidth), y));
        }

        for (int i = 0; i < branchRoomCount; i++)
        {
            int anchor = Random.Range(1, mainRoomCount - 2);
            float side = Random.value > 0.5f ? 1f : -1f;
            Vector2 branch = rooms[anchor] + new Vector2(Random.Range(-2f, 2f), side * (roomSize.y + Random.Range(3f, 5f)));
            rooms.Add(branch);
        }

        return rooms;
    }

    private void CreateRoom(Transform root, Vector2 center, int index)
    {
        GameObject room = new GameObject($"Room_{index:00}");
        room.transform.SetParent(root, false);
        room.transform.position = center;

        CreatePlatform(room.transform, "Floor", new Vector2(0f, -roomSize.y * 0.45f), new Vector2(roomSize.x, 0.7f), new Color(0.34f, 0.23f, 0.13f, 1f));
        CreatePlatform(room.transform, "Left Wall", new Vector2(-roomSize.x * 0.5f, 0f), new Vector2(0.45f, roomSize.y), new Color(0.18f, 0.12f, 0.08f, 1f));
        CreatePlatform(room.transform, "Right Wall", new Vector2(roomSize.x * 0.5f, 0f), new Vector2(0.45f, roomSize.y), new Color(0.18f, 0.12f, 0.08f, 1f));

        int platformCount = Random.Range(1, 4);
        for (int i = 0; i < platformCount; i++)
        {
            float width = Random.Range(3f, 5.2f);
            float x = Random.Range(-roomSize.x * 0.3f, roomSize.x * 0.3f);
            float y = Random.Range(-0.4f, roomSize.y * 0.32f);
            CreatePlatform(room.transform, $"Step_{i}", new Vector2(x, y), new Vector2(width, 0.35f), new Color(0.42f, 0.31f, 0.17f, 1f));
        }

        CreateBackdrop(room.transform, index);

        if (index > 0)
        {
            int spawnCount = index % 3 == 0 ? 2 : 1;
            for (int i = 0; i < spawnCount; i++)
            {
                Transform spawn = CreateMarker(room.transform, $"EnemySpawn_{index}_{i}", new Vector2(center.x + Random.Range(-3.5f, 3.5f), center.y - roomSize.y * 0.25f));
                enemySpawnPoints.Add(spawn);
            }
        }
    }

    private void ConnectRooms(Transform root, Vector2 a, Vector2 b, int index)
    {
        Vector2 center = (a + b) * 0.5f;
        Vector2 delta = b - a;
        if (Mathf.Abs(delta.x) >= Mathf.Abs(delta.y))
        {
            CreatePlatform(root, $"Corridor_{index}", new Vector2(center.x, center.y - roomSize.y * 0.45f), new Vector2(Mathf.Abs(delta.x) + corridorWidth, 0.55f), new Color(0.29f, 0.2f, 0.11f, 1f));
        }
        else
        {
            CreatePlatform(root, $"VerticalLink_{index}", new Vector2(center.x, center.y), new Vector2(3.2f, 0.45f), new Color(0.29f, 0.2f, 0.11f, 1f));
            CreatePlatform(root, $"VerticalLinkStep_{index}", new Vector2(center.x + 2.4f, center.y + Mathf.Sign(delta.y) * 2.2f), new Vector2(3.2f, 0.45f), new Color(0.29f, 0.2f, 0.11f, 1f));
        }
    }

    private void CreatePlatform(Transform parent, string name, Vector2 localPosition, Vector2 size, Color color)
    {
        GameObject go = new GameObject(name, typeof(BoxCollider2D));
        go.layer = LayerMask.NameToLayer("Ground");
        go.transform.SetParent(parent, false);
        go.transform.localPosition = localPosition;

        GameObject visual = new GameObject("Visual", typeof(SpriteRenderer));
        visual.transform.SetParent(go.transform, false);
        SpriteRenderer sr = visual.GetComponent<SpriteRenderer>();
        sr.sprite = platformSprite;
        sr.color = color;
        sr.sortingOrder = -1;
        if (platformSprite != null && platformSprite.bounds.size.x > 0f && platformSprite.bounds.size.y > 0f)
        {
            visual.transform.localScale = new Vector3(size.x / platformSprite.bounds.size.x, size.y / platformSprite.bounds.size.y, 1f);
        }

        BoxCollider2D collider = go.GetComponent<BoxCollider2D>();
        collider.size = size;
    }

    private void CreateBackdrop(Transform parent, int index)
    {
        if (backgroundSprite == null)
        {
            return;
        }

        GameObject backdrop = new GameObject("Bronze Backdrop", typeof(SpriteRenderer));
        backdrop.transform.SetParent(parent, false);
        backdrop.transform.localPosition = new Vector3(0f, 0.5f, 1f);
        backdrop.transform.localScale = new Vector3(3f, 2f, 1f);
        SpriteRenderer sr = backdrop.GetComponent<SpriteRenderer>();
        sr.sprite = backgroundSprite;
        sr.color = index % 2 == 0 ? new Color(0.08f, 0.15f, 0.14f, 0.42f) : new Color(0.16f, 0.1f, 0.05f, 0.38f);
        sr.sortingOrder = -5;
    }

    private Transform CreateMarker(Transform parent, string name, Vector2 position)
    {
        GameObject marker = new GameObject(name);
        marker.transform.SetParent(parent, false);
        marker.transform.position = position;
        return marker.transform;
    }

    private void PlacePlayer(Vector2 position)
    {
        if (player == null)
        {
            GameObject playerObject = GameObject.FindGameObjectWithTag("Player");
            player = playerObject != null ? playerObject.transform : null;
        }

        if (player != null)
        {
            player.position = position;
        }
    }
}
