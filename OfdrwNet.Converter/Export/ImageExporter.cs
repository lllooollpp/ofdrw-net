using OfdrwNet.Graphics;
using SkiaSharp;

namespace OfdrwNet.Converter.Export;

/// <summary>
/// 图片导出器
/// 对应 Java 版本的 org.ofdrw.converter.export.ImageExporter
/// 将OFD页面转换为图像文件（PNG、JPEG、BMP、WEBP等）
/// </summary>
public class ImageExporter : OFDExporterBase
{
    private readonly SKEncodedImageFormat _imageFormat;
    private readonly int _quality;
    private readonly float _dpi;

    /// <summary>
    /// 构造函数，默认导出为PNG格式
    /// </summary>
    /// <param name="ofdPath">OFD文件路径</param>
    /// <param name="outputDir">输出目录</param>
    /// <param name="dpi">分辨率，默认150 DPI</param>
    public ImageExporter(string ofdPath, string outputDir, float dpi = 150f) 
        : this(ofdPath, outputDir, SKEncodedImageFormat.Png, 100, dpi)
    {
    }

    /// <summary>
    /// 构造函数
    /// </summary>
    /// <param name="ofdPath">OFD文件路径</param>
    /// <param name="outputDir">输出目录</param>
    /// <param name="imageFormat">图像格式</param>
    /// <param name="quality">图像质量（0-100），仅对JPEG有效</param>
    /// <param name="dpi">分辨率</param>
    public ImageExporter(string ofdPath, string outputDir, SKEncodedImageFormat imageFormat, int quality = 100, float dpi = 150f) 
        : base(ofdPath, outputDir)
    {
        _imageFormat = imageFormat;
        _quality = Math.Max(0, Math.Min(100, quality));
        _dpi = Math.Max(72f, dpi);
    }

    /// <summary>
    /// 导出单个页面为图像
    /// </summary>
    /// <param name="pageNum">页码（从0开始）</param>
    /// <param name="cancellationToken">取消令牌</param>
    protected override async Task ExportPageAsync(int pageNum, CancellationToken cancellationToken)
    {
        var pageInfo = GetPageInfo(pageNum);
        var pageSize = pageInfo.Size;
        
        // 计算图像尺寸
        var scale = _dpi / 72f; // 72 DPI为基础
        var width = (int)(pageSize.Width * scale * GraphicsConstants.MmToPoint);
        var height = (int)(pageSize.Height * scale * GraphicsConstants.MmToPoint);

        // 创建SkiaSharp绘制表面
        var imageInfo = new SKImageInfo(width, height, SKColorType.Rgba8888, SKAlphaType.Premul);
        using var surface = SKSurface.Create(imageInfo);
        using var canvas = surface.Canvas;

        // 清除背景为白色
        canvas.Clear(SKColors.White);

        // 设置坐标变换
        canvas.Scale(scale * GraphicsConstants.MmToPoint);

        // 渲染页面内容
        await RenderPageContent(canvas, pageInfo, cancellationToken);

        // 保存图像
        var extension = GetFileExtension(_imageFormat);
        var outputPath = GenerateOutputFileName(pageNum, extension);
        
        using var image = surface.Snapshot();
        using var data = image.Encode(_imageFormat, _quality);
        
        await File.WriteAllBytesAsync(outputPath, data.ToArray(), cancellationToken);
        
        _outputPaths.Add(outputPath);
        
        Console.WriteLine($"页面 {pageNum + 1} 已导出到: {outputPath}");
    }

    /// <summary>
    /// 渲染页面内容
    /// </summary>
    /// <param name="canvas">Skia画布</param>
    /// <param name="pageInfo">页面信息</param>
    /// <param name="cancellationToken">取消令牌</param>
    private async Task RenderPageContent(SKCanvas canvas, OfdrwNet.Reader.PageInfo pageInfo, CancellationToken cancellationToken)
    {
        // 获取页面的所有图层
        var layers = pageInfo.GetAllLayers();
        
        foreach (var layer in layers)
        {
            cancellationToken.ThrowIfCancellationRequested();
            await RenderLayer(canvas, layer);
        }
    }

    /// <summary>
    /// 渲染单个图层
    /// </summary>
    /// <param name="canvas">Skia画布</param>
    /// <param name="layer">图层元素</param>
    private async Task RenderLayer(SKCanvas canvas, System.Xml.Linq.XElement layer)
    {
        // 这里需要根据OFD的图层结构来渲染
        // 由于这是一个复杂的渲染过程，这里提供基础框架
        // 实际实现需要解析OFD的各种图形对象（文本、图像、路径等）
        
        // 获取图层中的所有对象
        var textObjects = layer.Elements("TextObject");
        var imageObjects = layer.Elements("ImageObject");
        var pathObjects = layer.Elements("PathObject");

        // 渲染文本对象
        foreach (var textObj in textObjects)
        {
            await RenderTextObject(canvas, textObj);
        }

        // 渲染图像对象
        foreach (var imageObj in imageObjects)
        {
            await RenderImageObject(canvas, imageObj);
        }

        // 渲染路径对象
        foreach (var pathObj in pathObjects)
        {
            await RenderPathObject(canvas, pathObj);
        }
    }

    /// <summary>
    /// 渲染文本对象
    /// </summary>
    private async Task RenderTextObject(SKCanvas canvas, System.Xml.Linq.XElement textObject)
    {
        // 解析文本属性
        var boundary = ParseBoundary(textObject.Attribute("Boundary")?.Value);
        var fontSize = float.Parse(textObject.Attribute("Size")?.Value ?? "12");
        
        // 获取文本内容
        var textCodeElements = textObject.Elements("TextCode");
        foreach (var textCode in textCodeElements)
        {
            var x = float.Parse(textCode.Attribute("X")?.Value ?? "0");
            var y = float.Parse(textCode.Attribute("Y")?.Value ?? "0");
            var text = textCode.Value;

            if (!string.IsNullOrEmpty(text))
            {
                using var paint = new SKPaint
                {
                    Color = SKColors.Black,
                    TextSize = fontSize,
                    IsAntialias = true,
                    Typeface = SKTypeface.Default
                };

                canvas.DrawText(text, x, y, paint);
            }
        }
        
        await Task.CompletedTask;
    }

    /// <summary>
    /// 渲染图像对象
    /// </summary>
    private async Task RenderImageObject(SKCanvas canvas, System.Xml.Linq.XElement imageObject)
    {
        // 这里需要从OFD容器中加载图像资源并绘制
        // 简化实现，实际需要通过ResourceLocator加载图像
        await Task.CompletedTask;
    }

    /// <summary>
    /// 渲染路径对象
    /// </summary>
    private async Task RenderPathObject(SKCanvas canvas, System.Xml.Linq.XElement pathObject)
    {
        // 解析路径数据并绘制
        var pathData = pathObject.Element("AbbreviatedData")?.Value;
        if (!string.IsNullOrEmpty(pathData))
        {
            using var path = SKPath.ParseSvgPathData(pathData);
            using var paint = new SKPaint
            {
                Color = SKColors.Black,
                Style = SKPaintStyle.Stroke,
                StrokeWidth = 1,
                IsAntialias = true
            };

            canvas.DrawPath(path, paint);
        }
        
        await Task.CompletedTask;
    }

    /// <summary>
    /// 解析边界框
    /// </summary>
    private SKRect ParseBoundary(string? boundaryStr)
    {
        if (string.IsNullOrEmpty(boundaryStr))
            return SKRect.Empty;

        var parts = boundaryStr.Split(' ', StringSplitOptions.RemoveEmptyEntries);
        if (parts.Length >= 4)
        {
            var x = float.Parse(parts[0]);
            var y = float.Parse(parts[1]);
            var width = float.Parse(parts[2]);
            var height = float.Parse(parts[3]);
            return new SKRect(x, y, x + width, y + height);
        }

        return SKRect.Empty;
    }

    /// <summary>
    /// 获取图像格式对应的文件扩展名
    /// </summary>
    private static string GetFileExtension(SKEncodedImageFormat format)
    {
        return format switch
        {
            SKEncodedImageFormat.Png => ".png",
            SKEncodedImageFormat.Jpeg => ".jpg",
            SKEncodedImageFormat.Bmp => ".bmp",
            SKEncodedImageFormat.Webp => ".webp",
            SKEncodedImageFormat.Gif => ".gif",
            SKEncodedImageFormat.Ico => ".ico",
            _ => ".png"
        };
    }
}