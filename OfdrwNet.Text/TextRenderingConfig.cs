namespace OfdrwNet.Text;

/// <summary>
/// 文本渲染配置，用于调整文本位置和渲染参数
/// </summary>
public class TextRenderingConfig
{
    /// <summary>
    /// 单例实例
    /// </summary>
    public static TextRenderingConfig Instance { get; } = new TextRenderingConfig();

    private TextRenderingConfig() { }

    /// <summary>
    /// GDI 基线偏移系数（用于向上调整文本位置）
    /// 默认值 0.15f，增大此值会使文本更向上移动
    /// </summary>
    public float GdiBaselineOffsetFactor { get; set; } = 1.0f;

    /// <summary>
    /// Skia 基线偏移系数（用于向下调整文本位置）
    /// 默认值 0.8f，调整此值可以控制文本的垂直位置
    /// </summary>
    public float SkiaBaselineOffsetFactor { get; set; } = 1.0f;

    /// <summary>
    /// 是否启用基线校正
    /// </summary>
    public bool EnableBaselineCorrection { get; set; } = true;

    /// <summary>
    /// 是否启用调试输出
    /// </summary>
    public bool EnableDebugOutput { get; set; } = false;

    /// <summary>
    /// 字体大小缩放因子
    /// </summary>
    public float FontSizeScaleFactor { get; set; } = 1.0f;

    /// <summary>
    /// 重置为默认设置
    /// </summary>
    public void ResetToDefaults()
    {
    GdiBaselineOffsetFactor = 1.0f;
    SkiaBaselineOffsetFactor = 1.0f;
        EnableBaselineCorrection = true;
        EnableDebugOutput = false;
        FontSizeScaleFactor = 1.0f;
    }

    /// <summary>
    /// 应用自定义设置
    /// </summary>
    /// <param name="gdiOffset">GDI 偏移系数</param>
    /// <param name="skiaOffset">Skia 偏移系数</param>
    /// <param name="enableCorrection">是否启用校正</param>
    public void ApplySettings(float gdiOffset, float skiaOffset, bool enableCorrection = true)
    {
        GdiBaselineOffsetFactor = gdiOffset;
        SkiaBaselineOffsetFactor = skiaOffset;
        EnableBaselineCorrection = enableCorrection;
    }

    /// <summary>
    /// 获取配置摘要
    /// </summary>
    /// <returns>配置信息字符串</returns>
    public string GetConfigSummary()
    {
        return $"TextRenderingConfig: " +
               $"GDI偏移={GdiBaselineOffsetFactor:F3}, " +
               $"Skia偏移={SkiaBaselineOffsetFactor:F3}, " +
               $"基线校正={EnableBaselineCorrection}, " +
               $"字体缩放={FontSizeScaleFactor:F2}";
    }
}
