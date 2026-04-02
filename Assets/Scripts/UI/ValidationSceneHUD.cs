using System.Text;
using UnityEngine;
using UnityEngine.UI;

public class ValidationSceneHUD : MonoBehaviour
{
    public PlayerController player;
    public RunDirectorLite runDirector;
    public RunInventory inventory;
    public Text statusText;
    public Text itemText;

    private readonly StringBuilder builder = new StringBuilder(128);
    private float nextRefresh;

    private void Update()
    {
        if (Time.unscaledTime < nextRefresh) return;
        nextRefresh = Time.unscaledTime + 0.2f;

        RefreshStatus();
        RefreshItems();
    }

    private void RefreshStatus()
    {
        if (statusText == null) return;

        int hp = player != null ? player.currentHealth : 0;
        int hpMax = player != null ? player.maxHealth : 0;
        float threat = runDirector != null ? runDirector.GetThreatLevel() : 0f;

        statusText.text = "HP " + hp + "/" + hpMax + "\nThreat " + threat.ToString("0.00");
    }

    private void RefreshItems()
    {
        if (itemText == null) return;

        if (inventory == null || inventory.Entries == null || inventory.Entries.Count == 0)
        {
            itemText.text = "Items: (none)";
            return;
        }

        builder.Length = 0;
        builder.Append("Items: ");

        for (int i = 0; i < inventory.Entries.Count; i++)
        {
            var entry = inventory.Entries[i];
            if (entry == null || entry.item == null) continue;

            if (builder.Length > 7) builder.Append("  |  ");
            builder.Append(entry.item.displayName);
            builder.Append(" x");
            builder.Append(entry.stack);
        }

        itemText.text = builder.ToString();
    }
}
