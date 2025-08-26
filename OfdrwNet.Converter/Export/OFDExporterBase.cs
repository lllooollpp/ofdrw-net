using OfdrwNet.Reader;
using OfdrwNet.Reader.Model;

namespace OfdrwNet.Converter.Export;

/// <summary>
/// OFD导出器抽象基类
/// 提供OFD文档读取和页面处理的通用功能
/// </summary>
public abstract class OFDExporterBase : IOFDExporter
{
    protected readonly string _ofdPath;
    protected readonly string _outputDir;
    protected OfdReader? _reader;
    protected readonly List<string> _outputPaths;
    protected bool _disposed = false;

    /// <summary>
    /// 构造函数
    /// </summary>
    /// <param name="ofdPath">OFD文件路径</param>
    /// <param name="outputDir">输出目录路径</param>
    protected OFDExporterBase(string ofdPath, string outputDir)
    {
        _ofdPath = ofdPath ?? throw new ArgumentNullException(nameof(ofdPath));
        _outputDir = outputDir ?? throw new ArgumentNullException(nameof(outputDir));
        _outputPaths = new List<string>();

        if (!File.Exists(_ofdPath))
        {
            throw new FileNotFoundException($"OFD文件不存在: {_ofdPath}");
        }

        // 确保输出目录存在
        if (!Directory.Exists(_outputDir))
        {
            Directory.CreateDirectory(_outputDir);
        }
    }

    /// <summary>
    /// 初始化OFD读取器
    /// </summary>
    protected virtual void Initialize()
    {
        if (_reader == null)
        {
            _reader = new OfdReader(_ofdPath);
        }
    }

    /// <summary>
    /// 导出所有页面
    /// </summary>
    public virtual async Task ExportAsync(CancellationToken cancellationToken = default)
    {
        Initialize();
        
        var pageCount = _reader!.GetNumberOfPages();
        await ExportAsync(0, pageCount - 1, cancellationToken);
    }

    /// <summary>
    /// 导出指定页面
    /// </summary>
    public virtual async Task ExportAsync(int pageNum, CancellationToken cancellationToken = default)
    {
        Initialize();
        
        ValidatePageNumber(pageNum);
        await ExportPageAsync(pageNum, cancellationToken);
    }

    /// <summary>
    /// 导出指定页面范围
    /// </summary>
    public virtual async Task ExportAsync(int startPageNum, int endPageNum, CancellationToken cancellationToken = default)
    {
        Initialize();
        
        ValidatePageRange(startPageNum, endPageNum);
        
        for (int pageNum = startPageNum; pageNum <= endPageNum; pageNum++)
        {
            cancellationToken.ThrowIfCancellationRequested();
            await ExportPageAsync(pageNum, cancellationToken);
        }
    }

    /// <summary>
    /// 导出单个页面的抽象方法
    /// </summary>
    /// <param name="pageNum">页码（从0开始）</param>
    /// <param name="cancellationToken">取消令牌</param>
    protected abstract Task ExportPageAsync(int pageNum, CancellationToken cancellationToken);

    /// <summary>
    /// 获取输出文件路径
    /// </summary>
    public virtual List<string> GetOutputPaths()
    {
        return new List<string>(_outputPaths);
    }

    /// <summary>
    /// 获取页面信息
    /// </summary>
    /// <param name="pageNum">页码（从0开始）</param>
    /// <returns>页面信息</returns>
    protected PageInfo GetPageInfo(int pageNum)
    {
        return _reader!.GetPageInfo(pageNum + 1); // OfdReader使用1基索引
    }

    /// <summary>
    /// 验证页码有效性
    /// </summary>
    /// <param name="pageNum">页码</param>
    protected void ValidatePageNumber(int pageNum)
    {
        if (pageNum < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(pageNum), "页码不能小于0");
        }

        var pageCount = _reader!.GetNumberOfPages();
        if (pageNum >= pageCount)
        {
            throw new ArgumentOutOfRangeException(nameof(pageNum), $"页码{pageNum}超出范围，总页数为{pageCount}");
        }
    }

    /// <summary>
    /// 验证页码范围有效性
    /// </summary>
    /// <param name="startPageNum">起始页码</param>
    /// <param name="endPageNum">结束页码</param>
    protected void ValidatePageRange(int startPageNum, int endPageNum)
    {
        if (startPageNum < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(startPageNum), "起始页码不能小于0");
        }

        if (endPageNum < startPageNum)
        {
            throw new ArgumentException("结束页码不能小于起始页码");
        }

        var pageCount = _reader!.GetNumberOfPages();
        if (endPageNum >= pageCount)
        {
            throw new ArgumentOutOfRangeException(nameof(endPageNum), $"结束页码{endPageNum}超出范围，总页数为{pageCount}");
        }
    }

    /// <summary>
    /// 生成输出文件名
    /// </summary>
    /// <param name="pageNum">页码</param>
    /// <param name="extension">文件扩展名</param>
    /// <returns>完整的输出文件路径</returns>
    protected string GenerateOutputFileName(int pageNum, string extension)
    {
        var fileName = $"{Path.GetFileNameWithoutExtension(_ofdPath)}_page_{pageNum + 1:D3}{extension}";
        return Path.Combine(_outputDir, fileName);
    }

    /// <summary>
    /// 生成单文件输出名称
    /// </summary>
    /// <param name="extension">文件扩展名</param>
    /// <returns>完整的输出文件路径</returns>
    protected string GenerateSingleOutputFileName(string extension)
    {
        var fileName = $"{Path.GetFileNameWithoutExtension(_ofdPath)}{extension}";
        return Path.Combine(_outputDir, fileName);
    }

    /// <summary>
    /// 释放资源
    /// </summary>
    public virtual void Dispose()
    {
        if (!_disposed)
        {
            _reader?.Dispose();
            _disposed = true;
        }
    }
}