using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class GameUIBootstrapper : MonoBehaviour
{
    private Canvas canvas;
    private Camera worldCamera;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void CreateForScene()
    {
        if (FindObjectOfType<GameUIBootstrapper>() != null)
        {
            return;
        }

        GameObject existingUi = GameObject.Find("Game UI");
        if (existingUi != null && existingUi.GetComponent<Canvas>() != null)
        {
            existingUi.AddComponent<GameUIBootstrapper>();
            return;
        }

        new GameObject("Game UI Bootstrapper").AddComponent<GameUIBootstrapper>();
    }

    private void Start()
    {
        worldCamera = Camera.main;
        canvas = GetComponent<Canvas>();
        if (canvas == null)
        {
            GameObject existingUi = GameObject.Find("Game UI");
            canvas = existingUi != null ? existingUi.GetComponent<Canvas>() : null;
        }

        if (canvas == null)
        {
            canvas = CreateCanvas();
        }

        EnsureEventSystem();
        BuildPlayerHud();
        BuildEnemyHealthBars();
    }

    private Canvas CreateCanvas()
    {
        GameObject canvasObject = new GameObject("Game UI", typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
        Canvas createdCanvas = canvasObject.GetComponent<Canvas>();
        createdCanvas.renderMode = RenderMode.ScreenSpaceOverlay;

        CanvasScaler scaler = canvasObject.GetComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920f, 1080f);
        scaler.matchWidthOrHeight = 0.5f;

        return createdCanvas;
    }

    private void EnsureEventSystem()
    {
        if (FindObjectOfType<EventSystem>() != null)
        {
            return;
        }

        new GameObject("EventSystem", typeof(EventSystem), typeof(StandaloneInputModule));
    }

    private void BuildPlayerHud()
    {
        GameObject player = GameObject.FindGameObjectWithTag("Player");
        if (player == null)
        {
            return;
        }

        Stats playerStats = player.GetComponentInChildren<Stats>();
        PlayerInventory inventory = player.GetComponent<PlayerInventory>();

        RectTransform healthRoot = FindChildRect(canvas.transform, "Player Health");
        if (healthRoot == null)
        {
            healthRoot = CreatePanel("Player Health", canvas.transform, new Vector2(24f, -24f), new Vector2(360f, 64f), TextAnchor.UpperLeft);
        }

        UIHealthBar healthBar = EnsureHealthBar(healthRoot, "HP", new Color(0.75f, 0.08f, 0.11f, 1f), true);
        healthBar.Bind(playerStats);

        GameObject weaponHudObject = FindChild(canvas.transform, "Weapon HUD");
        if (weaponHudObject == null)
        {
            weaponHudObject = new GameObject("Weapon HUD", typeof(RectTransform), typeof(WeaponHUD), typeof(HorizontalLayoutGroup));
            weaponHudObject.transform.SetParent(canvas.transform, false);
        }

        RectTransform weaponRect = (RectTransform)weaponHudObject.transform;
        weaponRect.anchorMin = new Vector2(0f, 0f);
        weaponRect.anchorMax = new Vector2(0f, 0f);
        weaponRect.pivot = new Vector2(0f, 0f);
        weaponRect.anchoredPosition = new Vector2(24f, 24f);
        weaponRect.sizeDelta = new Vector2(620f, 52f);

        HorizontalLayoutGroup layout = weaponHudObject.GetComponent<HorizontalLayoutGroup>();
        layout.spacing = 8f;
        layout.childForceExpandWidth = false;
        layout.childForceExpandHeight = false;
        layout.childAlignment = TextAnchor.MiddleLeft;

        WeaponHUD weaponHud = weaponHudObject.GetComponent<WeaponHUD>();
        if (weaponHud == null)
        {
            weaponHud = weaponHudObject.AddComponent<WeaponHUD>();
        }

        weaponHud.Configure(weaponHudObject.transform);
        weaponHud.Bind(inventory);
    }

    private void BuildEnemyHealthBars()
    {
        Transform enemyBarRoot = GetOrCreateEnemyBarRoot();
        Stats[] allStats = FindObjectsOfType<Stats>();
        foreach (Stats stats in allStats)
        {
            Transform owner = stats.transform.root;
            if (owner.CompareTag("Player"))
            {
                continue;
            }

            if (FindChild(enemyBarRoot, $"{owner.name} Health") != null)
            {
                continue;
            }

            RectTransform barRoot = CreatePanel($"{owner.name} Health", enemyBarRoot, Vector2.zero, new Vector2(110f, 16f), TextAnchor.MiddleCenter);
            UIHealthBar healthBar = EnsureHealthBar(barRoot, string.Empty, new Color(0.85f, 0.16f, 0.13f, 1f), false);
            healthBar.Bind(stats);

            WorldHealthBarUI worldBar = barRoot.gameObject.AddComponent<WorldHealthBarUI>();
            worldBar.Bind(owner, stats, worldCamera);
        }
    }

    private static RectTransform CreatePanel(string name, Transform parent, Vector2 anchoredPosition, Vector2 size, TextAnchor anchor)
    {
        GameObject panel = new GameObject(name, typeof(RectTransform));
        panel.transform.SetParent(parent, false);

        RectTransform rect = (RectTransform)panel.transform;
        rect.sizeDelta = size;
        rect.anchoredPosition = anchoredPosition;

        if (anchor == TextAnchor.UpperLeft)
        {
            rect.anchorMin = new Vector2(0f, 1f);
            rect.anchorMax = new Vector2(0f, 1f);
            rect.pivot = new Vector2(0f, 1f);
        }
        else
        {
            rect.anchorMin = new Vector2(0.5f, 0.5f);
            rect.anchorMax = new Vector2(0.5f, 0.5f);
            rect.pivot = new Vector2(0.5f, 0.5f);
        }

        return rect;
    }

    private static UIHealthBar EnsureHealthBar(RectTransform parent, string label, Color fillColor, bool showValue)
    {
        Transform existingBackground = parent.Find("Background");
        if (existingBackground == null)
        {
            return CreateHealthBar(parent, label, fillColor, showValue);
        }

        Image fill = existingBackground.Find("Fill") != null
            ? existingBackground.Find("Fill").GetComponent<Image>()
            : null;
        Text valueText = existingBackground.Find("Text") != null
            ? existingBackground.Find("Text").GetComponent<Text>()
            : null;

        UIHealthBar healthBar = parent.GetComponent<UIHealthBar>();
        if (healthBar == null)
        {
            healthBar = parent.gameObject.AddComponent<UIHealthBar>();
        }

        healthBar.Configure(fill, valueText);
        return healthBar;
    }

    public static UIHealthBar CreateHealthBar(RectTransform parent, string label, Color fillColor, bool showValue)
    {
        GameObject backgroundObject = new GameObject("Background", typeof(RectTransform), typeof(Image));
        backgroundObject.transform.SetParent(parent, false);

        RectTransform backgroundRect = (RectTransform)backgroundObject.transform;
        backgroundRect.anchorMin = Vector2.zero;
        backgroundRect.anchorMax = Vector2.one;
        backgroundRect.offsetMin = Vector2.zero;
        backgroundRect.offsetMax = Vector2.zero;

        Image background = backgroundObject.GetComponent<Image>();
        background.color = new Color(0.04f, 0.045f, 0.055f, 0.88f);

        GameObject fillObject = new GameObject("Fill", typeof(RectTransform), typeof(Image));
        fillObject.transform.SetParent(backgroundObject.transform, false);

        RectTransform fillRect = (RectTransform)fillObject.transform;
        fillRect.anchorMin = Vector2.zero;
        fillRect.anchorMax = Vector2.one;
        fillRect.offsetMin = new Vector2(3f, 3f);
        fillRect.offsetMax = new Vector2(-3f, -3f);

        Image fill = fillObject.GetComponent<Image>();
        fill.color = fillColor;
        fill.type = Image.Type.Filled;
        fill.fillMethod = Image.FillMethod.Horizontal;
        fill.fillOrigin = 0;
        fill.fillAmount = 1f;

        Text valueText = null;
        if (showValue || !string.IsNullOrEmpty(label))
        {
            GameObject textObject = new GameObject("Text", typeof(RectTransform), typeof(Text));
            textObject.transform.SetParent(backgroundObject.transform, false);

            RectTransform textRect = (RectTransform)textObject.transform;
            textRect.anchorMin = Vector2.zero;
            textRect.anchorMax = Vector2.one;
            textRect.offsetMin = Vector2.zero;
            textRect.offsetMax = Vector2.zero;

            valueText = textObject.GetComponent<Text>();
            valueText.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            valueText.fontSize = showValue ? 18 : 12;
            valueText.alignment = TextAnchor.MiddleCenter;
            valueText.color = Color.white;
            valueText.text = label;
        }

        UIHealthBar healthBar = parent.gameObject.AddComponent<UIHealthBar>();
        healthBar.Configure(fill, valueText);
        return healthBar;
    }

    private Transform GetOrCreateEnemyBarRoot()
    {
        GameObject existing = FindChild(canvas.transform, "Enemy Health Bars");
        if (existing != null)
        {
            return existing.transform;
        }

        GameObject root = new GameObject("Enemy Health Bars", typeof(RectTransform));
        root.transform.SetParent(canvas.transform, false);

        RectTransform rect = (RectTransform)root.transform;
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.offsetMin = Vector2.zero;
        rect.offsetMax = Vector2.zero;

        return root.transform;
    }

    private static RectTransform FindChildRect(Transform parent, string childName)
    {
        GameObject child = FindChild(parent, childName);
        return child != null ? child.GetComponent<RectTransform>() : null;
    }

    private static GameObject FindChild(Transform parent, string childName)
    {
        for (int i = 0; i < parent.childCount; i++)
        {
            Transform child = parent.GetChild(i);
            if (child.name == childName)
            {
                return child.gameObject;
            }
        }

        return null;
    }
}
