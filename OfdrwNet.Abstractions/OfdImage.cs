namespace OfdrwNet.Abstractions;

/// <summary>
/// OFD 文档图像
/// </summary>
public class OfdImage
{
    /// <summary>
    /// 页码
    /// </summary>
    public int Page { get; set; }

    /// <summary>
    /// X 坐标（mm）
    /// </summary>
    public double X { get; set; }

    /// <summary>
    /// Y 坐标（mm, 以页面左上为原点向下为正）
    /// </summary>
    public double Y { get; set; }

    /// <summary>
    /// 宽度（mm）
    /// </summary>
    public double Width { get; set; }

    /// <summary>
    /// 高度（mm）
    /// </summary>
    public double Height { get; set; }

    /// <summary>
    /// 图片数据
    /// </summary>
    public byte[] ImageData { get; set; } = System.Array.Empty<byte>();

    /// <summary>
    /// 图片格式（PNG/JPG/...）
    /// </summary>
    public string Format { get; set; } = "PNG";

    /// <summary>
    /// 可选：坐标变换矩阵 CTM (a b c d e f) 已换算到 mm，并已转换到 OFD 坐标系（页面左上为原点，y 向下）
    /// 若为 null 或长度 != 6 表示不输出 CTM
    /// </summary>
    public double[]? CTM { get; set; }
}
