using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class RunRewardChoiceUI : MonoBehaviour
{
    [SerializeField] private Transform buttonRoot;
    [SerializeField] private Text titleText;

    private readonly List<Button> buttons = new();
    private Action<ItemData> onRewardChosen;

    private void Awake()
    {
        gameObject.SetActive(false);
    }

    public void Configure(Transform root, Text title)
    {
        buttonRoot = root;
        titleText = title;
    }

    public void ShowRewards(RunRewardTableData table, Action<ItemData> chosenCallback)
    {
        onRewardChosen = chosenCallback;
        gameObject.SetActive(true);

        if (titleText != null)
        {
            titleText.text = "选择时代遗物";
        }

        ClearButtons();
        if (table == null || table.rewards == null || table.rewards.Length == 0)
        {
            return;
        }

        List<ItemData> pool = new(table.rewards);
        int count = Mathf.Clamp(table.rewardChoiceCount, 1, Mathf.Min(3, pool.Count));
        for (int i = 0; i < count; i++)
        {
            int index = UnityEngine.Random.Range(0, pool.Count);
            ItemData item = pool[index];
            pool.RemoveAt(index);
            CreateButton(item);
        }
    }

    private void CreateButton(ItemData item)
    {
        if (buttonRoot == null || item == null)
        {
            return;
        }

        GameObject buttonObject = new GameObject($"{item.displayName} Reward", typeof(RectTransform), typeof(Image), typeof(Button));
        buttonObject.transform.SetParent(buttonRoot, false);
        RectTransform rect = (RectTransform)buttonObject.transform;
        rect.sizeDelta = new Vector2(280f, 96f);

        Image image = buttonObject.GetComponent<Image>();
        image.color = new Color(0.08f, 0.07f, 0.05f, 0.96f);
        Outline outline = buttonObject.AddComponent<Outline>();
        outline.effectColor = new Color(0.72f, 0.5f, 0.22f, 0.9f);
        outline.effectDistance = new Vector2(2f, -2f);

        Button button = buttonObject.GetComponent<Button>();
        button.onClick.AddListener(() => Choose(item));
        buttons.Add(button);

        CreateText(buttonObject.transform, "Name", item.displayName, 18, TextAnchor.UpperCenter, new Vector2(0f, -12f), new Vector2(250f, 28f));
        CreateText(buttonObject.transform, "Description", item.description, 13, TextAnchor.MiddleCenter, new Vector2(0f, -48f), new Vector2(250f, 52f));
    }

    private void Choose(ItemData item)
    {
        onRewardChosen?.Invoke(item);
        gameObject.SetActive(false);
    }

    private void ClearButtons()
    {
        if (buttonRoot == null)
        {
            return;
        }

        for (int i = buttonRoot.childCount - 1; i >= 0; i--)
        {
            Destroy(buttonRoot.GetChild(i).gameObject);
        }
        buttons.Clear();
    }

    private static void CreateText(Transform parent, string name, string text, int size, TextAnchor anchor, Vector2 position, Vector2 sizeDelta)
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
    }
}
