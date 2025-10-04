using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using iText.Kernel.Pdf;
using Microsoft.Extensions.Logging;
using OfdrwNet.Converter.Options;
using OfdrwNet.Abstractions;
using OfdrwNet.Font;

namespace OfdrwNet.Converter.Refactor;

/// <summary>
/// 负责提取 PDF 页面上的字体并写入 OFD。
/// 使用 OfdrwNet.Font 模块完成字体处理和提取。
/// </summary>
internal sealed class PdfFontExtractor : IPdfContentExtractor
{
    private readonly OfdrwNet.Font.PdfFontExtractor _fontExtractor = new();

    /// <summary>
    /// 获取已提取的字体映射（兼容性接口）
    /// </summary>
    public IReadOnlyDictionary<string, string?> ExtractedFonts =>
        _fontExtractor.ExtractedFonts.ToDictionary(kv => kv.Key, kv => kv.Value.TempFilePath);

    public async Task ExtractAsync(PdfDocument pdfDoc, IOfdDocWriter ofd, PdfToOfdOptions options, ILogger? logger, CancellationToken token)
    {
        if (pdfDoc == null) throw new ArgumentNullException(nameof(pdfDoc));
        if (ofd is not OfdWriter ofdWriter)
        {
            logger?.LogWarning("[PDF2OFD][Font] 当前 writer 无法写入字体资源，提取被跳过");
            return;
        }

        // 创建字体提取选项
        var fontOptions = new FontExtractionOptions
        {
            ExtractAndEmbedFonts = options.ExtractAndEmbedFonts,
            NormalizeSubsetFontName = options.NormalizeSubsetFontName,
            PageFilter = options.PageFilter
        };

        // 使用 ofdrw.Font 模块提取字体
        await _fontExtractor.ExtractFontsAsync(pdfDoc, fontOptions, logger, token);

        // 注册字体到 OFD
        _fontExtractor.RegisterFontsToOfd(ofdWriter, logger);
    }

    /// <summary>
    /// 清理临时字体文件
    /// </summary>
    /// <param name="logger">日志记录器</param>
    public void CleanupTempFiles(ILogger? logger)
    {
        _fontExtractor.CleanupTempFiles(logger);
    }
}
