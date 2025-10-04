using OfdrwNet.Core;
using SkiaSharp;

namespace OfdrwNet.Image;

/// <summary>
/// 图像导出配置实现
/// </summary>
public class ImageExportConfig : IImageExportConfig
{
    /// <summary>
    /// 图像格式
    /// </summary>
    public SKEncodedImageFormat ImageFormat { get; set; } = SKEncodedImageFormat.Png;

    /// <summary>
    /// 图像质量（0-100），仅对JPEG有效
    /// </summary>
    public int Quality { get; set; } = 100;

    /// <summary>
    /// 分辨率（DPI）
    /// </summary>
    public float Dpi { get; set; } = 150f;

    /// <summary>
    /// 构造函数
    /// </summary>
    public ImageExportConfig()
    {
    }

    /// <summary>
    /// 构造函数
    /// </summary>
    /// <param name="imageFormat">图像格式</param>
    /// <param name="quality">图像质量</param>
    /// <param name="dpi">分辨率</param>
    public ImageExportConfig(SKEncodedImageFormat imageFormat, int quality = 100, float dpi = 150f)
    {
        ImageFormat = imageFormat;
        Quality = Math.Max(0, Math.Min(100, quality));
        Dpi = Math.Max(72f, dpi);
    }
}
