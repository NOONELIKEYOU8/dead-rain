using UnityEngine;

public class WorldHealthBarUI : MonoBehaviour
{
    [SerializeField] private RectTransform rectTransform;
    [SerializeField] private Vector3 worldOffset = new Vector3(0f, 1.35f, 0f);
    [SerializeField] private bool hideWhenFull = true;

    private Transform target;
    private Stats stats;
    private Camera worldCamera;

    public void Bind(Transform targetTransform, Stats targetStats, Camera camera)
    {
        target = targetTransform;
        stats = targetStats;
        worldCamera = camera;

        if (stats != null)
        {
            stats.OnHealthChanged += HandleHealthChanged;
            HandleHealthChanged(stats.CurrentHealth, stats.MaxHealth);
        }
    }

    private void OnDestroy()
    {
        if (stats != null)
        {
            stats.OnHealthChanged -= HandleHealthChanged;
        }
    }

    private void LateUpdate()
    {
        if (target == null || rectTransform == null)
        {
            gameObject.SetActive(false);
            return;
        }

        if (worldCamera == null)
        {
            worldCamera = Camera.main;
        }

        if (worldCamera == null)
        {
            return;
        }

        rectTransform.position = worldCamera.WorldToScreenPoint(target.position + worldOffset);
    }

    private void HandleHealthChanged(float current, float max)
    {
        if (!hideWhenFull)
        {
            gameObject.SetActive(current > 0f);
            return;
        }

        gameObject.SetActive(current > 0f && current < max);
    }
}
