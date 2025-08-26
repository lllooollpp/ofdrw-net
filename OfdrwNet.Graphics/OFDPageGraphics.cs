using OfdrwNet.Core.BasicType;
using OfdrwNet.Packaging.Container;
using SkiaSharp;
using System.Text;
using System.Xml.Linq;

namespace OfdrwNet.Graphics;

/// <summary>
/// OFD页面图形绘制器
/// 对应 Java 版本的 org.ofdrw.graphics2d.OFDPageGraphics2D
/// 提供基于SkiaSharp的OFD页面图形绘制功能
/// </summary>
public class OFDPageGraphics : IDisposable
{
    private readonly DrawContext _drawContext;
    private readonly List<DrawCommand> _drawCommands;
    private bool _disposed = false;

    /// <summary>
    /// 页面ID
    /// </summary>
    public int PageId { get; }

    /// <summary>
    /// 页面宽度（毫米）
    /// </summary>
    public double Width { get; }

    /// <summary>
    /// 页面高度（毫米）
    /// </summary>
    public double Height { get; }

    /// <summary>
    /// 绘制上下文
    /// </summary>
    public DrawContext Graphics => _drawContext;

    /// <summary>
    /// 构造函数
    /// </summary>
    /// <param name="pageId">页面ID</param>
    /// <param name="width">页面宽度（毫米）</param>
    /// <param name="height">页面高度（毫米）</param>
    public OFDPageGraphics(int pageId, double width, double height)
    {
        PageId = pageId;
        Width = width;
        Height = height;
        _drawContext = new DrawContext(width, height);
        _drawCommands = new List<DrawCommand>();
    }

    /// <summary>
    /// 生成页面内容到容器
    /// </summary>
    /// <param name="pageContainer">页面容器</param>
    /// <param name="idGenerator">ID生成器</param>
    public async Task GenerateContentAsync(VirtualContainer pageContainer, Func<int> idGenerator)
    {
        try
        {
            // 保存绘制的图像
            var imageData = _drawContext.GetImageData();
            var imageName = $"image_{PageId}.png";
            await pageContainer.PutObjAsync(imageName, imageData);

            // 生成页面内容XML
            var contentXml = GenerateContentXml(imageName, idGenerator);
            pageContainer.PutObj("Content.xml", contentXml);

            // 如果有矢量绘制命令，生成矢量内容
            if (_drawCommands.Count > 0)
            {
                var vectorContent = GenerateVectorContent(idGenerator);
                pageContainer.PutObj("Vector.xml", vectorContent);
            }
        }
        catch (Exception ex)
        {
            throw new InvalidOperationException($"生成页面 {PageId} 内容时发生错误", ex);
        }
    }

    /// <summary>
    /// 添加绘制命令
    /// </summary>
    /// <param name="command">绘制命令</param>
    public void AddDrawCommand(DrawCommand command)
    {
        _drawCommands.Add(command);
    }

    /// <summary>
    /// 绘制直线
    /// </summary>
    /// <param name="x1">起点X坐标（毫米）</param>
    /// <param name="y1">起点Y坐标（毫米）</param>
    /// <param name="x2">终点X坐标（毫米）</param>
    /// <param name="y2">终点Y坐标（毫米）</param>
    public void DrawLine(double x1, double y1, double x2, double y2)
    {
        _drawContext.DrawLine(x1, y1, x2, y2);
        _drawCommands.Add(new LineCommand(x1, y1, x2, y2, _drawContext.StrokeStyle, _drawContext.LineWidth));
    }

    /// <summary>
    /// 绘制矩形
    /// </summary>
    /// <param name="x">左上角X坐标（毫米）</param>
    /// <param name="y">左上角Y坐标（毫米）</param>
    /// <param name="width">宽度（毫米）</param>
    /// <param name="height">高度（毫米）</param>
    public void DrawRect(double x, double y, double width, double height)
    {
        _drawContext.DrawRect(x, y, width, height);
        _drawCommands.Add(new RectCommand(x, y, width, height, _drawContext.StrokeStyle, _drawContext.LineWidth));
    }

    /// <summary>
    /// 填充矩形
    /// </summary>
    /// <param name="x">左上角X坐标（毫米）</param>
    /// <param name="y">左上角Y坐标（毫米）</param>
    /// <param name="width">宽度（毫米）</param>
    /// <param name="height">高度（毫米）</param>
    public void FillRect(double x, double y, double width, double height)
    {
        _drawContext.FillRect(x, y, width, height);
        _drawCommands.Add(new FillRectCommand(x, y, width, height, _drawContext.FillStyle));
    }

    /// <summary>
    /// 绘制圆形
    /// </summary>
    /// <param name="centerX">圆心X坐标（毫米）</param>
    /// <param name="centerY">圆心Y坐标（毫米）</param>
    /// <param name="radius">半径（毫米）</param>
    public void DrawCircle(double centerX, double centerY, double radius)
    {
        _drawContext.DrawCircle(centerX, centerY, radius);
        _drawCommands.Add(new CircleCommand(centerX, centerY, radius, _drawContext.StrokeStyle, _drawContext.LineWidth));
    }

    /// <summary>
    /// 填充圆形
    /// </summary>
    /// <param name="centerX">圆心X坐标（毫米）</param>
    /// <param name="centerY">圆心Y坐标（毫米）</param>
    /// <param name="radius">半径（毫米）</param>
    public void FillCircle(double centerX, double centerY, double radius)
    {
        _drawContext.FillCircle(centerX, centerY, radius);
        _drawCommands.Add(new FillCircleCommand(centerX, centerY, radius, _drawContext.FillStyle));
    }

    /// <summary>
    /// 绘制文本
    /// </summary>
    /// <param name="text">文本内容</param>
    /// <param name="x">X坐标（毫米）</param>
    /// <param name="y">Y坐标（毫米）</param>
    /// <param name="fontSize">字体大小（毫米）</param>
    public void DrawText(string text, double x, double y, double fontSize = 3.0)
    {
        _drawContext.DrawText(text, x, y, fontSize);
        _drawCommands.Add(new TextCommand(text, x, y, fontSize, _drawContext.FillStyle));
    }

    /// <summary>
    /// 设置线宽
    /// </summary>
    /// <param name="width">线宽（毫米）</param>
    public void SetLineWidth(double width)
    {
        _drawContext.SetLineWidth(width);
    }

    /// <summary>
    /// 设置描边颜色
    /// </summary>
    /// <param name="color">颜色字符串</param>
    public void SetStrokeColor(string color)
    {
        _drawContext.StrokeStyle = color;
    }

    /// <summary>
    /// 设置填充颜色
    /// </summary>
    /// <param name="color">颜色字符串</param>
    public void SetFillColor(string color)
    {
        _drawContext.FillStyle = color;
    }

    /// <summary>
    /// 生成页面内容XML
    /// </summary>
    /// <param name="imageName">图像文件名</param>
    /// <param name="idGenerator">ID生成器</param>
    /// <returns>内容XML字符串</returns>
    private string GenerateContentXml(string imageName, Func<int> idGenerator)
    {
        var contentId = idGenerator();
        var imageId = idGenerator();

        var xml = new XElement("ofd:Content",
            new XAttribute(XNamespace.Xmlns + "ofd", "http://www.ofdspec.org/2016"),
            new XElement("ofd:Layer",
                new XAttribute("ID", contentId),
                new XElement("ofd:ImageObject",
                    new XAttribute("ID", imageId),
                    new XAttribute("Boundary", $"0 0 {Width} {Height}"),
                    new XAttribute("ResourceID", imageId),
                    imageName
                )
            )
        );

        return $"<?xml version=\"1.0\" encoding=\"UTF-8\"?>\n{xml}";
    }

    /// <summary>
    /// 生成矢量内容XML
    /// </summary>
    /// <param name="idGenerator">ID生成器</param>
    /// <returns>矢量内容XML字符串</returns>
    private string GenerateVectorContent(Func<int> idGenerator)
    {
        var vectorElements = new List<XElement>();

        foreach (var command in _drawCommands)
        {
            var element = command.ToXElement(idGenerator);
            if (element != null)
            {
                vectorElements.Add(element);
            }
        }

        var xml = new XElement("ofd:Content",
            new XAttribute(XNamespace.Xmlns + "ofd", "http://www.ofdspec.org/2016"),
            new XElement("ofd:Layer",
                new XAttribute("ID", idGenerator()),
                vectorElements
            )
        );

        return $"<?xml version=\"1.0\" encoding=\"UTF-8\"?>\n{xml}";
    }

    /// <summary>
    /// 释放资源
    /// </summary>
    public void Dispose()
    {
        if (!_disposed)
        {
            _drawContext?.Dispose();
            _drawCommands?.Clear();
            _disposed = true;
        }
    }
}

/// <summary>
/// 绘制命令基类
/// </summary>
public abstract class DrawCommand
{
    /// <summary>
    /// 转换为XML元素
    /// </summary>
    /// <param name="idGenerator">ID生成器</param>
    /// <returns>XML元素</returns>
    public abstract XElement? ToXElement(Func<int> idGenerator);
}

/// <summary>
/// 直线绘制命令
/// </summary>
public class LineCommand : DrawCommand
{
    public double X1 { get; }
    public double Y1 { get; }
    public double X2 { get; }
    public double Y2 { get; }
    public string Color { get; }
    public double Width { get; }

    public LineCommand(double x1, double y1, double x2, double y2, string color, double width)
    {
        X1 = x1; Y1 = y1; X2 = x2; Y2 = y2;
        Color = color; Width = width;
    }

    public override XElement? ToXElement(Func<int> idGenerator)
    {
        return new XElement("ofd:PathObject",
            new XAttribute("ID", idGenerator()),
            new XAttribute("Boundary", $"{Math.Min(X1, X2)} {Math.Min(Y1, Y2)} {Math.Abs(X2 - X1)} {Math.Abs(Y2 - Y1)}"),
            new XElement("ofd:AbbreviatedData", $"M {X1} {Y1} L {X2} {Y2}")
        );
    }
}

/// <summary>
/// 矩形绘制命令
/// </summary>
public class RectCommand : DrawCommand
{
    public double X { get; }
    public double Y { get; }
    public double Width { get; }
    public double Height { get; }
    public string Color { get; }
    public double LineWidth { get; }

    public RectCommand(double x, double y, double width, double height, string color, double lineWidth)
    {
        X = x; Y = y; Width = width; Height = height;
        Color = color; LineWidth = lineWidth;
    }

    public override XElement? ToXElement(Func<int> idGenerator)
    {
        return new XElement("ofd:PathObject",
            new XAttribute("ID", idGenerator()),
            new XAttribute("Boundary", $"{X} {Y} {Width} {Height}"),
            new XElement("ofd:AbbreviatedData", $"M {X} {Y} L {X + Width} {Y} L {X + Width} {Y + Height} L {X} {Y + Height} Z")
        );
    }
}

/// <summary>
/// 填充矩形命令
/// </summary>
public class FillRectCommand : DrawCommand
{
    public double X { get; }
    public double Y { get; }
    public double Width { get; }
    public double Height { get; }
    public string Color { get; }

    public FillRectCommand(double x, double y, double width, double height, string color)
    {
        X = x; Y = y; Width = width; Height = height; Color = color;
    }

    public override XElement? ToXElement(Func<int> idGenerator)
    {
        return new XElement("ofd:PathObject",
            new XAttribute("ID", idGenerator()),
            new XAttribute("Boundary", $"{X} {Y} {Width} {Height}"),
            new XAttribute("Fill", "true"),
            new XElement("ofd:AbbreviatedData", $"M {X} {Y} L {X + Width} {Y} L {X + Width} {Y + Height} L {X} {Y + Height} Z")
        );
    }
}

/// <summary>
/// 圆形绘制命令
/// </summary>
public class CircleCommand : DrawCommand
{
    public double CenterX { get; }
    public double CenterY { get; }
    public double Radius { get; }
    public string Color { get; }
    public double LineWidth { get; }

    public CircleCommand(double centerX, double centerY, double radius, string color, double lineWidth)
    {
        CenterX = centerX; CenterY = centerY; Radius = radius;
        Color = color; LineWidth = lineWidth;
    }

    public override XElement? ToXElement(Func<int> idGenerator)
    {
        var left = CenterX - Radius;
        var top = CenterY - Radius;
        var size = Radius * 2;

        return new XElement("ofd:PathObject",
            new XAttribute("ID", idGenerator()),
            new XAttribute("Boundary", $"{left} {top} {size} {size}"),
            new XElement("ofd:AbbreviatedData", 
                $"M {CenterX} {CenterY - Radius} A {Radius} {Radius} 0 1 1 {CenterX} {CenterY + Radius} A {Radius} {Radius} 0 1 1 {CenterX} {CenterY - Radius} Z")
        );
    }
}

/// <summary>
/// 填充圆形命令
/// </summary>
public class FillCircleCommand : DrawCommand
{
    public double CenterX { get; }
    public double CenterY { get; }
    public double Radius { get; }
    public string Color { get; }

    public FillCircleCommand(double centerX, double centerY, double radius, string color)
    {
        CenterX = centerX; CenterY = centerY; Radius = radius; Color = color;
    }

    public override XElement? ToXElement(Func<int> idGenerator)
    {
        var left = CenterX - Radius;
        var top = CenterY - Radius;
        var size = Radius * 2;

        return new XElement("ofd:PathObject",
            new XAttribute("ID", idGenerator()),
            new XAttribute("Boundary", $"{left} {top} {size} {size}"),
            new XAttribute("Fill", "true"),
            new XElement("ofd:AbbreviatedData",
                $"M {CenterX} {CenterY - Radius} A {Radius} {Radius} 0 1 1 {CenterX} {CenterY + Radius} A {Radius} {Radius} 0 1 1 {CenterX} {CenterY - Radius} Z")
        );
    }
}

/// <summary>
/// 文本绘制命令
/// </summary>
public class TextCommand : DrawCommand
{
    public string Text { get; }
    public double X { get; }
    public double Y { get; }
    public double FontSize { get; }
    public string Color { get; }

    public TextCommand(string text, double x, double y, double fontSize, string color)
    {
        Text = text; X = x; Y = y; FontSize = fontSize; Color = color;
    }

    public override XElement? ToXElement(Func<int> idGenerator)
    {
        if (string.IsNullOrEmpty(Text)) return null;

        return new XElement("ofd:TextObject",
            new XAttribute("ID", idGenerator()),
            new XAttribute("Boundary", $"{X} {Y} {Text.Length * FontSize * 0.6} {FontSize}"),
            new XAttribute("Font", "1"),
            new XAttribute("Size", FontSize),
            new XElement("ofd:TextCode",
                new XAttribute("X", X),
                new XAttribute("Y", Y),
                Text
            )
        );
    }
}