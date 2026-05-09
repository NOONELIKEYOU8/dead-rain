using UnityEngine;

public class CameraFollow2D : MonoBehaviour
{
    [SerializeField] private Transform target;
    [SerializeField] private Vector3 offset = new Vector3(1.5f, 2.5f, -10f);
    [SerializeField] private float smoothTime = 0.12f;

    private Vector3 velocity;

    private void LateUpdate()
    {
        if (target == null)
        {
            GameObject player = GameObject.FindGameObjectWithTag("Player");
            target = player != null ? player.transform : null;
        }

        if (target == null)
        {
            return;
        }

        Vector3 desired = target.position + offset;
        desired.z = offset.z;
        transform.position = Vector3.SmoothDamp(transform.position, desired, ref velocity, smoothTime);
    }
}
