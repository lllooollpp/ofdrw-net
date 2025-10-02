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
    /// 文本框顶部 Y（mm），优先使用 PDF 提取的实际位置。
    /// </summary>
    public double? TopY { get; set; }

    /// <summary>
    /// 文本框底部 Y（mm），优先使用 PDF 提取的实际位置。
    /// </summary>
    public double? BottomY { get; set; }

    /// <summary>
    /// 字符起始 X 坐标（mm），与 Text 中字符一一对应（可选）。
    /// </summary>
    public double[]? CharStarts { get; set; }

    /// <summary>
    /// 字符宽度/进宽（mm），与 Text 中字符一一对应（可选）。
    /// </summary>
    public double[]? CharAdvances { get; set; }

    /// <summary>
    /// 字符基线 Y 坐标（mm），用于精确行对齐（可选）。
    /// </summary>
    public double? BaselineY { get; set; }

    /// <summary>
    /// 字形偏移量（mm）
    /// </summary>
    public float[]? DeltaX { get; set; }

    /// <summary>
    /// 可选：坐标变换矩阵 CTM (a b c d e f) 已换算到 mm，并已转换到 OFD 坐标系。
    /// 如果不为空且长度为6，将在 TextObject 上输出 CTM 属性。
    /// </summary>
    public double[]? CTM { get; set; }

    /// <summary>
    /// 平均字符进宽（mm），用于后续聚合 gap 判定（可选）。
    /// </summary>
    public double? AvgAdvance { get; set; }

    /// <summary>
    /// 空格字符参考宽度（mm）（dynamic space width），用于合成空格与分词（可选）。
    /// </summary>
    public double? SpaceAdvance { get; set; }

    /// <summary>
    /// DeltaX 的语义模式：Step=相邻差；Cumulative=相对于首字符累计。保留 null 表示未知，默认为 Step。
    /// </summary>
    public string? DeltaXMode { get; set; }

    /// <summary>
    /// 字形编码列表（用于 CGTransform/Glyphs），可选。
    /// </summary>
    public int[]? Glyphs { get; set; }
}
