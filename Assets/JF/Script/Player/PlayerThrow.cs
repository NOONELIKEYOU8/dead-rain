using UnityEngine;

public class PlayerThrow : MonoBehaviour
{
    public GameObject bombPrefab;
    public Transform throwPoint;

    void Update()
    {
        if (Input.GetKeyDown(KeyCode.J))
        {
            ThrowBomb();
        }
    }

    void ThrowBomb()
    {
        if (bombPrefab == null || throwPoint == null)
        {
            Debug.LogWarning("请先设置 bombPrefab 和 throwPoint");
            return;
        }

        GameObject bombObj = Instantiate(bombPrefab, throwPoint.position, Quaternion.identity);

        Bomb bombScript = bombObj.GetComponent<Bomb>();
        if (bombScript != null)
        {
            Vector2 direction = new Vector2(transform.right.x, 0.5f).normalized;
            bombScript.ThrowBomb(direction);
        }
        else
        {
            Debug.LogError("bombPrefab 上没有 Bomb 脚本！");
        }
    }
}