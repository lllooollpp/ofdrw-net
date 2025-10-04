using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using iText.Kernel.Pdf;
using iText.Kernel.Pdf.Canvas.Parser;
using Microsoft.Extensions.Logging;
using OfdrwNet.Converter.Options;
using OfdrwNet.Abstractions;

namespace OfdrwNet.Converter.Refactor;

internal class VectorExtractor : IPdfContentExtractor
{
    public Task ExtractAsync(PdfDocument pdfDoc, IOfdDocWriter ofd, PdfToOfdOptions options, ILogger? logger, System.Threading.CancellationToken token)
    {
        logger?.LogDebug("[PDF2OFD][Vector] PDF总页数: {Total}", pdfDoc.GetNumberOfPages());

        // T074: 检查是否启用颜色转换和布局检测
        var hasColorConverter = options.ColorConverter != null && options.EnableColorValidation;
        if (hasColorConverter)
        {
            logger?.LogDebug("[PDF2OFD][Vector] 颜色转换已启用");
        }

        int totalPages = pdfDoc.GetNumberOfPages();
        if (options.MaxDegreeOfParallelism <= 1)
        {
            for (int i = 1; i <= totalPages; i++)
            {
                token.ThrowIfCancellationRequested();
                if (options.PageFilter != null && !options.PageFilter(i)) { logger?.LogDebug("[PDF2OFD][Vector] Page {P} 被过滤", i); continue; }
                ProcessPage(pdfDoc, ofd, logger, i, options);
            }
        }
        else
        {
            logger?.LogInformation("[PDF2OFD][Vector] 并行处理启用 MaxDOP={D}", options.MaxDegreeOfParallelism);
            var pages = Enumerable.Range(1, totalPages).Where(p => options.PageFilter == null || options.PageFilter(p)).ToList();
            var parallelOptions = new ParallelOptions { MaxDegreeOfParallelism = options.MaxDegreeOfParallelism, CancellationToken = token };
            var pathsPerPage = new System.Collections.Concurrent.ConcurrentDictionary<int, List<OfdPath>>();
            Parallel.ForEach(pages, parallelOptions, i =>
            {
                try
                {
                    var page = pdfDoc.GetPage(i);
                    var pageHeightPt = page.GetPageSize().GetHeight();
                    var listener = new VectorPathListener(logger, pageHeightPt);
                    new PdfCanvasProcessor(listener).ProcessPageContent(page);
                    var paths = listener.GetPaths();
                    foreach (var p in paths)
                    {
                        p.Page = i;
                        // T074: 矢量路径颜色转换 (如果启用)
                        if (hasColorConverter)
                        {
                            // TODO: 对p.StrokeColor和p.FillColor进行sRGB转换
                            // 当前作为集成占位
                        }
                    }
                    pathsPerPage[i] = paths;
                }
                catch (Exception ex)
                {
                    logger?.LogError(ex, "[PDF2OFD][Vector] Page {P} 并行处理失败", i);
                    throw;
                }
            });
            foreach (var kv in pathsPerPage.OrderBy(k => k.Key))
            {
                foreach (var path in kv.Value) (ofd as OfdWriter)?.AddPath(path);
            }
        }
        return Task.CompletedTask;
    }

    private static void ProcessPage(PdfDocument pdfDoc, IOfdDocWriter ofd, ILogger? logger, int pageNum, PdfToOfdOptions options)
    {
        var page = pdfDoc.GetPage(pageNum);
        var pageHeightPt = page.GetPageSize().GetHeight();
        var listener = new VectorPathListener(logger, pageHeightPt);
        new PdfCanvasProcessor(listener).ProcessPageContent(page);
        var paths = listener.GetPaths();

        logger?.LogInformation("[PDF2OFD][Vector] Page {Page} 提取到 {Count} 条路径", pageNum, paths.Count);

        if (paths.Count == 0) return;

        // T074: 矢量路径颜色转换 (如果启用)
        var hasColorConverter = options.ColorConverter != null && options.EnableColorValidation;

        foreach (var p in paths)
        {
            p.Page = pageNum;

            logger?.LogInformation("[PDF2OFD][Vector] Page {Page} 路径详情: X={X}, Y={Y}, W={W}, H={H}, Stroke={Stroke}, Fill={Fill}, LineWidth={LW}, StrokeColor={SC}, FillColor={FC}, PathData前30字符={PathData}",
                pageNum, p.X, p.Y, p.Width, p.Height, p.Stroke, p.Fill, p.LineWidth, p.StrokeColor, p.FillColor,
                p.PathData != null && p.PathData.Length > 30 ? p.PathData.Substring(0, 30) : p.PathData);

            // T074: 颜色转换集成占位
            if (hasColorConverter)
            {
                // TODO: 实际实现需要:
                // 1. 从path获取StrokeColor/FillColor
                // 2. 如果是CMYK/RGB,调用ColorConverter.ConvertAsync
                // 3. 更新path的颜色属性
                logger?.LogDebug("[PDF2OFD][Vector] Page {Page} ColorConverter可用,待实现颜色转换", pageNum);
            }

            (ofd as OfdWriter)?.AddPath(p);
        }
    }
}
