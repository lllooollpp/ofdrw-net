using OfdrwNet.Core;
using System.Text;
using System.Xml.Linq;

namespace OfdrwNet.Text;

/// <summary>
/// 增强的文本解析器，支持更复杂的文本对象解析
/// </summary>
public class EnhancedTextParser : ITextParser
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

        // 解析字体ID
        var fontId = textObject.Attribute("Font")?.Value;

        // 提取所有文本内容（支持多个 TextCode）
        var textBuilder = new StringBuilder();
        var textCodeElements = textObject.Elements("TextCode");
        var textCodes = new List<TextCodeInfo>();

        foreach (var textCode in textCodeElements)
        {
            var x = float.TryParse(textCode.Attribute("X")?.Value, out var xVal) ? xVal : 0f;
            var y = float.TryParse(textCode.Attribute("Y")?.Value, out var yVal) ? yVal : 0f;
            var text = textCode.Value ?? string.Empty;

            if (!string.IsNullOrEmpty(text))
            {
                textBuilder.Append(text);
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

        var content = textBuilder.ToString();
        if (string.IsNullOrWhiteSpace(content))
            return null;

        return new EnhancedTextBlock
        {
            X = boundary.Left,
            Y = boundary.Top,
            Width = boundary.Width,
            Height = boundary.Height,
            FontSize = fontSize,
            FontId = fontId,
            Content = content.Trim(),
            TextCodes = textCodes,
            OriginalElement = textObject
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
    /// 解析浮点数组字符串
    /// </summary>
    /// <param name="str">数组字符串</param>
    /// <returns>浮点数组</returns>
    private float[]? ParseFloatArray(string? str)
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
/// 增强的文本块，包含更多信息
/// </summary>
public class EnhancedTextBlock : ITextBlock
{
    public float X { get; set; }
    public float Y { get; set; }
    public float Width { get; set; }
    public float Height { get; set; }
    public float FontSize { get; set; }
    public string Content { get; set; } = string.Empty;

    /// <summary>
    /// 字体ID
    /// </summary>
    public string? FontId { get; set; }

    /// <summary>
    /// TextCode 信息列表
    /// </summary>
    public List<TextCodeInfo> TextCodes { get; set; } = new();

    /// <summary>
    /// 原始XML元素
    /// </summary>
    public XElement? OriginalElement { get; set; }

    /// <summary>
    /// 文本块的右边界
    /// </summary>
    public float Right => X + Width;

    /// <summary>
    /// 文本块的底边界
    /// </summary>
    public float Bottom => Y + Height;

    /// <summary>
    /// 中心点
    /// </summary>
    public System.Drawing.PointF Center => new(X + Width / 2, Y + Height / 2);

    /// <summary>
    /// 是否与另一个文本块在同一行
    /// </summary>
    /// <param name="other">另一个文本块</param>
    /// <param name="tolerance">容差</param>
    /// <returns>是否在同一行</returns>
    public bool IsOnSameLineWith(EnhancedTextBlock other, float tolerance = 2f)
    {
        return Math.Abs(this.Y - other.Y) <= tolerance;
    }

    /// <summary>
    /// 是否可以与另一个文本块合并（在同一行且相邻）
    /// </summary>
    /// <param name="other">另一个文本块</param>
    /// <param name="lineTolerance">行容差</param>
    /// <param name="spaceTolerance">间距容差</param>
    /// <returns>是否可以合并</returns>
    public bool CanMergeWith(EnhancedTextBlock other, float lineTolerance = 2f, float spaceTolerance = 10f)
    {
        if (!IsOnSameLineWith(other, lineTolerance))
            return false;

        // 检查水平距离
        var distance = Math.Abs(this.Right - other.X);
        return distance <= spaceTolerance;
    }
}

/// <summary>
/// 智能文本合并器，支持更好的文本合并策略
/// </summary>
public class SmartTextMerger : ITextMerger
{
    /// <summary>
    /// 合并文本块为连续文本
    /// </summary>
    /// <param name="textBlocks">文本块列表</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>合并后的文本</returns>
    public async Task<string> MergeTextBlocksAsync(IEnumerable<ITextBlock> textBlocks, CancellationToken cancellationToken = default)
    {
        var enhancedBlocks = textBlocks
            .OfType<EnhancedTextBlock>()
            .Where(b => !string.IsNullOrWhiteSpace(b.Content))
            .ToList();

        if (enhancedBlocks.Count == 0)
            return string.Empty;

        // 按位置排序
        var sortedBlocks = enhancedBlocks
            .OrderBy(b => b.Y)
            .ThenBy(b => b.X)
            .ToList();

        var lines = new List<List<EnhancedTextBlock>>();
        var currentLine = new List<EnhancedTextBlock>();
        var currentY = sortedBlocks[0].Y;
        var lineHeight = sortedBlocks[0].FontSize * 1.5f; // 估算行高

        // 将文本块分组到行
        foreach (var block in sortedBlocks)
        {
            cancellationToken.ThrowIfCancellationRequested();

            // 如果Y坐标变化超过行高的一半，认为是新行
            if (Math.Abs(block.Y - currentY) > lineHeight / 2)
            {
                if (currentLine.Count > 0)
                {
                    lines.Add(currentLine);
                    currentLine = new List<EnhancedTextBlock>();
                }
                currentY = block.Y;
                lineHeight = block.FontSize * 1.5f; // 更新行高
            }

            currentLine.Add(block);
        }

        // 添加最后一行
        if (currentLine.Count > 0)
        {
            lines.Add(currentLine);
        }

        // 合并每一行的文本
        var textBuilder = new StringBuilder();
        for (int i = 0; i < lines.Count; i++)
        {
            cancellationToken.ThrowIfCancellationRequested();

            var line = lines[i];
            var lineText = MergeLine(line);

            if (!string.IsNullOrWhiteSpace(lineText))
            {
                textBuilder.AppendLine(lineText);
            }
        }

        await Task.CompletedTask;
        return textBuilder.ToString().TrimEnd();
    }

    /// <summary>
    /// 合并单行的文本块
    /// </summary>
    /// <param name="lineBlocks">行内的文本块</param>
    /// <returns>合并后的行文本</returns>
    private string MergeLine(List<EnhancedTextBlock> lineBlocks)
    {
        if (lineBlocks.Count == 0)
            return string.Empty;

        if (lineBlocks.Count == 1)
            return lineBlocks[0].Content.Trim();

        // 按X坐标排序
        var sortedBlocks = lineBlocks.OrderBy(b => b.X).ToList();
        var lineText = new StringBuilder();

        for (int i = 0; i < sortedBlocks.Count; i++)
        {
            var block = sortedBlocks[i];
            var content = block.Content.Trim();

            if (string.IsNullOrEmpty(content))
                continue;

            // 如果不是第一个块，检查是否需要添加空格
            if (i > 0)
            {
                var prevBlock = sortedBlocks[i - 1];
                var gap = block.X - prevBlock.Right;
                var avgCharWidth = prevBlock.FontSize * 0.6f; // 估算字符宽度

                // 如果间距超过一个字符宽度，添加空格
                if (gap > avgCharWidth)
                {
                    var spaceCount = Math.Min((int)(gap / avgCharWidth), 5); // 最多5个空格
                    lineText.Append(new string(' ', spaceCount));
                }
            }

            lineText.Append(content);
        }

        return lineText.ToString();
    }
}
