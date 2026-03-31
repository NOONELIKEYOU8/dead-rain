using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 挂在 World-Space Canvas 上的血条控制脚本。
/// 通过 Image（Filled）实现血量百分比显示。
/// 
/// 核心设计：Canvas 使用世界空间（RectTransform 单位 = Unity 世界单位），
/// 不依赖 localScale 缩放，杜绝缩放错位问题。
/// </summary>
public class HealthBarCanvas : MonoBehaviour
{
    [Tooltip("血条背景图（可空）")]
    public Image bgImage;
    [Tooltip("血条填充图（需为 Filled 类型）")]
    public Image fillImage;

    [Header("Colors")]
    public Color fullColor    = Color.green;
    public Color midColor     = Color.yellow;
    public Color lowColor     = Color.red;
    [Range(0f, 1f)]
    public float midThreshold = 0.5f;
    [Range(0f, 1f)]
    public float lowThreshold = 0.25f;

    // ─── 公有方法 ────────────────────────────────────────────────────────
    /// <summary>更新血条显示（当前血量 / 最大血量）</summary>
    public void UpdateBar(int current, int max)
    {
        if (fillImage == null) return;
        float ratio = max > 0 ? Mathf.Clamp01((float)current / max) : 0f;
        fillImage.fillAmount = ratio;

        if (ratio <= lowThreshold)
            fillImage.color = lowColor;
        else if (ratio <= midThreshold)
            fillImage.color = midColor;
        else
            fillImage.color = fullColor;
    }

    // ─── 静态工厂 ────────────────────────────────────────────────────────
    /// <summary>
    /// 动态创建 World-Space Canvas 血条。
    /// 
    /// barWidth  : 血条宽度（世界单位），建议与敌人 Sprite 宽度匹配
    /// barHeight : 血条高度（世界单位），0.12~0.18 为宜
    /// worldPos  : 血条中心世界坐标
    /// </summary>
    public static HealthBarCanvas CreateDefault(
        Vector3 worldPos,
        float   barWidth  = 1.0f,
        float   barHeight = 0.14f)
    {
        // ── Canvas 根对象（独立于敌人 Transform，避免随敌人翻转/缩放）
        var canvasGO = new GameObject("EnemyHealthBar");

        var canvas = canvasGO.AddComponent<Canvas>();
        canvas.renderMode   = RenderMode.WorldSpace;
        canvas.sortingOrder = 20;          // 确保显示在角色上层
        canvas.pixelPerfect = false;

        // CanvasScaler：dynamicPixelsPerUnit 仅影响渲染像素密度（越高越清晰），不影响世界空间尺寸
        var scaler = canvasGO.AddComponent<CanvasScaler>();
        scaler.dynamicPixelsPerUnit = 32f; // 血条尺寸约 1 世界单位，32px/unit 足够清晰

        canvasGO.AddComponent<GraphicRaycaster>().enabled = false;

        // ── RectTransform：World Space Canvas 中 sizeDelta 直接等于世界单位（与 pixelsPerUnit 无关）
        // 公式：屏幕/世界宽度 = sizeDelta.x * localScale.x
        // localScale = 1 时，sizeDelta = 世界单位，直接赋值即可
        var rt = canvasGO.GetComponent<RectTransform>();
        rt.position   = worldPos;
        rt.rotation   = Quaternion.identity;
        rt.localScale = Vector3.one;
        rt.sizeDelta  = new Vector2(barWidth, barHeight);

        // ── 背景 Image（深灰，略带透明）
        var bgGO  = new GameObject("BG");
        bgGO.transform.SetParent(canvasGO.transform, false);
        var bgImg = bgGO.AddComponent<Image>();
        bgImg.color = new Color(0.1f, 0.1f, 0.1f, 0.85f);
        var bgRt = bgGO.GetComponent<RectTransform>();
        bgRt.anchorMin  = Vector2.zero;
        bgRt.anchorMax  = Vector2.one;
        bgRt.offsetMin  = Vector2.zero;
        bgRt.offsetMax  = Vector2.zero;

        // ── 填充 Image（Filled，绿色）
        var fillGO  = new GameObject("Fill");
        fillGO.transform.SetParent(canvasGO.transform, false);
        var fillImg = fillGO.AddComponent<Image>();
        fillImg.color      = Color.green;
        fillImg.type       = Image.Type.Filled;
        fillImg.fillMethod = Image.FillMethod.Horizontal;
        fillImg.fillAmount = 1f;
        var fillRt = fillGO.GetComponent<RectTransform>();
        fillRt.anchorMin = Vector2.zero;
        fillRt.anchorMax = Vector2.one;
        // 内缩 2 像素留边框感
        fillRt.offsetMin = new Vector2(2f, 2f);
        fillRt.offsetMax = new Vector2(-2f, -2f);

        // ── 边框 Image（白色描边，可选）
        var borderGO  = new GameObject("Border");
        borderGO.transform.SetParent(canvasGO.transform, false);
        var borderImg = borderGO.AddComponent<Image>();
        borderImg.color = new Color(1f, 1f, 1f, 0.4f);
        borderImg.type  = Image.Type.Sliced;   // 若无9宫格Sprite则退回Simple
        var borderRt = borderGO.GetComponent<RectTransform>();
        borderRt.anchorMin = Vector2.zero;
        borderRt.anchorMax = Vector2.one;
        borderRt.offsetMin = Vector2.zero;
        borderRt.offsetMax = Vector2.zero;
        // 边框放在 BG 之下（将 BG 和 Fill 移到前面）
        bgGO.transform.SetSiblingIndex(0);
        fillGO.transform.SetSiblingIndex(1);
        borderGO.transform.SetSiblingIndex(2);

        // ── 挂载脚本
        var hbc = canvasGO.AddComponent<HealthBarCanvas>();
        hbc.bgImage   = bgImg;
        hbc.fillImage = fillImg;

        return hbc;
    }
}
