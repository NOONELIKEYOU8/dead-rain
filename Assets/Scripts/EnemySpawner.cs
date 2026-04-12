using System.Collections;
using UnityEngine;

/// <summary>
/// 简单的敌人生成器：负责初次生成与在敌人被销毁后延迟重生。
/// 使用方法：在场景中创建一个空对象作为生成点，添加此组件并把敌人 Prefab 拖到 enemyPrefab 字段。
/// 注意：生成点与巡逻边界应为空 Transform（没有 Collider），否则可能干扰物理行为。
/// </summary>
public class EnemySpawner : MonoBehaviour
{
    [Tooltip("敌人预制体（GameObject）")]
    public GameObject enemyPrefab;
    [Tooltip("生成点（若为空则使用此对象的位置）")]
    public Transform spawnPoint;
    [Tooltip("是否在 Start 时自动生成")] 
    public bool spawnOnStart = true;
    [Tooltip("敌人死亡后等待多长时间再复活（秒）")]
    public float respawnDelay = 2f;

    GameObject currentInstance;
    // 当我们订阅多个 Damageable 时保存引用以便解除订阅
    System.Collections.Generic.List<Damageable> subscribedDamageables = new System.Collections.Generic.List<Damageable>();
    [Tooltip("启用调试日志（Console 中显示生成/死亡/复活流程）")]
    public bool debug = true;

    void Start()
    {
        if (spawnPoint == null) spawnPoint = transform;
        if (spawnOnStart) Spawn();
    }

    public void Spawn()
    {
        if (enemyPrefab == null)
        {
            Debug.LogWarning("EnemySpawner: enemyPrefab 未设置，无法生成敌人。", this);
            return;
        }
        if (currentInstance != null) return;
        if (debug) Debug.Log($"EnemySpawner: Spawning prefab '{(enemyPrefab!=null?enemyPrefab.name:"<null>")}' at {spawnPoint.position}", this);
        currentInstance = Instantiate(enemyPrefab, spawnPoint.position, Quaternion.identity) as GameObject;
        if (currentInstance == null)
        {
            Debug.LogWarning("EnemySpawner: Instantiate returned null (enemyPrefab may be invalid)", this);
            return;
        }

        // 订阅该实例及其子对象上所有的 Damageable.OnDeath
        subscribedDamageables.Clear();
        var dmgChildren = currentInstance.GetComponentsInChildren<Damageable>();
        if (dmgChildren != null && dmgChildren.Length > 0)
        {
            foreach (var d in dmgChildren)
            {
                if (d == null) continue;
                d.OnDeath += HandleInstanceDeath;
                subscribedDamageables.Add(d);
                if (debug) Debug.Log($"EnemySpawner: Subscribed to OnDeath of '{d.name}'", this);
            }
        }
        else
        {
            if (debug) Debug.LogWarning("EnemySpawner: Spawned instance has no Damageable components; falling back to polling.", this);
            StartCoroutine(WatchInstance());
        }
    }

    void HandleInstanceDeath(Damageable d)
    {
        if (debug) Debug.Log($"EnemySpawner: HandleInstanceDeath called from '{(d!=null?d.name:"<null>")}'", this);
        // 解除对所有订阅的 Damageable 的订阅
        foreach (var sd in subscribedDamageables)
        {
            if (sd != null) sd.OnDeath -= HandleInstanceDeath;
        }
        subscribedDamageables.Clear();

        // 将当前实例清空并进入延迟复活流程
        currentInstance = null;
        StartCoroutine(RespawnAfterDelay());
    }

    IEnumerator WatchInstance()
    {
        // 等待实例被销毁（Unity 中 Destroy 后对象 == null）
        while (currentInstance != null)
        {
            yield return null;
        }
        yield return new WaitForSeconds(respawnDelay);
        Spawn();
    }

    IEnumerator RespawnAfterDelay()
    {
        yield return new WaitForSeconds(respawnDelay);
        Spawn();
    }

    /// <summary>
    /// 强制触发复活（先删除现有实例，再按 delay 复活）
    /// </summary>
    public void RespawnNow()
    {
        if (currentInstance != null) Destroy(currentInstance);
        StartCoroutine(_RespawnNow());
    }

    IEnumerator _RespawnNow()
    {
        yield return new WaitForSeconds(respawnDelay);
        Spawn();
    }
}
