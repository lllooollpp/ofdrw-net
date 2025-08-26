using OfdrwNet.Graphics;
using OfdrwNet.Layout;

namespace OfdrwNet.Converter.OfdConverter;

/// <summary>
/// OFD转换器基类
/// 提供转换为OFD格式的通用功能
/// </summary>
public abstract class OFDConverterBase : IOFDConverter
{
    protected readonly string _outputPath;
    protected PageLayout _pageLayout;
    protected OFDGraphicsDocument? _ofdDocument;
    protected bool _disposed = false;

    /// <summary>
    /// 构造函数
    /// </summary>
    /// <param name="outputPath">输出OFD文件路径</param>
    protected OFDConverterBase(string outputPath)
    {
        _outputPath = outputPath ?? throw new ArgumentNullException(nameof(outputPath));
        _pageLayout = PageLayout.A4(); // 默认A4页面

        // 确保输出目录存在
        var outputDir = Path.GetDirectoryName(_outputPath);
        if (!string.IsNullOrEmpty(outputDir) && !Directory.Exists(outputDir))
        {
            Directory.CreateDirectory(outputDir);
        }
    }

    /// <summary>
    /// 设置页面布局
    /// </summary>
    public virtual void SetPageLayout(PageLayout pageLayout)
    {
        _pageLayout = pageLayout ?? throw new ArgumentNullException(nameof(pageLayout));
    }

    /// <summary>
    /// 获取输出文件路径
    /// </summary>
    public virtual string GetOutputPath() => _outputPath;

    /// <summary>
    /// 转换文档
    /// </summary>
    public abstract Task ConvertAsync(string inputPath, CancellationToken cancellationToken = default);

    /// <summary>
    /// 转换指定页面范围
    /// </summary>
    public virtual async Task ConvertAsync(string inputPath, int startPage, int endPage, CancellationToken cancellationToken = default)
    {
        // 默认实现：转换所有页面然后裁剪
        // 子类可以重写此方法以提供更高效的实现
        await ConvertAsync(inputPath, cancellationToken);
    }

    /// <summary>
    /// 初始化OFD文档
    /// </summary>
    protected virtual void InitializeOfdDocument()
    {
        if (_ofdDocument == null)
        {
            _ofdDocument = new OFDGraphicsDocument(_outputPath);
        }
    }

    /// <summary>
    /// 完成转换并保存文档
    /// </summary>
    protected virtual async Task FinalizeConversion()
    {
        if (_ofdDocument != null)
        {
            await _ofdDocument.SaveAsync();
            Console.WriteLine($"OFD文档已保存到: {_outputPath}");
        }
    }

    /// <summary>
    /// 验证输入文件
    /// </summary>
    /// <param name="inputPath">输入文件路径</param>
    protected virtual void ValidateInputFile(string inputPath)
    {
        if (string.IsNullOrEmpty(inputPath))
        {
            throw new ArgumentException("输入文件路径不能为空", nameof(inputPath));
        }

        if (!File.Exists(inputPath))
        {
            throw new FileNotFoundException($"输入文件不存在: {inputPath}");
        }
    }

    /// <summary>
    /// 创建新页面
    /// </summary>
    /// <returns>OFD页面图形对象</returns>
    protected virtual OFDPageGraphics CreateNewPage()
    {
        InitializeOfdDocument();
        return _ofdDocument!.NewPage(_pageLayout.Width, _pageLayout.Height);
    }

    /// <summary>
    /// 创建指定尺寸的新页面
    /// </summary>
    /// <param name="width">页面宽度（毫米）</param>
    /// <param name="height">页面高度（毫米）</param>
    /// <returns>OFD页面图形对象</returns>
    protected virtual OFDPageGraphics CreateNewPage(double width, double height)
    {
        InitializeOfdDocument();
        return _ofdDocument!.NewPage(width, height);
    }

    /// <summary>
    /// 释放资源
    /// </summary>
    public virtual void Dispose()
    {
        if (!_disposed)
        {
            _ofdDocument?.Dispose();
            _disposed = true;
        }
    }
}