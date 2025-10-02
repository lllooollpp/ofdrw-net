namespace OfdrwNet.Converter.Domain;

/// <summary>
/// 图像资源描述
/// </summary>
public sealed class ImageResource
{
    /// <summary>
    /// 图像唯一标识符
    /// </summary>
    public required string Id { get; init; }

    /// <summary>
    /// 图像格式 (JPEG, PNG, TIFF等)
    /// </summary>
    public required string Format { get; init; }

    /// <summary>
    /// 分辨率 (DPI)
    /// </summary>
    public int Dpi { get; init; } = 96;

    /// <summary>
    /// 颜色空间 (RGB, CMYK, Gray等)
    /// </summary>
    public string? ColorSpace { get; init; }

    /// <summary>
    /// 图像文件引用路径
    /// </summary>
    public required string FileRef { get; init; }

    /// <summary>
    /// 图像宽度（像素）
    /// </summary>
    public int Width { get; init; }

    /// <summary>
    /// 图像高度（像素）
    /// </summary>
    public int Height { get; init; }

    /// <summary>
    /// 颜色通道数
    /// </summary>
    public int Channels { get; init; } = 3;

    /// <summary>
    /// 是否包含 Alpha 通道
    /// </summary>
    public bool HasAlpha { get; init; }

    /// <summary>
    /// 压缩质量（0-100）
    /// </summary>
    public int? Quality { get; init; }
}
