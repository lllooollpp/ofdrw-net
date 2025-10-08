namespace OfdrwNet.Text;

/// <summary>
/// 原始字形运行（低级文本对象）
/// 描述OFD文档中文本的渲染信息，包括字体、位置、变换等
/// </summary>
public class RawGlyphRun
{
    /// <summary>
    /// 字体名称
    /// </summary>
    public string FontName { get; set; } = "SimSun";

    /// <summary>
    /// 字体大小（毫米单位）
    /// </summary>
    public double FontSizeMm { get; set; }

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
    /// 文本内容
    /// </summary>
    public string Text { get; set; } = string.Empty;

    /// <summary>
    /// X轴字符间距偏移数组
    /// </summary>
    public double[]? DeltaX { get; set; }

    /// <summary>
    /// Y轴字符间距偏移数组
    /// </summary>
    public double[]? DeltaY { get; set; }

    /// <summary>
    /// 字形索引数组
    /// </summary>
    public int[]? Glyphs { get; set; }

    /// <summary>
    /// 所在页码
    /// </summary>
    public int Page { get; set; } = 1;

    /// <summary>
    /// 变换矩阵 [a, b, c, d, e, f]
    /// </summary>
    public double[]? CTM { get; set; }

    /// <summary>
    /// 基线Y坐标
    /// </summary>
    public double? BaselineY { get; set; }

    /// <summary>
    /// 字符起始位置数组
    /// </summary>
    public double[]? CharStarts { get; set; }

    /// <summary>
    /// 字符前进距离数组
    /// </summary>
    public double[]? CharAdvances { get; set; }

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
    /// 计算字符数量
    /// </summary>
    /// <returns>字符数量</returns>
    public int GetCharacterCount()
    {
        return Text?.Length ?? 0;
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
    /// 克隆当前对象
    /// </summary>
    /// <returns>克隆的对象</returns>
    public RawGlyphRun Clone()
    {
        return new RawGlyphRun
        {
            FontName = FontName,
            FontSizeMm = FontSizeMm,
            X = X,
            Y = Y,
            Width = Width,
            Height = Height,
            Text = Text,
            DeltaX = DeltaX?.ToArray(),
            DeltaY = DeltaY?.ToArray(),
            Glyphs = Glyphs?.ToArray(),
            Page = Page,
            CTM = CTM?.ToArray(),
            BaselineY = BaselineY,
            CharStarts = CharStarts?.ToArray(),
            CharAdvances = CharAdvances?.ToArray()
        };
    }

    /// <summary>
    /// 转换为字符串表示
    /// </summary>
    /// <returns>字符串表示</returns>
    public override string ToString()
    {
        return $"RawGlyphRun[Page={Page}, Font={FontName}, Size={FontSizeMm:F1}mm, " +
               $"Pos=({X:F1},{Y:F1}), Size=({Width:F1}x{Height:F1}), Text=\"{Text}\"]";
    }
}
