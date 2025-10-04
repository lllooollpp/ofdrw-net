using OfdrwNet.Graphics;
using OfdrwNet.Core;
using OfdrwNet.Image;
using SkiaSharp;

namespace OfdrwNet.Converter.Export;

/// <summary>
/// 图片导出器 (OFD -> Raster Image)
/// 重命名: 原 ImageExporter => OfdImageExporter，更明确方向；保留旧类（过时）兼容。
/// </summary>
public class OfdImageExporter : OFDExporterBase
{
    private readonly IImageExporter _imageExporter;
    private readonly IImageExportConfig _config;

    /// <summary>
    /// 构造函数，默认导出为PNG格式
    /// </summary>
    /// <param name="ofdPath">OFD文件路径</param>
    /// <param name="outputDir">输出目录</param>
    /// <param name="dpi">分辨率，默认150 DPI</param>
    public OfdImageExporter(string ofdPath, string outputDir, float dpi = 150f)
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
    public OfdImageExporter(string ofdPath, string outputDir, SKEncodedImageFormat imageFormat, int quality = 100, float dpi = 150f)
        : base(ofdPath, outputDir)
    {
        _config = new OfdrwNet.Image.ImageExportConfig(imageFormat, quality, dpi);

        // 创建图像导出器，传入获取页面信息和图形常量的委托
        _imageExporter = CreateDefaultImageExporter();
    }

    /// <summary>
    /// 构造函数，支持依赖注入
    /// </summary>
    /// <param name="ofdPath">OFD文件路径</param>
    /// <param name="outputDir">输出目录</param>
    /// <param name="config">导出配置</param>
    /// <param name="imageExporter">图像导出器，如果为null则使用默认实现</param>
    public OfdImageExporter(string ofdPath, string outputDir, IImageExportConfig config, IImageExporter? imageExporter = null)
        : base(ofdPath, outputDir)
    {
        _config = config;
        _imageExporter = imageExporter ?? CreateDefaultImageExporter();
    }

    /// <summary>
    /// 创建默认的图像导出器
    /// </summary>
    /// <returns>图像导出器实例</returns>
    private IImageExporter CreateDefaultImageExporter()
    {
        // 使用 OfdrwNet.Image 项目中的 ImageExporter，传入获取页面信息和相关委托
        return new OfdrwNet.Image.ImageExporter(
            getPageInfo: GetPageInfo,
            getPageCount: () => _reader?.GetNumberOfPages() ?? 0,
            getGraphicsConstants: () => new { MmToPoint = GraphicsConstants.MmToPoint },
            config: _config
        );
    }

    /// <summary>
    /// 导出单个页面为图像
    /// </summary>
    /// <param name="pageNum">页码（从0开始）</param>
    /// <param name="cancellationToken">取消令牌</param>
    protected override async Task ExportPageAsync(int pageNum, CancellationToken cancellationToken)
    {
        var imageData = await _imageExporter.ExportPageToImageAsync(pageNum, cancellationToken);

        var extension = _imageExporter.GetFileExtension(_config.ImageFormat);
        var outputPath = GenerateOutputFileName(pageNum, extension);

        await File.WriteAllBytesAsync(outputPath, imageData, cancellationToken);

        _outputPaths.Add(outputPath);

        System.Diagnostics.Debug.WriteLine($"页面 {pageNum + 1} 已导出到: {outputPath}");
    }

    /// <summary>
    /// 导出所有页面为图像（兼容API）
    /// </summary>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>输出文件路径列表</returns>
    public async Task<List<string>> ExportAllPagesAsync(CancellationToken cancellationToken = default)
    {
        var imageDataList = await _imageExporter.ExportAllPagesToImageAsync(cancellationToken);
        var imageDataArray = imageDataList.ToArray();

        var extension = _imageExporter.GetFileExtension(_config.ImageFormat);

        for (int i = 0; i < imageDataArray.Length; i++)
        {
            var outputPath = GenerateOutputFileName(i, extension);
            await File.WriteAllBytesAsync(outputPath, imageDataArray[i], cancellationToken);
            _outputPaths.Add(outputPath);
        }

        return GetOutputPaths();
    }

    /// <summary>
    /// 获取页面数量
    /// </summary>
    /// <returns>页面数量</returns>
    public int GetPageCount()
    {
        Initialize();
        return _reader?.GetNumberOfPages() ?? 0;
    }
}

/// <summary>
/// 兼容旧名称，后续版本将移除。
/// </summary>
[System.Obsolete("Use OfdImageExporter instead. This shim will be removed in a future release.")]
public class ImageExporter : OfdImageExporter
{
    public ImageExporter(string ofdPath, string outputDir, float dpi = 150f) : base(ofdPath, outputDir, dpi) { }
    public ImageExporter(string ofdPath, string outputDir, SKEncodedImageFormat fmt, int quality = 100, float dpi = 150f) : base(ofdPath, outputDir, fmt, quality, dpi) { }
}
