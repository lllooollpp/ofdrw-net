using System;
using System.Threading;
using System.Threading.Tasks;
using iText.Kernel.Pdf;
using Microsoft.Extensions.Logging;
using OfdrwNet.Abstractions;
using OfdrwNet.Core.BasicType;
using OfdrwNet.Core.Annotation;

namespace OfdrwNet.Converter.Refactor;

/// <summary>
/// 注释提取器：从 PDF 中提取注释并转换为 OFD 注释对象集合。
/// 迁移自 ConvertHelper 内部 ExtractAnnotations / ConvertPdfAnnotationToOfd 系列方法。
/// </summary>
internal class AnnotationExtractor : IPdfContentExtractor
{
    private static int _nextAnnotationId = 1;
    private static int GetNextAnnotationId() => System.Threading.Interlocked.Increment(ref _nextAnnotationId);

    public Task ExtractAsync(PdfDocument pdfDoc, IOfdDocWriter ofd, ConvertHelper.PdfToOfdOptions options, ILogger? logger, CancellationToken token)
    {
        int totalPages = pdfDoc.GetNumberOfPages();
        logger?.LogDebug("[PDF2OFD][Annotation] PDF总页数: {TotalPages}", totalPages);

        for (int i = 1; i <= totalPages; i++)
        {
            token.ThrowIfCancellationRequested();
            if (options.PageFilter != null && !options.PageFilter(i))
            {
                logger?.LogDebug("[PDF2OFD][Annotation] Page {PageNum} 被过滤", i);
                continue;
            }

            var page = pdfDoc.GetPage(i);
            var annotations = page.GetAnnotations();
            if (annotations == null || annotations.Count == 0)
            {
                logger?.LogDebug("[PDF2OFD][Annotation] Page {PageNum} 未发现注释", i);
                continue;
            }

            logger?.LogDebug("[PDF2OFD][Annotation] Page {PageNum} 发现 {AnnotationCount} 个注释", i, annotations.Count);
            foreach (var annotation in annotations)
            {
                try
                {
                    var ofdAnnotation = ConvertPdfAnnotationToOfd(annotation, i, logger);
                    if (ofdAnnotation != null)
                    {
                        (ofd as OfdWriter)?.AddAnnotation(ofdAnnotation);
                    }
                }
                catch (Exception ex)
                {
                    logger?.LogWarning(ex, "[PDF2OFD][Annotation] Page {PageNum} 转换注释失败", i);
                }
            }
        }
        return Task.CompletedTask;
    }

    private static object? ConvertPdfAnnotationToOfd(iText.Kernel.Pdf.Annot.PdfAnnotation pdfAnnotation, int pageIndex, ILogger? logger)
    {
        if (pdfAnnotation == null) return null;
        try
        {
            var subtype = pdfAnnotation.GetSubtype();
            logger?.LogDebug("[PDF2OFD][Annotation] 转换注释类型: {Type}", subtype?.ToString());

            var rect = pdfAnnotation.GetRectangle();
            if (rect == null)
            {
                logger?.LogWarning("[PDF2OFD][Annotation] 注释缺少边界框，跳过");
                return null;
            }
            var rectangle = rect.ToRectangle();
            double x = rectangle.GetX() * ConvertHelper.Pt2Mm;
            double y = rectangle.GetY() * ConvertHelper.Pt2Mm;
            double width = rectangle.GetWidth() * ConvertHelper.Pt2Mm;
            double height = rectangle.GetHeight() * ConvertHelper.Pt2Mm;
            var boundary = new StBox(x, y, width, height);
            var annotationId = new StId(GetNextAnnotationId());
            var pageId = new StId(pageIndex);

            if (subtype != null)
            {
                var subtypeStr = subtype.ToString();
                switch (subtypeStr)
                {
                    case "/Highlight":
                        return ConvertHighlightAnnotation(pdfAnnotation, annotationId, pageId, boundary, logger);
                    case "/Text":
                        return ConvertTextAnnotation(pdfAnnotation, annotationId, pageId, boundary, logger);
                    case "/Link":
                        return ConvertLinkAnnotation(pdfAnnotation, annotationId, pageId, boundary, logger);
                    case "/Stamp":
                        return ConvertStampAnnotation(pdfAnnotation, annotationId, pageId, boundary, logger);
                    default:
                        logger?.LogWarning("[PDF2OFD][Annotation] 不支持的注释类型: {Type}", subtypeStr);
                        break;
                }
            }
            return null;
        }
        catch (Exception ex)
        {
            logger?.LogWarning(ex, "[PDF2OFD][Annotation] 转换注释失败");
            return null;
        }
    }

    private static object? ConvertHighlightAnnotation(iText.Kernel.Pdf.Annot.PdfAnnotation pdfAnnotation, StId annotationId, StId pageId, StBox boundary, ILogger? logger)
    {
        try
        {
            var rgbColorSpace = new OfdrwNet.Core.Resource.ColorSpace(new StId(1), OfdrwNet.Core.Resource.ColorSpaceType.RGB);
            var color = new OfdrwNet.Core.Resource.Color(new StId(1), rgbColorSpace)
            {
                Components = new double[] { 1.0, 1.0, 0.0 }
            };
            var colorArray = pdfAnnotation.GetPdfObject().GetAsArray(iText.Kernel.Pdf.PdfName.C);
            if (colorArray != null && colorArray.Size() >= 3)
            {
                try
                {
                    var r = colorArray.GetAsNumber(0).FloatValue();
                    var g = colorArray.GetAsNumber(1).FloatValue();
                    var b = colorArray.GetAsNumber(2).FloatValue();
                    color.Components = new double[] { r, g, b };
                }
                catch (Exception ex)
                {
                    logger?.LogDebug(ex, "[PDF2OFD][Annotation] 无法解析高亮颜色，使用默认黄色");
                }
            }
            var highlightAnnotation = new HighlightAnnotation(annotationId, pageId, boundary, color);
            var contents = pdfAnnotation.GetContents();
            if (contents != null && !string.IsNullOrEmpty(contents.ToString()))
            {
                highlightAnnotation.Content = contents.ToString();
            }
            var author = pdfAnnotation.GetPdfObject().GetAsString(iText.Kernel.Pdf.PdfName.T);
            if (author != null)
            {
                highlightAnnotation.Creator = author.ToString();
            }
            var title = pdfAnnotation.GetPdfObject().GetAsString(iText.Kernel.Pdf.PdfName.Subj);
            if (title != null)
            {
                highlightAnnotation.Title = title.ToString();
            }
            var creationDate = pdfAnnotation.GetPdfObject().GetAsString(iText.Kernel.Pdf.PdfName.CreationDate);
            if (creationDate != null)
            {
                try
                {
                    highlightAnnotation.CreationDate = ParsePdfDate(creationDate.ToString());
                }
                catch (Exception ex)
                {
                    logger?.LogDebug(ex, "[PDF2OFD][Annotation] 无法解析创建日期");
                }
            }
            highlightAnnotation.AddHighlightArea(boundary);
            logger?.LogDebug("[PDF2OFD][Annotation] 成功转换高亮注释: {Id}", annotationId);
            return highlightAnnotation;
        }
        catch (Exception ex)
        {
            logger?.LogWarning(ex, "[PDF2OFD][Annotation] 转换高亮注释失败");
            return null;
        }
    }

    private static object? ConvertTextAnnotation(iText.Kernel.Pdf.Annot.PdfAnnotation pdfAnnotation, StId annotationId, StId pageId, StBox boundary, ILogger? logger)
        => ConvertHighlightAnnotation(pdfAnnotation, annotationId, pageId, boundary, logger);

    private static object? ConvertLinkAnnotation(iText.Kernel.Pdf.Annot.PdfAnnotation pdfAnnotation, StId annotationId, StId pageId, StBox boundary, ILogger? logger)
    {
        try
        {
            var linkType = LinkType.Url;
            var target = "#";
            var action = pdfAnnotation.GetPdfObject().GetAsDictionary(iText.Kernel.Pdf.PdfName.A);
            if (action != null)
            {
                var actionType = action.GetAsName(iText.Kernel.Pdf.PdfName.S);
                if (actionType != null)
                {
                    var actionTypeStr = actionType.ToString();
                    if (actionTypeStr == "/URI")
                    {
                        var uri = action.GetAsString(iText.Kernel.Pdf.PdfName.URI);
                        if (uri != null)
                        {
                            linkType = LinkType.Url;
                            target = uri.ToString();
                        }
                    }
                    else if (actionTypeStr == "/GoTo")
                    {
                        var dest = action.Get(iText.Kernel.Pdf.PdfName.D);
                        if (dest != null)
                        {
                            linkType = LinkType.Page;
                            target = dest.ToString();
                        }
                    }
                }
            }
            var linkAnnotation = new LinkAnnotation(annotationId, pageId, boundary, linkType, target ?? "#");
            var contents = pdfAnnotation.GetContents();
            if (contents != null && !string.IsNullOrEmpty(contents.ToString()))
            {
                linkAnnotation.Content = contents.ToString();
            }
            var author = pdfAnnotation.GetPdfObject().GetAsString(iText.Kernel.Pdf.PdfName.T);
            if (author != null)
            {
                linkAnnotation.Creator = author.ToString();
            }
            var creationDate = pdfAnnotation.GetPdfObject().GetAsString(iText.Kernel.Pdf.PdfName.CreationDate);
            if (creationDate != null)
            {
                try
                {
                    linkAnnotation.CreationDate = ParsePdfDate(creationDate.ToString());
                }
                catch (Exception ex)
                {
                    logger?.LogDebug(ex, "[PDF2OFD][Annotation] 无法解析创建日期");
                }
            }
            logger?.LogDebug("[PDF2OFD][Annotation] 成功转换链接注释: {Id}", annotationId);
            return linkAnnotation;
        }
        catch (Exception ex)
        {
            logger?.LogWarning(ex, "[PDF2OFD][Annotation] 转换链接注释失败");
            return null;
        }
    }

    private static object? ConvertStampAnnotation(iText.Kernel.Pdf.Annot.PdfAnnotation pdfAnnotation, StId annotationId, StId pageId, StBox boundary, ILogger? logger)
    {
        logger?.LogDebug("[PDF2OFD][Annotation] 图章注释转换为高亮注释: {Id}", annotationId);
        return ConvertHighlightAnnotation(pdfAnnotation, annotationId, pageId, boundary, logger);
    }

    private static DateTime ParsePdfDate(string pdfDate)
    {
        if (string.IsNullOrEmpty(pdfDate) || !pdfDate.StartsWith("D:"))
            throw new ArgumentException("Invalid PDF date format");
        var dateStr = pdfDate.Substring(2);
        if (dateStr.Length < 14) throw new ArgumentException("PDF date string too short");
        var year = int.Parse(dateStr.Substring(0, 4));
        var month = int.Parse(dateStr.Substring(4, 2));
        var day = int.Parse(dateStr.Substring(6, 2));
        var hour = int.Parse(dateStr.Substring(8, 2));
        var minute = int.Parse(dateStr.Substring(10, 2));
        var second = int.Parse(dateStr.Substring(12, 2));
        return new DateTime(year, month, day, hour, minute, second, DateTimeKind.Local);
    }
}
