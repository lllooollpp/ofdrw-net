using OfdrwNet.Core.BasicType;
using System.Drawing;

namespace OfdrwNet.Graphics;

/// <summary>
/// OFD画布绘制器
/// 对应 Java 版本的 org.ofdrw.layout.element.canvas.Canvas
/// 提供高级绘制功能的画布接口
/// </summary>
public class Canvas : IDisposable
{
    private readonly OFDPageGraphics _pageGraphics;
    private readonly List<ICanvasElement> _elements;
    private bool _disposed = false;

    /// <summary>
    /// 画布宽度（毫米）
    /// </summary>
    public double Width => _pageGraphics.Width;

    /// <summary>
    /// 画布高度（毫米）
    /// </summary>
    public double Height => _pageGraphics.Height;

    /// <summary>
    /// 绘制上下文
    /// </summary>
    public DrawContext DrawContext => _pageGraphics.Graphics;

    /// <summary>
    /// 构造函数
    /// </summary>
    /// <param name="pageGraphics">页面图形绘制器</param>
    public Canvas(OFDPageGraphics pageGraphics)
    {
        _pageGraphics = pageGraphics ?? throw new ArgumentNullException(nameof(pageGraphics));
        _elements = new List<ICanvasElement>();
    }

    /// <summary>
    /// 添加绘制元素
    /// </summary>
    /// <param name="element">绘制元素</param>
    public void Add(ICanvasElement element)
    {
        if (element != null)
        {
            _elements.Add(element);
        }
    }

    /// <summary>
    /// 绘制直线
    /// </summary>
    /// <param name="start">起点</param>
    /// <param name="end">终点</param>
    /// <returns>直线元素</returns>
    public LineElement DrawLine(PointF start, PointF end)
    {
        var line = new LineElement(start, end);
        Add(line);
        return line;
    }

    /// <summary>
    /// 绘制矩形
    /// </summary>
    /// <param name="bounds">矩形边界</param>
    /// <returns>矩形元素</returns>
    public RectangleElement DrawRectangle(RectangleF bounds)
    {
        var rect = new RectangleElement(bounds);
        Add(rect);
        return rect;
    }

    /// <summary>
    /// 绘制圆形
    /// </summary>
    /// <param name="center">圆心</param>
    /// <param name="radius">半径</param>
    /// <returns>圆形元素</returns>
    public CircleElement DrawCircle(PointF center, double radius)
    {
        var circle = new CircleElement(center, radius);
        Add(circle);
        return circle;
    }

    /// <summary>
    /// 绘制文本
    /// </summary>
    /// <param name="text">文本内容</param>
    /// <param name="position">文本位置</param>
    /// <param name="fontSize">字体大小（毫米）</param>
    /// <returns>文本元素</returns>
    public TextElement DrawText(string text, PointF position, double fontSize = 3.0)
    {
        var textElement = new TextElement(text, position, fontSize);
        Add(textElement);
        return textElement;
    }

    /// <summary>
    /// 渲染所有元素到页面
    /// </summary>
    public void Render()
    {
        foreach (var element in _elements)
        {
            element.Draw(_pageGraphics);
        }
    }

    /// <summary>
    /// 清空所有元素
    /// </summary>
    public void Clear()
    {
        foreach (var element in _elements)
        {
            if (element is IDisposable disposable)
            {
                disposable.Dispose();
            }
        }
        _elements.Clear();
    }

    /// <summary>
    /// 释放资源
    /// </summary>
    public void Dispose()
    {
        if (!_disposed)
        {
            Clear();
            _disposed = true;
        }
    }
}

/// <summary>
/// 画布绘制元素接口
/// </summary>
public interface ICanvasElement
{
    /// <summary>
    /// 绘制到页面
    /// </summary>
    /// <param name="pageGraphics">页面图形绘制器</param>
    void Draw(OFDPageGraphics pageGraphics);

    /// <summary>
    /// 元素边界
    /// </summary>
    RectangleF Bounds { get; }

    /// <summary>
    /// 描边颜色
    /// </summary>
    string? StrokeColor { get; set; }

    /// <summary>
    /// 填充颜色
    /// </summary>
    string? FillColor { get; set; }

    /// <summary>
    /// 线宽
    /// </summary>
    double LineWidth { get; set; }

    /// <summary>
    /// 是否可见
    /// </summary>
    bool Visible { get; set; }
}

/// <summary>
/// 绘制元素基类
/// </summary>
public abstract class CanvasElementBase : ICanvasElement
{
    /// <summary>
    /// 元素边界
    /// </summary>
    public RectangleF Bounds { get; protected set; }

    /// <summary>
    /// 描边颜色
    /// </summary>
    public string? StrokeColor { get; set; } = "#000000";

    /// <summary>
    /// 填充颜色
    /// </summary>
    public string? FillColor { get; set; }

    /// <summary>
    /// 线宽
    /// </summary>
    public double LineWidth { get; set; } = GraphicsConstants.DefaultLineWidth;

    /// <summary>
    /// 是否可见
    /// </summary>
    public bool Visible { get; set; } = true;

    /// <summary>
    /// 绘制到页面
    /// </summary>
    /// <param name="pageGraphics">页面图形绘制器</param>
    public abstract void Draw(OFDPageGraphics pageGraphics);

    /// <summary>
    /// 应用绘制样式
    /// </summary>
    /// <param name="pageGraphics">页面图形绘制器</param>
    protected void ApplyStyle(OFDPageGraphics pageGraphics)
    {
        if (!string.IsNullOrEmpty(StrokeColor))
        {
            pageGraphics.SetStrokeColor(StrokeColor);
        }
        
        if (!string.IsNullOrEmpty(FillColor))
        {
            pageGraphics.SetFillColor(FillColor);
        }
        
        pageGraphics.SetLineWidth(LineWidth);
    }
}

/// <summary>
/// 直线元素
/// </summary>
public class LineElement : CanvasElementBase
{
    public PointF StartPoint { get; set; }
    public PointF EndPoint { get; set; }

    public LineElement(PointF start, PointF end)
    {
        StartPoint = start;
        EndPoint = end;
        UpdateBounds();
    }

    public override void Draw(OFDPageGraphics pageGraphics)
    {
        if (!Visible) return;

        ApplyStyle(pageGraphics);
        pageGraphics.DrawLine(StartPoint.X, StartPoint.Y, EndPoint.X, EndPoint.Y);
    }

    private void UpdateBounds()
    {
        var left = Math.Min(StartPoint.X, EndPoint.X);
        var top = Math.Min(StartPoint.Y, EndPoint.Y);
        var width = Math.Abs(EndPoint.X - StartPoint.X);
        var height = Math.Abs(EndPoint.Y - StartPoint.Y);
        Bounds = new RectangleF(left, top, width, height);
    }
}

/// <summary>
/// 矩形元素
/// </summary>
public class RectangleElement : CanvasElementBase
{
    public bool Filled { get; set; } = false;

    public RectangleElement(RectangleF bounds)
    {
        Bounds = bounds;
    }

    public override void Draw(OFDPageGraphics pageGraphics)
    {
        if (!Visible) return;

        ApplyStyle(pageGraphics);
        
        if (Filled && !string.IsNullOrEmpty(FillColor))
        {
            pageGraphics.FillRect(Bounds.X, Bounds.Y, Bounds.Width, Bounds.Height);
        }
        
        if (!string.IsNullOrEmpty(StrokeColor))
        {
            pageGraphics.DrawRect(Bounds.X, Bounds.Y, Bounds.Width, Bounds.Height);
        }
    }
}

/// <summary>
/// 圆形元素
/// </summary>
public class CircleElement : CanvasElementBase
{
    public PointF Center { get; set; }
    public double Radius { get; set; }
    public bool Filled { get; set; } = false;

    public CircleElement(PointF center, double radius)
    {
        Center = center;
        Radius = radius;
        UpdateBounds();
    }

    public override void Draw(OFDPageGraphics pageGraphics)
    {
        if (!Visible) return;

        ApplyStyle(pageGraphics);
        
        if (Filled && !string.IsNullOrEmpty(FillColor))
        {
            pageGraphics.FillCircle(Center.X, Center.Y, Radius);
        }
        
        if (!string.IsNullOrEmpty(StrokeColor))
        {
            pageGraphics.DrawCircle(Center.X, Center.Y, Radius);
        }
    }

    private void UpdateBounds()
    {
        var left = Center.X - Radius;
        var top = Center.Y - Radius;
        var size = Radius * 2;
        Bounds = new RectangleF((float)left, (float)top, (float)size, (float)size);
    }
}

/// <summary>
/// 文本元素
/// </summary>
public class TextElement : CanvasElementBase
{
    public string Text { get; set; }
    public PointF Position { get; set; }
    public double FontSize { get; set; }

    public TextElement(string text, PointF position, double fontSize)
    {
        Text = text ?? string.Empty;
        Position = position;
        FontSize = fontSize;
        UpdateBounds();
    }

    public override void Draw(OFDPageGraphics pageGraphics)
    {
        if (!Visible || string.IsNullOrEmpty(Text)) return;

        ApplyStyle(pageGraphics);
        pageGraphics.DrawText(Text, Position.X, Position.Y, FontSize);
    }

    private void UpdateBounds()
    {
        // 估算文本边界
        var width = Text.Length * FontSize * 0.6; // 粗略估算
        var height = FontSize;
        Bounds = new RectangleF(Position.X, Position.Y, (float)width, (float)height);
    }
}