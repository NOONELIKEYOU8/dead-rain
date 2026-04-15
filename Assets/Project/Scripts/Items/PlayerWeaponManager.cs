using System;
using System.Collections.Generic;
using UnityEngine;

[DisallowMultipleComponent]
/// <summary>
/// 武器管理器：P 切换武器，J 让当前武器执行 Attack。
/// OnUse 表示“装备/进入使用状态”，Attack 才是实际攻击。
/// </summary>
public class PlayerWeaponManager : MonoBehaviour
{
    [Serializable]
    public class WeaponSlot
    {
        [Tooltip("用于调试和后续 UI 展示的武器名")]
        public string slotName = "Weapon";

        [Tooltip("当前槽位对应的武器预制体")]
        public Weapon weaponPrefab;

        [Tooltip("武器相对挂点的初始位置偏移（朝右时生效，朝左会自动镜像X）")]
        public Vector3 localPositionOffset = Vector3.zero;
    }

    [Header("Weapon Slots")]
    [SerializeField] private List<WeaponSlot> weaponSlots = new List<WeaponSlot>();
    [SerializeField] private int defaultWeaponIndex = 0;

    [Header("Input")]
    [SerializeField] private KeyCode switchKey = KeyCode.P;
    [SerializeField] private KeyCode attackKey = KeyCode.J;

    [Header("Spawn")]
    [SerializeField] private Transform weaponSpawnPoint;

    private int currentWeaponIndex = -1;
    private Weapon currentWeaponInstance;
    private Collider2D[] playerColliders;

    private void Awake()
    {
        playerColliders = GetComponentsInChildren<Collider2D>(true);

        if (weaponSpawnPoint == null)
        {
            weaponSpawnPoint = transform;
        }

        if (weaponSlots.Count > 0)
        {
            EquipWeapon(Mathf.Clamp(defaultWeaponIndex, 0, weaponSlots.Count - 1));
        }
    }

    private void OnDisable()
    {
        ClearCurrentWeapon();
    }

    private void Update()
    {
        if (Input.GetKeyDown(switchKey))
        {
            EquipNext();
        }

        if (Input.GetKeyDown(attackKey))
        {
            AttackCurrentWeapon();
        }
    }

    public void EquipWeapon(int index)
    {
        if (!IsValidIndex(index))
        {
            Debug.LogWarning($"武器索引越界: {index}");
            return;
        }

        if (index == currentWeaponIndex && currentWeaponInstance != null)
        {
            return;
        }

        currentWeaponIndex = index;
        Debug.Log($"切换武器 -> [{currentWeaponIndex + 1}] {weaponSlots[currentWeaponIndex].slotName}");

        EquipCurrentWeaponInstance();
    }

    public void EquipNext()
    {
        if (weaponSlots.Count == 0)
        {
            return;
        }

        int next = (currentWeaponIndex + 1 + weaponSlots.Count) % weaponSlots.Count;
        EquipWeapon(next);
    }

    public void AttackCurrentWeapon()
    {
        if (currentWeaponInstance == null)
        {
            return;
        }

        if (currentWeaponInstance is Bomb bomb)
        {
            bomb.SetHeldState(false, null);
            SetIgnoreCollisionWithPlayer(bomb, false);
        }

        bool success = currentWeaponInstance.Attack(gameObject);
        if (!success)
        {
            Debug.LogWarning($"武器攻击失败: {weaponSlots[currentWeaponIndex].slotName}");
        }

        if (currentWeaponInstance is Bomb)
        {
            currentWeaponInstance = null;
            SpawnCurrentWeaponInstance();
        }
    }

    private void EquipCurrentWeaponInstance()
    {
        ClearCurrentWeapon();
        SpawnCurrentWeaponInstance();
    }


    /// 生成当前武器实例并设置父对象
    private void SpawnCurrentWeaponInstance()
    {
        if (!IsValidIndex(currentWeaponIndex))
        {
            return;
        }

        WeaponSlot slot = weaponSlots[currentWeaponIndex];
        if (slot.weaponPrefab == null)
        {
            return;
        }

        Vector3 spawnPos = weaponSpawnPoint != null ? weaponSpawnPoint.position : transform.position;
        Quaternion spawnRot = weaponSpawnPoint != null ? weaponSpawnPoint.rotation : Quaternion.identity;

        currentWeaponInstance = Instantiate(slot.weaponPrefab, spawnPos, spawnRot);
        currentWeaponInstance.transform.SetParent(weaponSpawnPoint, false);
        currentWeaponInstance.transform.localPosition = Vector3.zero;
        currentWeaponInstance.transform.localRotation = Quaternion.identity;
        currentWeaponInstance.OnUse(gameObject);

        if (currentWeaponInstance is Bomb bomb)
        {
            bomb.SetHeldState(true, weaponSpawnPoint);
            SetIgnoreCollisionWithPlayer(bomb, true);
        }

        ApplyLocalPositionOffset(currentWeaponInstance.transform, slot.localPositionOffset);
    }

    private void ApplyLocalPositionOffset(Transform weaponTransform, Vector3 localOffset)
    {
        if (weaponTransform == null)
        {
            return;
        }

        // 角色朝左时镜像 X 偏移，保证同一配置在左右朝向都合理。
        float facing = transform.localScale.x >= 0f ? 1f : -1f;
        Vector3 finalOffset = localOffset;
        finalOffset.x *= facing;
        weaponTransform.localPosition = finalOffset;
    }

    private void SetIgnoreCollisionWithPlayer(Bomb bomb, bool ignore)
    {
        if (bomb == null || playerColliders == null)
        {
            return;
        }

        Collider2D bombCollider = bomb.GetComponent<Collider2D>();
        if (bombCollider == null)
        {
            return;
        }

        for (int i = 0; i < playerColliders.Length; i++)
        {
            if (playerColliders[i] != null)
            {
                Physics2D.IgnoreCollision(playerColliders[i], bombCollider, ignore);
            }
        }
    }

    private void ClearCurrentWeapon()
    {
        if (currentWeaponInstance != null)
        {
            Destroy(currentWeaponInstance.gameObject);
        }

        currentWeaponInstance = null;
    }

    private bool IsValidIndex(int index)
    {
        return index >= 0 && index < weaponSlots.Count;
    }
}
