using OfdrwNet.Core.BasicType;
using SkiaSharp;
using System.Drawing;

namespace OfdrwNet.Graphics;

/// <summary>
/// 绘制上下文
/// 对应 Java 版本的 org.ofdrw.layout.element.canvas.DrawContext
/// 基于 SkiaSharp 实现的 OFD 图形绘制上下文
/// </summary>
public class DrawContext : IDisposable
{
    private readonly SKCanvas _canvas;
    private readonly SKSurface _surface;
    private readonly Stack<SKMatrix> _transformStack = new();
    private readonly Stack<SKPaint> _paintStack = new();
    private bool _disposed = false;

    /// <summary>
    /// 画布宽度（毫米）
    /// </summary>
    public double Width { get; }

    /// <summary>
    /// 画布高度（毫米）
    /// </summary>
    public double Height { get; }

    /// <summary>
    /// 当前画笔
    /// </summary>
    public SKPaint Paint { get; private set; }

    /// <summary>
    /// 当前描边样式
    /// </summary>
    public string StrokeStyle { get; set; } = "#000000";

    /// <summary>
    /// 当前填充样式
    /// </summary>
    public string FillStyle { get; set; } = "#000000";

    /// <summary>
    /// 全局透明度
    /// </summary>
    public double GlobalAlpha { get; set; } = 1.0;

    /// <summary>
    /// 线宽
    /// </summary>
    public double LineWidth { get; set; } = GraphicsConstants.DefaultLineWidth;

    /// <summary>
    /// 构造函数
    /// </summary>
    /// <param name="width">画布宽度（毫米）</param>
    /// <param name="height">画布高度（毫米）</param>
    public DrawContext(double width, double height)
    {
        Width = width;
        Height = height;

        // 将毫米转换为像素（使用72 DPI）
        var pixelWidth = (int)(width * GraphicsConstants.MmToPoint);
        var pixelHeight = (int)(height * GraphicsConstants.MmToPoint);

        var imageInfo = new SKImageInfo(pixelWidth, pixelHeight, SKColorType.Rgba8888, SKAlphaType.Premul);
        _surface = SKSurface.Create(imageInfo);
        _canvas = _surface.Canvas;

        // 初始化画笔
        Paint = new SKPaint
        {
            Color = SKColors.Black,
            StrokeWidth = (float)(LineWidth * GraphicsConstants.MmToPoint),
            IsAntialias = true,
            Style = SKPaintStyle.Stroke
        };

        // 设置坐标系：原点在左上角，Y轴向下
        _canvas.Scale(GraphicsConstants.MmToPoint, GraphicsConstants.MmToPoint);
    }

    /// <summary>
    /// 保存当前绘制状态
    /// </summary>
    public void Save()
    {
        _canvas.Save();
        _transformStack.Push(_canvas.TotalMatrix);
        _paintStack.Push(Paint.Clone());
    }

    /// <summary>
    /// 恢复之前保存的绘制状态
    /// </summary>
    public void Restore()
    {
        _canvas.Restore();
        
        if (_transformStack.Count > 0)
        {
            _transformStack.Pop();
        }
        
        if (_paintStack.Count > 0)
        {
            Paint?.Dispose();
            Paint = _paintStack.Pop();
        }
    }

    /// <summary>
    /// 开始新的路径
    /// </summary>
    public void BeginPath()
    {
        // SkiaSharp中路径是通过SKPath对象管理的
        // 这里主要是为了API兼容性
    }

    /// <summary>
    /// 移动到指定点
    /// </summary>
    /// <param name="x">X坐标（毫米）</param>
    /// <param name="y">Y坐标（毫米）</param>
    public void MoveTo(double x, double y)
    {
        // 在SkiaSharp中，MoveTo通过SKPath对象进行
        // 这里保留方法用于后续路径绘制
    }

    /// <summary>
    /// 画线到指定点
    /// </summary>
    /// <param name="x">X坐标（毫米）</param>
    /// <param name="y">Y坐标（毫米）</param>
    public void LineTo(double x, double y)
    {
        // 在SkiaSharp中，LineTo通过SKPath对象进行
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
        UpdatePaintForStroke();
        _canvas.DrawLine((float)x1, (float)y1, (float)x2, (float)y2, Paint);
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
        UpdatePaintForStroke();
        var rect = new SKRect((float)x, (float)y, (float)(x + width), (float)(y + height));
        _canvas.DrawRect(rect, Paint);
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
        UpdatePaintForFill();
        var rect = new SKRect((float)x, (float)y, (float)(x + width), (float)(y + height));
        _canvas.DrawRect(rect, Paint);
    }

    /// <summary>
    /// 绘制圆形
    /// </summary>
    /// <param name="centerX">圆心X坐标（毫米）</param>
    /// <param name="centerY">圆心Y坐标（毫米）</param>
    /// <param name="radius">半径（毫米）</param>
    public void DrawCircle(double centerX, double centerY, double radius)
    {
        UpdatePaintForStroke();
        _canvas.DrawCircle((float)centerX, (float)centerY, (float)radius, Paint);
    }

    /// <summary>
    /// 填充圆形
    /// </summary>
    /// <param name="centerX">圆心X坐标（毫米）</param>
    /// <param name="centerY">圆心Y坐标（毫米）</param>
    /// <param name="radius">半径（毫米）</param>
    public void FillCircle(double centerX, double centerY, double radius)
    {
        UpdatePaintForFill();
        _canvas.DrawCircle((float)centerX, (float)centerY, (float)radius, Paint);
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
        if (string.IsNullOrEmpty(text)) return;

        UpdatePaintForFill();
        Paint.TextSize = (float)(fontSize * GraphicsConstants.MmToPoint);
        _canvas.DrawText(text, (float)x, (float)y, Paint);
    }

    /// <summary>
    /// 执行描边
    /// </summary>
    public void Stroke()
    {
        // 在SkiaSharp中，描边是通过设置Paint.Style = SKPaintStyle.Stroke实现的
        // 具体的路径绘制需要结合SKPath对象
    }

    /// <summary>
    /// 执行填充
    /// </summary>
    public void Fill()
    {
        // 在SkiaSharp中，填充是通过设置Paint.Style = SKPaintStyle.Fill实现的
    }

    /// <summary>
    /// 设置线宽
    /// </summary>
    /// <param name="width">线宽（毫米）</param>
    public void SetLineWidth(double width)
    {
        LineWidth = width;
        Paint.StrokeWidth = (float)(width * GraphicsConstants.MmToPoint);
    }

    /// <summary>
    /// 设置全局透明度
    /// </summary>
    /// <param name="alpha">透明度（0.0-1.0）</param>
    public void SetGlobalAlpha(double alpha)
    {
        GlobalAlpha = Math.Max(0.0, Math.Min(1.0, alpha));
        Paint.Color = Paint.Color.WithAlpha((byte)(GlobalAlpha * 255));
    }

    /// <summary>
    /// 获取生成的图像数据
    /// </summary>
    /// <returns>图像数据</returns>
    public byte[] GetImageData()
    {
        using var image = _surface.Snapshot();
        using var data = image.Encode(SKEncodedImageFormat.Png, 100);
        return data.ToArray();
    }

    /// <summary>
    /// 更新画笔为描边模式
    /// </summary>
    private void UpdatePaintForStroke()
    {
        Paint.Style = SKPaintStyle.Stroke;
        Paint.Color = ParseColor(StrokeStyle).WithAlpha((byte)(GlobalAlpha * 255));
        Paint.StrokeWidth = (float)(LineWidth * GraphicsConstants.MmToPoint);
    }

    /// <summary>
    /// 更新画笔为填充模式
    /// </summary>
    private void UpdatePaintForFill()
    {
        Paint.Style = SKPaintStyle.Fill;
        Paint.Color = ParseColor(FillStyle).WithAlpha((byte)(GlobalAlpha * 255));
    }

    /// <summary>
    /// 解析颜色字符串
    /// </summary>
    /// <param name="colorString">颜色字符串</param>
    /// <returns>SKColor对象</returns>
    private static SKColor ParseColor(string colorString)
    {
        if (string.IsNullOrEmpty(colorString))
            return SKColors.Black;

        // 支持#RRGGBB格式
        if (colorString.StartsWith("#") && colorString.Length == 7)
        {
            if (uint.TryParse(colorString[1..], System.Globalization.NumberStyles.HexNumber, null, out uint color))
            {
                return new SKColor((byte)(color >> 16), (byte)(color >> 8), (byte)color);
            }
        }

        // 支持预定义颜色名称
        return colorString.ToLowerInvariant() switch
        {
            "red" => SKColors.Red,
            "green" => SKColors.Green,
            "blue" => SKColors.Blue,
            "white" => SKColors.White,
            "black" => SKColors.Black,
            "yellow" => SKColors.Yellow,
            "cyan" => SKColors.Cyan,
            "magenta" => SKColors.Magenta,
            _ => SKColors.Black
        };
    }

    /// <summary>
    /// 释放资源
    /// </summary>
    public void Dispose()
    {
        if (!_disposed)
        {
            Paint?.Dispose();
            _canvas?.Dispose();
            _surface?.Dispose();
            
            // 清理堆栈中的对象
            while (_paintStack.Count > 0)
            {
                _paintStack.Pop()?.Dispose();
            }
            
            _disposed = true;
        }
    }
}