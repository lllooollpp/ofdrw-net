using OfdrwNet.Core;
using System.Text;
using System.Xml.Linq;

namespace OfdrwNet.Text;

/// <summary>
/// 文本解析器，负责解析OFD文档中的文本对象
/// </summary>
public class TextParser : ITextParser
{
    /// <summary>
    /// 解析文本对象
    /// </summary>
    /// <param name="textObject">文本对象元素</param>
    /// <returns>文本块</returns>
    public ITextBlock? ParseTextObject(XElement textObject)
    {
        // 解析边界框
        var boundaryAttr = textObject.Attribute("Boundary")?.Value;
        if (string.IsNullOrEmpty(boundaryAttr))
            return null;

        var boundary = ParseBoundary(boundaryAttr);
        if (boundary.IsEmpty)
            return null;

        // 解析字体大小
        var fontSize = float.Parse(textObject.Attribute("Size")?.Value ?? "12");

        // 提取所有文本内容
        var textBuilder = new StringBuilder();
        var textCodeElements = textObject.Elements("TextCode");

        foreach (var textCode in textCodeElements)
        {
            var text = textCode.Value;
            if (!string.IsNullOrEmpty(text))
            {
                textBuilder.Append(text);
            }
        }

        var content = textBuilder.ToString();
        if (string.IsNullOrWhiteSpace(content))
            return null;

        return new TextBlock
        {
            X = boundary.Left,
            Y = boundary.Top,
            Width = boundary.Width,
            Height = boundary.Height,
            FontSize = fontSize,
            Content = content.Trim()
        };
    }

    /// <summary>
    /// 解析边界框字符串
    /// </summary>
    /// <param name="boundaryStr">边界框字符串</param>
    /// <returns>边界框</returns>
    public System.Drawing.RectangleF ParseBoundary(string boundaryStr)
    {
        var parts = boundaryStr.Split(' ', StringSplitOptions.RemoveEmptyEntries);
        if (parts.Length >= 4)
        {
            var x = float.Parse(parts[0]);
            var y = float.Parse(parts[1]);
            var width = float.Parse(parts[2]);
            var height = float.Parse(parts[3]);
            return new System.Drawing.RectangleF(x, y, width, height);
        }
        return System.Drawing.RectangleF.Empty;
    }
}
