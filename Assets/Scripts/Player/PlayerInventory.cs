using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlayerInventory : MonoBehaviour
{
    public Weapon[] weapons;
    public ShieldWeapon shield;

    [SerializeField] private int primaryWeaponIndex;
    [SerializeField] private int secondaryWeaponIndex = 1;

    public int Version { get; private set; }

    private void Update()
    {
        for (int i = 0; i < weapons.Length && i < 9; i++)
        {
            if (Input.GetKeyDown((KeyCode)((int)KeyCode.Alpha1 + i)))
            {
                SetPrimaryWeapon(i);
            }
        }
    }

    public Weapon GetWeapon(CombatInputs input)
    {
        return input == CombatInputs.primary
            ? GetWeaponAt(primaryWeaponIndex)
            : shield;
    }

    public ShieldWeapon GetShield()
    {
        return shield;
    }

    private void SetPrimaryWeapon(int index)
    {
        Weapon weapon = GetWeaponAt(index);
        if (index == primaryWeaponIndex || weapon == null || weapon == shield)
        {
            return;
        }

        primaryWeaponIndex = index;
        Version++;
    }

    private Weapon GetWeaponAt(int index)
    {
        if (weapons == null || index < 0 || index >= weapons.Length)
        {
            return null;
        }

        return weapons[index];
    }

    private void OnValidate()
    {
        if (weapons == null || weapons.Length == 0)
        {
            primaryWeaponIndex = 0;
            secondaryWeaponIndex = 0;
            return;
        }

        primaryWeaponIndex = Mathf.Clamp(primaryWeaponIndex, 0, weapons.Length - 1);
        secondaryWeaponIndex = Mathf.Clamp(secondaryWeaponIndex, 0, weapons.Length - 1);
    }
}
