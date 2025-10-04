using OfdrwNet.Core;
using SkiaSharp;

namespace OfdrwNet.Image;

/// <summary>
/// 图像导出器实现，包含完整的图像导出逻辑
/// </summary>
public class ImageExporter : IImageExporter
{
    private readonly IImageRenderer _renderer;
    private readonly IImageExportConfig _config;
    private readonly Func<int, dynamic>? _getPageInfo;
    private readonly Func<int>? _getPageCount;
    private readonly Func<dynamic>? _getGraphicsConstants;

    /// <summary>
    /// 构造函数，用于依赖注入模式
    /// </summary>
    /// <param name="getPageInfo">获取页面信息的委托</param>
    /// <param name="getPageCount">获取页面总数的委托</param>
    /// <param name="getGraphicsConstants">获取图形常量的委托</param>
    /// <param name="config">导出配置</param>
    /// <param name="renderer">图像渲染器</param>
    public ImageExporter(
        Func<int, dynamic>? getPageInfo = null,
        Func<int>? getPageCount = null,
        Func<dynamic>? getGraphicsConstants = null,
        IImageExportConfig? config = null,
        IImageRenderer? renderer = null)
    {
        _getPageInfo = getPageInfo;
        _getPageCount = getPageCount;
        _getGraphicsConstants = getGraphicsConstants;
        _config = config ?? new ImageExportConfig();
        _renderer = renderer ?? new ImageRenderer();
    }

    /// <summary>
    /// 导出指定页面为图像
    /// </summary>
    /// <param name="pageNum">页码（从0开始）</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>导出的图像数据</returns>
    public async Task<byte[]> ExportPageToImageAsync(int pageNum, CancellationToken cancellationToken = default)
    {
        if (_getPageInfo == null || _getGraphicsConstants == null)
        {
            return Array.Empty<byte>();
        }

        var pageInfo = _getPageInfo(pageNum);
        var pageSize = pageInfo.Size;
        var graphicsConstants = _getGraphicsConstants();

        // 计算图像尺寸
        var scale = _config.Dpi / 72f; // 72 DPI为基础
        var width = (int)(pageSize.Width * scale * graphicsConstants.MmToPoint);
        var height = (int)(pageSize.Height * scale * graphicsConstants.MmToPoint);

        // 创建SkiaSharp绘制表面
        var imageInfo = new SKImageInfo(width, height, SKColorType.Rgba8888, SKAlphaType.Premul);
        using var surface = SKSurface.Create(imageInfo);
        using var canvas = surface.Canvas;

        // 清除背景为白色
        canvas.Clear(SKColors.White);

        // 设置坐标变换
        canvas.Scale(scale * graphicsConstants.MmToPoint);

        // 渲染页面内容
        await _renderer.RenderPageContentAsync(canvas, pageInfo, cancellationToken);

        // 生成图像数据
        using var image = surface.Snapshot();
        using var data = image.Encode(_config.ImageFormat, _config.Quality);

        return data.ToArray();
    }

    /// <summary>
    /// 导出所有页面为图像
    /// </summary>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>所有页面的图像数据列表</returns>
    public async Task<IEnumerable<byte[]>> ExportAllPagesToImageAsync(CancellationToken cancellationToken = default)
    {
        if (_getPageCount == null)
        {
            return Enumerable.Empty<byte[]>();
        }

        var pageCount = _getPageCount();
        var imageDataList = new List<byte[]>();

        for (int pageNum = 0; pageNum < pageCount; pageNum++)
        {
            cancellationToken.ThrowIfCancellationRequested();
            var imageData = await ExportPageToImageAsync(pageNum, cancellationToken);
            imageDataList.Add(imageData);
        }

        return imageDataList;
    }

    /// <summary>
    /// 获取图像格式对应的文件扩展名
    /// </summary>
    /// <param name="format">图像格式</param>
    /// <returns>文件扩展名</returns>
    public string GetFileExtension(SKEncodedImageFormat format)
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
