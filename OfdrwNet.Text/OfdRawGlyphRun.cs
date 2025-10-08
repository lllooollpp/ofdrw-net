namespace OfdrwNet.Text;

/// <summary>
/// OFD 原始字形运行对象（统一的低级文本对象）
/// 对应原 RawGlyphRun，提供字形级别的文本处理能力
/// </summary>
public class OfdRawGlyphRun
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
    /// 文本内容
    /// </summary>
    public string Text { get; set; } = string.Empty;

    /// <summary>
    /// 字符间距 X 偏移数组
    /// </summary>
    public double[]? DeltaX { get; set; }

    /// <summary>
    /// 字符间距 Y 偏移数组
    /// </summary>
    public double[]? DeltaY { get; set; }

    /// <summary>
    /// 字形索引数组
    /// </summary>
    public int[]? Glyphs { get; set; }

    /// <summary>
    /// 页码（从1开始）
    /// </summary>
    public int Page { get; set; } = 1;

    /// <summary>
    /// 坐标变换矩阵 (a, b, c, d, e, f)
    /// </summary>
    public double[]? CTM { get; set; }

    /// <summary>
    /// 基线 Y 坐标
    /// </summary>
    public double? BaselineY { get; set; }

    /// <summary>
    /// 字符起始位置数组
    /// </summary>
    public double[]? CharStarts { get; set; }

    /// <summary>
    /// 字符前进宽度数组
    /// </summary>
    public double[]? CharAdvances { get; set; }

    /// <summary>
    /// 从原 RawGlyphRun 转换为 OfdRawGlyphRun
    /// </summary>
    /// <param name="source">原始字形运行对象</param>
    /// <returns>OFD 字形运行对象</returns>
    public static OfdRawGlyphRun FromRawGlyphRun(object source)
    {
        // 使用反射获取原对象属性（避免直接依赖）
        var sourceType = source.GetType();

        return new OfdRawGlyphRun
        {
            FontName = GetProperty<string>(source, sourceType, "FontName") ?? "SimSun",
            FontSizeMm = GetProperty<double>(source, sourceType, "FontSizeMm"),
            X = GetProperty<double>(source, sourceType, "X"),
            Y = GetProperty<double>(source, sourceType, "Y"),
            Width = GetProperty<double>(source, sourceType, "Width"),
            Height = GetProperty<double>(source, sourceType, "Height"),
            Text = GetProperty<string>(source, sourceType, "Text") ?? string.Empty,
            DeltaX = GetProperty<double[]?>(source, sourceType, "DeltaX"),
            DeltaY = GetProperty<double[]?>(source, sourceType, "DeltaY"),
            Glyphs = GetProperty<int[]?>(source, sourceType, "Glyphs"),
            Page = GetProperty<int>(source, sourceType, "Page"),
            CTM = GetProperty<double[]?>(source, sourceType, "CTM"),
            BaselineY = GetProperty<double?>(source, sourceType, "BaselineY"),
            CharStarts = GetProperty<double[]?>(source, sourceType, "CharStarts"),
            CharAdvances = GetProperty<double[]?>(source, sourceType, "CharAdvances")
        };
    }

    private static T? GetProperty<T>(object source, Type sourceType, string propertyName)
    {
        var property = sourceType.GetProperty(propertyName);
        if (property != null && property.CanRead)
        {
            var value = property.GetValue(source);
            if (value is T result)
                return result;
        }
        return default(T);
    }
}
