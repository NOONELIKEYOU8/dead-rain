using System;
using System.Collections.Generic;
using UnityEngine;

public class RunInventorySystem : MonoBehaviour
{
    [Serializable]
    public struct ItemStack
    {
        public ItemData item;
        public int count;
    }

    private readonly Dictionary<ItemData, int> stacks = new();
    private readonly List<ItemStack> cachedStacks = new();

    public event Action<ItemData, int> OnItemAdded;
    public event Action<ItemData, int> OnItemRemoved;
    public event Action<ItemData, int> OnItemStackChanged;

    public IReadOnlyList<ItemStack> Stacks
    {
        get
        {
            cachedStacks.Clear();
            foreach (KeyValuePair<ItemData, int> pair in stacks)
            {
                cachedStacks.Add(new ItemStack { item = pair.Key, count = pair.Value });
            }

            return cachedStacks;
        }
    }

    public void AddItem(ItemData item, int amount = 1)
    {
        if (item == null || amount <= 0)
        {
            return;
        }

        int current = GetStackCount(item);
        int next = item.stackable ? current + amount : Mathf.Min(1, current + amount);
        stacks[item] = next;

        OnItemAdded?.Invoke(item, next);
        OnItemStackChanged?.Invoke(item, next);
    }

    public void RemoveItem(ItemData item, int amount = 1)
    {
        if (item == null || amount <= 0 || !stacks.ContainsKey(item))
        {
            return;
        }

        int next = stacks[item] - amount;
        if (next <= 0)
        {
            stacks.Remove(item);
            OnItemRemoved?.Invoke(item, 0);
            OnItemStackChanged?.Invoke(item, 0);
            return;
        }

        stacks[item] = next;
        OnItemRemoved?.Invoke(item, next);
        OnItemStackChanged?.Invoke(item, next);
    }

    public int GetStackCount(ItemData item)
    {
        return item != null && stacks.TryGetValue(item, out int count) ? count : 0;
    }
}
