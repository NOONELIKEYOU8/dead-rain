using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class RunUIBootstrapper : MonoBehaviour
{
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void CreateForScene()
    {
        if (FindObjectOfType<RunUIBootstrapper>() != null)
        {
            return;
        }

        new GameObject("Run UI Bootstrapper").AddComponent<RunUIBootstrapper>();
    }

    private void Start()
    {
        Canvas canvas = FindObjectOfType<Canvas>();
        if (canvas == null)
        {
            GameObject canvasObject = new GameObject("Game UI", typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
            canvas = canvasObject.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            CanvasScaler scaler = canvasObject.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920f, 1080f);
        }

        if (FindObjectOfType<EventSystem>() == null)
        {
            new GameObject("EventSystem", typeof(EventSystem), typeof(StandaloneInputModule));
        }

        BuildRunHud(canvas.transform);
        BuildRewardUI(canvas.transform);
    }

    private void BuildRunHud(Transform canvas)
    {
        if (canvas.Find("Run HUD") != null)
        {
            return;
        }

        GameObject root = new GameObject("Run HUD", typeof(RectTransform), typeof(RunHudUI));
        root.transform.SetParent(canvas, false);
        RectTransform rect = (RectTransform)root.transform;
        rect.anchorMin = new Vector2(1f, 1f);
        rect.anchorMax = new Vector2(1f, 1f);
        rect.pivot = new Vector2(1f, 1f);
        rect.anchoredPosition = new Vector2(-24f, -24f);
        rect.sizeDelta = new Vector2(360f, 260f);

        Text era = CreateText(root.transform, "Era", "", 22, TextAnchor.UpperRight, new Vector2(0f, -16f), new Vector2(340f, 32f));
        Text time = CreateText(root.transform, "Time", "", 18, TextAnchor.UpperRight, new Vector2(0f, -50f), new Vector2(340f, 28f));
        Text difficulty = CreateText(root.transform, "Difficulty", "", 16, TextAnchor.UpperRight, new Vector2(0f, -82f), new Vector2(340f, 28f));
        Text kills = CreateText(root.transform, "Kills", "", 16, TextAnchor.UpperRight, new Vector2(0f, -112f), new Vector2(340f, 28f));
        Text items = CreateText(root.transform, "Items", "", 14, TextAnchor.UpperRight, new Vector2(0f, -170f), new Vector2(340f, 120f));

        RectTransform bossRoot = CreatePanel("Boss Health", canvas, new Vector2(0f, -72f), new Vector2(620f, 42f), TextAnchor.UpperCenter);
        UIHealthBar bossBar = GameUIBootstrapper.CreateHealthBar(bossRoot, "", new Color(0.6f, 0.06f, 0.1f, 1f), false);
        Text bossName = CreateText(bossRoot, "Boss Name", "", 18, TextAnchor.MiddleCenter, Vector2.zero, new Vector2(600f, 32f));

        root.GetComponent<RunHudUI>().Configure(era, time, difficulty, kills, items, bossBar, bossName);
    }

    private void BuildRewardUI(Transform canvas)
    {
        if (canvas.Find("Run Reward Choice") != null)
        {
            return;
        }

        GameObject root = new GameObject("Run Reward Choice", typeof(RectTransform), typeof(Image), typeof(RunRewardChoiceUI));
        root.transform.SetParent(canvas, false);
        RectTransform rect = (RectTransform)root.transform;
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.offsetMin = Vector2.zero;
        rect.offsetMax = Vector2.zero;
        root.GetComponent<Image>().color = new Color(0f, 0f, 0f, 0.72f);

        Text title = CreateText(root.transform, "Title", "选择时代遗物", 34, TextAnchor.MiddleCenter, new Vector2(0f, 180f), new Vector2(720f, 60f));

        GameObject buttons = new GameObject("Buttons", typeof(RectTransform), typeof(HorizontalLayoutGroup));
        buttons.transform.SetParent(root.transform, false);
        RectTransform buttonsRect = (RectTransform)buttons.transform;
        buttonsRect.anchorMin = new Vector2(0.5f, 0.5f);
        buttonsRect.anchorMax = new Vector2(0.5f, 0.5f);
        buttonsRect.pivot = new Vector2(0.5f, 0.5f);
        buttonsRect.anchoredPosition = Vector2.zero;
        buttonsRect.sizeDelta = new Vector2(920f, 120f);
        HorizontalLayoutGroup layout = buttons.GetComponent<HorizontalLayoutGroup>();
        layout.spacing = 24f;
        layout.childAlignment = TextAnchor.MiddleCenter;
        layout.childForceExpandHeight = false;
        layout.childForceExpandWidth = false;

        root.GetComponent<RunRewardChoiceUI>().Configure(buttons.transform, title);
        root.SetActive(false);
    }

    private static RectTransform CreatePanel(string name, Transform parent, Vector2 anchoredPosition, Vector2 size, TextAnchor anchor)
    {
        GameObject panel = new GameObject(name, typeof(RectTransform));
        panel.transform.SetParent(parent, false);
        RectTransform rect = (RectTransform)panel.transform;
        rect.sizeDelta = size;
        rect.anchoredPosition = anchoredPosition;
        rect.anchorMin = anchor == TextAnchor.UpperCenter ? new Vector2(0.5f, 1f) : new Vector2(0.5f, 0.5f);
        rect.anchorMax = rect.anchorMin;
        rect.pivot = anchor == TextAnchor.UpperCenter ? new Vector2(0.5f, 1f) : new Vector2(0.5f, 0.5f);
        return rect;
    }

    private static Text CreateText(Transform parent, string name, string text, int size, TextAnchor anchor, Vector2 position, Vector2 sizeDelta)
    {
        GameObject textObject = new GameObject(name, typeof(RectTransform), typeof(Text), typeof(Outline));
        textObject.transform.SetParent(parent, false);
        RectTransform rect = (RectTransform)textObject.transform;
        rect.anchoredPosition = position;
        rect.sizeDelta = sizeDelta;
        Text uiText = textObject.GetComponent<Text>();
        uiText.text = text;
        uiText.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        uiText.fontSize = size;
        uiText.alignment = anchor;
        uiText.color = Color.white;
        uiText.horizontalOverflow = HorizontalWrapMode.Wrap;
        uiText.verticalOverflow = VerticalWrapMode.Truncate;
        Outline outline = textObject.GetComponent<Outline>();
        outline.effectColor = Color.black;
        outline.effectDistance = new Vector2(1f, -1f);
        return uiText;
    }
}
