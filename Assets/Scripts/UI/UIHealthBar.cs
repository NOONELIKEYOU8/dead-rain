using UnityEngine;
using UnityEngine.UI;

public class UIHealthBar : MonoBehaviour
{
    [SerializeField] private Image fillImage;
    [SerializeField] private Text valueText;
    [SerializeField] private float smoothing = 12f;

    private Stats stats;
    private float targetPercent = 1f;
    private float currentHealth;
    private float maxHealth = 1f;

    public void Configure(Image fill, Text value)
    {
        fillImage = fill;
        valueText = value;
    }

    public void Bind(Stats targetStats)
    {
        if (stats != null)
        {
            stats.OnHealthChanged -= HandleHealthChanged;
        }

        stats = targetStats;

        if (stats == null)
        {
            gameObject.SetActive(false);
            return;
        }

        stats.OnHealthChanged += HandleHealthChanged;
        HandleHealthChanged(stats.CurrentHealth, stats.MaxHealth);
    }

    private void OnDestroy()
    {
        if (stats != null)
        {
            stats.OnHealthChanged -= HandleHealthChanged;
        }
    }

    private void Update()
    {
        if (fillImage == null)
        {
            return;
        }

        fillImage.fillAmount = Mathf.MoveTowards(fillImage.fillAmount, targetPercent, smoothing * Time.unscaledDeltaTime);
    }

    private void HandleHealthChanged(float current, float max)
    {
        currentHealth = current;
        maxHealth = Mathf.Max(1f, max);
        targetPercent = Mathf.Clamp01(currentHealth / maxHealth);

        if (fillImage != null && fillImage.fillAmount <= 0f)
        {
            fillImage.fillAmount = targetPercent;
        }

        if (valueText != null)
        {
            valueText.text = $"{Mathf.CeilToInt(currentHealth)} / {Mathf.CeilToInt(maxHealth)}";
        }
    }
}
