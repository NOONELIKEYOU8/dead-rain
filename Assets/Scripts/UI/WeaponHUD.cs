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

        shieldBackground.color = inputHandler.SecondaryAttackHeld ? selectedColor : shieldColor;
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
                slotBackgrounds[visibleIndex].color = i == inventory.PrimaryWeaponIndex ? selectedColor : normalColor;
            }

            visibleIndex++;
        }
    }

    private Image CreateSlot(string key, string label, Color color)
    {
        GameObject slot = new GameObject($"{label} Slot", typeof(RectTransform), typeof(Image));
        slot.transform.SetParent(slotRoot, false);

        RectTransform slotRect = (RectTransform)slot.transform;
        slotRect.sizeDelta = new Vector2(86f, 44f);

        Image background = slot.GetComponent<Image>();
        background.color = color;
        slotBackgrounds.Add(background);

        CreateText(slot.transform, "Key", key, 16, TextAnchor.MiddleCenter, new Vector2(20f, 0f), new Vector2(28f, 32f));
        CreateText(slot.transform, "Label", label, 13, TextAnchor.MiddleLeft, new Vector2(54f, 0f), new Vector2(50f, 32f));

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
    }
}
