#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public static class BuildScriptValidationScene
{
    private const string ScenePath = "Assets/Scenes/ScriptValidation_Auto.unity";
    private const string GeneratedItemsFolder = "Assets/Items/Generated";

    [MenuItem("工具/DeadRain/构建脚本验证场景")]
    public static void BuildFromMenu()
    {
        BuildInternal(showDialog: true);
    }

    public static void BuildFromCommandLine()
    {
        BuildInternal(showDialog: false);
    }

    private static void BuildInternal(bool showDialog)
    {
        EnsureFolder("Assets/Items");
        EnsureFolder(GeneratedItemsFolder);

        ItemData damageItem = GetOrCreateItem(
            GeneratedItemsFolder + "/itm_damage_mark.asset",
            "itm_damage_mark",
            "狂战印记",
            ItemEffectType.DamagePercent,
            0.15f,
            20,
            "每层提升伤害。"
        );

        ItemData critItem = GetOrCreateItem(
            GeneratedItemsFolder + "/itm_crit_lens.asset",
            "itm_crit_lens",
            "猎杀镜片",
            ItemEffectType.CritChanceFlat,
            0.05f,
            20,
            "每层提升暴击率。"
        );

        Scene scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

        var cameraGO = new GameObject("Main Camera");
        cameraGO.tag = "MainCamera";
        cameraGO.transform.position = new Vector3(0f, 0f, -10f);
        var camera = cameraGO.AddComponent<Camera>();
        camera.orthographic = true;
        camera.orthographicSize = 5f;
        camera.clearFlags = CameraClearFlags.SolidColor;
        camera.backgroundColor = new Color(0.11f, 0.11f, 0.14f, 1f);
        cameraGO.AddComponent<AudioListener>();
        var cameraFollow = cameraGO.AddComponent<CameraFollow>();

        var ground = new GameObject("Ground");
        ground.transform.position = new Vector3(0f, -2.2f, 0f);
        var groundRenderer = ground.AddComponent<SpriteRenderer>();
        groundRenderer.sprite = CreateSolidSprite(new Color(0.28f, 0.30f, 0.34f, 1f), 16, 16, 16f);
        ground.transform.localScale = new Vector3(30f, 1f, 1f);
        var groundCollider = ground.AddComponent<BoxCollider2D>();
        groundCollider.size = new Vector2(30f, 1f);

        var player = CreatePlayer();
        player.transform.position = new Vector3(-3f, -1f, 0f);

        cameraFollow.target = player.transform;

        var runDirectorGO = new GameObject("RunDirector");
        var runDirector = runDirectorGO.AddComponent<RunDirectorLite>();

        var dropServiceGO = new GameObject("DropService");
        var dropService = dropServiceGO.AddComponent<DropService>();

        var inventory = player.GetComponent<RunInventory>();
        var itemService = player.GetComponent<ItemEffectService>();
        itemService.inventory = inventory;
        itemService.itemCatalog = new[] { damageItem, critItem };

        var playerController = player.GetComponent<PlayerController>();

        dropService.targetInventory = inventory;
        dropService.difficultyProviderBehaviour = runDirector;
        dropService.dropRules = new[]
        {
            new DropService.DropRule
            {
                enemyTypeId = "Enemy",
                item = damageItem,
                baseChance = 0.7f
            },
            new DropService.DropRule
            {
                enemyTypeId = "Enemy",
                item = critItem,
                baseChance = 0.5f
            }
        };

        var spawnPoint = new GameObject("EnemySpawnPoint");
        spawnPoint.transform.position = new Vector3(3f, -1f, 0f);

        var minionPrefab = GetOrCreateMinionPrefab();

        var spawnerGO = new GameObject("EnemySpawner");
        var spawner = spawnerGO.AddComponent<EnemySpawner>();
        spawner.spawnPoint = spawnPoint.transform;
        spawner.enemyPrefab = minionPrefab;
        spawner.spawnOnStart = true;
        spawner.respawnDelay = 2.5f;

        var gmGO = new GameObject("GameManager");
        var gm = gmGO.AddComponent<GameManager>();
        var playerSpawn = new GameObject("PlayerSpawn");
        playerSpawn.transform.position = player.transform.position;
        gm.playerSpawn = playerSpawn.transform;

        CreateHUD(playerController, runDirector, inventory);

        EditorSceneManager.SaveScene(scene, ScenePath, true);
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();

        if (showDialog)
        {
            EditorUtility.DisplayDialog(
                "构建完成",
                "脚本验证场景已创建:\n" + ScenePath + "\n\n可直接打开运行测试。",
                "确定"
            );
        }

        Debug.Log("[BuildScriptValidationScene] 场景已生成: " + ScenePath);
    }

    private static GameObject CreatePlayer()
    {
        var player = new GameObject("Player");
        player.tag = "Player";

        var sr = player.AddComponent<SpriteRenderer>();
        sr.color = new Color(0.33f, 0.8f, 1f, 1f);
        sr.sprite = CreateSolidSprite(new Color(0.33f, 0.8f, 1f, 1f), 16, 32, 20f);

        var rb = player.AddComponent<Rigidbody2D>();
        rb.gravityScale = 3f;
        rb.freezeRotation = true;

        var col = player.AddComponent<CapsuleCollider2D>();
        col.size = new Vector2(0.8f, 1.6f);

        var controller = player.AddComponent<PlayerController>();
        controller.groundLayer = LayerMask.GetMask("Default");

        var combat = player.AddComponent<PlayerCombatModule>();
        combat.enableRanged = true;
        combat.meleeTargetLayers = ~0;

        player.AddComponent<RunInventory>();
        player.AddComponent<ItemEffectService>();

        return player;
    }

    private static void CreateHUD(PlayerController player, RunDirectorLite runDirector, RunInventory inventory)
    {
        var existingEventSystem = Object.FindObjectOfType<EventSystem>();
        if (existingEventSystem == null)
        {
            new GameObject("EventSystem", typeof(EventSystem), typeof(StandaloneInputModule));
        }

        var canvasGO = new GameObject("HUDCanvas", typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
        var canvas = canvasGO.GetComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 100;

        var scaler = canvasGO.GetComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920f, 1080f);

        var panelGO = new GameObject("TopLeftPanel", typeof(Image));
        panelGO.transform.SetParent(canvasGO.transform, false);
        var panelRT = panelGO.GetComponent<RectTransform>();
        panelRT.anchorMin = new Vector2(0f, 1f);
        panelRT.anchorMax = new Vector2(0f, 1f);
        panelRT.pivot = new Vector2(0f, 1f);
        panelRT.anchoredPosition = new Vector2(24f, -24f);
        panelRT.sizeDelta = new Vector2(520f, 220f);
        var panelImage = panelGO.GetComponent<Image>();
        panelImage.color = new Color(0f, 0f, 0f, 0.45f);

        Font font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");

        var titleText = CreateText(panelGO.transform, "TitleText", font, 22, FontStyle.Bold, TextAnchor.UpperLeft);
        var titleRT = titleText.rectTransform;
        titleRT.anchorMin = new Vector2(0f, 1f);
        titleRT.anchorMax = new Vector2(0f, 1f);
        titleRT.pivot = new Vector2(0f, 1f);
        titleRT.anchoredPosition = new Vector2(16f, -14f);
        titleRT.sizeDelta = new Vector2(488f, 32f);
        titleText.text = "DeadRain - Script Validation";
        titleText.color = new Color(0.88f, 0.94f, 1f, 1f);

        var statusText = CreateText(panelGO.transform, "StatusText", font, 18, FontStyle.Normal, TextAnchor.UpperLeft);
        var statusRT = statusText.rectTransform;
        statusRT.anchorMin = new Vector2(0f, 1f);
        statusRT.anchorMax = new Vector2(0f, 1f);
        statusRT.pivot = new Vector2(0f, 1f);
        statusRT.anchoredPosition = new Vector2(16f, -54f);
        statusRT.sizeDelta = new Vector2(488f, 76f);
        float threat = runDirector != null ? runDirector.GetThreatLevel() : 0f;
        int hp = player != null ? player.currentHealth : 0;
        int hpMax = player != null ? player.maxHealth : 0;
        statusText.text = "HP " + hp + "/" + hpMax + "\nThreat " + threat.ToString("0.00");
        statusText.color = new Color(0.9f, 0.95f, 1f, 1f);

        var itemText = CreateText(panelGO.transform, "ItemText", font, 16, FontStyle.Normal, TextAnchor.UpperLeft);
        var itemRT = itemText.rectTransform;
        itemRT.anchorMin = new Vector2(0f, 1f);
        itemRT.anchorMax = new Vector2(0f, 1f);
        itemRT.pivot = new Vector2(0f, 1f);
        itemRT.anchoredPosition = new Vector2(16f, -134f);
        itemRT.sizeDelta = new Vector2(488f, 46f);
        itemText.text = "Items: (spawn then pick up to test)";
        itemText.color = new Color(1f, 0.92f, 0.74f, 1f);

        var hintText = CreateText(panelGO.transform, "HintText", font, 14, FontStyle.Normal, TextAnchor.LowerLeft);
        var hintRT = hintText.rectTransform;
        hintRT.anchorMin = new Vector2(0f, 0f);
        hintRT.anchorMax = new Vector2(0f, 0f);
        hintRT.pivot = new Vector2(0f, 0f);
        hintRT.anchoredPosition = new Vector2(16f, 12f);
        hintRT.sizeDelta = new Vector2(488f, 28f);
        hintText.text = "Move:A/D  Jump:Space  Attack:J  Ranged:L  Parry:K  Roll:LeftShift";
        hintText.color = new Color(0.85f, 0.91f, 1f, 1f);

    }

    private static Text CreateText(Transform parent, string name, Font font, int fontSize, FontStyle style, TextAnchor anchor)
    {
        var go = new GameObject(name, typeof(RectTransform), typeof(Text));
        go.transform.SetParent(parent, false);
        var text = go.GetComponent<Text>();
        text.font = font;
        text.fontSize = fontSize;
        text.fontStyle = style;
        text.alignment = anchor;
        text.horizontalOverflow = HorizontalWrapMode.Wrap;
        text.verticalOverflow = VerticalWrapMode.Overflow;
        return text;
    }

    private static Sprite CreateSolidSprite(Color color, int width, int height, float pixelsPerUnit)
    {
        var tex = new Texture2D(width, height, TextureFormat.RGBA32, false);
        for (int y = 0; y < height; y++)
        {
            for (int x = 0; x < width; x++)
            {
                bool border = x == 0 || y == 0 || x == width - 1 || y == height - 1;
                tex.SetPixel(x, y, border ? Color.black : color);
            }
        }
        tex.filterMode = FilterMode.Point;
        tex.wrapMode = TextureWrapMode.Clamp;
        tex.Apply();

        return Sprite.Create(
            tex,
            new Rect(0f, 0f, width, height),
            new Vector2(0.5f, 0.5f),
            pixelsPerUnit,
            0,
            SpriteMeshType.FullRect);
    }

    private static GameObject GetOrCreateMinionPrefab()
    {
        const string prefabPath = "Assets/Prefabs/MinionEnemy.prefab";
        var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(prefabPath);
        if (prefab != null)
        {
            return prefab;
        }

        EnsureFolder("Assets/Prefabs");
        EnsureFolder("Assets/Prefabs/Generated");

        var temp = new GameObject("MinionEnemy_Temp");
        temp.AddComponent<SpriteRenderer>().color = new Color(0.2f, 0.95f, 0.8f, 1f);

        var rb = temp.AddComponent<Rigidbody2D>();
        rb.gravityScale = 3f;
        rb.freezeRotation = true;

        var col = temp.AddComponent<BoxCollider2D>();
        col.size = new Vector2(0.8f, 1.2f);

        var minion = temp.AddComponent<MinionEnemy>();
        minion.enemyTypeId = "Enemy";
        minion.useActiveStrike = true;
        minion.strikeRange = 1.1f;
        minion.strikeDamage = 1;

        var saved = PrefabUtility.SaveAsPrefabAsset(temp, "Assets/Prefabs/Generated/MinionEnemy.prefab");
        Object.DestroyImmediate(temp);
        return saved;
    }

    private static ItemData GetOrCreateItem(
        string assetPath,
        string itemId,
        string displayName,
        ItemEffectType effectType,
        float effectValue,
        int maxStack,
        string description)
    {
        var item = AssetDatabase.LoadAssetAtPath<ItemData>(assetPath);
        if (item != null)
        {
            return item;
        }

        item = ScriptableObject.CreateInstance<ItemData>();
        item.itemId = itemId;
        item.displayName = displayName;
        item.effectType = effectType;
        item.effectValue = effectValue;
        item.maxStack = maxStack;
        item.description = description;

        AssetDatabase.CreateAsset(item, assetPath);
        EditorUtility.SetDirty(item);
        return item;
    }

    private static void EnsureFolder(string folderPath)
    {
        if (AssetDatabase.IsValidFolder(folderPath)) return;

        int splitIndex = folderPath.LastIndexOf('/');
        if (splitIndex <= 0) return;

        string parent = folderPath.Substring(0, splitIndex);
        string child = folderPath.Substring(splitIndex + 1);

        EnsureFolder(parent);
        if (!AssetDatabase.IsValidFolder(folderPath))
        {
            AssetDatabase.CreateFolder(parent, child);
        }
    }
}
#endif
