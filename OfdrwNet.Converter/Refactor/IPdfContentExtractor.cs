using iText.Kernel.Pdf;
using Microsoft.Extensions.Logging;
using OfdrwNet.Converter.Options;
using OfdrwNet.Abstractions;
using System.Threading;
using System.Threading.Tasks;

namespace OfdrwNet.Converter.Refactor;

/// <summary>
/// PDF -> OFD 各类内容提取器接口（单一职责拆分类）。
/// </summary>
internal interface IPdfContentExtractor
{
    /// <summary>
    /// 执行提取，将内容写入 OFD Writer。
    /// </summary>
    Task ExtractAsync(PdfDocument pdfDoc, IOfdDocWriter ofd, PdfToOfdOptions options, ILogger? logger, CancellationToken token);
}
