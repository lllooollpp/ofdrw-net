using System.Threading;
using System.Threading.Tasks;
using iText.Kernel.Pdf;
using Microsoft.Extensions.Logging;
using OfdrwNet.Abstractions;

namespace OfdrwNet.Converter.Refactor;

/// <summary>
/// 表单提取器（暂为空壳）。未来负责将 PDF AcroForm 字段转换为 OFD 表单结构。
/// 当前实现仅输出日志并返回完成，用于解耦 ConvertHelper。
/// </summary>
internal class FormExtractor : IPdfContentExtractor
{
    public Task ExtractAsync(PdfDocument pdfDoc, IOfdDocWriter ofd, ConvertHelper.PdfToOfdOptions options, ILogger? logger, CancellationToken token)
    {
        logger?.LogDebug("[PDF2OFD][Form] 当前版本未实现表单提取，空壳返回");
        return Task.CompletedTask;
    }
}
