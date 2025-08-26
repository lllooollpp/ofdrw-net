namespace OfdrwNet.Layout.Element;

/// <summary>
/// 段落元素
/// 对应 Java 版本的 org.ofdrw.layout.element.Paragraph
/// 用于文本内容的显示和布局
/// </summary>
public class Paragraph : Div
{
    /// <summary>
    /// 文本内容
    /// </summary>
    public string Text { get; set; } = string.Empty;

    /// <summary>
    /// 字体大小 (毫米)
    /// </summary>
    public double FontSize { get; set; } = 3.0;

    /// <summary>
    /// 字体名称
    /// </summary>
    public string FontName { get; set; } = "宋体";

    /// <summary>
    /// 文字颜色
    /// </summary>
    public string Color { get; set; } = "#000000";

    /// <summary>
    /// 文本对齐方式
    /// </summary>
    public TextAlign TextAlign { get; set; } = TextAlign.Left;

    /// <summary>
    /// 行高倍数
    /// </summary>
    public double LineHeight { get; set; } = 1.2;

    /// <summary>
    /// 字符间距
    /// </summary>
    public double LetterSpacing { get; set; } = 0;

    /// <summary>
    /// 元素类型标识
    /// </summary>
    public override string ElementType => "Paragraph";

    /// <summary>
    /// 默认构造函数
    /// </summary>
    public Paragraph() : base() { }

    /// <summary>
    /// 使用文本构造段落
    /// </summary>
    /// <param name="text">文本内容</param>
    public Paragraph(string text) : base()
    {
        Text = text ?? string.Empty;
    }

    /// <summary>
    /// 使用文本和字体大小构造段落
    /// </summary>
    /// <param name="text">文本内容</param>
    /// <param name="fontSize">字体大小</param>
    public Paragraph(string text, double fontSize) : base()
    {
        Text = text ?? string.Empty;
        FontSize = fontSize;
    }

    /// <summary>
    /// 使用位置、尺寸和文本构造段落
    /// </summary>
    /// <param name="x">X坐标</param>
    /// <param name="y">Y坐标</param>
    /// <param name="width">宽度</param>
    /// <param name="height">高度</param>
    /// <param name="text">文本内容</param>
    public Paragraph(double x, double y, double width, double height, string text) 
        : base(x, y, width, height)
    {
        Text = text ?? string.Empty;
    }

    /// <summary>
    /// 使用位置、尺寸、文本和字体大小构造段落
    /// </summary>
    /// <param name="x">X坐标</param>
    /// <param name="y">Y坐标</param>
    /// <param name="width">宽度</param>
    /// <param name="height">高度</param>
    /// <param name="text">文本内容</param>
    /// <param name="fontSize">字体大小</param>
    public Paragraph(double x, double y, double width, double height, string text, double fontSize) 
        : base(x, y, width, height)
    {
        Text = text ?? string.Empty;
        FontSize = fontSize;
    }

    /// <summary>
    /// 设置文本内容
    /// </summary>
    /// <param name="text">文本内容</param>
    /// <returns>this</returns>
    public Paragraph SetText(string text)
    {
        Text = text ?? string.Empty;
        return this;
    }

    /// <summary>
    /// 设置字体大小
    /// </summary>
    /// <param name="fontSize">字体大小</param>
    /// <returns>this</returns>
    public Paragraph SetFontSize(double fontSize)
    {
        FontSize = fontSize;
        return this;
    }

    /// <summary>
    /// 设置字体名称
    /// </summary>
    /// <param name="fontName">字体名称</param>
    /// <returns>this</returns>
    public Paragraph SetFont(string fontName)
    {
        FontName = fontName ?? "宋体";
        return this;
    }

    /// <summary>
    /// 设置文字颜色
    /// </summary>
    /// <param name="color">颜色值</param>
    /// <returns>this</returns>
    public Paragraph SetColor(string color)
    {
        Color = color ?? "#000000";
        return this;
    }

    /// <summary>
    /// 设置文字颜色 (RGB)
    /// </summary>
    /// <param name="r">红色分量 (0-255)</param>
    /// <param name="g">绿色分量 (0-255)</param>
    /// <param name="b">蓝色分量 (0-255)</param>
    /// <returns>this</returns>
    public Paragraph SetColor(int r, int g, int b)
    {
        Color = $"#{r:X2}{g:X2}{b:X2}";
        return this;
    }

    /// <summary>
    /// 设置文本对齐方式
    /// </summary>
    /// <param name="align">对齐方式</param>
    /// <returns>this</returns>
    public Paragraph SetTextAlign(TextAlign align)
    {
        TextAlign = align;
        return this;
    }

    /// <summary>
    /// 设置行高
    /// </summary>
    /// <param name="lineHeight">行高倍数</param>
    /// <returns>this</returns>
    public Paragraph SetLineHeight(double lineHeight)
    {
        LineHeight = lineHeight;
        return this;
    }

    /// <summary>
    /// 设置字符间距
    /// </summary>
    /// <param name="spacing">间距值</param>
    /// <returns>this</returns>
    public Paragraph SetLetterSpacing(double spacing)
    {
        LetterSpacing = spacing;
        return this;
    }

    /// <summary>
    /// 预处理方法，计算文本尺寸等
    /// </summary>
    /// <param name="width">可用宽度</param>
    public override void DoPrepare(double width)
    {
        base.DoPrepare(width);
        
        // 如果没有设置高度，根据字体大小和行高计算
        if (Height == null && !string.IsNullOrEmpty(Text))
        {
            // 简单计算：根据字体大小和行数估算高度
            var lines = EstimateLineCount(Text, width);
            Height = FontSize * LineHeight * lines;
        }
    }

    /// <summary>
    /// 估算文本行数
    /// </summary>
    /// <param name="text">文本内容</param>
    /// <param name="availableWidth">可用宽度</param>
    /// <returns>估算的行数</returns>
    private int EstimateLineCount(string text, double availableWidth)
    {
        if (string.IsNullOrEmpty(text) || availableWidth <= 0)
        {
            return 1;
        }

        // 简单估算：假设每个字符宽度约为字体大小的0.8倍
        var charWidth = FontSize * 0.8;
        var charsPerLine = (int)(availableWidth / charWidth);
        
        if (charsPerLine <= 0)
        {
            return text.Length; // 每行一个字符
        }

        return Math.Max(1, (int)Math.Ceiling((double)text.Length / charsPerLine));
    }

    /// <summary>
    /// 转换为字符串表示
    /// </summary>
    /// <returns>字符串表示</returns>
    public override string ToString()
    {
        var preview = Text.Length > 20 ? Text.Substring(0, 20) + "..." : Text;
        return $"Paragraph(\"{preview}\", {X}, {Y}, {Width}, {Height}, Font={FontName} {FontSize}mm)";
    }
}

/// <summary>
/// 文本对齐方式枚举
/// </summary>
public enum TextAlign
{
    /// <summary>
    /// 左对齐
    /// </summary>
    Left,

    /// <summary>
    /// 居中对齐
    /// </summary>
    Center,

    /// <summary>
    /// 右对齐
    /// </summary>
    Right,

    /// <summary>
    /// 两端对齐
    /// </summary>
    Justify
}