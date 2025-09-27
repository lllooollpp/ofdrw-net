using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using iText.Kernel.Geom;
using iText.Kernel.Pdf;
using iText.Kernel.Pdf.Canvas.Parser;
using iText.Kernel.Pdf.Canvas.Parser.Data;
using iText.Kernel.Pdf.Canvas.Parser.Listener;
using Microsoft.Extensions.Logging;
using OfdrwNet.Abstractions;
using SkiaSharp;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Formats.Png;

namespace OfdrwNet.Converter.Refactor;

using OfdrwNet.Converter.Refactor.Utils;

// 重命名：原 ImageExtractor → PdfImageExtractor，更清晰来源方向（PDF -> OFD）
internal class PdfImageExtractor : IPdfContentExtractor
{
    public Task ExtractAsync(PdfDocument pdfDoc, IOfdDocWriter ofd, ConvertHelper.PdfToOfdOptions options, ILogger? logger, System.Threading.CancellationToken token)
    {
    if (!options.ExtractImage)
        {
            logger?.LogDebug("[PDF2OFD][Image] ExtractImage=false 跳过图片提取");
            return Task.CompletedTask;
        }
        int totalPages = pdfDoc.GetNumberOfPages();
        logger?.LogDebug("[PDF2OFD][Image] PDF总页数: {Total}", totalPages);
        if (options.MaxDegreeOfParallelism <= 1)
        {
            for (int i = 1; i <= totalPages; i++)
            {
                token.ThrowIfCancellationRequested();
                if (options.PageFilter != null && !options.PageFilter(i)) { logger?.LogDebug("[PDF2OFD][Image] Page {P} 被过滤", i); continue; }
                ProcessSinglePage(pdfDoc, ofd, options, logger, i);
            }
        }
        else
        {
            logger?.LogInformation("[PDF2OFD][Image] 并行处理启用 MaxDOP={D}", options.MaxDegreeOfParallelism);
            var pages = Enumerable.Range(1, totalPages).Where(p => options.PageFilter == null || options.PageFilter(p)).ToList();
            var parallelOptions = new ParallelOptions { MaxDegreeOfParallelism = options.MaxDegreeOfParallelism, CancellationToken = token };
            var imagesPerPage = new System.Collections.Concurrent.ConcurrentDictionary<int, List<OfdImage>>();
            Parallel.ForEach(pages, parallelOptions, i =>
            {
                try
                {
                    var page = pdfDoc.GetPage(i);
                    var listener = new InternalImageRenderListener(i, page.GetPageSize(), options, logger);
                    new PdfCanvasProcessor(listener).ProcessPageContent(page);
                    imagesPerPage[i] = listener.Images;
                }
                catch (Exception ex)
                {
                    logger?.LogError(ex, "[PDF2OFD][Image] Page {Page} 并行处理失败", i);
                    throw;
                }
            });
            foreach (var kv in imagesPerPage.OrderBy(k => k.Key))
            {
                foreach (var img in kv.Value)
                {
                    (ofd as OfdWriter)?.AddImage(img);
                }
            }
        }
        return Task.CompletedTask;
    }

    private static void ProcessSinglePage(PdfDocument pdfDoc, IOfdDocWriter ofd, ConvertHelper.PdfToOfdOptions options, ILogger? logger, int pageNum)
    {
        var page = pdfDoc.GetPage(pageNum);
    var listener = new InternalImageRenderListener(pageNum, page.GetPageSize(), options, logger);
        new PdfCanvasProcessor(listener).ProcessPageContent(page);
        foreach (var img in listener.Images) (ofd as OfdWriter)?.AddImage(img);
    }

    private class InternalImageRenderListener : IEventListener
    {
        private readonly int _pageNum;
    private readonly iText.Kernel.Geom.Rectangle _pageSize;
        private readonly ConvertHelper.PdfToOfdOptions _options;
        private readonly ILogger? _logger;
        public List<OfdImage> Images { get; } = new();
    public InternalImageRenderListener(int pageNum, iText.Kernel.Geom.Rectangle pageSize, ConvertHelper.PdfToOfdOptions options, ILogger? logger)
        { _pageNum = pageNum; _pageSize = pageSize; _options = options; _logger = logger; }
        public void EventOccurred(IEventData data, EventType type)
        {
            if (type != EventType.RENDER_IMAGE) return;
            var renderInfo = (ImageRenderInfo)data;
            var imageObject = renderInfo.GetImage();
            if (imageObject == null) { _logger?.LogWarning("[PDF2OFD][Image] Page {P} 空图片对象", _pageNum); return; }
            byte[]? imageBytes = null;
            try
            {
                try { imageBytes = imageObject.GetImageBytes(); }
                catch (iText.IO.Exceptions.IOException ex) when (ex.Message.Contains("color space") && ex.Message.Contains("not supported"))
                { imageBytes = HandleUnsupportedColorSpace(renderInfo, imageObject, ex); }
                catch (Exception exGeneral)
                { _logger?.LogWarning(exGeneral, "[PDF2OFD][Image] Page {Page} 初次解码失败，尝试强制", _pageNum); try { imageBytes = imageObject.GetImageBytes(true); } catch (Exception hard) { _logger?.LogError(hard, "[PDF2OFD][Image] Page {Page} 强制解码失败", _pageNum); return; } }
                if (imageBytes == null) imageBytes = ImageDecodeHelper.GetTransparentPng();
                var matrix = renderInfo.GetImageCtm();
                float w = matrix.Get(Matrix.I11); float h = matrix.Get(Matrix.I22); float x = matrix.Get(Matrix.I31); float y = matrix.Get(Matrix.I32);
                if (w < 0) { x += w; w = -w; } if (h < 0) { y += h; h = -h; }
                y = _pageSize.GetHeight() - (y + h);
                x = (float)(x * ConvertHelper.Pt2Mm); y = (float)(y * ConvertHelper.Pt2Mm); w = (float)(w * ConvertHelper.Pt2Mm); h = (float)(h * ConvertHelper.Pt2Mm);
                double[]? ctm = null;
                try
                {
                    var a = matrix.Get(Matrix.I11); var b = matrix.Get(Matrix.I12); var c = matrix.Get(Matrix.I21); var d = matrix.Get(Matrix.I22);
                    ctm = new double[]{ a * ConvertHelper.Pt2Mm, b * ConvertHelper.Pt2Mm, c * ConvertHelper.Pt2Mm, d * ConvertHelper.Pt2Mm, x, y };
                }
                catch { _logger?.LogDebug("[PDF2OFD][Image] Page {P} CTM 计算失败降级", _pageNum); }
                Images.Add(new OfdImage { Page=_pageNum, X=x, Y=y, Width=w, Height=h, ImageData=imageBytes, Format=imageObject.IdentifyImageFileExtension(), CTM=ctm });
            }
            catch (Exception ex)
            { _logger?.LogError(ex, "[PDF2OFD][Image] Page {Page} 提取异常", _pageNum); }
        }
        public ICollection<EventType> GetSupportedEvents() => new[]{ EventType.RENDER_IMAGE };

        private byte[] HandleUnsupportedColorSpace(ImageRenderInfo renderInfo, iText.Kernel.Pdf.Xobject.PdfImageXObject imageObject, Exception original)
        {
            _logger?.LogWarning(original, "[PDF2OFD][Image] Page {Page} 色彩空间不支持，进入回退", _pageNum);
            var pdfStream = imageObject.GetPdfObject();
            try
            {
                var csObj = pdfStream.Get(iText.Kernel.Pdf.PdfName.ColorSpace);
                string csDesc = csObj switch
                {
                    iText.Kernel.Pdf.PdfName n => n.ToString(),
                    iText.Kernel.Pdf.PdfArray arr => string.Join(" ", arr),
                    iText.Kernel.Pdf.PdfString s => s.ToString(),
                    _ => csObj?.GetType().Name ?? "<null>"
                };
                var filterObj = pdfStream.Get(iText.Kernel.Pdf.PdfName.Filter);
                string filterDesc = filterObj switch
                {
                    iText.Kernel.Pdf.PdfName n => n.ToString(),
                    iText.Kernel.Pdf.PdfArray arr => string.Join(",", arr),
                    _ => filterObj?.ToString() ?? "<null>"
                };
                _logger?.LogDebug("[PDF2OFD][Image] Page {Page} Unsupported CS detail CS={CS} Filter={Filter}", _pageNum, csDesc, filterDesc);
            } catch { }

            var rawBytes = pdfStream.GetBytes(true);
            var filterNames = ImageDecodeHelper.ExtractFilters(pdfStream);
            // DCT/JPX 快速路径
            if (filterNames.Contains("DCTDecode"))
            {
                var bytes = pdfStream.GetBytes(false);
                _logger?.LogInformation("[PDF2OFD][Image] Page {Page} 复用 JPEG 原始流 {Len} bytes", _pageNum, bytes.Length);
                return bytes;
            }
            if (filterNames.Contains("JPXDecode"))
            {
                var originalBytes = pdfStream.GetBytes(false);
                try
                {
                    using var sk = SKBitmap.Decode(originalBytes);
                    if (sk != null)
                    {
                        using var img = SKImage.FromBitmap(sk);
                        using var data = img.Encode(SKEncodedImageFormat.Png, 100);
                        _logger?.LogInformation("[PDF2OFD][Image] Page {Page} JPX->PNG 转码成功", _pageNum);
                        return data.ToArray();
                    }
                }
                catch (Exception exJpx)
                { _logger?.LogWarning(exJpx, "[PDF2OFD][Image] Page {Page} JPX 转码失败保留原始", _pageNum); }
                return originalBytes;
            }
            if (rawBytes?.Length > 0)
            {
                // 尝试 ImageSharp
                if (ImageDecodeHelper.TryImageSharp(rawBytes, out var png1)) return png1;
                if (ImageDecodeHelper.TrySkia(rawBytes, out var png2)) return png2;
                if (ImageDecodeHelper.TryHeuristicRebuild(pdfStream, rawBytes, out var rebuilt)) return rebuilt;
            }
            // 最后再硬解
            try { return imageObject.GetImageBytes(true); } catch { return ImageDecodeHelper.GetTransparentPng(); }
        }

    }
}
