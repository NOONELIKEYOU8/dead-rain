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

        RectTransform playerHealth = EnsureRectChild(uiObject.transform, "Player Health");
        ConfigurePlayerHealth(playerHealth);

        RectTransform weaponHud = EnsureRectChild(uiObject.transform, "Weapon HUD");
        ConfigureWeaponHud(weaponHud);

        RectTransform enemyRoot = EnsureRectChild(uiObject.transform, "Enemy Health Bars");
        ConfigureFullScreenRect(enemyRoot);

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
        rect.anchoredPosition = new Vector2(24f, -24f);
        rect.sizeDelta = new Vector2(360f, 64f);

        if (rect.GetComponent<UIHealthBar>() == null)
        {
            GameUIBootstrapper.CreateHealthBar(rect, "HP", new Color(0.75f, 0.08f, 0.11f, 1f), true);
        }
    }

    private static void ConfigureWeaponHud(RectTransform rect)
    {
        rect.anchorMin = new Vector2(0f, 0f);
        rect.anchorMax = new Vector2(0f, 0f);
        rect.pivot = new Vector2(0f, 0f);
        rect.anchoredPosition = new Vector2(24f, 24f);
        rect.sizeDelta = new Vector2(620f, 52f);

        HorizontalLayoutGroup layout = EnsureComponent<HorizontalLayoutGroup>(rect.gameObject);
        layout.spacing = 8f;
        layout.childForceExpandWidth = false;
        layout.childForceExpandHeight = false;
        layout.childAlignment = TextAnchor.MiddleLeft;

        WeaponHUD hud = EnsureComponent<WeaponHUD>(rect.gameObject);
        hud.Configure(rect);

        for (int i = rect.childCount - 1; i >= 0; i--)
        {
            Object.DestroyImmediate(rect.GetChild(i).gameObject);
        }

        PlayerInventory inventory = Object.FindObjectOfType<PlayerInventory>();
        if (inventory != null && inventory.weapons != null)
        {
            for (int i = 0; i < inventory.weapons.Length; i++)
            {
                Weapon weapon = inventory.weapons[i];
                if (weapon != null && weapon != inventory.GetShield())
                {
                    Color color = i == inventory.PrimaryWeaponIndex
                        ? new Color(0.95f, 0.85f, 0.45f, 1f)
                        : new Color(0.12f, 0.13f, 0.16f, 0.9f);
                    CreatePreviewSlot(rect, (i + 1).ToString(), weapon.name, color);
                }
            }
        }

        CreatePreviewSlot(rect, "R", "Shield", new Color(0.25f, 0.45f, 0.9f, 0.9f));
    }

    private static void ConfigureFullScreenRect(RectTransform rect)
    {
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.offsetMin = Vector2.zero;
        rect.offsetMax = Vector2.zero;
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

    private static void CreatePreviewSlot(Transform parent, string key, string label, Color color)
    {
        GameObject slot = new GameObject($"{label} Slot", typeof(RectTransform), typeof(Image));
        slot.transform.SetParent(parent, false);

        RectTransform rect = (RectTransform)slot.transform;
        rect.sizeDelta = new Vector2(86f, 44f);

        Image background = slot.GetComponent<Image>();
        background.color = color;

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
    }
}
