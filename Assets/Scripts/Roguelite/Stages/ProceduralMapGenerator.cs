using System;
using System.Collections.Generic;
using UnityEngine;

public class ProceduralMapGenerator : MonoBehaviour
{
    [Serializable]
    public class RoomGraphNode
    {
        public int id;
        public int depth;
        public int parentId = -1;
        public RoomType roomType;
        public Vector2Int gridPosition;
        public Vector2 worldPosition;
        public readonly List<int> linkedNodeIds = new();
    }

    [SerializeField] private ProceduralMapSettings settings;
    [SerializeField] private EraStageData era;
    [SerializeField] private Transform player;
    [SerializeField] private Camera targetCamera;
    [SerializeField] private Sprite placeholderSprite;
    [SerializeField] private bool generateOnAwake = true;

    private readonly List<RoomGraphNode> graph = new();
    private readonly List<Transform> enemySpawnPoints = new();
    private Transform bossSpawnPoint;
    private Transform portalPoint;
    private GameObject generatedRoot;

    public IReadOnlyList<RoomGraphNode> Graph => graph;
    public IReadOnlyList<Transform> EnemySpawnPoints => enemySpawnPoints;
    public Transform BossSpawnPoint => bossSpawnPoint;
    public Transform PortalPoint => portalPoint;

    private void Awake()
    {
        if (generateOnAwake)
        {
            Generate();
        }
    }

    public void Configure(ProceduralMapSettings mapSettings, EraStageData eraStage, Transform playerTransform = null, Camera cameraTarget = null)
    {
        settings = mapSettings;
        era = eraStage;
        if (playerTransform != null)
        {
            player = playerTransform;
        }

        if (cameraTarget != null)
        {
            targetCamera = cameraTarget;
        }
    }

    public void Generate()
    {
        ClearGenerated();
        enemySpawnPoints.Clear();
        bossSpawnPoint = null;
        portalPoint = null;

        ProceduralMapSettings activeSettings = settings;
        if (activeSettings == null)
        {
            activeSettings = ScriptableObject.CreateInstance<ProceduralMapSettings>();
        }

        int seed = activeSettings.randomizeSeed ? UnityEngine.Random.Range(1, int.MaxValue) : activeSettings.seed;
        UnityEngine.Random.InitState(seed);

        BuildRoomGraph(activeSettings);
        if (activeSettings.runConnectivityCheck && !ValidateConnectivity())
        {
            Debug.LogWarning("Generated room graph failed connectivity check; regenerating with a simple linear fallback.");
            BuildLinearFallback(activeSettings);
        }

        generatedRoot = new GameObject($"Generated Map {seed}");
        generatedRoot.transform.SetParent(transform, false);

        for (int i = 0; i < graph.Count; i++)
        {
            InstantiateRoom(graph[i], activeSettings);
        }

        CreateLinks(activeSettings);
        PlacePlayer();
        PositionCamera();
    }

    public Transform[] GetSpawnPointArray()
    {
        return enemySpawnPoints.ToArray();
    }

    private void BuildRoomGraph(ProceduralMapSettings activeSettings)
    {
        graph.Clear();
        Dictionary<Vector2Int, int> occupied = new();

        int mainLength = Mathf.Max(4, activeSettings.mainPathLength);
        Vector2Int position = Vector2Int.zero;
        RoomGraphNode previous = null;

        for (int i = 0; i < mainLength; i++)
        {
            RoomType type = RoomType.Combat;
            if (i == 0)
            {
                type = RoomType.Start;
            }
            else if (i == mainLength - 3 && activeSettings.requireBossAnteRoom)
            {
                type = RoomType.BossAnte;
            }
            else if (i == mainLength - 2)
            {
                type = RoomType.Boss;
            }
            else if (i == mainLength - 1)
            {
                type = RoomType.Exit;
            }
            else if (i == Mathf.Max(2, mainLength / 2) && activeSettings.requireKeyAndLockedRoom)
            {
                type = RoomType.Locked;
            }

            RoomGraphNode node = AddNode(type, position, i, previous, activeSettings, occupied);
            previous = node;

            int verticalStep = UnityEngine.Random.value > 0.58f ? UnityEngine.Random.Range(-1, 2) : 0;
            position += new Vector2Int(1, verticalStep);
        }

        int branchCount = UnityEngine.Random.Range(activeSettings.minBranchCount, activeSettings.maxBranchCount + 1);
        bool placedTreasure = false;
        bool placedChallenge = false;
        bool placedKey = false;

        for (int branch = 0; branch < branchCount; branch++)
        {
            int anchorIndex = UnityEngine.Random.Range(1, Mathf.Max(2, mainLength - 3));
            RoomGraphNode anchor = graph[anchorIndex];
            Vector2Int branchPosition = anchor.gridPosition;
            RoomGraphNode parent = anchor;
            int branchLength = UnityEngine.Random.Range(1, activeSettings.maxBranchLength + 1);
            int side = UnityEngine.Random.value > 0.5f ? 1 : -1;

            for (int step = 0; step < branchLength; step++)
            {
                branchPosition += step == 0
                    ? new Vector2Int(0, side)
                    : new Vector2Int(UnityEngine.Random.Range(-1, 2), side);

                if (occupied.ContainsKey(branchPosition))
                {
                    branchPosition += new Vector2Int(0, side);
                }

                RoomType type = RoomType.Combat;
                bool leaf = step == branchLength - 1;
                if (leaf && activeSettings.requireTreasureRoom && !placedTreasure)
                {
                    type = RoomType.Treasure;
                    placedTreasure = true;
                }
                else if (leaf && activeSettings.requireChallengeRoom && !placedChallenge)
                {
                    type = RoomType.Challenge;
                    placedChallenge = true;
                }
                else if (leaf && activeSettings.requireKeyAndLockedRoom && !placedKey)
                {
                    type = RoomType.Key;
                    placedKey = true;
                }
                else if (leaf && activeSettings.allowSecretRooms && UnityEngine.Random.value < 0.25f)
                {
                    type = RoomType.Secret;
                }

                parent = AddNode(type, branchPosition, parent.depth + 1, parent, activeSettings, occupied);
            }
        }
    }

    private RoomGraphNode AddNode(RoomType type, Vector2Int gridPosition, int depth, RoomGraphNode parent, ProceduralMapSettings activeSettings, Dictionary<Vector2Int, int> occupied)
    {
        RoomGraphNode node = new RoomGraphNode
        {
            id = graph.Count,
            depth = depth,
            parentId = parent != null ? parent.id : -1,
            roomType = type,
            gridPosition = gridPosition,
            worldPosition = new Vector2(gridPosition.x * activeSettings.roomSpacingX, gridPosition.y * activeSettings.roomSpacingY)
        };

        graph.Add(node);
        occupied[gridPosition] = node.id;

        if (parent != null)
        {
            node.linkedNodeIds.Add(parent.id);
            parent.linkedNodeIds.Add(node.id);
        }

        return node;
    }

    private void BuildLinearFallback(ProceduralMapSettings activeSettings)
    {
        graph.Clear();
        Dictionary<Vector2Int, int> occupied = new();
        RoomGraphNode previous = null;
        int length = Mathf.Max(4, activeSettings.mainPathLength);
        for (int i = 0; i < length; i++)
        {
            RoomType type = i == 0 ? RoomType.Start : i == length - 2 ? RoomType.Boss : i == length - 1 ? RoomType.Exit : RoomType.Combat;
            previous = AddNode(type, new Vector2Int(i, 0), i, previous, activeSettings, occupied);
        }
    }

    private bool ValidateConnectivity()
    {
        if (graph.Count == 0)
        {
            return false;
        }

        HashSet<int> visited = new();
        Queue<int> queue = new();
        queue.Enqueue(0);
        visited.Add(0);

        while (queue.Count > 0)
        {
            RoomGraphNode node = graph[queue.Dequeue()];
            foreach (int linkedId in node.linkedNodeIds)
            {
                if (visited.Add(linkedId))
                {
                    queue.Enqueue(linkedId);
                }
            }
        }

        return visited.Count == graph.Count;
    }

    private void InstantiateRoom(RoomGraphNode node, ProceduralMapSettings activeSettings)
    {
        RoomDefinition prefab = PickRoomPrefab(node.roomType);
        GameObject roomObject = null;

        if (prefab != null)
        {
            roomObject = Instantiate(prefab.gameObject, node.worldPosition, Quaternion.identity, generatedRoot.transform);
            roomObject.name = $"Room_{node.id:00}_{node.roomType}";
        }
        else
        {
            roomObject = CreatePlaceholderRoom(node, activeSettings);
        }

        RoomDefinition definition = roomObject.GetComponent<RoomDefinition>();
        CacheRoomMarkers(node, roomObject.transform, definition);
    }

    private RoomDefinition PickRoomPrefab(RoomType roomType)
    {
        if (era == null || era.roomPrefabPool == null || era.roomPrefabPool.Length == 0)
        {
            return null;
        }

        List<RoomDefinition> matches = new();
        foreach (RoomDefinition room in era.roomPrefabPool)
        {
            if (room != null && room.roomType == roomType)
            {
                matches.Add(room);
            }
        }

        if (matches.Count == 0)
        {
            foreach (RoomDefinition room in era.roomPrefabPool)
            {
                if (room != null && room.roomType == RoomType.Combat)
                {
                    matches.Add(room);
                }
            }
        }

        return matches.Count > 0 ? matches[UnityEngine.Random.Range(0, matches.Count)] : null;
    }

    private GameObject CreatePlaceholderRoom(RoomGraphNode node, ProceduralMapSettings activeSettings)
    {
        GameObject room = new GameObject($"Room_{node.id:00}_{node.roomType}");
        room.transform.SetParent(generatedRoot.transform, false);
        room.transform.position = node.worldPosition;

        RoomDefinition definition = room.AddComponent<RoomDefinition>();
        definition.roomId = room.name;
        definition.roomType = node.roomType;
        definition.width = Mathf.RoundToInt(activeSettings.roomSpacingX * 0.8f);
        definition.height = Mathf.RoundToInt(activeSettings.roomSpacingY * 0.55f);

        float width = activeSettings.roomSpacingX * 0.78f;
        float height = activeSettings.roomSpacingY * 0.5f;
        CreatePlatform(room.transform, "Floor", new Vector2(0f, -height * 0.45f), new Vector2(width, 0.8f), GetRoomColor(node.roomType));
        CreatePlatform(room.transform, "Left Wall", new Vector2(-width * 0.5f, 0f), new Vector2(0.45f, height), new Color(0.13f, 0.09f, 0.06f, 1f));
        CreatePlatform(room.transform, "Right Wall", new Vector2(width * 0.5f, 0f), new Vector2(0.45f, height), new Color(0.13f, 0.09f, 0.06f, 1f));

        if (node.roomType == RoomType.Combat || node.roomType == RoomType.Challenge || node.roomType == RoomType.BossAnte)
        {
            CreatePlatform(room.transform, "Step_Left", new Vector2(-width * 0.22f, -0.1f), new Vector2(5.2f, 0.35f), new Color(0.34f, 0.24f, 0.13f, 1f));
            CreatePlatform(room.transform, "Step_Right", new Vector2(width * 0.22f, 1.65f), new Vector2(5.2f, 0.35f), new Color(0.34f, 0.24f, 0.13f, 1f));
        }

        definition.playerStartPoint = CreateMarker(room.transform, "PlayerStart", new Vector2(-width * 0.35f, -height * 0.25f));
        definition.enemySpawnPoints = new[]
        {
            CreateMarker(room.transform, "EnemySpawn_A", new Vector2(-width * 0.18f, -height * 0.25f)),
            CreateMarker(room.transform, "EnemySpawn_B", new Vector2(width * 0.18f, -height * 0.25f))
        };
        definition.itemSpawnPoints = new[] { CreateMarker(room.transform, "ItemSpawn", new Vector2(0f, -height * 0.18f)) };
        definition.treasureSpawnPoints = new[] { CreateMarker(room.transform, "TreasureSpawn", new Vector2(0f, -height * 0.18f)) };
        definition.bossSpawnPoint = CreateMarker(room.transform, "BossSpawn", new Vector2(0f, -height * 0.25f));
        definition.exitPoint = CreateMarker(room.transform, "ExitPoint", new Vector2(width * 0.35f, -height * 0.15f));
        return room;
    }

    private void CacheRoomMarkers(RoomGraphNode node, Transform room, RoomDefinition definition)
    {
        if (definition == null)
        {
            return;
        }

        if (node.roomType != RoomType.Start && definition.enemySpawnPoints != null)
        {
            foreach (Transform spawn in definition.enemySpawnPoints)
            {
                if (spawn != null)
                {
                    enemySpawnPoints.Add(spawn);
                }
            }
        }

        if (node.roomType == RoomType.Start && definition.playerStartPoint != null)
        {
            definition.playerStartPoint.name = "RunPlayerStart";
        }

        if (node.roomType == RoomType.Boss)
        {
            bossSpawnPoint = definition.bossSpawnPoint != null ? definition.bossSpawnPoint : room;
        }

        if (node.roomType == RoomType.Exit)
        {
            portalPoint = definition.exitPoint != null ? definition.exitPoint : room;
        }
    }

    private void CreateLinks(ProceduralMapSettings activeSettings)
    {
        HashSet<string> created = new();
        for (int i = 0; i < graph.Count; i++)
        {
            RoomGraphNode node = graph[i];
            foreach (int linkedId in node.linkedNodeIds)
            {
                string key = Mathf.Min(node.id, linkedId) + "_" + Mathf.Max(node.id, linkedId);
                if (!created.Add(key))
                {
                    continue;
                }

                RoomGraphNode other = graph[linkedId];
                CreateConnector(node.worldPosition, other.worldPosition, activeSettings, key);
            }
        }
    }

    private void CreateConnector(Vector2 a, Vector2 b, ProceduralMapSettings activeSettings, string key)
    {
        Vector2 delta = b - a;
        Vector2 center = (a + b) * 0.5f;
        if (Mathf.Abs(delta.y) <= activeSettings.maxVerticalStep)
        {
            CreatePlatform(generatedRoot.transform, $"Connector_{key}", new Vector2(center.x, center.y - activeSettings.roomSpacingY * 0.225f), new Vector2(Mathf.Abs(delta.x) + 4f, 0.55f), new Color(0.26f, 0.18f, 0.1f, 1f), true);
            return;
        }

        int steps = Mathf.Max(2, Mathf.CeilToInt(Mathf.Abs(delta.y) / Mathf.Max(1f, activeSettings.maxVerticalStep)));
        for (int i = 1; i <= steps; i++)
        {
            float t = i / (float)(steps + 1);
            Vector2 stepPosition = Vector2.Lerp(a, b, t);
            stepPosition.y -= activeSettings.roomSpacingY * 0.16f;
            CreatePlatform(generatedRoot.transform, $"Connector_{key}_Step_{i}", stepPosition, new Vector2(5f, 0.45f), new Color(0.26f, 0.18f, 0.1f, 1f), true);
        }
    }

    private void PlacePlayer()
    {
        if (player == null)
        {
            GameObject playerObject = GameObject.FindGameObjectWithTag("Player");
            player = playerObject != null ? playerObject.transform : null;
        }

        if (player == null || graph.Count == 0)
        {
            return;
        }

        Transform start = generatedRoot.transform.Find("Room_00_Start/RunPlayerStart");
        player.position = start != null ? start.position : graph[0].worldPosition + new Vector2(-5f, 0.5f);
    }

    private void PositionCamera()
    {
        if (targetCamera == null)
        {
            targetCamera = Camera.main;
        }

        if (targetCamera != null && graph.Count > 0)
        {
            targetCamera.transform.position = new Vector3(graph[0].worldPosition.x + 2f, graph[0].worldPosition.y + 3f, -10f);
        }
    }

    private void ClearGenerated()
    {
        if (generatedRoot == null)
        {
            Transform existing = transform.Find("Generated Map");
            generatedRoot = existing != null ? existing.gameObject : null;
        }

        if (generatedRoot != null)
        {
            if (Application.isPlaying)
            {
                Destroy(generatedRoot);
            }
            else
            {
                DestroyImmediate(generatedRoot);
            }
        }
    }

    private Transform CreateMarker(Transform parent, string name, Vector2 localPosition)
    {
        GameObject marker = new GameObject(name);
        marker.transform.SetParent(parent, false);
        marker.transform.localPosition = localPosition;
        return marker.transform;
    }

    private void CreatePlatform(Transform parent, string name, Vector2 localPosition, Vector2 size, Color color, bool worldPosition = false)
    {
        GameObject platform = new GameObject(name, typeof(BoxCollider2D));
        platform.layer = LayerMask.NameToLayer("Ground");
        platform.transform.SetParent(parent, false);
        if (worldPosition)
        {
            platform.transform.position = localPosition;
        }
        else
        {
            platform.transform.localPosition = localPosition;
        }

        GameObject visual = new GameObject("Visual", typeof(SpriteRenderer));
        visual.transform.SetParent(platform.transform, false);
        SpriteRenderer renderer = visual.GetComponent<SpriteRenderer>();
        renderer.sprite = placeholderSprite;
        renderer.color = color;
        renderer.sortingOrder = -1;

        if (placeholderSprite != null && placeholderSprite.bounds.size.x > 0f && placeholderSprite.bounds.size.y > 0f)
        {
            visual.transform.localScale = new Vector3(size.x / placeholderSprite.bounds.size.x, size.y / placeholderSprite.bounds.size.y, 1f);
        }

        platform.GetComponent<BoxCollider2D>().size = size;
    }

    private Color GetRoomColor(RoomType type)
    {
        switch (type)
        {
            case RoomType.Start:
                return new Color(0.22f, 0.32f, 0.22f, 1f);
            case RoomType.Treasure:
            case RoomType.Key:
                return new Color(0.46f, 0.34f, 0.12f, 1f);
            case RoomType.Challenge:
            case RoomType.Locked:
                return new Color(0.38f, 0.16f, 0.1f, 1f);
            case RoomType.Boss:
                return new Color(0.3f, 0.09f, 0.07f, 1f);
            case RoomType.Exit:
                return new Color(0.15f, 0.31f, 0.34f, 1f);
            default:
                return new Color(0.34f, 0.23f, 0.13f, 1f);
        }
    }

    private void OnDrawGizmosSelected()
    {
        if (settings != null && !settings.drawDebugGraph)
        {
            return;
        }

        Gizmos.color = Color.cyan;
        foreach (RoomGraphNode node in graph)
        {
            Vector3 position = transform.position + (Vector3)node.worldPosition;
            Gizmos.DrawWireCube(position, new Vector3(8f, 5f, 0f));
            foreach (int linkedId in node.linkedNodeIds)
            {
                if (linkedId > node.id && linkedId < graph.Count)
                {
                    Gizmos.DrawLine(position, transform.position + (Vector3)graph[linkedId].worldPosition);
                }
            }
        }
    }
}
