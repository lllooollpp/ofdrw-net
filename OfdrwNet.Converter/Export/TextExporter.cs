using System.Text;
using OfdrwNet.Core;
using OfdrwNet.Text;

namespace OfdrwNet.Converter.Export;

/// <summary>
/// 文本导出器
/// 对应 Java 版本的 org.ofdrw.converter.export.TextExporter
/// 从OFD文档中提取纯文本内容
/// </summary>
public class TextExporter : OFDExporterBase
{
    private readonly Encoding _encoding;
    private readonly ITextExtractor _textExtractor;
    private readonly string _lineBreak;

    /// <summary>
    /// 构造函数
    /// </summary>
    /// <param name="ofdPath">OFD文件路径</param>
    /// <param name="outputPath">输出文本文件路径</param>
    /// <param name="encoding">文本编码，默认UTF-8</param>
    /// <param name="textExtractor">文本提取器，如果为null则使用默认实现</param>
    public TextExporter(string ofdPath, string outputPath, Encoding? encoding = null, ITextExtractor? textExtractor = null)
        : base(ofdPath, Path.GetDirectoryName(outputPath) ?? ".")
    {
        _encoding = encoding ?? Encoding.UTF8;
        _lineBreak = Environment.NewLine;

        // 创建文本提取器，传入获取页面信息的委托
        _textExtractor = textExtractor ?? CreateDefaultTextExtractor();

        // 设置输出文件路径
        _outputPaths.Add(outputPath);
    }

    /// <summary>
    /// 创建默认的文本提取器
    /// </summary>
    /// <returns>文本提取器实例</returns>
    private ITextExtractor CreateDefaultTextExtractor()
    {
        // 使用 ofdrw.Text 项目中的 TextExtractor，传入获取页面信息和页数的委托
        return new TextExtractor(
            getPageInfo: GetPageInfo,
            getPageCount: () => _reader?.GetNumberOfPages() ?? 0
        );
    }

    /// <summary>
    /// 导出所有页面的文本
    /// </summary>
    public override async Task ExportAsync(CancellationToken cancellationToken = default)
    {
        Initialize();

        var allText = await _textExtractor.ExtractAllTextAsync(cancellationToken);

        // 写入文件
        var outputPath = _outputPaths[0];
        await File.WriteAllTextAsync(outputPath, allText, _encoding, cancellationToken);

        System.Diagnostics.Debug.WriteLine($"文本已导出到: {outputPath}");
    }

    /// <summary>
    /// 导出单个页面（内部实现）
    /// </summary>
    protected override async Task ExportPageAsync(int pageNum, CancellationToken cancellationToken)
    {
        var pageText = await _textExtractor.ExtractPageTextAsync(pageNum, cancellationToken);

        var outputPath = GenerateOutputFileName(pageNum, ".txt");
        await File.WriteAllTextAsync(outputPath, pageText, _encoding, cancellationToken);

        // 更新输出路径列表
        if (!_outputPaths.Contains(outputPath))
        {
            _outputPaths.Add(outputPath);
        }

        System.Diagnostics.Debug.WriteLine($"页面 {pageNum + 1} 文本已导出到: {outputPath}");
    }
}
