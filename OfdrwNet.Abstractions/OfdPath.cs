namespace OfdrwNet.Abstractions;

/// <summary>
/// OFD 文档路径
/// </summary>
public class OfdPath
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
    /// 路径数据（SVG路径格式）
    /// </summary>
    public string PathData { get; set; } = string.Empty;

    /// <summary>
    /// 可选：坐标变换矩阵 CTM (a b c d e f) 已换算到 mm，并已转换到 OFD 坐标系（页面左上为原点，y 向下）
    /// 若为 null 或长度 != 6 表示不输出 CTM
    /// </summary>
    public double[]? CTM { get; set; }

    /// <summary>
    /// 描边颜色 (RGB格式，如 "255 0 0" 表示红色)
    /// </summary>
    public string? StrokeColor { get; set; }

    /// <summary>
    /// 填充颜色 (RGB格式，如 "0 255 0" 表示绿色)
    /// </summary>
    public string? FillColor { get; set; }

    /// <summary>
    /// 虚线模式（单位：mm）。数组元素依次为实线段长度、间隔长度等。
    /// </summary>
    public double[]? DashPattern { get; set; }

    /// <summary>
    /// 线宽（mm）
    /// </summary>
    public double? LineWidth { get; set; }

    /// <summary>
    /// 是否填充
    /// </summary>
    public bool? Fill { get; set; }

    /// <summary>
    /// 是否描边
    /// </summary>
    public bool? Stroke { get; set; }
}
