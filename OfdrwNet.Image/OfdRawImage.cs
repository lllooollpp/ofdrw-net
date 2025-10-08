namespace OfdrwNet.Image;

/// <summary>
/// OFD 原始图片资源(页面对象引用)
/// </summary>
public class OfdRawImage
{
    /// <summary>
    /// 图像格式
    /// </summary>
    public string Format { get; set; } = "PNG"; // PNG/JPG/GIF/BMP/TIFF/WEBP

    /// <summary>
    /// X 坐标
    /// </summary>
    public double X { get; set; }

    /// <summary>
    /// Y 坐标
    /// </summary>
    public double Y { get; set; }

    /// <summary>
    /// 宽度
    /// </summary>
    public double Width { get; set; }

    /// <summary>
    /// 高度
    /// </summary>
    public double Height { get; set; }

    /// <summary>
    /// 图像数据
    /// </summary>
    public byte[] Data { get; set; } = Array.Empty<byte>();

    /// <summary>
    /// 页码
    /// </summary>
    public int Page { get; set; } = 1;

    /// <summary>
    /// 资源ID
    /// </summary>
    public int ResourceID { get; set; }

    /// <summary>
    /// 哈希值
    /// </summary>
    public string Hash { get; set; } = string.Empty; // SHA256

    /// <summary>
    /// 是否为首次资源
    /// </summary>
    public bool IsFirstResource { get; set; }

    /// <summary>
    /// 变换矩阵
    /// </summary>
    public double[]? CTM { get; set; }

    /// <summary>
    /// 序列号
    /// </summary>
    public int Sequence { get; set; }

    /// <summary>
    /// Z 轴层级
    /// </summary>
    public int Z { get; set; }

    /// <summary>
    /// 透明度
    /// </summary>
    public int Alpha { get; set; } = 255;

    /// <summary>
    /// 替代文本
    /// </summary>
    public string? AltText { get; set; }
}
