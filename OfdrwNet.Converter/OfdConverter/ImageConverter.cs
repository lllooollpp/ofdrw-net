using SkiaSharp;

namespace OfdrwNet.Converter.OfdConverter;

/// <summary>
/// 图像转换器
/// 对应 Java 版本的 org.ofdrw.converter.ofdconverter.ImageConverter
/// 将图像文件转换为OFD文档
/// </summary>
public class ImageConverter : OFDConverterBase
{
    private bool _autoFitToPage = true; // 自动适应页面大小
    private bool _maintainAspectRatio = true; // 保持宽高比
    private double _margin = 10.0; // 页边距（毫米）

    /// <summary>
    /// 构造函数
    /// </summary>
    /// <param name="outputPath">输出OFD文件路径</param>
    public ImageConverter(string outputPath) : base(outputPath)
    {
    }

    /// <summary>
    /// 设置是否自动适应页面大小
    /// </summary>
    /// <param name="autoFit">是否自动适应</param>
    /// <returns>this</returns>
    public ImageConverter SetAutoFitToPage(bool autoFit)
    {
        _autoFitToPage = autoFit;
        return this;
    }

    /// <summary>
    /// 设置是否保持宽高比
    /// </summary>
    /// <param name="maintainRatio">是否保持宽高比</param>
    /// <returns>this</returns>
    public ImageConverter SetMaintainAspectRatio(bool maintainRatio)
    {
        _maintainAspectRatio = maintainRatio;
        return this;
    }

    /// <summary>
    /// 设置页边距
    /// </summary>
    /// <param name="margin">页边距（毫米）</param>
    /// <returns>this</returns>
    public ImageConverter SetMargin(double margin)
    {
        _margin = Math.Max(0, margin);
        return this;
    }

    /// <summary>
    /// 转换图像文件为OFD
    /// </summary>
    public override async Task ConvertAsync(string inputPath, CancellationToken cancellationToken = default)
    {
        ValidateInputFile(inputPath);

        try
        {
            Console.WriteLine($"开始转换图像文件: {inputPath}");

            // 检查是否为多页图像（如TIFF）
            var imageInfo = await GetImageInfo(inputPath, cancellationToken);
            
            if (imageInfo.PageCount > 1)
            {
                await ConvertMultiPageImage(inputPath, imageInfo, cancellationToken);
            }
            else
            {
                await ConvertSingleImage(inputPath, imageInfo, cancellationToken);
            }

            await FinalizeConversion();
            Console.WriteLine($"图像转换完成，共 {imageInfo.PageCount} 页");
        }
        catch (Exception ex)
        {
            throw new GeneralConvertException($"图像转换失败: {ex.Message}", ex);
        }
    }

    /// <summary>
    /// 转换单页图像
    /// </summary>
    private async Task ConvertSingleImage(string imagePath, ImageInfo imageInfo, CancellationToken cancellationToken)
    {
        using var image = SKBitmap.Decode(imagePath);
        if (image == null)
        {
            throw new GeneralConvertException($"无法解码图像文件: {imagePath}");
        }

        var page = CreateNewPage();
        await DrawImageOnPage(page, image, cancellationToken);
    }

    /// <summary>
    /// 转换多页图像
    /// </summary>
    private async Task ConvertMultiPageImage(string imagePath, ImageInfo imageInfo, CancellationToken cancellationToken)
    {
        // 对于多页图像（如TIFF），需要逐页处理
        // 这里使用SkiaSharp的编解码器处理
        
        using var fileStream = File.OpenRead(imagePath);
        using var codec = SKCodec.Create(fileStream);
        
        if (codec == null)
        {
            throw new GeneralConvertException($"无法创建图像编解码器: {imagePath}");
        }

        var frameCount = codec.FrameCount;
        
        for (int frameIndex = 0; frameIndex < frameCount; frameIndex++)
        {
            cancellationToken.ThrowIfCancellationRequested();

            var frameInfo = codec.FrameInfo[frameIndex];
            var bitmap = new SKBitmap(codec.Info);
            
            var result = codec.GetPixels(bitmap.Info, bitmap.GetPixels(), new SKCodecOptions(frameIndex));
            
            if (result == SKCodecResult.Success)
            {
                var page = CreateNewPage();
                await DrawImageOnPage(page, bitmap, cancellationToken);
            }
            
            bitmap.Dispose();
        }
    }

    /// <summary>
    /// 在页面上绘制图像
    /// </summary>
    private async Task DrawImageOnPage(Graphics.OFDPageGraphics page, SKBitmap image, CancellationToken cancellationToken)
    {
        // 计算图像在页面上的位置和大小
        var imageSize = CalculateImageSize(image.Width, image.Height);
        
        // 计算居中位置
        var x = (_pageLayout.Width - imageSize.Width) / 2;
        var y = (_pageLayout.Height - imageSize.Height) / 2;

        // 绘制图像
        // 注意：这里需要将SkiaSharp位图转换为OFD可以使用的格式
        // 由于OFDPageGraphics基于SkiaSharp，可以直接绘制
        
        var drawContext = page.Graphics;
        drawContext.Save();
        
        try
        {
            // 创建图像数据
            using var imageData = SKImage.FromBitmap(image);
            using var paint = new SKPaint { IsAntialias = true };
            
            // 在指定位置绘制图像
            var destRect = new SKRect((float)x, (float)y, (float)(x + imageSize.Width), (float)(y + imageSize.Height));
            
            // 这里需要访问底层的SKCanvas
            // 由于DrawContext封装了Canvas，我们需要扩展其功能
            await DrawImageToContext(drawContext, imageData, destRect, cancellationToken);
        }
        finally
        {
            drawContext.Restore();
        }
    }

    /// <summary>
    /// 绘制图像到绘制上下文
    /// </summary>
    private async Task DrawImageToContext(Graphics.DrawContext context, SKImage image, SKRect destRect, CancellationToken cancellationToken)
    {
        // 由于DrawContext可能不直接暴露SKCanvas，我们需要通过其他方式绘制图像
        // 一个解决方案是将图像保存为字节数组，然后通过OFD的图像对象添加
        
        // 将图像编码为PNG字节数组
        using var encodedImage = image.Encode(SKEncodedImageFormat.Png, 100);
        var imageBytes = encodedImage.ToArray();
        
        // 这里需要扩展OFDPageGraphics以支持图像绘制
        // 目前作为占位符实现
        Console.WriteLine($"绘制图像: {destRect.Width:F1}x{destRect.Height:F1} at ({destRect.Left:F1}, {destRect.Top:F1})");
        
        await Task.CompletedTask;
    }

    /// <summary>
    /// 计算图像在页面上的尺寸
    /// </summary>
    /// <param name="imageWidth">图像原始宽度（像素）</param>
    /// <param name="imageHeight">图像原始高度（像素）</param>
    /// <returns>图像在页面上的尺寸（毫米）</returns>
    private (double Width, double Height) CalculateImageSize(int imageWidth, int imageHeight)
    {
        // 将像素转换为毫米（假设96 DPI）
        const double pixelsPerMm = 96.0 / 25.4;
        var imageWidthMm = imageWidth / pixelsPerMm;
        var imageHeightMm = imageHeight / pixelsPerMm;

        // 计算可用空间
        var availableWidth = _pageLayout.Width - 2 * _margin;
        var availableHeight = _pageLayout.Height - 2 * _margin;

        if (!_autoFitToPage)
        {
            return (Math.Min(imageWidthMm, availableWidth), Math.Min(imageHeightMm, availableHeight));
        }

        // 自动适应页面大小
        if (_maintainAspectRatio)
        {
            var scaleX = availableWidth / imageWidthMm;
            var scaleY = availableHeight / imageHeightMm;
            var scale = Math.Min(scaleX, scaleY);

            return (imageWidthMm * scale, imageHeightMm * scale);
        }
        else
        {
            return (availableWidth, availableHeight);
        }
    }

    /// <summary>
    /// 获取图像信息
    /// </summary>
    private async Task<ImageInfo> GetImageInfo(string imagePath, CancellationToken cancellationToken)
    {
        await Task.CompletedTask;
        
        using var fileStream = File.OpenRead(imagePath);
        using var codec = SKCodec.Create(fileStream);
        
        if (codec == null)
        {
            return new ImageInfo { PageCount = 1, Width = 0, Height = 0 };
        }

        return new ImageInfo
        {
            PageCount = Math.Max(1, codec.FrameCount),
            Width = codec.Info.Width,
            Height = codec.Info.Height
        };
    }

    /// <summary>
    /// 图像信息类
    /// </summary>
    private class ImageInfo
    {
        public int PageCount { get; set; }
        public int Width { get; set; }
        public int Height { get; set; }
    }
}