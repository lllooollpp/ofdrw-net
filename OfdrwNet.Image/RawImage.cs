using System.Security.Cryptography;
using System.Text;

namespace OfdrwNet.Image;

/// <summary>
/// 原始图片资源（页面对象引用）
/// 描述OFD文档中图片的位置、尺寸、格式等信息
/// </summary>
public class RawImage
{
    /// <summary>
    /// 图片格式
    /// </summary>
    public string Format { get; set; } = "PNG"; // PNG/JPG/GIF/BMP/TIFF/WEBP

    /// <summary>
    /// X坐标（毫米单位）
    /// </summary>
    public double X { get; set; }

    /// <summary>
    /// Y坐标（毫米单位）
    /// </summary>
    public double Y { get; set; }

    /// <summary>
    /// 宽度（毫米单位）
    /// </summary>
    public double Width { get; set; }

    /// <summary>
    /// 高度（毫米单位）
    /// </summary>
    public double Height { get; set; }

    /// <summary>
    /// 图片数据
    /// </summary>
    public byte[] Data { get; set; } = Array.Empty<byte>();

    /// <summary>
    /// 所在页码
    /// </summary>
    public int Page { get; set; } = 1;

    /// <summary>
    /// 资源ID
    /// </summary>
    public int ResourceID { get; set; }

    /// <summary>
    /// 数据哈希值（SHA256）
    /// </summary>
    public string Hash { get; set; } = string.Empty;

    /// <summary>
    /// 是否为首个资源
    /// </summary>
    public bool IsFirstResource { get; set; }

    /// <summary>
    /// 变换矩阵 [a, b, c, d, e, f]
    /// </summary>
    public double[]? CTM { get; set; }

    /// <summary>
    /// 序列号
    /// </summary>
    public int Sequence { get; set; }

    /// <summary>
    /// Z轴深度
    /// </summary>
    public int Z { get; set; }

    /// <summary>
    /// 透明度（0-255）
    /// </summary>
    public int Alpha { get; set; } = 255;

    /// <summary>
    /// 替代文本（可访问性）
    /// </summary>
    public string? AltText { get; set; }

    /// <summary>
    /// 获取边界矩形
    /// </summary>
    /// <returns>边界矩形</returns>
    public (double X, double Y, double Width, double Height) GetBounds()
    {
        return (X, Y, Width, Height);
    }

    /// <summary>
    /// 检查是否与指定区域相交
    /// </summary>
    /// <param name="x">区域X坐标</param>
    /// <param name="y">区域Y坐标</param>
    /// <param name="width">区域宽度</param>
    /// <param name="height">区域高度</param>
    /// <returns>是否相交</returns>
    public bool IntersectsWith(double x, double y, double width, double height)
    {
        return X < x + width && X + Width > x &&
               Y < y + height && Y + Height > y;
    }

    /// <summary>
    /// 计算数据哈希值
    /// </summary>
    /// <returns>SHA256哈希值</returns>
    public string ComputeDataHash()
    {
        if (Data == null || Data.Length == 0)
            return string.Empty;

        using var sha256 = SHA256.Create();
        var hashBytes = sha256.ComputeHash(Data);
        return Convert.ToHexString(hashBytes).ToLowerInvariant();
    }

    /// <summary>
    /// 更新哈希值
    /// </summary>
    public void UpdateHash()
    {
        Hash = ComputeDataHash();
    }

    /// <summary>
    /// 获取有效的变换矩阵
    /// </summary>
    /// <returns>变换矩阵，如果CTM为空则返回单位矩阵</returns>
    public double[] GetTransformMatrix()
    {
        return CTM ?? new double[] { 1, 0, 0, 1, 0, 0 };
    }

    /// <summary>
    /// 获取图片文件扩展名
    /// </summary>
    /// <returns>文件扩展名</returns>
    public string GetFileExtension()
    {
        return Format.ToLowerInvariant() switch
        {
            "png" => ".png",
            "jpg" or "jpeg" => ".jpg",
            "gif" => ".gif",
            "bmp" => ".bmp",
            "tiff" => ".tiff",
            "webp" => ".webp",
            _ => ".png"
        };
    }

    /// <summary>
    /// 获取MIME类型
    /// </summary>
    /// <returns>MIME类型</returns>
    public string GetMimeType()
    {
        return Format.ToLowerInvariant() switch
        {
            "png" => "image/png",
            "jpg" or "jpeg" => "image/jpeg",
            "gif" => "image/gif",
            "bmp" => "image/bmp",
            "tiff" => "image/tiff",
            "webp" => "image/webp",
            _ => "image/png"
        };
    }

    /// <summary>
    /// 检查是否为有效图片格式
    /// </summary>
    /// <returns>是否为有效格式</returns>
    public bool IsValidFormat()
    {
        var validFormats = new[] { "png", "jpg", "jpeg", "gif", "bmp", "tiff", "webp" };
        return validFormats.Contains(Format.ToLowerInvariant());
    }

    /// <summary>
    /// 克隆当前对象
    /// </summary>
    /// <returns>克隆的对象</returns>
    public RawImage Clone()
    {
        return new RawImage
        {
            Format = Format,
            X = X,
            Y = Y,
            Width = Width,
            Height = Height,
            Data = Data.ToArray(),
            Page = Page,
            ResourceID = ResourceID,
            Hash = Hash,
            IsFirstResource = IsFirstResource,
            CTM = CTM?.ToArray(),
            Sequence = Sequence,
            Z = Z,
            Alpha = Alpha,
            AltText = AltText
        };
    }

    /// <summary>
    /// 转换为字符串表示
    /// </summary>
    /// <returns>字符串表示</returns>
    public override string ToString()
    {
        return $"RawImage[Page={Page}, ID={ResourceID}, Format={Format}, " +
               $"Pos=({X:F1},{Y:F1}), Size=({Width:F1}x{Height:F1}), " +
               $"DataSize={Data.Length}, Alpha={Alpha}]";
    }
}
