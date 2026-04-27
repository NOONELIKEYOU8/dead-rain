using UnityEngine;

public class WeaponController : MonoBehaviour
{
    [Header("Weapon Settings")]
    public Transform handBone;
    public Vector3 positionOffset;
    public Vector3 rotationOffset;

    void LateUpdate()
    {
        if (handBone != null)
        {
            // Follow the hand bone position and rotation
            transform.position = handBone.position + handBone.TransformDirection(positionOffset);
            transform.rotation = handBone.rotation * Quaternion.Euler(rotationOffset);
        }
    }
}