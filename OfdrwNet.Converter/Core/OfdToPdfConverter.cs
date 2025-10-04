using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using OfdrwNet.Reader;
using OfdrwNet.Converter.Export;
using OfdrwNet.Converter.Options;
using Microsoft.Extensions.Logging;

namespace OfdrwNet.Converter.Core;

/// <summary>
/// OFD 到 PDF 转换器
/// 负责处理所有 OFD 到 PDF 格式的转换操作
/// </summary>
public class OfdToPdfConverter
{
    public const double Pt2Mm = 25.4 / 72.0; // 点到毫米转换常数

    /// <summary>
    /// 转换使用库枚举（预留）
    /// </summary>
    public enum Lib
    {
        IText,
        PDFBox
    }

    /// <summary>
    /// 当前使用的转换库（默认 IText 语义）
    /// </summary>
    public static Lib CurrentLib { get; private set; } = Lib.IText;

    /// <summary>
    /// 使用 IText 兼容实现
    /// </summary>
    public static void UseIText() => CurrentLib = Lib.IText;

    /// <summary>
    /// 使用 PDFBox 兼容实现
    /// </summary>
    public static void UsePDFBox() => CurrentLib = Lib.PDFBox;

    #region 同步转换方法

    /// <summary>
    /// 将 OFD 流转换为 PDF 流
    /// </summary>
    public void ConvertToPdf(Stream input, Stream output, PdfExportOptions? options = null)
    {
        ConvertToPdfAsync(input, output, options).GetAwaiter().GetResult();
    }

    /// <summary>
    /// 将 OFD 流转换为 PDF 文件
    /// </summary>
    public void ConvertToPdf(Stream input, string outputPath, PdfExportOptions? options = null)
    {
        ConvertToPdfAsync(input, outputPath, options).GetAwaiter().GetResult();
    }

    /// <summary>
    /// 将 OFD 文件转换为 PDF 流
    /// </summary>
    public void ConvertToPdf(string inputPath, Stream output, PdfExportOptions? options = null)
    {
        ConvertToPdfAsync(inputPath, output, options).GetAwaiter().GetResult();
    }

    /// <summary>
    /// 将 OFD 文件转换为 PDF 文件
    /// </summary>
    public void ConvertToPdf(string inputPath, string outputPath, PdfExportOptions? options = null)
    {
        ConvertToPdfAsync(inputPath, outputPath, options).GetAwaiter().GetResult();
    }

    /// <summary>
    /// 从已解压的 OFD 目录转换为 PDF
    /// </summary>
    public void ConvertFromUnzipped(string unzippedPathRoot, string outputPath, bool deleteOnClose, PdfExportOptions? options = null)
    {
        ConvertFromUnzippedAsync(unzippedPathRoot, outputPath, deleteOnClose, options).GetAwaiter().GetResult();
    }

    #endregion

    #region 异步转换方法

    /// <summary>
    /// 异步将 OFD 流转换为 PDF 流
    /// </summary>
    public Task ConvertToPdfAsync(Stream input, Stream output, PdfExportOptions? options = null, CancellationToken token = default)
    {
        return ConvertInternalAsync(input, output, options, token);
    }

    /// <summary>
    /// 异步将 OFD 流转换为 PDF 文件
    /// </summary>
    public Task ConvertToPdfAsync(Stream input, string outputPath, PdfExportOptions? options = null, CancellationToken token = default)
    {
        return ConvertInternalAsync(input, outputPath, options, token);
    }

    /// <summary>
    /// 异步将 OFD 文件转换为 PDF 流
    /// </summary>
    public Task ConvertToPdfAsync(string inputPath, Stream output, PdfExportOptions? options = null, CancellationToken token = default)
    {
        return ConvertInternalAsync(inputPath, output, options, token);
    }

    /// <summary>
    /// 异步将 OFD 文件转换为 PDF 文件
    /// </summary>
    public Task ConvertToPdfAsync(string inputPath, string outputPath, PdfExportOptions? options = null, CancellationToken token = default)
    {
        return ConvertInternalAsync(inputPath, outputPath, options, token);
    }

    /// <summary>
    /// 从已解压的 OFD 目录异步转换为 PDF
    /// </summary>
    public async Task ConvertFromUnzippedAsync(string unzippedPathRoot, string outputPath, bool deleteOnClose, PdfExportOptions? options = null, CancellationToken token = default)
    {
        if (string.IsNullOrWhiteSpace(unzippedPathRoot) || !Directory.Exists(unzippedPathRoot))
            throw new ArgumentException("已解压目录不存在", nameof(unzippedPathRoot));

        string tempOfd = Path.ChangeExtension(Path.GetTempFileName(), ".ofd");
        string zipPath = tempOfd + ".zip";
        File.Move(tempOfd, zipPath);
        System.IO.Compression.ZipFile.CreateFromDirectory(unzippedPathRoot, zipPath);
        string ofdPath = Path.ChangeExtension(zipPath, ".ofd");
        File.Move(zipPath, ofdPath);

        try
        {
            await RunPdfExportAsync(ofdPath, outputPath, options, token);
        }
        finally
        {
            ConvertUtils.SafeDelete(ofdPath);
            if (deleteOnClose)
            {
                try
                {
                    Directory.Delete(unzippedPathRoot, true);
                }
                catch { /* ignore */ }
            }
        }
    }

    #endregion

    #region 内部实现

    /// <summary>
    /// 内部统一转换实现
    /// </summary>
    private Task ConvertInternalAsync(object input, object output, PdfExportOptions? options, CancellationToken token)
    {
        return Task.Run(async () =>
        {
            try
            {
                var (ofdFilePath, tempInputFile) = ConvertUtils.NormalizeInputToTempOfd(input);

                bool outputIsStream = output is Stream;
                string? targetPdfPath = outputIsStream ? Path.GetTempFileName() : ConvertUtils.NormalizeOutputPath(output);
                if (targetPdfPath == null)
                {
                    throw new ArgumentException("不支持的输出格式(output)，仅支持 Stream、string 文件路径");
                }

                await RunPdfExportAsync(ofdFilePath, targetPdfPath, options, token).ConfigureAwait(false);

                if (outputIsStream)
                {
                    using var fs = File.OpenRead(targetPdfPath);
                    fs.CopyTo((Stream)output);
                }

                ConvertUtils.SafeDelete(tempInputFile);
                if (outputIsStream)
                    ConvertUtils.SafeDelete(targetPdfPath);
            }
            catch (GeneralConvertException)
            {
                throw;
            }
            catch (Exception ex)
            {
                throw new GeneralConvertException("convert to pdf failed", ex);
            }
        }, token);
    }

    /// <summary>
    /// 执行 PDF 导出的核心逻辑
    /// </summary>
    private async Task RunPdfExportAsync(string ofdFilePath, string pdfOutputPath, PdfExportOptions? options, CancellationToken token)
    {
        Directory.CreateDirectory(Path.GetDirectoryName(Path.GetFullPath(pdfOutputPath)) ?? ".");

        int? start = options?.StartPage;
        int? end = options?.EndPage;
        float dpi = options?.Dpi is float d && d >= 72f ? d : 150f;

        // 依据库选择：当前两个枚举均走同一 iText PDFExporter
        var exporter = new PDFExporter(ofdFilePath, pdfOutputPath, dpi, options?.PreserveLayout ?? false, options?.Progress,
            options?.StatsJsonPath, options?.FontMapper, options?.EmbedFonts ?? false, options?.RealImageEmbedding ?? false, options?.PageFilter);

        // 构造要导出的页列表（1-based）
        List<int> pages;
        using (var tmpReader = new OfdReader(ofdFilePath))
        {
            int total = tmpReader.GetNumberOfPages();
            int s = Math.Clamp(start ?? 1, 1, total);
            int e = Math.Clamp(end ?? total, 1, total);
            if (s > e)
                (s, e) = (e, s);
            pages = new List<int>();
            for (int i = s; i <= e; i++)
            {
                if (options?.PageFilter == null || options.PageFilter(i))
                    pages.Add(i);
            }
            if (pages.Count == 0)
                throw new ArgumentException("页面过滤后无可导出页面");
        }

        int totalExport = pages.Count;
        int done = 0;
        foreach (var page1Based in pages)
        {
            token.ThrowIfCancellationRequested();
            // 为保持现有结构：首次调用时启动范围（最小->最大），但内部仍会全量；因此改为一次调用范围版本：
            // 简化：若是首次页则调用整体范围导出，然后跳出循环。
            if (page1Based == pages[0])
            {
                int first = pages[0];
                int last = pages[^1];
                await exporter.ExportAsync(first - 1, last - 1, token).ConfigureAwait(false);
                done = totalExport; // 由于范围内包含过滤页但 exporter 仍输出全部，后续可进一步实现跳页逻辑。当前进度直接标记完成。
                options?.Progress?.Report((done, totalExport));
                break;
            }
        }

        // 若需要更精细 per-page 进度，可未来改造 PDFExporter 以接受页列表。
    }

    #endregion
}
