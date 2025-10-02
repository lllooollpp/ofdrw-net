using System.Drawing;

namespace OfdrwNet.Core.Resources;

/// <summary>
/// 描述转换过程中的图像资源。
/// </summary>
public sealed class ImageResource
{
    /// <summary>
    /// 创建图像资源描述。
    /// </summary>
    public ImageResource(
        string id,
        string format,
        Size pixelSize,
        double dpiX,
        double dpiY,
        string colorSpace,
        string? fileRef = null,
        long? lengthBytes = null)
    {
        if (string.IsNullOrWhiteSpace(id))
        {
            throw new ArgumentException("Image id cannot be null or empty.", nameof(id));
        }

        if (string.IsNullOrWhiteSpace(format))
        {
            throw new ArgumentException("Image format cannot be null or empty.", nameof(format));
        }

        Id = id;
        Format = format.ToLowerInvariant();
        PixelSize = pixelSize;
        DpiX = dpiX <= 0 ? throw new ArgumentOutOfRangeException(nameof(dpiX)) : dpiX;
        DpiY = dpiY <= 0 ? throw new ArgumentOutOfRangeException(nameof(dpiY)) : dpiY;
        ColorSpace = colorSpace ?? throw new ArgumentNullException(nameof(colorSpace));
        FileRef = fileRef;
        LengthBytes = lengthBytes;
    }

    /// <summary>
    /// 唯一标识。
    /// </summary>
    public string Id { get; }

    /// <summary>
    /// 图像编码格式，例如 png、jpeg。
    /// </summary>
    public string Format { get; }

    /// <summary>
    /// 像素尺寸。
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
    /// 颜色空间（sRGB/CMYK/Gray 等）。
    /// </summary>
    public string ColorSpace { get; }

    /// <summary>
    /// 打包内的文件引用路径。
    /// </summary>
    public string? FileRef { get; }

    /// <summary>
    /// 原始图像字节大小（可选）。
    /// </summary>
    public long? LengthBytes { get; }

    /// <summary>
    /// 是否为矢量图像（例如 svg）。
    /// </summary>
    public bool IsVector => Format is "svg" or "svgz";

    /// <summary>
    /// 当前图像的分辨率是否超过阈值。
    /// </summary>
    public bool ExceedsDpi(double maxDpi)
    {
        if (maxDpi <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(maxDpi));
        }

        return DpiX > maxDpi || DpiY > maxDpi;
    }

    /// <summary>
    /// 以新文件引用创建副本。
    /// </summary>
    public ImageResource WithFileRef(string fileRef)
    {
        if (string.IsNullOrWhiteSpace(fileRef))
        {
            throw new ArgumentException("File reference cannot be null or whitespace.", nameof(fileRef));
        }

        return new ImageResource(Id, Format, PixelSize, DpiX, DpiY, ColorSpace, fileRef, LengthBytes);
    }

    /// <summary>
    /// 以新 DPI 创建副本。
    /// </summary>
    public ImageResource WithDpi(double dpiX, double dpiY)
    {
        return new ImageResource(Id, Format, PixelSize, dpiX, dpiY, ColorSpace, FileRef, LengthBytes);
    }

    /// <inheritdoc />
    public override string ToString()
    {
        return $"ImageResource[{Id}, {Format}, {PixelSize.Width}x{PixelSize.Height}@{DpiX:F1}/{DpiY:F1}dpi, {ColorSpace}]";
    }
}
