#if UNITY_EDITOR
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

public static class RogueliteRoomPrototypeBuilder
{
    private const string RoomFolder = "Assets/Prefabs/Roguelite/Rooms/Bronze";
    private const string SettingsPath = "Assets/Scripts/Roguelite/GeneratedData/Runtime/BronzeMapSettings.asset";
    private const string BronzeEraPath = "Assets/Scripts/Roguelite/GeneratedData/Eras/BronzeEra.asset";
    private const string BronzeScenePath = "Assets/Scenes/Era_Bronze_Fixed.unity";

    [MenuItem("Tools/Dead Rain/Build Bronze Room Prefabs")]
    public static void BuildBronzeRooms()
    {
        EnsureFolder(RoomFolder);
        EnsureFolder("Assets/Scripts/Roguelite/GeneratedData/Runtime");

        Sprite sprite = LoadPlaceholderSprite();
        RoomDefinition[] rooms =
        {
            CreateRoomPrefab("Bronze_StartRoom", RoomType.Start, sprite),
            CreateRoomPrefab("Bronze_CombatRoom_A", RoomType.Combat, sprite),
            CreateRoomPrefab("Bronze_CombatRoom_B", RoomType.Combat, sprite),
            CreateRoomPrefab("Bronze_TreasureRoom", RoomType.Treasure, sprite),
            CreateRoomPrefab("Bronze_ChallengeRoom", RoomType.Challenge, sprite),
            CreateRoomPrefab("Bronze_KeyRoom", RoomType.Key, sprite),
            CreateRoomPrefab("Bronze_LockedRoom", RoomType.Locked, sprite),
            CreateRoomPrefab("Bronze_BossAnteRoom", RoomType.BossAnte, sprite),
            CreateRoomPrefab("Bronze_BossRoom", RoomType.Boss, sprite),
            CreateRoomPrefab("Bronze_ExitRoom", RoomType.Exit, sprite),
            CreateRoomPrefab("Bronze_SecretRoom", RoomType.Secret, sprite)
        };

        ProceduralMapSettings settings = AssetDatabase.LoadAssetAtPath<ProceduralMapSettings>(SettingsPath);
        if (settings == null)
        {
            settings = ScriptableObject.CreateInstance<ProceduralMapSettings>();
            AssetDatabase.CreateAsset(settings, SettingsPath);
        }

        settings.randomizeSeed = true;
        settings.mainPathLength = 8;
        settings.minBranchCount = 2;
        settings.maxBranchCount = 4;
        settings.maxBranchLength = 3;
        settings.roomSpacingX = 34f;
        settings.roomSpacingY = 20f;
        settings.runConnectivityCheck = true;
        settings.drawDebugGraph = true;
        EditorUtility.SetDirty(settings);

        EraStageData bronzeEra = AssetDatabase.LoadAssetAtPath<EraStageData>(BronzeEraPath);
        if (bronzeEra != null)
        {
            bronzeEra.roomPrefabPool = rooms;
            bronzeEra.nextEra = EraId.QinHanThreeKingdoms;
            bronzeEra.nextEraId = "era_qinhanthreekingdoms";
            EditorUtility.SetDirty(bronzeEra);
        }

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        Debug.Log($"Built {rooms.Length} bronze room prefabs and map settings.");
    }

    [MenuItem("Tools/Dead Rain/Create Bronze Fixed Scene")]
    public static void CreateBronzeFixedScene()
    {
        EraStageData bronzeEra = AssetDatabase.LoadAssetAtPath<EraStageData>(BronzeEraPath);
        DifficultyScalingData difficulty = AssetDatabase.LoadAssetAtPath<DifficultyScalingData>("Assets/Scripts/Roguelite/GeneratedData/Runtime/DefaultDifficulty.asset");
        RunRewardTableData rewards = AssetDatabase.LoadAssetAtPath<RunRewardTableData>("Assets/Scripts/Roguelite/GeneratedData/Runtime/BronzeRewardTable.asset");
        GameObject bossPrefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/Roguelite/Bosses/BronzeKingShadow.prefab");
        GameObject playerPrefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/Roguelite/Player_RunPrototype.prefab");
        Sprite sprite = LoadPureBlockSprite();

        EnsureFolder("Assets/Scenes");
        UnityEngine.SceneManagement.Scene scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
        scene.name = "Era_Bronze_Fixed";

        GameObject cameraObject = new GameObject("Main Camera", typeof(Camera), typeof(AudioListener));
        cameraObject.tag = "MainCamera";
        cameraObject.transform.position = new Vector3(-2f, 3f, -10f);
        Camera camera = cameraObject.GetComponent<Camera>();
        camera.orthographic = true;
        camera.orthographicSize = 6f;
        camera.backgroundColor = new Color(0.05f, 0.07f, 0.07f, 1f);

        GameObject player = null;
        if (playerPrefab != null)
        {
            player = (GameObject)PrefabUtility.InstantiatePrefab(playerPrefab);
        }

        if (player == null)
        {
            player = new GameObject("Player", typeof(Rigidbody2D), typeof(BoxCollider2D), typeof(SpriteRenderer));
            player.tag = "Player";
        }

        player.name = "Player";
        player.transform.position = new Vector3(-6f, 0.5f, 0f);
        player.SetActive(true);

        CameraFollow2D follow = cameraObject.AddComponent<CameraFollow2D>();
        SetSerializedReference(follow, "target", player.transform);

        if (player.GetComponent<RunInventorySystem>() == null)
        {
            player.AddComponent<RunInventorySystem>();
        }
        if (player.GetComponent<PlayerRunStats>() == null)
        {
            player.AddComponent<PlayerRunStats>();
        }
        if (player.GetComponent<PlayerStats>() == null)
        {
            player.AddComponent<PlayerStats>();
        }

        GameObject afterImagePrefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/AfterImage.prefab");
        if (afterImagePrefab != null)
        {
            GameObject poolObject = new GameObject("PlayerAfterImagePool", typeof(PlayerAfterImagePool));
            SetSerializedReference(poolObject.GetComponent<PlayerAfterImagePool>(), "afterImagePrefab", afterImagePrefab);
        }

        GameObject mapObject = new GameObject("Fixed Bronze Map Prototype");
        BronzeMapGenerator generator = EnsureComponent<BronzeMapGenerator>(mapObject);
        SetSerializedReference(generator, "player", player != null ? player.transform : null);
        SetSerializedReference(generator, "targetCamera", camera);
        SetSerializedReference(generator, "blockSprite", sprite);

        GameObject managerObject = new GameObject("Roguelite Runtime");

        GameRunManager run = EnsureComponent<GameRunManager>(managerObject);
        SetSerializedReference(run, "difficultyScaling", difficulty);
        SetSerializedReference(run, "startingEra", bronzeEra);

        DifficultyDirector director = EnsureComponent<DifficultyDirector>(managerObject);
        SetSerializedReference(director, "difficultyScaling", difficulty);
        SetSerializedReference(director, "runManager", run);

        EraStageSystem eraSystem = EnsureComponent<EraStageSystem>(managerObject);
        SerializedObject eraSo = new SerializedObject(eraSystem);
        SerializedProperty eras = eraSo.FindProperty("eras");
        eras.arraySize = 1;
        eras.GetArrayElementAtIndex(0).objectReferenceValue = bronzeEra;
        eraSo.ApplyModifiedPropertiesWithoutUndo();

        EnemySpawnDirector spawnDirector = EnsureComponent<EnemySpawnDirector>(managerObject);
        SetSerializedReference(spawnDirector, "eraStageSystem", eraSystem);
        SetSerializedReference(spawnDirector, "proceduralMapGenerator", null);
        SetSerializedReference(spawnDirector, "mapGenerator", generator);

        RunFlowController flow = EnsureComponent<RunFlowController>(managerObject);
        SetSerializedReference(flow, "spawnDirector", spawnDirector);
        SetSerializedReference(flow, "eraStageSystem", eraSystem);
        SetSerializedReference(flow, "proceduralMapGenerator", null);
        SetSerializedReference(flow, "mapGenerator", generator);
        SetSerializedReference(flow, "rewardTable", rewards);
        SetSerializedReference(flow, "bronzeBossPrefab", bossPrefab != null ? bossPrefab.GetComponent<BossBase>() : null);
        SetSerializedBool(flow, "spawnBossByKillCount", false);
        SetSerializedReference(generator, "flowController", flow);

        GameObject portal = new GameObject("Next Era Portal", typeof(SpriteRenderer), typeof(BoxCollider2D), typeof(NextEraPortal));
        SpriteRenderer portalRenderer = portal.GetComponent<SpriteRenderer>();
        portalRenderer.sprite = sprite;
        portalRenderer.color = new Color(0.2f, 0.9f, 1f, 0.75f);
        portalRenderer.sortingOrder = 1;
        portal.GetComponent<BoxCollider2D>().isTrigger = true;
        portal.SetActive(false);
        SetSerializedReference(flow, "nextEraPortal", portal);
        portal.GetComponent<NextEraPortal>().Initialize(flow);

        GameObject backdrop = new GameObject("Bronze Backdrop", typeof(SpriteRenderer));
        backdrop.transform.position = new Vector3(0f, 2.5f, 1f);
        backdrop.transform.localScale = new Vector3(13f, 7f, 1f);
        SpriteRenderer backdropRenderer = backdrop.GetComponent<SpriteRenderer>();
        backdropRenderer.sprite = sprite;
        backdropRenderer.color = new Color(0.08f, 0.15f, 0.14f, 0.55f);
        backdropRenderer.sortingOrder = -5;

        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene, BronzeScenePath);
        Debug.Log($"Created bronze fixed scene at {BronzeScenePath}.");
    }

    [MenuItem("Tools/Dead Rain/Create Bronze Procedural Scene")]
    public static void CreateBronzeProceduralScene()
    {
        CreateBronzeFixedScene();
    }

    private static RoomDefinition CreateRoomPrefab(string name, RoomType type, Sprite sprite)
    {
        GameObject root = new GameObject(name);
        RoomDefinition room = root.AddComponent<RoomDefinition>();
        room.roomId = name;
        room.roomType = type;
        room.width = 28;
        room.height = type == RoomType.Boss ? 14 : 11;
        room.entranceDirections = type == RoomType.Start ? RoomConnectionDirection.None : RoomConnectionDirection.Left | RoomConnectionDirection.Up | RoomConnectionDirection.Down;
        room.exitDirections = type == RoomType.Exit ? RoomConnectionDirection.None : RoomConnectionDirection.Right | RoomConnectionDirection.Up | RoomConnectionDirection.Down;
        room.difficultyWeight = type == RoomType.Challenge ? 3 : type == RoomType.Boss ? 5 : 1;
        room.allowEliteSpawns = type == RoomType.Combat || type == RoomType.Challenge || type == RoomType.BossAnte;
        room.requiresKey = type == RoomType.Locked;
        room.lockId = type == RoomType.Locked ? "bronze_main_key" : string.Empty;
        room.eraTags = new[] { "bronze", "placeholder" };

        float width = type == RoomType.Boss ? 32f : 26f;
        float height = type == RoomType.Boss ? 13f : 10f;
        CreatePlatform(root.transform, "Floor", new Vector2(0f, -height * 0.45f), new Vector2(width, 0.8f), RoomColor(type), sprite);
        CreatePlatform(root.transform, "Left Wall", new Vector2(-width * 0.5f, 0f), new Vector2(0.45f, height), new Color(0.12f, 0.08f, 0.05f, 1f), sprite);
        CreatePlatform(root.transform, "Right Wall", new Vector2(width * 0.5f, 0f), new Vector2(0.45f, height), new Color(0.12f, 0.08f, 0.05f, 1f), sprite);

        if (type == RoomType.Combat || type == RoomType.Challenge || type == RoomType.BossAnte || type == RoomType.Locked)
        {
            CreatePlatform(root.transform, "Step_Left", new Vector2(-width * 0.22f, 0.2f), new Vector2(5.2f, 0.35f), new Color(0.34f, 0.24f, 0.13f, 1f), sprite);
            CreatePlatform(root.transform, "Step_Right", new Vector2(width * 0.22f, 1.9f), new Vector2(5.2f, 0.35f), new Color(0.34f, 0.24f, 0.13f, 1f), sprite);
        }

        room.playerStartPoint = CreateMarker(root.transform, "PlayerStart", new Vector2(-width * 0.35f, -height * 0.25f));
        room.enemySpawnPoints = new[]
        {
            CreateMarker(root.transform, "EnemySpawn_A", new Vector2(-width * 0.18f, -height * 0.25f)),
            CreateMarker(root.transform, "EnemySpawn_B", new Vector2(width * 0.18f, -height * 0.25f))
        };
        room.itemSpawnPoints = new[] { CreateMarker(root.transform, "ItemSpawn", new Vector2(0f, -height * 0.18f)) };
        room.treasureSpawnPoints = new[] { CreateMarker(root.transform, "TreasureSpawn", new Vector2(0f, -height * 0.18f)) };
        room.bossSpawnPoint = CreateMarker(root.transform, "BossSpawn", new Vector2(0f, -height * 0.25f));
        room.exitPoint = CreateMarker(root.transform, "ExitPoint", new Vector2(width * 0.35f, -height * 0.18f));
        room.connectors = new[]
        {
            CreateConnector(root.transform, "Connector_Left", RoomConnectionDirection.Left, new Vector2(-width * 0.5f, -height * 0.22f)),
            CreateConnector(root.transform, "Connector_Right", RoomConnectionDirection.Right, new Vector2(width * 0.5f, -height * 0.22f)),
            CreateConnector(root.transform, "Connector_Up", RoomConnectionDirection.Up, new Vector2(0f, height * 0.25f)),
            CreateConnector(root.transform, "Connector_Down", RoomConnectionDirection.Down, new Vector2(0f, -height * 0.45f))
        };

        string path = $"{RoomFolder}/{name}.prefab";
        GameObject prefab = PrefabUtility.SaveAsPrefabAsset(root, path);
        Object.DestroyImmediate(root);
        return prefab.GetComponent<RoomDefinition>();
    }

    private static RoomConnector CreateConnector(Transform parent, string name, RoomConnectionDirection direction, Vector2 localPosition)
    {
        GameObject marker = new GameObject(name, typeof(RoomConnector));
        marker.transform.SetParent(parent, false);
        marker.transform.localPosition = localPosition;
        RoomConnector connector = marker.GetComponent<RoomConnector>();
        connector.direction = direction;
        connector.connectorId = name;
        return connector;
    }

    private static Transform CreateMarker(Transform parent, string name, Vector2 localPosition)
    {
        GameObject marker = new GameObject(name);
        marker.transform.SetParent(parent, false);
        marker.transform.localPosition = localPosition;
        return marker.transform;
    }

    private static void CreatePlatform(Transform parent, string name, Vector2 localPosition, Vector2 size, Color color, Sprite sprite)
    {
        GameObject platform = new GameObject(name, typeof(BoxCollider2D));
        platform.layer = LayerMask.NameToLayer("Ground");
        platform.transform.SetParent(parent, false);
        platform.transform.localPosition = localPosition;

        GameObject visual = new GameObject("Visual", typeof(SpriteRenderer));
        visual.transform.SetParent(platform.transform, false);
        SpriteRenderer renderer = visual.GetComponent<SpriteRenderer>();
        renderer.sprite = sprite;
        renderer.color = color;
        renderer.sortingOrder = -1;
        if (sprite != null && sprite.bounds.size.x > 0f && sprite.bounds.size.y > 0f)
        {
            visual.transform.localScale = new Vector3(size.x / sprite.bounds.size.x, size.y / sprite.bounds.size.y, 1f);
        }

        platform.GetComponent<BoxCollider2D>().size = size;
    }

    private static Color RoomColor(RoomType type)
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
            case RoomType.Secret:
                return new Color(0.16f, 0.13f, 0.22f, 1f);
            default:
                return new Color(0.34f, 0.23f, 0.13f, 1f);
        }
    }

    private static Sprite LoadPlaceholderSprite()
    {
        return LoadPureBlockSprite();
    }

    private static Sprite LoadPureBlockSprite()
    {
        const string spritePath = "Assets/Sprites/Generated/Placeholder_PureColorBlock.png";
        Sprite sprite = AssetDatabase.LoadAssetAtPath<Sprite>(spritePath);
        if (sprite != null)
        {
            return sprite;
        }

        EnsureFolder("Assets/Sprites/Generated");
        Texture2D texture = new Texture2D(16, 16, TextureFormat.RGBA32, false);
        Color[] pixels = new Color[16 * 16];
        for (int i = 0; i < pixels.Length; i++)
        {
            pixels[i] = Color.white;
        }

        texture.SetPixels(pixels);
        texture.Apply();
        File.WriteAllBytes(spritePath, texture.EncodeToPNG());
        Object.DestroyImmediate(texture);

        AssetDatabase.ImportAsset(spritePath);
        TextureImporter importer = (TextureImporter)AssetImporter.GetAtPath(spritePath);
        importer.textureType = TextureImporterType.Sprite;
        importer.spritePixelsPerUnit = 16f;
        importer.filterMode = FilterMode.Point;
        importer.textureCompression = TextureImporterCompression.Uncompressed;
        importer.SaveAndReimport();
        return AssetDatabase.LoadAssetAtPath<Sprite>(spritePath);
    }

    private static T EnsureComponent<T>(GameObject target) where T : Component
    {
        T component = target.GetComponent<T>();
        return component != null ? component : target.AddComponent<T>();
    }

    private static void SetSerializedReference(Object target, string fieldName, Object value)
    {
        SerializedObject so = new SerializedObject(target);
        SerializedProperty property = so.FindProperty(fieldName);
        if (property != null)
        {
            property.objectReferenceValue = value;
            so.ApplyModifiedPropertiesWithoutUndo();
        }
    }

    private static void SetSerializedBool(Object target, string fieldName, bool value)
    {
        SerializedObject so = new SerializedObject(target);
        SerializedProperty property = so.FindProperty(fieldName);
        if (property != null)
        {
            property.boolValue = value;
            so.ApplyModifiedPropertiesWithoutUndo();
        }
    }

    private static void EnsureFolder(string path)
    {
        if (AssetDatabase.IsValidFolder(path))
        {
            return;
        }

        string parent = Path.GetDirectoryName(path).Replace("\\", "/");
        string name = Path.GetFileName(path);
        EnsureFolder(parent);
        AssetDatabase.CreateFolder(parent, name);
    }
}
#endif
