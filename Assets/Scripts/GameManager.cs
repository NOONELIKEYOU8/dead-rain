using System.Collections;
using UnityEngine;

public class GameManager : MonoBehaviour
{
    public static GameManager Instance { get; private set; }

    [Header("Prefabs & Spawns")]
    public Transform playerPrefab;
    public Transform playerSpawn;
    public Transform enemyPrefab;
    public Transform enemySpawn;

    public float respawnDelay = 1.2f;

    private void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);
    }

    public void OnPlayerDead()
    {
        StartCoroutine(RespawnPlayer());
    }

    IEnumerator RespawnPlayer()
    {
        yield return new WaitForSeconds(respawnDelay);
        if (playerPrefab != null && playerSpawn != null)
        {
            var go = Instantiate(playerPrefab, playerSpawn.position, Quaternion.identity);
            go.name = playerPrefab.name;
            var cam = Camera.main;
            if (cam != null)
            {
                var cf = cam.GetComponent<CameraFollow>();
                if (cf != null) cf.target = go.transform;
            }
        }
    }
}
