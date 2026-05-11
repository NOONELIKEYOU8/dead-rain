using System.Collections.Generic;
using UnityEngine;

public class BronzeMapGenerator : MonoBehaviour
{
    [SerializeField] private int seed;
    [SerializeField] private bool randomizeSeed = true;
    [SerializeField] private bool randomizeBossAltar = true;
    [SerializeField] private Sprite blockSprite;
    [SerializeField] private Transform player;
    [SerializeField] private Camera targetCamera;
    [SerializeField] private RunFlowController flowController;

    private static Sprite runtimeBlockSprite;

    private readonly List<Transform> enemySpawnPoints = new();
    private Transform bossSpawnPoint;
    private Transform portalPoint;

    public IReadOnlyList<Transform> EnemySpawnPoints => enemySpawnPoints;
    public Transform BossSpawnPoint => bossSpawnPoint;
    public Transform PortalPoint => portalPoint;

    private struct AreaSpec
    {
        public string name;
        public Vector2 center;
        public Vector2 size;
        public Color color;
        public int enemySpawnCount;
        public bool hasUpperRoute;

        public AreaSpec(string name, Vector2 center, Vector2 size, Color color, int enemySpawnCount, bool hasUpperRoute)
        {
            this.name = name;
            this.center = center;
            this.size = size;
            this.color = color;
            this.enemySpawnCount = enemySpawnCount;
            this.hasUpperRoute = hasUpperRoute;
        }
    }

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

        GameObject root = new GameObject("Fixed Bronze Block Map");
        root.transform.SetParent(transform, false);

        AreaSpec[] areas = CreateFixedAreas();
        CreateBackdrop(root.transform);
        CreateBaseGround(root.transform);

        for (int i = 0; i < areas.Length; i++)
        {
            CreateArea(root.transform, areas[i], i);
        }

        CreateBranchPlatforms(root.transform);
        CreateBossAltar(root.transform, areas);

        AreaSpec start = areas[0];
        AreaSpec exit = areas[9];
        PlacePlayer(new Vector2(start.center.x - 7f, -2.2f));
        portalPoint = CreateMarker(root.transform, "Next Era Portal Point", new Vector2(exit.center.x + exit.size.x * 0.34f, -2.1f));

        if (targetCamera == null)
        {
            targetCamera = Camera.main;
        }

        if (targetCamera != null)
        {
            targetCamera.transform.position = new Vector3(start.center.x + 3f, 1.5f, -10f);
        }
    }

    public Transform[] GetSpawnPointArray()
    {
        return enemySpawnPoints.ToArray();
    }

    private AreaSpec[] CreateFixedAreas()
    {
        Color bronze = new Color(0.35f, 0.22f, 0.1f, 1f);
        Color verdigris = new Color(0.12f, 0.31f, 0.25f, 1f);
        Color fire = new Color(0.44f, 0.15f, 0.06f, 1f);
        Color stone = new Color(0.16f, 0.14f, 0.12f, 1f);
        Color reward = new Color(0.48f, 0.35f, 0.11f, 1f);

        return new[]
        {
            new AreaSpec("Start Camp", new Vector2(-18f, 0f), new Vector2(20f, 8f), verdigris, 0, false),
            new AreaSpec("Broken Gate", new Vector2(10f, 0f), new Vector2(22f, 9f), bronze, 2, true),
            new AreaSpec("Long Road", new Vector2(38f, 0f), new Vector2(22f, 8f), bronze, 2, false),
            new AreaSpec("Lower Yard", new Vector2(66f, 0f), new Vector2(24f, 9f), stone, 3, true),
            new AreaSpec("Casting Foundry", new Vector2(96f, 0f), new Vector2(28f, 10f), fire, 3, true),
            new AreaSpec("Raised Causeway", new Vector2(128f, 0f), new Vector2(28f, 10f), verdigris, 3, true),
            new AreaSpec("Bronze Hall", new Vector2(160f, 0f), new Vector2(30f, 11f), bronze, 4, true),
            new AreaSpec("Cistern Walk", new Vector2(194f, 0f), new Vector2(30f, 10f), verdigris, 3, false),
            new AreaSpec("Old Market", new Vector2(228f, 0f), new Vector2(30f, 10f), bronze, 4, true),
            new AreaSpec("Outer Wall", new Vector2(264f, 0f), new Vector2(34f, 12f), stone, 3, true),
            new AreaSpec("Oracle Shrine", new Vector2(100f, 15f), new Vector2(24f, 8f), reward, 2, false),
            new AreaSpec("Treasure Loft", new Vector2(160f, 17f), new Vector2(24f, 8f), reward, 1, false),
            new AreaSpec("Root Cellar", new Vector2(130f, -14f), new Vector2(28f, 8f), stone, 3, false),
            new AreaSpec("Trial Pit", new Vector2(198f, -13f), new Vector2(30f, 9f), fire, 4, false),
            new AreaSpec("High Roof", new Vector2(230f, 17f), new Vector2(24f, 8f), verdigris, 1, false)
        };
    }

    private void CreateBaseGround(Transform root)
    {
        CreateBlock(root, "Continuous Ground", new Vector2(124f, -4.3f), new Vector2(330f, 2.2f), GroundColor(), -1, true);
        CreateBlock(root, "Left Boundary", new Vector2(-42f, 4f), new Vector2(2f, 18f), WallColor(), -1, true);
        CreateBlock(root, "Right Boundary", new Vector2(292f, 5f), new Vector2(2f, 20f), WallColor(), -1, true);

        for (int i = 0; i < 14; i++)
        {
            float x = -28f + i * 23f;
            float height = 1.1f + (i % 3) * 0.45f;
            CreateBlock(root, $"Ground Block Detail_{i}", new Vector2(x, -2.65f + height * 0.2f), new Vector2(5f + i % 2, height), DetailColor(), -1, true);
        }
    }

    private void CreateArea(Transform root, AreaSpec area, int index)
    {
        GameObject areaObject = new GameObject($"{index:00}_{area.name}");
        areaObject.transform.SetParent(root, false);
        areaObject.transform.position = area.center;

        CreateBlock(areaObject.transform, "Back Wall", new Vector2(0f, 1.4f), new Vector2(area.size.x, area.size.y), new Color(area.color.r, area.color.g, area.color.b, 0.34f), -4, false);

        if (index != 0)
        {
            CreateBlock(areaObject.transform, "Left Pillar", new Vector2(-area.size.x * 0.42f, 0.2f), new Vector2(1.2f, 5.4f), WallColor(), -1, true);
        }

        if (index % 3 != 1)
        {
            CreateBlock(areaObject.transform, "Right Pillar", new Vector2(area.size.x * 0.42f, 0.2f), new Vector2(1.2f, 5.4f), WallColor(), -1, true);
        }

        CreateBlock(areaObject.transform, "Low Platform", new Vector2(-area.size.x * 0.16f, -0.8f), new Vector2(6.2f, 0.55f), area.color, -1, true);
        CreateBlock(areaObject.transform, "Mid Platform", new Vector2(area.size.x * 0.16f, 1.2f), new Vector2(6.8f, 0.55f), area.color, -1, true);

        if (area.hasUpperRoute)
        {
            CreateBlock(areaObject.transform, "Upper Platform", new Vector2(-area.size.x * 0.04f, 3.3f), new Vector2(8f, 0.55f), StepColor(), -1, true);
            CreateBlock(areaObject.transform, "Small Step A", new Vector2(-area.size.x * 0.32f, 0.8f), new Vector2(3.5f, 0.5f), StepColor(), -1, true);
            CreateBlock(areaObject.transform, "Small Step B", new Vector2(area.size.x * 0.34f, 2.7f), new Vector2(3.5f, 0.5f), StepColor(), -1, true);
        }

        if (index == 4 || index == 6 || index == 8)
        {
            CreateBlock(areaObject.transform, "Ceiling Beam", new Vector2(0f, 5.1f), new Vector2(area.size.x * 0.68f, 0.55f), WallColor(), -1, true);
        }

        CreateEnemySpawnPoints(areaObject.transform, area);
    }

    private void CreateEnemySpawnPoints(Transform parent, AreaSpec area)
    {
        for (int i = 0; i < area.enemySpawnCount; i++)
        {
            float t = area.enemySpawnCount == 1 ? 0.5f : (i + 1f) / (area.enemySpawnCount + 1f);
            Vector2 spawn = new Vector2(Mathf.Lerp(-area.size.x * 0.34f, area.size.x * 0.34f, t), -2.35f);
            enemySpawnPoints.Add(CreateMarker(parent, $"EnemySpawn_{i}", spawn));
        }
    }

    private void CreateBranchPlatforms(Transform root)
    {
        CreateStair(root, "Shrine Stair", new Vector2(82f, -2.1f), new Vector2(98f, 11f), 6);
        CreateStair(root, "Treasure Stair", new Vector2(144f, -2f), new Vector2(160f, 13f), 7);
        CreateStair(root, "Roof Stair", new Vector2(214f, -2f), new Vector2(230f, 13f), 7);
        CreateStair(root, "Cellar Descent", new Vector2(116f, -3.1f), new Vector2(130f, -10f), 5);
        CreateStair(root, "Trial Descent", new Vector2(184f, -3.1f), new Vector2(198f, -9f), 5);

        CreateBlock(root, "Root Cellar Floor", new Vector2(130f, -17.2f), new Vector2(32f, 1.2f), GroundColor(), -1, true);
        CreateBlock(root, "Trial Pit Floor", new Vector2(198f, -16.4f), new Vector2(34f, 1.2f), GroundColor(), -1, true);
    }

    private void CreateStair(Transform root, string name, Vector2 from, Vector2 to, int steps)
    {
        for (int i = 0; i <= steps; i++)
        {
            float t = i / (float)steps;
            Vector2 pos = Vector2.Lerp(from, to, t);
            CreateBlock(root, $"{name}_{i}", pos, new Vector2(4.8f, 0.55f), PathColor(), -1, true);
        }
    }

    private void CreateBossAltar(Transform root, AreaSpec[] areas)
    {
        int[] candidates = { 4, 6, 7, 10, 13 };
        int chosenIndex = randomizeBossAltar ? candidates[Random.Range(0, candidates.Length)] : candidates[0];
        AreaSpec area = areas[chosenIndex];
        Vector2 altarPosition = new Vector2(area.center.x, area.center.y - 2.25f);

        GameObject altar = new GameObject("Random Boss Altar", typeof(BoxCollider2D), typeof(BossAltar));
        altar.transform.SetParent(root, false);
        altar.transform.position = altarPosition;

        BoxCollider2D trigger = altar.GetComponent<BoxCollider2D>();
        trigger.isTrigger = true;
        trigger.size = new Vector2(4f, 3.2f);

        CreateBlock(altar.transform, "Altar Base", Vector2.zero, new Vector2(4.2f, 0.65f), new Color(0.44f, 0.25f, 0.08f, 1f), 1, false);
        GameObject pillar = CreateBlock(altar.transform, "Altar Pillar", new Vector2(0f, 0.9f), new Vector2(1.3f, 2.1f), new Color(0.55f, 0.34f, 0.11f, 1f), 2, false);
        CreateBlock(altar.transform, "Altar Flame", new Vector2(0f, 2.25f), new Vector2(1f, 1f), new Color(0.95f, 0.28f, 0.08f, 1f), 3, false);

        bossSpawnPoint = CreateMarker(altar.transform, "Boss Spawn Point", new Vector2(6f, 0.5f));

        if (flowController == null)
        {
            flowController = FindObjectOfType<RunFlowController>();
        }

        BossAltar bossAltar = altar.GetComponent<BossAltar>();
        bossAltar.Initialize(flowController, bossSpawnPoint, pillar.GetComponent<SpriteRenderer>());
    }

    private void CreateBackdrop(Transform root)
    {
        CreateBlock(root, "Solid Sky Color", new Vector2(124f, 9f), new Vector2(340f, 62f), new Color(0.04f, 0.07f, 0.07f, 1f), -10, false);
        CreateBlock(root, "Distant Block Wall", new Vector2(124f, -0.5f), new Vector2(330f, 15f), new Color(0.08f, 0.12f, 0.1f, 1f), -9, false);
    }

    private GameObject CreateBlock(Transform parent, string name, Vector2 localPosition, Vector2 size, Color color, int sortingOrder, bool solid)
    {
        GameObject go = new GameObject(name, typeof(SpriteRenderer));
        go.transform.SetParent(parent, false);
        go.transform.localPosition = localPosition;

        SpriteRenderer renderer = go.GetComponent<SpriteRenderer>();
        renderer.sprite = GetBlockSprite();
        renderer.color = color;
        renderer.sortingOrder = sortingOrder;
        go.transform.localScale = new Vector3(size.x, size.y, 1f);

        if (solid)
        {
            go.layer = LayerMask.NameToLayer("Ground");
            BoxCollider2D collider = go.AddComponent<BoxCollider2D>();
            collider.size = Vector2.one;
        }

        return go;
    }

    private Transform CreateMarker(Transform parent, string name, Vector2 localPosition)
    {
        GameObject marker = new GameObject(name);
        marker.transform.SetParent(parent, false);
        marker.transform.localPosition = localPosition;
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

    private Sprite GetBlockSprite()
    {
        if (blockSprite != null)
        {
            return blockSprite;
        }

        if (runtimeBlockSprite == null)
        {
            Texture2D texture = new Texture2D(1, 1, TextureFormat.RGBA32, false);
            texture.name = "Runtime_PureColorBlock";
            texture.SetPixel(0, 0, Color.white);
            texture.Apply();
            runtimeBlockSprite = Sprite.Create(texture, new Rect(0f, 0f, 1f, 1f), new Vector2(0.5f, 0.5f), 1f);
            runtimeBlockSprite.name = "Runtime_PureColorBlock";
        }

        return runtimeBlockSprite;
    }

    private static Color GroundColor() => new Color(0.24f, 0.18f, 0.11f, 1f);
    private static Color WallColor() => new Color(0.11f, 0.09f, 0.07f, 1f);
    private static Color StepColor() => new Color(0.32f, 0.24f, 0.14f, 1f);
    private static Color PathColor() => new Color(0.27f, 0.2f, 0.12f, 1f);
    private static Color DetailColor() => new Color(0.18f, 0.27f, 0.22f, 1f);
}
