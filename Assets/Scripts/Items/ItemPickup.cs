using UnityEngine;

[RequireComponent(typeof(Collider2D))]
public class ItemPickup : MonoBehaviour
{
    public ItemData itemData;
    public int stackAmount = 1;
    public RunInventory targetInventory;

    private void Reset()
    {
        var col = GetComponent<Collider2D>();
        col.isTrigger = true;
    }

    private void Awake()
    {
        if (targetInventory != null) return;

        var player = GameObject.FindWithTag("Player");
        if (player != null)
        {
            targetInventory = player.GetComponent<RunInventory>();
        }
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (!other.CompareTag("Player")) return;
        if (itemData == null) return;

        var inventory = targetInventory != null ? targetInventory : other.GetComponent<RunInventory>();
        if (inventory == null) return;

        inventory.AddItem(itemData, Mathf.Max(1, stackAmount));
        Destroy(gameObject);
    }
}
