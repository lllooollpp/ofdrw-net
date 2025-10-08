using System;
using System.Threading;
using System.Threading.Tasks;
using iText.Kernel.Pdf;
using Microsoft.Extensions.Logging;
using OfdrwNet;
using OfdrwNet.Abstractions;
using OfdrwNet.Converter.Options;
using OfdrwNet.Image;

namespace OfdrwNet.Converter.Refactor;

/// <summary>
/// 负责提取 PDF 页面上的图片并写入 OFD。
/// 使用 OfdrwNet.Image 模块完成图像处理和提取。
/// </summary>
internal sealed class PdfImageContentExtractor : IPdfContentExtractor
{
    public Task ExtractAsync(PdfDocument pdfDoc, IOfdDocWriter ofd, PdfToOfdOptions options, ILogger? logger, CancellationToken token)
    {
        if (pdfDoc == null) throw new ArgumentNullException(nameof(pdfDoc));
        if (ofd is not OfdWriter ofdWriter)
        {
            logger?.LogWarning("[PDF2OFD][Image] 当前 writer 无法写入图片资源，提取被跳过");
            return Task.CompletedTask;
        }

        var processingOptions = new ImageProcessingOptions
        {
            MakeWhiteBackgroundTransparent = options.MakeWhiteBackgroundTransparent,
            WhiteThreshold = options.WhiteThreshold
        };

        var extractedImages = PdfImageExtractor.ExtractDocumentImages(
            pdfDoc,
            processingOptions,
            options.PageFilter,
            options.ImageOrdering,
            logger,
            token);

        if (extractedImages.Count == 0)
        {
            logger?.LogDebug("[PDF2OFD][Image] 未捕获任何页面图片");
            return Task.CompletedTask;
        }

        foreach (var imageData in extractedImages)
        {
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

        logger?.LogInformation("[PDF2OFD][Image] 已写入 {Count} 张图片", extractedImages.Count);
        return Task.CompletedTask;
    }


}
