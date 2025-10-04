using OfdrwNet.Core;

namespace OfdrwNet.Text;

/// <summary>
/// 文本块，表示一段连续的文本及其位置信息
/// </summary>
public class TextBlock : ITextBlock
{
    public float X { get; set; }
    public float Y { get; set; }
    public float Width { get; set; }
    public float Height { get; set; }
    public float FontSize { get; set; }
    public string Content { get; set; } = string.Empty;
}
