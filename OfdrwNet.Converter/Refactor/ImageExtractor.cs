using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using iText.Kernel.Pdf;
using iText.Kernel.Pdf.Canvas.Parser;
using Microsoft.Extensions.Logging;
using OfdrwNet;
using OfdrwNet.Abstractions;
using OfdrwNet.Converter.Options;
using OfdrwNet.Image;
using PdfRectangle = iText.Kernel.Geom.Rectangle;

namespace OfdrwNet.Converter.Refactor;

/// <summary>
/// 负责提取 PDF 页面上的图片并写入 OFD。
/// 使用 OfdrwNet.Image 模块完成图像处理和提取。
/// </summary>
internal sealed class PdfImageExtractor : IPdfContentExtractor
{
    public Task ExtractAsync(PdfDocument pdfDoc, IOfdDocWriter ofd, PdfToOfdOptions options, ILogger? logger, CancellationToken token)
    {
        if (pdfDoc == null) throw new ArgumentNullException(nameof(pdfDoc));
        if (ofd is not OfdWriter ofdWriter)
        {
            logger?.LogWarning("[PDF2OFD][Image] 当前 writer 无法写入图片资源，提取被跳过");
            return Task.CompletedTask;
        }

        var imageOptions = new ImageProcessingOptions
        {
            MakeWhiteBackgroundTransparent = options.MakeWhiteBackgroundTransparent,
            WhiteThreshold = options.WhiteThreshold
        };

        int totalPages = pdfDoc.GetNumberOfPages();
        for (int pageNumber = 1; pageNumber <= totalPages; pageNumber++)
        {
            token.ThrowIfCancellationRequested();
            if (options.PageFilter != null && !options.PageFilter(pageNumber))
            {
                logger?.LogDebug("[PDF2OFD][Image] Page {Page} 被过滤", pageNumber);
                continue;
            }

            var page = pdfDoc.GetPage(pageNumber);
            var listener = new ImageRenderEventListener(pageNumber, page.GetPageSize(), imageOptions, logger);
            new PdfCanvasProcessor(listener).ProcessPageContent(page);

            var orderedImages = OfdrwNet.Image.PdfImageExtractor.OrderImages(listener.Images, options.ImageOrdering);
            foreach (var imageData in orderedImages)
            {
                // 转换为 OfdImage 格式
                var ofdImage = new OfdImage
                {
                    Page = imageData.Page,
                    X = imageData.X,
                    Y = imageData.Y,
                    Width = imageData.Width,
                    Height = imageData.Height,
                    ImageData = imageData.ImageData,
                    Format = imageData.Format,
                    CTM = imageData.CTM
                };
                ofdWriter.AddImage(ofdImage);
            }

            logger?.LogInformation("[PDF2OFD][Image] Page {Page} 输出 {Count} 张图片", pageNumber, listener.Images.Count);
        }

        return Task.CompletedTask;
    }


}
