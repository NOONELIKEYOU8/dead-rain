using System;
using System.Collections.Generic;
using UnityEngine;

public class RunInventory : MonoBehaviour
{
    [Serializable]
    public class ItemStackEntry
    {
        public ItemData item;
        public int stack;
    }

    [SerializeField]
    private List<ItemStackEntry> entries = new List<ItemStackEntry>();

    public event Action<ItemData, int> OnItemStackChanged;

    public IReadOnlyList<ItemStackEntry> Entries => entries;

    public int AddItem(ItemData item, int amount = 1)
    {
        if (item == null || amount <= 0) return 0;

        var entry = FindEntry(item.itemId);
        if (entry == null)
        {
            entry = new ItemStackEntry
            {
                item = item,
                stack = 0
            };
            entries.Add(entry);
        }

        entry.stack = Mathf.Clamp(entry.stack + amount, 0, Mathf.Max(1, item.maxStack));
        OnItemStackChanged?.Invoke(item, entry.stack);
        BattleEvents.RaiseItemPicked(item.itemId, entry.stack);
        return entry.stack;
    }

    public int RemoveItem(ItemData item, int amount = 1)
    {
        if (item == null || amount <= 0) return 0;

        var entry = FindEntry(item.itemId);
        if (entry == null) return 0;

        entry.stack = Mathf.Max(0, entry.stack - amount);
        OnItemStackChanged?.Invoke(item, entry.stack);

        if (entry.stack == 0) entries.Remove(entry);
        return entry.stack;
    }

    public int GetStack(string itemId)
    {
        var entry = FindEntry(itemId);
        return entry != null ? entry.stack : 0;
    }

    private ItemStackEntry FindEntry(string itemId)
    {
        for (int i = 0; i < entries.Count; i++)
        {
            if (entries[i] != null && entries[i].item != null && entries[i].item.itemId == itemId)
            {
                return entries[i];
            }
        }
        return null;
    }
}
