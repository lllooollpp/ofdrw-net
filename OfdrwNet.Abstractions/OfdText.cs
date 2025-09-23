namespace OfdrwNet.Abstractions;

/// <summary>
/// OFD 文档文本
/// </summary>
public class OfdText
{
    /// <summary>
    /// 页码
    /// </summary>
    public int Page { get; set; }

    /// <summary>
    /// 文本内容
    /// </summary>
    public string Text { get; set; } = string.Empty;

    /// <summary>
    /// X 坐标（mm）
    /// </summary>
    public double X { get; set; }

    /// <summary>
    /// Y 坐标（mm）
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
    /// 字体名称
    /// </summary>
    public string FontFamily { get; set; } = "DefaultFont";

    /// <summary>
    /// 字体大小（mm）
    /// </summary>
    public double FontSize { get; set; }

    /// <summary>
    /// 字形偏移量（mm）
    /// </summary>
    public float[]? DeltaX { get; set; }

    /// <summary>
    /// 可选：坐标变换矩阵 CTM (a b c d e f) 已换算到 mm，并已转换到 OFD 坐标系。
    /// 如果不为空且长度为6，将在 TextObject 上输出 CTM 属性。
    /// </summary>
    public double[]? CTM { get; set; }
}
