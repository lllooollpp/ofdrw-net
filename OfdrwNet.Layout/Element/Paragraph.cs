using System.Collections.Generic;
using OfdrwNet.Layout.Element.Canvas;

namespace OfdrwNet.Layout.Element;

/// <summary>
/// 段落
/// 
/// 对应 Java 版本的 org.ofdrw.layout.element.Paragraph
/// </summary>
public class Paragraph : Div<Paragraph>
{
    /// <summary>
    /// 首行缩进字符数
    /// null 标识没有缩进
    /// </summary>
    public int? FirstLineIndent { get; set; }

    /// <summary>
    /// 首行缩进数值，单位：mm
    /// </summary>
    public double? FirstLineIndentWidth { get; set; }

    /// <summary>
    /// 行间距
    /// </summary>
    public double LineSpace { get; set; } = 2d;

    /// <summary>
    /// 默认字体
    /// </summary>
    public object? DefaultFont { get; set; }

    /// <summary>
    /// 默认字号
    /// </summary>
    public double? DefaultFontSize { get; set; }

    /// <summary>
    /// 文字内容
    /// </summary>
    public List<Span> Contents { get; }

    /// <summary>
    /// 元素内行缓存
    /// </summary>
    public List<TxtLineBlock> Lines { get; }

    /// <summary>
    /// 字体在段落内的浮动方向，默认为：左浮动
    /// </summary>
    public TextAlign TextAlign { get; set; } = TextAlign.Start;

    /// <summary>
    /// 创建一个段落对象
    /// 注意如果不主动对 Paragraph 设置宽度，那么Paragraph
    /// 会独占整个段，并且与段具有相同宽度，也就是页面宽度
    /// </summary>
    public Paragraph()
    {
        Clear = Clear.Both;
        Contents = new List<Span>();
        Lines = new List<TxtLineBlock>();
    }

    /// <summary>
    /// 创建一个固定大小段落对象
    /// </summary>
    /// <param name="width">内容宽度</param>
    /// <param name="height">内容高度</param>
    public Paragraph(double width, double height) : this()
    {
        SetWidth(width).SetHeight(height);
    }

    /// <summary>
    /// 创建绝对定位段落对象
    /// </summary>
    /// <param name="x">固定布局的盒式模型左上角X坐标</param>
    /// <param name="y">固定布局的盒式模型左上角Y坐标</param>
    /// <param name="width">宽度</param>
    /// <param name="height">高度</param>
    public Paragraph(double x, double y, double width, double height) : base(x, y, width, height)
    {
        Contents = new List<Span>();
        Lines = new List<TxtLineBlock>();
    }

    /// <summary>
    /// 创建绝对定位段落对象
    /// </summary>
    /// <param name="x">固定布局的盒式模型左上角X坐标</param>
    /// <param name="y">固定布局的盒式模型左上角Y坐标</param>
    /// <param name="width">宽度</param>
    /// <param name="height">高度</param>
    /// <param name="text">文本内容</param>
    /// <param name="fontSize">字号</param>
    public Paragraph(double x, double y, double width, double height, string text, double fontSize) 
        : base(x, y, width, height)
    {
        if (text == null)
            throw new ArgumentException("文字内容为null");

        Contents = new List<Span>();
        Lines = new List<TxtLineBlock>();
        SetFontSize(fontSize);
        Add(text);
    }

    /// <summary>
    /// 新建一个段落对象
    /// </summary>
    /// <param name="text">文字内容</param>
    public Paragraph(string text) : this()
    {
        if (text == null)
            throw new ArgumentException("文字内容为null");
        Add(text);
    }

    /// <summary>
    /// 新建一个段落对象，并指定文字大小
    /// </summary>
    /// <param name="text">文字内容</param>
    /// <param name="defaultFontSize">默认字体大小</param>
    public Paragraph(string text, double defaultFontSize) : this()
    {
        if (text == null)
            throw new ArgumentException("文字内容为null");
        SetFontSize(defaultFontSize);
        Add(text);
    }

    /// <summary>
    /// 新建一个段落对象，并指定文字大小和字体
    /// </summary>
    /// <param name="text">文字内容</param>
    /// <param name="defaultFontSize">默认字体大小</param>
    /// <param name="defaultFont">默认字体</param>
    public Paragraph(string text, double defaultFontSize, object defaultFont) : this()
    {
        if (text == null)
            throw new ArgumentException("文字内容为null");
        SetFontSize(defaultFontSize);
        SetDefaultFont(defaultFont);
        Add(text);
    }

    /// <summary>
    /// 新建一个段落对象，并指定字体
    /// </summary>
    /// <param name="text">文字内容</param>
    /// <param name="defaultFont">默认字体</param>
    public Paragraph(string text, object defaultFont) : this()
    {
        if (text == null)
            throw new ArgumentException("文字内容为null");
        SetDefaultFont(defaultFont);
        Add(text);
    }

    /// <summary>
    /// 增加段落中的文字
    /// 文字样式使用span默认字体样式
    /// </summary>
    /// <param name="text">文本内容</param>
    /// <returns>this</returns>
    public Paragraph Add(string text)
    {
        if (text == null)
            return this;

        var newSpan = new Span(text);
        // 设置默认样式
        if (DefaultFont != null)
            newSpan.Font = DefaultFont;
        if (DefaultFontSize.HasValue)
            newSpan.FontSize = DefaultFontSize.Value;

        Contents.Add(newSpan);
        return this;
    }

    /// <summary>
    /// 增加段落中的内容对象
    /// </summary>
    /// <param name="span">内容对象</param>
    /// <returns>this</returns>
    public Paragraph Add(Span span)
    {
        if (span != null)
        {
            Contents.Add(span);
        }
        return this;
    }

    /// <summary>
    /// 设置默认字体大小
    /// </summary>
    /// <param name="fontSize">字体大小</param>
    /// <returns>this</returns>
    public Paragraph SetFontSize(double fontSize)
    {
        DefaultFontSize = fontSize;
        return this;
    }

    /// <summary>
    /// 设置默认字体
    /// </summary>
    /// <param name="font">字体</param>
    /// <returns>this</returns>
    public Paragraph SetDefaultFont(object font)
    {
        DefaultFont = font;
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
    /// 设置行间距
    /// </summary>
    /// <param name="lineSpace">行间距</param>
    /// <returns>this</returns>
    public Paragraph SetLineSpace(double lineSpace)
    {
        LineSpace = lineSpace;
        return this;
    }

    /// <summary>
    /// 设置首行缩进
    /// </summary>
    /// <param name="indent">缩进字符数</param>
    /// <returns>this</returns>
    public Paragraph SetFirstLineIndent(int indent)
    {
        FirstLineIndent = indent;
        return this;
    }

    /// <summary>
    /// 设置首行缩进宽度
    /// </summary>
    /// <param name="width">缩进宽度（毫米）</param>
    /// <returns>this</returns>
    public Paragraph SetFirstLineIndentWidth(double width)
    {
        FirstLineIndentWidth = width;
        return this;
    }
}



/// <summary>
/// 文本行块（占位符实现）
/// 待后续完善
/// </summary>
public class TxtLineBlock
{
    public double Width { get; set; }
    public double Height { get; set; }
    public List<object> Elements { get; set; } = new();
}

/// <summary>
/// 文本片段
/// </summary>
public class Span
{
    /// <summary>
    /// 文本内容
    /// </summary>
    public string Text { get; set; }

    /// <summary>
    /// 字体
    /// </summary>
    public object? Font { get; set; }

    /// <summary>
    /// 字体大小
    /// </summary>
    public double FontSize { get; set; } = 12d;

    /// <summary>
    /// 文字颜色 (R,G,B)
    /// </summary>
    public int[]? Color { get; set; }

    /// <summary>
    /// 是否粗体
    /// </summary>
    public bool Bold { get; set; }

    /// <summary>
    /// 是否斜体
    /// </summary>
    public bool Italic { get; set; }

    /// <summary>
    /// 是否下划线
    /// </summary>
    public bool Underline { get; set; }

    /// <summary>
    /// 构造文本片段
    /// </summary>
    /// <param name="text">文本内容</param>
    public Span(string text)
    {
        Text = text ?? throw new ArgumentNullException(nameof(text));
    }

    /// <summary>
    /// 设置颜色
    /// </summary>
    /// <param name="r">红色分量</param>
    /// <param name="g">绿色分量</param>
    /// <param name="b">蓝色分量</param>
    /// <returns>this</returns>
    public Span SetColor(int r, int g, int b)
    {
        Color = new[] { r, g, b };
        return this;
    }

    /// <summary>
    /// 设置字体大小
    /// </summary>
    /// <param name="fontSize">字体大小</param>
    /// <returns>this</returns>
    public Span SetFontSize(double fontSize)
    {
        FontSize = fontSize;
        return this;
    }

    /// <summary>
    /// 设置为粗体
    /// </summary>
    /// <param name="bold">是否粗体</param>
    /// <returns>this</returns>
    public Span SetBold(bool bold = true)
    {
        Bold = bold;
        return this;
    }

    /// <summary>
    /// 设置为斜体
    /// </summary>
    /// <param name="italic">是否斜体</param>
    /// <returns>this</returns>
    public Span SetItalic(bool italic = true)
    {
        Italic = italic;
        return this;
    }

    /// <summary>
    /// 设置下划线
    /// </summary>
    /// <param name="underline">是否下划线</param>
    /// <returns>this</returns>
    public Span SetUnderline(bool underline = true)
    {
        Underline = underline;
        return this;
    }
}