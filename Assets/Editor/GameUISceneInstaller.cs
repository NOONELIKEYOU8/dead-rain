using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public static class GameUISceneInstaller
{
    [MenuItem("Tools/Dead Rain/Install Scene UI")]
    public static void Install()
    {
        GameObject uiObject = GameObject.Find("Game UI");
        if (uiObject == null)
        {
            uiObject = new GameObject("Game UI", typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster), typeof(GameUIBootstrapper));
        }
        else
        {
            EnsureComponent<Canvas>(uiObject);
            EnsureComponent<CanvasScaler>(uiObject);
            EnsureComponent<GraphicRaycaster>(uiObject);
            EnsureComponent<GameUIBootstrapper>(uiObject);
        }

        Canvas canvas = uiObject.GetComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;

        CanvasScaler scaler = uiObject.GetComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920f, 1080f);
        scaler.matchWidthOrHeight = 0.5f;

        ConfigureHudTextureImports();

        RectTransform playerHealth = EnsureRectChild(uiObject.transform, "Player Health");
        ConfigurePlayerHealth(playerHealth);

        RectTransform weaponHud = EnsureRectChild(uiObject.transform, "Weapon HUD");
        ConfigureWeaponHud(weaponHud);

        RectTransform enemyRoot = EnsureRectChild(uiObject.transform, "Enemy Health Bars");
        ConfigureFullScreenRect(enemyRoot);

        RectTransform deathOverlay = EnsureRectChild(uiObject.transform, "Death Overlay");
        ConfigureDeathOverlay(deathOverlay);

        if (Object.FindObjectOfType<EventSystem>() == null)
        {
            new GameObject("EventSystem", typeof(EventSystem), typeof(StandaloneInputModule));
        }

        EditorUtility.SetDirty(uiObject);
        EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());
        EditorSceneManager.SaveOpenScenes();
    }

    private static void ConfigurePlayerHealth(RectTransform rect)
    {
        rect.anchorMin = new Vector2(0f, 1f);
        rect.anchorMax = new Vector2(0f, 1f);
        rect.pivot = new Vector2(0f, 1f);
        rect.anchoredPosition = new Vector2(28f, -28f);
        rect.sizeDelta = new Vector2(390f, 58f);

        ClearChildren(rect);
        GameUIBootstrapper.CreateHealthBar(rect, "HP", new Color(0.7f, 0.04f, 0.08f, 1f), true);

    }

    private static void ConfigureWeaponHud(RectTransform rect)
    {
        rect.anchorMin = new Vector2(0f, 0f);
        rect.anchorMax = new Vector2(0f, 0f);
        rect.pivot = new Vector2(0f, 0f);
        rect.anchoredPosition = new Vector2(28f, 26f);
        rect.sizeDelta = new Vector2(680f, 58f);

        HorizontalLayoutGroup layout = EnsureComponent<HorizontalLayoutGroup>(rect.gameObject);
        layout.spacing = 8f;
        layout.childForceExpandWidth = false;
        layout.childForceExpandHeight = false;
        layout.childAlignment = TextAnchor.MiddleLeft;

        Image backgroundArt = EnsureComponent<Image>(rect.gameObject);
        backgroundArt.sprite = null;
        backgroundArt.color = new Color(0f, 0f, 0f, 0f);
        backgroundArt.raycastTarget = false;

        WeaponHUD hud = EnsureComponent<WeaponHUD>(rect.gameObject);
        hud.Configure(rect);

        ClearChildren(rect);

        PlayerInventory inventory = Object.FindObjectOfType<PlayerInventory>();
        if (inventory != null && inventory.weapons != null)
        {
            for (int i = 0; i < inventory.weapons.Length; i++)
            {
                Weapon weapon = inventory.weapons[i];
                if (weapon != null && weapon != inventory.GetShield())
                {
                    Color color = i == inventory.PrimaryWeaponIndex
                        ? new Color(0.78f, 0.62f, 0.27f, 0.98f)
                        : new Color(0.055f, 0.065f, 0.075f, 0.94f);
                    Sprite sprite = i == inventory.PrimaryWeaponIndex ? LoadSprite("WeaponSlotSelected") : LoadSprite("WeaponSlot");
                    CreatePreviewSlot(rect, (i + 1).ToString(), weapon.name, color, sprite);
                }
            }
        }

        CreatePreviewSlot(rect, "R", "Shield", new Color(0.09f, 0.27f, 0.45f, 0.95f), LoadSprite("ShieldSlot"));
    }

    private static void ConfigureFullScreenRect(RectTransform rect)
    {
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.offsetMin = Vector2.zero;
        rect.offsetMax = Vector2.zero;
    }

    private static void ConfigureDeathOverlay(RectTransform rect)
    {
        ConfigureFullScreenRect(rect);

        Image background = EnsureComponent<Image>(rect.gameObject);
        background.color = new Color(0.02f, 0.01f, 0.01f, 0.72f);

        PlayerDeathOverlayUI overlay = EnsureComponent<PlayerDeathOverlayUI>(rect.gameObject);

        RectTransform messageRect = EnsureRectChild(rect, "Message");
        messageRect.anchorMin = new Vector2(0.5f, 0.5f);
        messageRect.anchorMax = new Vector2(0.5f, 0.5f);
        messageRect.pivot = new Vector2(0.5f, 0.5f);
        messageRect.anchoredPosition = Vector2.zero;
        messageRect.sizeDelta = new Vector2(520f, 180f);

        Text message = EnsureComponent<Text>(messageRect.gameObject);
        message.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        message.fontSize = 38;
        message.alignment = TextAnchor.MiddleCenter;
        message.color = new Color(0.85f, 0.08f, 0.08f, 1f);
        message.text = "YOU DIED\nPress R to restart";

        Outline outline = EnsureComponent<Outline>(messageRect.gameObject);
        outline.effectColor = Color.black;
        outline.effectDistance = new Vector2(2f, -2f);

        SetSerializedReference(overlay, "messageText", message);
        rect.gameObject.SetActive(false);
    }

    private static void ConfigureHudTextureImports()
    {
        string[] paths =
        {
            "Assets/Sprites/UI/DeadRain_HUD_Green.png",
            "Assets/Sprites/UI/DeadRain_HUD_Transparent.png",
            "Assets/Resources/UI/PlayerHealthFrame.png",
            "Assets/Resources/UI/EnemyHealthFrame.png",
            "Assets/Resources/UI/WeaponSlot.png",
            "Assets/Resources/UI/WeaponSlotSelected.png",
            "Assets/Resources/UI/ShieldSlot.png"
        };

        foreach (string path in paths)
        {
            TextureImporter importer = AssetImporter.GetAtPath(path) as TextureImporter;
            if (importer == null)
            {
                continue;
            }

            importer.textureType = TextureImporterType.Sprite;
            importer.spriteImportMode = SpriteImportMode.Single;
            importer.alphaIsTransparency = true;
            importer.mipmapEnabled = false;
            importer.SaveAndReimport();
        }
    }

    private static RectTransform EnsureRectChild(Transform parent, string childName)
    {
        Transform child = parent.Find(childName);
        if (child == null)
        {
            GameObject childObject = new GameObject(childName, typeof(RectTransform));
            childObject.transform.SetParent(parent, false);
            child = childObject.transform;
        }

        return (RectTransform)child;
    }

    private static T EnsureComponent<T>(GameObject target) where T : Component
    {
        T component = target.GetComponent<T>();
        return component != null ? component : target.AddComponent<T>();
    }

    private static void CreatePreviewSlot(Transform parent, string key, string label, Color color, Sprite sprite)
    {
        GameObject slot = new GameObject($"{label} Slot", typeof(RectTransform), typeof(Image));
        slot.transform.SetParent(parent, false);

        RectTransform rect = (RectTransform)slot.transform;
        rect.sizeDelta = new Vector2(86f, 44f);

        Image background = slot.GetComponent<Image>();
        background.sprite = sprite;
        background.preserveAspect = false;
        background.color = sprite != null ? Color.white : color;
        Outline slotOutline = slot.AddComponent<Outline>();
        slotOutline.effectColor = new Color(0.48f, 0.4f, 0.24f, 0.9f);
        slotOutline.effectDistance = new Vector2(1f, -1f);

        CreateText(slot.transform, "Key", key, 16, TextAnchor.MiddleCenter, new Vector2(20f, 0f), new Vector2(28f, 32f));
        CreateText(slot.transform, "Label", label, 13, TextAnchor.MiddleLeft, new Vector2(54f, 0f), new Vector2(50f, 32f));
    }

    private static void CreateText(Transform parent, string name, string text, int size, TextAnchor anchor, Vector2 anchoredPosition, Vector2 sizeDelta)
    {
        GameObject textObject = new GameObject(name, typeof(RectTransform), typeof(Text));
        textObject.transform.SetParent(parent, false);

        RectTransform rect = (RectTransform)textObject.transform;
        rect.anchoredPosition = anchoredPosition;
        rect.sizeDelta = sizeDelta;

        Text uiText = textObject.GetComponent<Text>();
        uiText.text = text;
        uiText.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        uiText.fontSize = size;
        uiText.alignment = anchor;
        uiText.color = Color.white;
        Outline outline = textObject.AddComponent<Outline>();
        outline.effectColor = new Color(0f, 0f, 0f, 0.75f);
        outline.effectDistance = new Vector2(1f, -1f);
    }

    private static Sprite LoadSprite(string name)
    {
        return AssetDatabase.LoadAssetAtPath<Sprite>($"Assets/Resources/UI/{name}.png");
    }

    private static void ClearChildren(Transform parent)
    {
        for (int i = parent.childCount - 1; i >= 0; i--)
        {
            Object.DestroyImmediate(parent.GetChild(i).gameObject);
        }
    }

    private static void SetSerializedReference(Object target, string fieldName, Object value)
    {
        System.Reflection.FieldInfo field = target.GetType().GetField(fieldName, System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic);
        field?.SetValue(target, value);
    }
}
