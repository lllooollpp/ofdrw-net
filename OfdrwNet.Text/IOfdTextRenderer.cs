using System.Xml.Linq;

namespace OfdrwNet.Text;

/// <summary>
/// 文本渲染器接口，支持多种渲染后端
/// </summary>
public interface IOfdTextRenderer
{
    /// <summary>
    /// 渲染文本对象
    /// </summary>
    /// <param name="context">渲染上下文</param>
    /// <param name="textObject">文本对象元素</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>渲染任务</returns>
    Task RenderTextObjectAsync(object context, XElement textObject, CancellationToken cancellationToken = default);

    /// <summary>
    /// 获取文本对象的边界
    /// </summary>
    /// <param name="textObject">文本对象元素</param>
    /// <returns>边界矩形</returns>
    System.Drawing.RectangleF GetTextObjectBounds(XElement textObject);

    /// <summary>
    /// 测试点是否在文本对象内
    /// </summary>
    /// <param name="textObject">文本对象元素</param>
    /// <param name="point">测试点</param>
    /// <param name="tolerance">容差</param>
    /// <returns>是否命中</returns>
    bool HitTest(XElement textObject, System.Drawing.PointF point, float tolerance = 0f);
}

/// <summary>
/// 文本渲染上下文信息
/// </summary>
public class TextRenderContext
{
    /// <summary>
    /// 缩放因子
    /// </summary>
    public float ScaleFactor { get; set; } = 1.0f;

    /// <summary>
    /// DPI设置
    /// </summary>
    public float DpiX { get; set; } = 96f;
    public float DpiY { get; set; } = 96f;

    /// <summary>
    /// 是否启用抗锯齿
    /// </summary>
    public bool AntiAlias { get; set; } = true;

    /// <summary>
    /// 字体回退策略
    /// </summary>
    public bool UseFontFallback { get; set; } = true;

    /// <summary>
    /// 文本质量设置
    /// </summary>
    public TextRenderingHint TextRenderingHint { get; set; } = TextRenderingHint.AntiAlias;
}

/// <summary>
/// 文本渲染质量提示
/// </summary>
public enum TextRenderingHint
{
    SystemDefault,
    SingleBitPerPixelGridFit,
    SingleBitPerPixel,
    AntiAliasGridFit,
    AntiAlias,
    ClearTypeGridFit
}

/// <summary>
/// 文本渲染工具类
/// </summary>
public static class TextRenderingUtils
{
    /// <summary>
    /// 解析文本对象的边界框
    /// </summary>
    /// <param name="textObject">文本对象</param>
    /// <returns>边界框</returns>
    public static System.Drawing.RectangleF ParseBoundary(XElement textObject)
    {
        var boundaryAttr = textObject.Attribute("Boundary")?.Value;
        if (string.IsNullOrEmpty(boundaryAttr))
            return System.Drawing.RectangleF.Empty;

        var parts = boundaryAttr.Split(' ', StringSplitOptions.RemoveEmptyEntries);
        if (parts.Length >= 4 &&
            float.TryParse(parts[0], out var x) &&
            float.TryParse(parts[1], out var y) &&
            float.TryParse(parts[2], out var width) &&
            float.TryParse(parts[3], out var height))
        {
            return new System.Drawing.RectangleF(x, y, width, height);
        }
        return System.Drawing.RectangleF.Empty;
    }

    /// <summary>
    /// 获取文本对象的字体大小
    /// </summary>
    /// <param name="textObject">文本对象</param>
    /// <returns>字体大小</returns>
    public static float GetFontSize(XElement textObject)
    {
        var sizeAttr = textObject.Attribute("Size")?.Value;
        return float.TryParse(sizeAttr, out var size) ? size : 12f;
    }

    /// <summary>
    /// 获取文本对象的字体ID
    /// </summary>
    /// <param name="textObject">文本对象</param>
    /// <returns>字体ID</returns>
    public static string? GetFontId(XElement textObject)
    {
        return textObject.Attribute("Font")?.Value;
    }

    /// <summary>
    /// 提取文本对象的所有TextCode内容
    /// </summary>
    /// <param name="textObject">文本对象</param>
    /// <returns>文本内容列表</returns>
    public static List<TextCodeInfo> ExtractTextCodes(XElement textObject)
    {
        var textCodes = new List<TextCodeInfo>();
        var textCodeElements = textObject.Elements("TextCode");

        foreach (var textCode in textCodeElements)
        {
            var x = float.TryParse(textCode.Attribute("X")?.Value, out var xVal) ? xVal : 0f;
            var y = float.TryParse(textCode.Attribute("Y")?.Value, out var yVal) ? yVal : 0f;
            var text = textCode.Value ?? string.Empty;

            if (!string.IsNullOrEmpty(text))
            {
                textCodes.Add(new TextCodeInfo
                {
                    X = x,
                    Y = y,
                    Text = text,
                    DeltaX = ParseFloatArray(textCode.Attribute("DeltaX")?.Value),
                    DeltaY = ParseFloatArray(textCode.Attribute("DeltaY")?.Value)
                });
            }
        }

        return textCodes;
    }

    /// <summary>
    /// 解析浮点数组字符串
    /// </summary>
    /// <param name="str">数组字符串</param>
    /// <returns>浮点数组</returns>
    private static float[]? ParseFloatArray(string? str)
    {
        if (string.IsNullOrEmpty(str))
            return null;

        var parts = str.Split(' ', StringSplitOptions.RemoveEmptyEntries);
        var result = new List<float>();

        foreach (var part in parts)
        {
            if (float.TryParse(part, out var value))
                result.Add(value);
        }

        return result.Count > 0 ? result.ToArray() : null;
    }
}

/// <summary>
/// TextCode 信息
/// </summary>
public class TextCodeInfo
{
    public float X { get; set; }
    public float Y { get; set; }
    public string Text { get; set; } = string.Empty;
    public float[]? DeltaX { get; set; }
    public float[]? DeltaY { get; set; }
}
