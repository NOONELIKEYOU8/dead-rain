using System;
using UnityEngine;

public class DropService : MonoBehaviour, IDropService
{
    [Serializable]
    public class DropRule
    {
        public string enemyTypeId = "Enemy";
        public ItemData item;
        [Range(0f, 1f)]
        public float baseChance = 0.2f;
    }

    public DropRule[] dropRules;
    public RunInventory targetInventory;
    public ItemPickup pickupPrefab;
    public MonoBehaviour difficultyProviderBehaviour;

    private IDifficultyProvider difficultyProvider;

    private void Awake()
    {
        if (difficultyProviderBehaviour != null)
        {
            difficultyProvider = difficultyProviderBehaviour as IDifficultyProvider;
        }

        if (targetInventory == null)
        {
            var player = GameObject.FindWithTag("Player");
            if (player != null) targetInventory = player.GetComponent<RunInventory>();
        }
    }

    private void OnEnable()
    {
        BattleEvents.OnEnemyDied += HandleEnemyDied;
    }

    private void OnDisable()
    {
        BattleEvents.OnEnemyDied -= HandleEnemyDied;
    }

    public string RollDrop(string enemyTypeId, float threatLevel)
    {
        if (dropRules == null || dropRules.Length == 0) return string.Empty;

        for (int i = 0; i < dropRules.Length; i++)
        {
            var rule = dropRules[i];
            if (rule == null || rule.item == null) continue;
            if (!string.Equals(rule.enemyTypeId, enemyTypeId, StringComparison.OrdinalIgnoreCase)) continue;

            float chance = Mathf.Clamp01(rule.baseChance * Mathf.Lerp(1f, 1.5f, Mathf.Clamp01(threatLevel - 1f)));
            if (UnityEngine.Random.value <= chance)
            {
                return rule.item.itemId;
            }
        }

        return string.Empty;
    }

    public void SpawnDrop(string dropId, Vector3 worldPos)
    {
        if (string.IsNullOrEmpty(dropId)) return;

        ItemData item = FindItem(dropId);
        if (item == null) return;

        ItemPickup pickup = pickupPrefab != null
            ? Instantiate(pickupPrefab, worldPos, Quaternion.identity)
            : CreateRuntimePickup(worldPos);

        if (pickup == null) return;

        pickup.itemData = item;
        pickup.stackAmount = 1;
        pickup.targetInventory = targetInventory;
    }

    private void HandleEnemyDied(GameObject enemy, CombatContext killingBlow)
    {
        if (enemy == null) return;

        float threat = difficultyProvider != null ? difficultyProvider.GetThreatLevel() : 1f;
        string enemyType = ResolveEnemyType(enemy);
        string dropId = RollDrop(enemyType, threat);
        if (string.IsNullOrEmpty(dropId)) return;

        SpawnDrop(dropId, enemy.transform.position);
    }

    private string ResolveEnemyType(GameObject enemy)
    {
        var typed = enemy.GetComponent<IEnemyTypeProvider>();
        if (typed != null)
        {
            return typed.GetEnemyTypeId();
        }

        return enemy.tag;
    }

    private ItemData FindItem(string itemId)
    {
        if (dropRules == null) return null;

        for (int i = 0; i < dropRules.Length; i++)
        {
            var rule = dropRules[i];
            if (rule == null || rule.item == null) continue;
            if (rule.item.itemId == itemId) return rule.item;
        }

        return null;
    }

    private ItemPickup CreateRuntimePickup(Vector3 worldPos)
    {
        var go = new GameObject("RuntimeItemPickup");
        go.transform.position = worldPos;

        var col = go.AddComponent<CircleCollider2D>();
        col.isTrigger = true;
        col.radius = 0.2f;

        return go.AddComponent<ItemPickup>();
    }
}
