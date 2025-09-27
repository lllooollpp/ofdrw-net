using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using iText.Kernel.Pdf;
using iText.Kernel.Pdf.Canvas.Parser;
using Microsoft.Extensions.Logging;
using OfdrwNet.Abstractions;

namespace OfdrwNet.Converter.Refactor;

internal class VectorExtractor : IPdfContentExtractor
{
    public Task ExtractAsync(PdfDocument pdfDoc, IOfdDocWriter ofd, ConvertHelper.PdfToOfdOptions options, ILogger? logger, System.Threading.CancellationToken token)
    {
        logger?.LogDebug("[PDF2OFD][Vector] PDF总页数: {Total}", pdfDoc.GetNumberOfPages());
        int totalPages = pdfDoc.GetNumberOfPages();
        if (options.MaxDegreeOfParallelism <= 1)
        {
            for (int i = 1; i <= totalPages; i++)
            {
                token.ThrowIfCancellationRequested();
                if (options.PageFilter != null && !options.PageFilter(i)) { logger?.LogDebug("[PDF2OFD][Vector] Page {P} 被过滤", i); continue; }
                ProcessPage(pdfDoc, ofd, logger, i);
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
                    var listener = new VectorPathListener(logger);
                    new PdfCanvasProcessor(listener).ProcessPageContent(pdfDoc.GetPage(i));
                    var paths = listener.GetPaths();
                    foreach (var p in paths) p.Page = i;
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

    private static void ProcessPage(PdfDocument pdfDoc, IOfdDocWriter ofd, ILogger? logger, int pageNum)
    {
        var listener = new VectorPathListener(logger);
        new PdfCanvasProcessor(listener).ProcessPageContent(pdfDoc.GetPage(pageNum));
        var paths = listener.GetPaths();
        if (paths.Count == 0) return;
        foreach (var p in paths) { p.Page = pageNum; (ofd as OfdWriter)?.AddPath(p); }
    }
}
