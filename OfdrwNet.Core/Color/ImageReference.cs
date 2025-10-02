using System;
using System.Drawing;

namespace OfdrwNet.Core.Color;

/// <summary>
/// 描述计算 ΔE 所需的图像引用。
/// </summary>
public sealed class ImageReference
{
    /// <summary>
    /// 创建图像引用。
    /// </summary>
    public ImageReference(string source, Size pixelSize, double dpiX, double dpiY, string colorSpace)
    {
        if (string.IsNullOrWhiteSpace(source))
        {
            throw new ArgumentException("Image source cannot be empty.", nameof(source));
        }

        if (dpiX <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(dpiX), dpiX, "DPI must be positive.");
        }

        if (dpiY <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(dpiY), dpiY, "DPI must be positive.");
        }

        if (string.IsNullOrWhiteSpace(colorSpace))
        {
            throw new ArgumentException("Color space cannot be empty.", nameof(colorSpace));
        }

        Source = source;
        PixelSize = pixelSize;
        DpiX = dpiX;
        DpiY = dpiY;
        ColorSpace = colorSpace;
    }

    /// <summary>
    /// 图像来源（可为文件路径或资源 ID）。
    /// </summary>
    public string Source { get; }

    /// <summary>
    /// 图像尺寸（像素）。
    /// </summary>
    public Size PixelSize { get; }

    /// <summary>
    /// 水平 DPI。
    /// </summary>
    public double DpiX { get; }

    /// <summary>
    /// 垂直 DPI。
    /// </summary>
    public double DpiY { get; }

    /// <summary>
    /// 颜色空间描述。
    /// </summary>
    public string ColorSpace { get; }

    /// <summary>
    /// 指示是否已经转换到标准 sRGB 空间。
    /// </summary>
    public bool IsSrgb => string.Equals(ColorSpace, "sRGB", StringComparison.OrdinalIgnoreCase);
}
