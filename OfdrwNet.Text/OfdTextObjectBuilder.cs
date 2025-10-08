using OfdrwNet.Core;
using System.Xml.Linq;

namespace OfdrwNet.Text;

/// <summary>
/// OFD 文本对象构建器，负责从原始字形运行构建 OFD 文本对象
/// 整合原 PageContentWriter 中的文本对象创建逻辑
/// </summary>
public class OfdTextObjectBuilder
{
    /// <summary>
    /// 从字形运行构建文本对象
    /// </summary>
    /// <param name="glyphRun">字形运行对象</param>
    /// <param name="fontMap">字体映射</param>
    /// <param name="nextId">下一个ID生成器</param>
    /// <returns>构建的文本对象元素</returns>
    public XElement? CreateTextObject(OfdRawGlyphRun glyphRun, IDictionary<string, object> fontMap, Func<int> nextId)
    {
        // 验证输入
        if (string.IsNullOrEmpty(glyphRun.Text) || glyphRun.FontSizeMm <= 0)
            return null;

        // 查找字体
        if (!fontMap.TryGetValue(glyphRun.FontName, out var fontObj))
            return null;

        // 获取字体ID（假设字体对象有ID属性）
        var fontId = GetFontId(fontObj);
        if (fontId == null)
            return null;

    // 计算边界
    var textLen = glyphRun.Text.Length;
    var width = glyphRun.Width > 0 ? glyphRun.Width : textLen * glyphRun.FontSizeMm * 0.6;

    // 行高因子
    const double lineHeightFactor = 1.2;
    var totalTextHeight = glyphRun.Height > 0 ? glyphRun.Height : glyphRun.FontSizeMm * lineHeightFactor;

    // 基线与边界位置关系
    var baselineAbsolute = glyphRun.BaselineY ?? glyphRun.Y;
    var boundaryTop = baselineAbsolute - (totalTextHeight * 0.8);
    var boundaryHeight = totalTextHeight;

        // 创建文本对象元素
        var textObject = new XElement("TextObject");
        textObject.SetAttributeValue("ID", nextId().ToString());
        textObject.SetAttributeValue("Font", fontId);
        textObject.SetAttributeValue("Size", glyphRun.FontSizeMm.ToString("F3"));
        textObject.SetAttributeValue("Boundary", $"{glyphRun.X:F3} {boundaryTop:F3} {width:F3} {boundaryHeight:F3}");
        textObject.SetAttributeValue("Stroke", "false");
        textObject.SetAttributeValue("Fill", "true");

        // 设置填充颜色为黑色
        var fillColorElement = new XElement("FillColor");
        fillColorElement.SetAttributeValue("ColorSpace", "1"); // RGB
        fillColorElement.SetAttributeValue("Value", "0 0 0"); // 黑色
        textObject.Add(fillColorElement);

        // 设置坐标变换矩阵（如果有）
        if (glyphRun.CTM != null && glyphRun.CTM.Length >= 6)
        {
            var ctmStr = string.Join(" ", glyphRun.CTM.Select(v => v.ToString("F6")));
            textObject.SetAttributeValue("CTM", ctmStr);
        }

        // 创建 TextCode 元素
        var textCodeX = (glyphRun.CharStarts?.Length > 0) ? glyphRun.CharStarts[0] - glyphRun.X : 0.0;
        var baselineRelative = baselineAbsolute - boundaryTop;
        if (baselineRelative < 0)
        {
            baselineRelative = 0;
        }
        else if (baselineRelative > boundaryHeight)
        {
            baselineRelative = boundaryHeight;
        }

        var textCode = new XElement("TextCode");
        textCode.SetAttributeValue("X", textCodeX.ToString("F3"));
        textCode.SetAttributeValue("Y", baselineRelative.ToString("F3"));
        textCode.Value = glyphRun.Text;

        // 设置字符间距（如果有）
        if (glyphRun.DeltaX?.Length > 0)
        {
            var deltaXStr = string.Join(" ", glyphRun.DeltaX.Select(d => d.ToString("F3")));
            textCode.SetAttributeValue("DeltaX", deltaXStr);
        }

        if (glyphRun.DeltaY?.Length > 0)
        {
            var deltaYStr = string.Join(" ", glyphRun.DeltaY.Select(d => d.ToString("F3")));
            textCode.SetAttributeValue("DeltaY", deltaYStr);
        }

        textObject.Add(textCode);

        return textObject;
    }

    /// <summary>
    /// 解析边界框字符串
    /// </summary>
    /// <param name="boundaryStr">边界框字符串</param>
    /// <returns>边界框</returns>
    public System.Drawing.RectangleF ParseBoundary(string? boundaryStr)
    {
        if (string.IsNullOrEmpty(boundaryStr))
            return System.Drawing.RectangleF.Empty;

        var parts = boundaryStr.Split(' ', StringSplitOptions.RemoveEmptyEntries);
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
    /// 从字体对象获取字体ID
    /// </summary>
    /// <param name="fontObj">字体对象</param>
    /// <returns>字体ID</returns>
    private string? GetFontId(object fontObj)
    {
        // 使用反射获取ID属性
        var fontType = fontObj.GetType();
        var idProperty = fontType.GetProperty("ID");
        if (idProperty != null && idProperty.CanRead)
        {
            var idValue = idProperty.GetValue(fontObj);
            return idValue?.ToString();
        }
        return null;
    }
}
