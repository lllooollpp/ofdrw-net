using System.Drawing;
using OfdrwNet.Core.BasicType;

namespace OfdrwNet.Core.Resource;

/// <summary>
/// OFD图像资源管理器
/// 用于管理OFD文档中的图像资源，包括嵌入、引用、格式转换等
/// </summary>
public class ImageResource : IDisposable
{
    /// <summary>
    /// 图像资源ID
    /// </summary>
    public StId Id { get; set; }

    /// <summary>
    /// 图像类型
    /// </summary>
    public ImageType Type { get; set; }

    /// <summary>
    /// 图像格式
    /// </summary>
    public ImageFormat Format { get; set; }

    /// <summary>
    /// 图像宽度（像素）
    /// </summary>
    public int Width { get; set; }

    /// <summary>
    /// 图像高度（像素）
    /// </summary>
    public int Height { get; set; }

    /// <summary>
    /// 图像数据
    /// </summary>
    public byte[]? ImageData { get; set; }

    /// <summary>
    /// 图像文件路径（如果是外部引用）
    /// </summary>
    public string? FilePath { get; set; }

    /// <summary>
    /// 图像在OFD中的相对路径
    /// </summary>
    public StLoc? ResourcePath { get; set; }

    /// <summary>
    /// 是否嵌入到OFD文件中
    /// </summary>
    public bool IsEmbedded { get; set; } = true;

    /// <summary>
    /// 图像摘要信息
    /// </summary>
    public string? Checksum { get; set; }

    /// <summary>
    /// 压缩质量（0-100，仅对JPEG有效）
    /// </summary>
    public int Quality { get; set; } = 85;

    /// <summary>
    /// 是否已释放资源
    /// </summary>
    private bool _disposed = false;

    /// <summary>
    /// 构造函数
    /// </summary>
    /// <param name="id">图像资源ID</param>
    public ImageResource(StId id)
    {
        Id = id;
        Type = ImageType.Bitmap;
        Format = ImageFormat.PNG;
        Width = 0;
        Height = 0;
    }

    /// <summary>
    /// 从文件创建图像资源
    /// </summary>
    /// <param name="id">资源ID</param>
    /// <param name="filePath">图像文件路径</param>
    /// <returns>图像资源对象</returns>
    public static ImageResource FromFile(StId id, string filePath)
    {
        if (string.IsNullOrEmpty(filePath) || !File.Exists(filePath))
        {
            throw new FileNotFoundException("图像文件不存在", filePath);
        }

        var resource = new ImageResource(id)
        {
            FilePath = filePath,
            ImageData = File.ReadAllBytes(filePath),
            Format = GetImageFormatFromExtension(Path.GetExtension(filePath))
        };

        // 获取图像尺寸
        resource.GetImageDimensions();
        
        // 计算校验和
        resource.ComputeChecksum();

        return resource;
    }

    /// <summary>
    /// 从字节数组创建图像资源
    /// </summary>
    /// <param name="id">资源ID</param>
    /// <param name="imageData">图像数据</param>
    /// <param name="format">图像格式</param>
    /// <returns>图像资源对象</returns>
    public static ImageResource FromBytes(StId id, byte[] imageData, ImageFormat format)
    {
        if (imageData == null || imageData.Length == 0)
        {
            throw new ArgumentException("图像数据不能为空", nameof(imageData));
        }

        var resource = new ImageResource(id)
        {
            ImageData = imageData,
            Format = format
        };

        // 获取图像尺寸
        resource.GetImageDimensions();
        
        // 计算校验和
        resource.ComputeChecksum();

        return resource;
    }

    /// <summary>
    /// 获取图像尺寸信息
    /// </summary>
    private void GetImageDimensions()
    {
        // 简化实现：设置默认尺寸
        // 实际项目中可以使用SkiaSharp或ImageSharp获取实际尺寸
        Width = 100;
        Height = 100;
    }

    /// <summary>
    /// 计算图像数据的校验和
    /// </summary>
    private void ComputeChecksum()
    {
        if (ImageData == null || ImageData.Length == 0)
            return;

        using var sha256 = System.Security.Cryptography.SHA256.Create();
        var hash = sha256.ComputeHash(ImageData);
        Checksum = Convert.ToHexString(hash);
    }

    /// <summary>
    /// 根据文件扩展名获取图像格式
    /// </summary>
    /// <param name="extension">文件扩展名</param>
    /// <returns>图像格式</returns>
    private static ImageFormat GetImageFormatFromExtension(string extension)
    {
        return extension.ToLowerInvariant() switch
        {
            ".jpg" or ".jpeg" => ImageFormat.JPEG,
            ".png" => ImageFormat.PNG,
            ".gif" => ImageFormat.GIF,
            ".bmp" => ImageFormat.BMP,
            ".tiff" or ".tif" => ImageFormat.TIFF,
            _ => ImageFormat.PNG
        };
    }

    /// <summary>
    /// 获取图像的MIME类型
    /// </summary>
    /// <returns>MIME类型</returns>
    public string GetMimeType()
    {
        return Format switch
        {
            ImageFormat.JPEG => "image/jpeg",
            ImageFormat.PNG => "image/png",
            ImageFormat.GIF => "image/gif",
            ImageFormat.BMP => "image/bmp",
            ImageFormat.TIFF => "image/tiff",
            _ => "image/png"
        };
    }

    /// <summary>
    /// 获取图像文件扩展名
    /// </summary>
    /// <returns>文件扩展名</returns>
    public string GetFileExtension()
    {
        return Format switch
        {
            ImageFormat.JPEG => ".jpg",
            ImageFormat.PNG => ".png",
            ImageFormat.GIF => ".gif",
            ImageFormat.BMP => ".bmp",
            ImageFormat.TIFF => ".tiff",
            _ => ".png"
        };
    }

    /// <summary>
    /// 生成资源路径
    /// </summary>
    /// <param name="resourceDir">资源目录</param>
    /// <returns>资源路径</returns>
    public StLoc GenerateResourcePath(string resourceDir = "Res")
    {
        var fileName = $"Image_{Id}{GetFileExtension()}";
        ResourcePath = new StLoc($"/{resourceDir}/{fileName}");
        return ResourcePath;
    }

    /// <summary>
    /// 转换为不同格式
    /// </summary>
    /// <param name="targetFormat">目标格式</param>
    /// <param name="quality">质量参数（仅对JPEG有效）</param>
    /// <returns>转换后的图像数据</returns>
    public byte[] ConvertTo(ImageFormat targetFormat, int quality = 85)
    {
        if (ImageData == null || ImageData.Length == 0)
        {
            throw new InvalidOperationException("图像数据为空，无法转换");
        }

        if (Format == targetFormat)
        {
            return ImageData;
        }

        // 这里应该使用专业的图像处理库，如SkiaSharp
        // 目前返回原始数据作为占位符
        return ImageData;
    }

    /// <summary>
    /// 压缩图像
    /// </summary>
    /// <param name="quality">压缩质量（0-100）</param>
    /// <returns>压缩后的图像数据</returns>
    public byte[] Compress(int quality = 85)
    {
        if (ImageData == null || ImageData.Length == 0)
        {
            throw new InvalidOperationException("图像数据为空，无法压缩");
        }

        Quality = Math.Max(0, Math.Min(100, quality));
        
        // 这里应该使用专业的图像处理库进行压缩
        // 目前返回原始数据作为占位符
        return ImageData;
    }

    /// <summary>
    /// 验证图像数据完整性
    /// </summary>
    /// <returns>验证结果</returns>
    public bool ValidateIntegrity()
    {
        if (ImageData == null || ImageData.Length == 0)
            return false;

        if (string.IsNullOrEmpty(Checksum))
            return true; // 没有校验和时认为有效

        using var sha256 = System.Security.Cryptography.SHA256.Create();
        var hash = sha256.ComputeHash(ImageData);
        var currentChecksum = Convert.ToHexString(hash);
        
        return string.Equals(Checksum, currentChecksum, StringComparison.OrdinalIgnoreCase);
    }

    /// <summary>
    /// 释放资源
    /// </summary>
    public void Dispose()
    {
        if (!_disposed)
        {
            ImageData = null;
            _disposed = true;
        }
    }

    /// <summary>
    /// 获取资源摘要信息
    /// </summary>
    /// <returns>摘要字符串</returns>
    public override string ToString()
    {
        return $"ImageResource[ID={Id}, Format={Format}, Size={Width}x{Height}, Embedded={IsEmbedded}]";
    }
}

/// <summary>
/// 图像类型枚举
/// </summary>
public enum ImageType
{
    /// <summary>
    /// 位图
    /// </summary>
    Bitmap,

    /// <summary>
    /// 矢量图
    /// </summary>
    Vector
}

/// <summary>
/// 图像格式枚举
/// </summary>
public enum ImageFormat
{
    /// <summary>
    /// PNG格式
    /// </summary>
    PNG,

    /// <summary>
    /// JPEG格式
    /// </summary>
    JPEG,

    /// <summary>
    /// GIF格式
    /// </summary>
    GIF,

    /// <summary>
    /// BMP格式
    /// </summary>
    BMP,

    /// <summary>
    /// TIFF格式
    /// </summary>
    TIFF
}