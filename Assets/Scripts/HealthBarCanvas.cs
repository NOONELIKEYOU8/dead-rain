using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// HealthBarCanvas — World-Space Canvas health bar with smooth fill animation and
/// optional phase-threshold markers (used by BossEnemy to show phase break points).
/// 
/// Core design:
///   Canvas renders in World Space; sizeDelta == world units when localScale == 1.
///   The fill Image uses Image.Type.Filled (Horizontal) for simple percentage display.
///   Smooth animation lerps fillAmount toward the target over a configurable speed.
/// 
/// Phase markers:
///   Up to two vertical marker lines can be drawn at arbitrary HP % positions.
///   Useful for Boss fights to indicate Phase 2 / Phase 3 thresholds.
/// </summary>
public class HealthBarCanvas : MonoBehaviour
{
    [Tooltip("Health bar background Image (dark grey)")]
    public Image bgImage;

    [Tooltip("Fill Image (must be Image.Type.Filled, Horizontal)")]
    public Image fillImage;

    [Header("Colors")]
    public Color fullColor = Color.green;
    public Color midColor  = Color.yellow;
    public Color lowColor  = Color.red;
    [Range(0f, 1f)] public float midThreshold = 0.5f;
    [Range(0f, 1f)] public float lowThreshold = 0.25f;

    [Header("Smooth Fill")]
    [Tooltip("Lerp speed for the fill animation (higher = faster)")]
    public float fillSpeed = 8f;

    [Header("Phase Markers (optional)")]
    [Tooltip("HP % positions for vertical marker lines. Leave empty to disable.")]
    public float[] markerThresholds = new float[0];
    [Tooltip("Color of the phase marker lines")]
    public Color markerColor = new Color(1f, 1f, 1f, 0.7f);

    // ─── Runtime ─────────────────────────────────────────────────────────
    private float _targetFill = 1f;   // desired fill (0–1)
    private Image[] _markerImages;    // dynamically created marker UI Images

    // ─── Unity lifecycle ─────────────────────────────────────────────────
    private void Update()
    {
        if (fillImage == null) return;

        // Smooth lerp toward target
        fillImage.fillAmount = Mathf.Lerp(fillImage.fillAmount, _targetFill,
                                          Time.deltaTime * fillSpeed);

        // Update color based on current fill
        float ratio = fillImage.fillAmount;
        if      (ratio <= lowThreshold) fillImage.color = lowColor;
        else if (ratio <= midThreshold) fillImage.color = midColor;
        else                            fillImage.color = fullColor;
    }

    // ─── Public API ──────────────────────────────────────────────────────

    /// <summary>Set the target health ratio. The bar will animate toward it.</summary>
    public void UpdateBar(int current, int max)
    {
        if (fillImage == null) return;
        _targetFill = max > 0 ? Mathf.Clamp01((float)current / max) : 0f;
    }

    /// <summary>
    /// Set phase marker thresholds at runtime (e.g. called by BossEnemy).
    /// Pass the HP % values at which to draw a vertical divider line.
    /// </summary>
    public void SetPhaseMarkers(float[] thresholds)
    {
        markerThresholds = thresholds;
        RebuildMarkers();
    }

    // ─── Marker construction ─────────────────────────────────────────────

    private void RebuildMarkers()
    {
        // Destroy old markers
        if (_markerImages != null)
        {
            foreach (var m in _markerImages)
                if (m != null) Destroy(m.gameObject);
        }

        if (markerThresholds == null || markerThresholds.Length == 0) return;

        _markerImages = new Image[markerThresholds.Length];
        var rt = GetComponent<RectTransform>();
        float barW = rt != null ? rt.sizeDelta.x : 1f;
        float barH = rt != null ? rt.sizeDelta.y : 0.14f;

        for (int i = 0; i < markerThresholds.Length; i++)
        {
            var mGO = new GameObject($"Marker_{i:00}");
            mGO.transform.SetParent(transform, false);

            var img = mGO.AddComponent<Image>();
            img.color = markerColor;

            var mRt = mGO.GetComponent<RectTransform>();
            // Position: fraction along the bar width
            float xFraction = markerThresholds[i];
            mRt.anchorMin  = new Vector2(xFraction, 0f);
            mRt.anchorMax  = new Vector2(xFraction, 1f);
            mRt.sizeDelta  = new Vector2(0.015f, 0f);  // thin vertical line
            mRt.offsetMin  = new Vector2(-0.0075f, 0f);
            mRt.offsetMax  = new Vector2( 0.0075f, 0f);

            _markerImages[i] = img;
        }
    }

    // ─── Static factory ──────────────────────────────────────────────────

    /// <summary>
    /// Dynamically create a World-Space Canvas health bar.
    /// barWidth  : world units (matches enemy sprite width).
    /// barHeight : world units (0.12–0.18 recommended).
    /// worldPos  : center position in world space.
    /// </summary>
    public static HealthBarCanvas CreateDefault(
        Vector3 worldPos,
        float   barWidth  = 1.0f,
        float   barHeight = 0.14f)
    {
        // ── Root Canvas ──
        var canvasGO = new GameObject("EnemyHealthBar");

        var canvas = canvasGO.AddComponent<Canvas>();
        canvas.renderMode   = RenderMode.WorldSpace;
        canvas.sortingOrder = 20;
        canvas.pixelPerfect = false;

        var scaler = canvasGO.AddComponent<CanvasScaler>();
        scaler.dynamicPixelsPerUnit = 32f;

        canvasGO.AddComponent<GraphicRaycaster>().enabled = false;

        var rt = canvasGO.GetComponent<RectTransform>();
        rt.position   = worldPos;
        rt.rotation   = Quaternion.identity;
        rt.localScale = Vector3.one;
        rt.sizeDelta  = new Vector2(barWidth, barHeight);

        // ── Background ──
        var bgGO  = new GameObject("BG");
        bgGO.transform.SetParent(canvasGO.transform, false);
        var bgImg = bgGO.AddComponent<Image>();
        bgImg.color = new Color(0.08f, 0.08f, 0.08f, 0.88f);
        var bgRt = bgGO.GetComponent<RectTransform>();
        bgRt.anchorMin = Vector2.zero; bgRt.anchorMax = Vector2.one;
        bgRt.offsetMin = Vector2.zero; bgRt.offsetMax = Vector2.zero;

        // ── Fill ──
        var fillGO  = new GameObject("Fill");
        fillGO.transform.SetParent(canvasGO.transform, false);
        var fillImg = fillGO.AddComponent<Image>();
        fillImg.color      = Color.green;
        fillImg.type       = Image.Type.Filled;
        fillImg.fillMethod = Image.FillMethod.Horizontal;
        fillImg.fillAmount = 1f;
        var fillRt = fillGO.GetComponent<RectTransform>();
        fillRt.anchorMin = Vector2.zero; fillRt.anchorMax = Vector2.one;
        fillRt.offsetMin = new Vector2(2f,  2f);
        fillRt.offsetMax = new Vector2(-2f, -2f);

        // ── Border ──
        var borderGO  = new GameObject("Border");
        borderGO.transform.SetParent(canvasGO.transform, false);
        var borderImg = borderGO.AddComponent<Image>();
        borderImg.color = new Color(1f, 1f, 1f, 0.35f);
        borderImg.type  = Image.Type.Sliced;
        var borderRt = borderGO.GetComponent<RectTransform>();
        borderRt.anchorMin = Vector2.zero; borderRt.anchorMax = Vector2.one;
        borderRt.offsetMin = Vector2.zero; borderRt.offsetMax = Vector2.zero;

        bgGO.transform.SetSiblingIndex(0);
        fillGO.transform.SetSiblingIndex(1);
        borderGO.transform.SetSiblingIndex(2);

        // ── Script ──
        var hbc = canvasGO.AddComponent<HealthBarCanvas>();
        hbc.bgImage   = bgImg;
        hbc.fillImage = fillImg;

        return hbc;
    }
}
