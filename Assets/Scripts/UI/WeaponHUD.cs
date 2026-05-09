using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class WeaponHUD : MonoBehaviour
{
    [SerializeField] private Transform slotRoot;
    [SerializeField] private Color selectedColor = new Color(0.95f, 0.85f, 0.45f, 1f);
    [SerializeField] private Color normalColor = new Color(0.12f, 0.13f, 0.16f, 0.9f);
    [SerializeField] private Color shieldColor = new Color(0.25f, 0.45f, 0.9f, 0.9f);

    private readonly List<Image> slotBackgrounds = new();
    private PlayerInventory inventory;
    private PlayerInputHandler inputHandler;
    private Image shieldBackground;
    private Sprite normalSlotSprite;
    private Sprite selectedSlotSprite;
    private Sprite shieldSlotSprite;

    private void Awake()
    {
        normalSlotSprite = Resources.Load<Sprite>("UI/WeaponSlot");
        selectedSlotSprite = Resources.Load<Sprite>("UI/WeaponSlotSelected");
        shieldSlotSprite = Resources.Load<Sprite>("UI/ShieldSlot");
    }

    public void Configure(Transform slots)
    {
        slotRoot = slots;
    }

    public void Bind(PlayerInventory playerInventory)
    {
        if (inventory != null)
        {
            inventory.OnWeaponChanged -= Refresh;
        }

        inventory = playerInventory;
        inputHandler = inventory != null ? inventory.GetComponent<PlayerInputHandler>() : null;

        if (inventory != null)
        {
            inventory.OnWeaponChanged += Refresh;
        }

        BuildSlots();
        Refresh();
    }

    private void OnDestroy()
    {
        if (inventory != null)
        {
            inventory.OnWeaponChanged -= Refresh;
        }
    }

    private void BuildSlots()
    {
        if (slotRoot == null)
        {
            return;
        }

        for (int i = slotRoot.childCount - 1; i >= 0; i--)
        {
            Destroy(slotRoot.GetChild(i).gameObject);
        }

        slotBackgrounds.Clear();

        if (inventory == null || inventory.Weapons == null)
        {
            return;
        }

        for (int i = 0; i < inventory.Weapons.Count; i++)
        {
            Weapon weapon = inventory.Weapons[i];
            if (weapon == null || weapon == inventory.GetShield())
            {
                continue;
            }

            CreateSlot($"{i + 1}", weapon.name, normalColor);
        }

        shieldBackground = CreateSlot("R", "Shield", shieldColor);
    }

    private void Update()
    {
        if (shieldBackground == null || inputHandler == null)
        {
            return;
        }

        shieldBackground.sprite = inputHandler.SecondaryAttackHeld ? selectedSlotSprite : shieldSlotSprite;
        shieldBackground.color = shieldBackground.sprite != null
            ? Color.white
            : inputHandler.SecondaryAttackHeld ? selectedColor : shieldColor;
    }

    private void Refresh()
    {
        if (inventory == null)
        {
            return;
        }

        int visibleIndex = 0;

        for (int i = 0; i < inventory.Weapons.Count; i++)
        {
            Weapon weapon = inventory.Weapons[i];
            if (weapon == null || weapon == inventory.GetShield())
            {
                continue;
            }

            if (visibleIndex < slotBackgrounds.Count)
            {
                bool selected = i == inventory.PrimaryWeaponIndex;
                slotBackgrounds[visibleIndex].sprite = selected ? selectedSlotSprite : normalSlotSprite;
                slotBackgrounds[visibleIndex].color = selected ? Color.white : Color.white;
            }

            visibleIndex++;
        }
    }

    private Image CreateSlot(string key, string label, Color color)
    {
        GameObject slot = new GameObject($"{label} Slot", typeof(RectTransform), typeof(Image));
        slot.transform.SetParent(slotRoot, false);

        RectTransform slotRect = (RectTransform)slot.transform;
        slotRect.sizeDelta = new Vector2(92f, 48f);

        Image background = slot.GetComponent<Image>();
        background.sprite = label == "Shield" ? shieldSlotSprite : normalSlotSprite;
        background.preserveAspect = false;
        background.color = background.sprite != null ? Color.white : color;
        Outline outline = slot.AddComponent<Outline>();
        outline.effectColor = new Color(0.55f, 0.47f, 0.28f, 0.8f);
        outline.effectDistance = new Vector2(1f, -1f);
        slotBackgrounds.Add(background);

        CreateText(slot.transform, "Key", key, 16, TextAnchor.MiddleCenter, new Vector2(18f, 0f), new Vector2(26f, 34f));
        CreateText(slot.transform, "Label", label, 13, TextAnchor.MiddleLeft, new Vector2(56f, 0f), new Vector2(54f, 34f));

        return background;
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
}
