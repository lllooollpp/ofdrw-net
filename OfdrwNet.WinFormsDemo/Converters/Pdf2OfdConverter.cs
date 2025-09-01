// 已弃用: Pdf2OfdConverter
// 说明: 此类的 PDF->OFD 功能已由统一的 ConvertHelper（双向转换入口）接管。
// 保留文件仅为兼容旧引用与编译通过，后续可彻底移除该文件及其引用。
// 若需要 PDF 元数据，可使用新的工具类（待实现）或直接通过 iText 读取。

#pragma warning disable 1591 // 抑制缺少 XML 文档警告（项目可能 TreatWarningsAsErrors）

using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using iText.Kernel.Pdf;
using Microsoft.Extensions.Logging;

namespace OfdrwNet.WinFormsDemo.Converters;

/// <summary>
/// [DEPRECATED] 旧版 PDF 转 OFD 转换器占位。请改用 ConvertHelper。
/// </summary>
[Obsolete("Pdf2OfdConverter 已弃用，请使用 ConvertHelper.ToPdf / 未来的 PdfToOfd 新实现（尚未合并）。")] 
public class Pdf2OfdConverter : IDisposable
{
    private readonly ILogger<Pdf2OfdConverter>? _logger;
    /// <summary>
    /// 构造函数（已弃用）。
    /// </summary>
    public Pdf2OfdConverter(ILogger<Pdf2OfdConverter> logger)
    {
        _logger = logger;
        _logger?.LogWarning("Pdf2OfdConverter 已弃用，将不再执行实际转换逻辑。请迁移到 ConvertHelper。");
    }

    /// <summary>
    /// 旧接口: 执行转换 (不再实现)。
    /// </summary>
    [Obsolete("使用 ConvertHelper 相关方法。")] 
    public Task<object?> ConvertAsync(string inputPdfPath, string outputOfdPath, CancellationToken cancellationToken = default)
    {
        throw new NotSupportedException("Pdf2OfdConverter 已弃用；请使用新的统一转换入口。");
    }

    /// <summary>
    /// 读取 PDF 文档基础信息（兼容 MainForm 旧调用）。
    /// </summary>
    public static PdfDocumentInfo GetDocumentInfo(string pdfPath)
    {
        try
        {
            using var r = new PdfReader(pdfPath);
            using var d = new PdfDocument(r);
            var info = d.GetDocumentInfo();
            return new PdfDocumentInfo
            {
                PageCount = d.GetNumberOfPages(),
                Title = info.GetTitle(),
                Author = info.GetAuthor(),
                Subject = info.GetSubject(),
                Creator = info.GetCreator(),
                Producer = info.GetProducer()
            };
        }
        catch
        {
            return new PdfDocumentInfo { PageCount = 0 };
        }
    }

    /// <summary>
    /// 无需释放资源，留空。
    /// </summary>
    public void Dispose() { }
}

/// <summary>
/// PDF 文档元信息（兼容旧代码）。
/// </summary>
public class PdfDocumentInfo 
{ 
    public int PageCount { get; set; } 
    public string? Title { get; set; } 
    public string? Author { get; set; } 
    public string? Subject { get; set; } 
    public string? Creator { get; set; } 
    public string? Producer { get; set; } 
}

#pragma warning restore 1591