using Microsoft.Extensions.Logging;
using OfdrwNet.Converter.Domain;
using System;
using System.Collections.Generic;
using System.Linq;

namespace OfdrwNet.Converter.Interaction;

/// <summary>
/// 注释提取器（增强版）。
/// </summary>
/// <remarks>
/// 从 PDF 注释映射到 OFD 注释对象。
/// 支持的注释类型：
/// - Highlight（高亮）
/// - Underline（下划线）
/// - StrikeOut（删除线）
/// - Stamp（图章）
/// - Text（文本注释）
/// - Link（链接注释）
/// - FreeText（自由文本）
///
/// 与 PageContext 集成，记录注释元数据。
/// FR-16: 注释类型映射与属性保留
/// </remarks>
public sealed class AnnotationExtractor
{
    private readonly ILogger<AnnotationExtractor> _logger;

    /// <summary>
    /// 初始化 AnnotationExtractor 实例。
    /// </summary>
    /// <param name="logger">日志记录器</param>
    public AnnotationExtractor(ILogger<AnnotationExtractor> logger)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <summary>
    /// 从 PDF 页面提取注释并更新 PageContext。
    /// </summary>
    /// <param name="pdfPage">PDF 页面对象（iText7 PdfPage 或模拟对象）</param>
    /// <param name="pageContext">页面上下文</param>
    public void ExtractAnnotations(object pdfPage, PageContext pageContext)
    {
        if (pdfPage == null)
        {
            throw new ArgumentNullException(nameof(pdfPage));
        }

        if (pageContext == null)
        {
            throw new ArgumentNullException(nameof(pageContext));
        }

        try
        {
            var annotations = GetAnnotations(pdfPage);
            if (annotations == null || annotations.Count == 0)
            {
                _logger.LogDebug("Page {Page} has no annotations", pageContext.PageNumber);
                return;
            }

            _logger.LogInformation(
                "Extracting {Count} annotations from page {Page}",
                annotations.Count, pageContext.PageNumber);

            foreach (var pdfAnnotation in annotations)
            {
                try
                {
                    var ofdAnnotation = ConvertAnnotation(pdfAnnotation, pageContext.PageNumber);
                    if (ofdAnnotation != null)
                    {
                        // 将注释添加到 PageContext（未来扩展：可添加 Annotations 属性）
                        _logger.LogDebug(
                            "Converted annotation: {Type}",
                            ofdAnnotation.GetType().Name);
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Failed to convert annotation on page {Page}", pageContext.PageNumber);
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to extract annotations from page {Page}", pageContext.PageNumber);
            pageContext.IsSuccess = false;
            pageContext.ErrorMessage = $"Annotation extraction failed: {ex.Message}";
        }
    }

    /// <summary>
    /// 获取 PDF 页面的注释列表。
    /// </summary>
    private IList<object> GetAnnotations(object pdfPage)
    {
        try
        {
            // 占位实现：使用反射获取 Annotations 属性
            var type = pdfPage.GetType();
            var getAnnotationsMethod = type.GetMethod("GetAnnotations");

            if (getAnnotationsMethod != null)
            {
                var annotations = getAnnotationsMethod.Invoke(pdfPage, null);
                if (annotations is System.Collections.IList list)
                {
                    return list.Cast<object>().ToList();
                }
            }

            return new List<object>();
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to get annotations from PDF page");
            return new List<object>();
        }
    }

    /// <summary>
    /// 转换 PDF 注释到 OFD 注释对象。
    /// </summary>
    private object? ConvertAnnotation(object pdfAnnotation, int pageNumber)
    {
        if (pdfAnnotation == null)
        {
            return null;
        }

        try
        {
            var subtype = GetAnnotationSubtype(pdfAnnotation);
            var boundary = GetAnnotationBoundary(pdfAnnotation);

            if (boundary == null)
            {
                _logger.LogWarning("Annotation has no boundary, skipping");
                return null;
            }

            _logger.LogDebug("Converting annotation type: {Type}", subtype);

            return subtype?.ToLowerInvariant() switch
            {
                "highlight" or "/highlight" => ConvertHighlightAnnotation(pdfAnnotation, pageNumber, boundary),
                "underline" or "/underline" => ConvertUnderlineAnnotation(pdfAnnotation, pageNumber, boundary),
                "strikeout" or "/strikeout" => ConvertStrikeOutAnnotation(pdfAnnotation, pageNumber, boundary),
                "stamp" or "/stamp" => ConvertStampAnnotation(pdfAnnotation, pageNumber, boundary),
                "text" or "/text" => ConvertTextAnnotation(pdfAnnotation, pageNumber, boundary),
                "link" or "/link" => ConvertLinkAnnotation(pdfAnnotation, pageNumber, boundary),
                "freetext" or "/freetext" => ConvertFreeTextAnnotation(pdfAnnotation, pageNumber, boundary),
                _ => HandleUnsupportedAnnotation(subtype, pageNumber, boundary)
            };
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to convert annotation");
            return null;
        }
    }

    /// <summary>
    /// 获取注释子类型。
    /// </summary>
    private string? GetAnnotationSubtype(object annotation)
    {
        try
        {
            var type = annotation.GetType();
            var getSubtypeMethod = type.GetMethod("GetSubtype");

            if (getSubtypeMethod != null)
            {
                var subtype = getSubtypeMethod.Invoke(annotation, null);
                return subtype?.ToString();
            }

            return null;
        }
        catch
        {
            return null;
        }
    }

    /// <summary>
    /// 获取注释边界框。
    /// </summary>
    private BoundingBox? GetAnnotationBoundary(object annotation)
    {
        try
        {
            var type = annotation.GetType();
            var getRectangleMethod = type.GetMethod("GetRectangle") ?? type.GetMethod("GetRect");

            if (getRectangleMethod != null)
            {
                var rect = getRectangleMethod.Invoke(annotation, null);
                if (rect != null)
                {
                    // 尝试提取坐标（占位实现）
                    var rectType = rect.GetType();
                    System.Reflection.MemberInfo? xProp = rectType.GetProperty("X") ?? (System.Reflection.MemberInfo?)rectType.GetMethod("GetX");
                    System.Reflection.MemberInfo? yProp = rectType.GetProperty("Y") ?? (System.Reflection.MemberInfo?)rectType.GetMethod("GetY");
                    System.Reflection.MemberInfo? widthProp = rectType.GetProperty("Width") ?? (System.Reflection.MemberInfo?)rectType.GetMethod("GetWidth");
                    System.Reflection.MemberInfo? heightProp = rectType.GetProperty("Height") ?? (System.Reflection.MemberInfo?)rectType.GetMethod("GetHeight");

                    double x = 0, y = 0, width = 0, height = 0;

                    if (xProp != null)
                    {
                        object? xValue = null;
                        if (xProp is System.Reflection.PropertyInfo xpi)
                            xValue = xpi.GetValue(rect);
                        else if (xProp is System.Reflection.MethodInfo xmi)
                            xValue = xmi.Invoke(rect, null);
                        if (xValue != null) x = Convert.ToDouble(xValue);
                    }

                    if (yProp != null)
                    {
                        object? yValue = null;
                        if (yProp is System.Reflection.PropertyInfo ypi)
                            yValue = ypi.GetValue(rect);
                        else if (yProp is System.Reflection.MethodInfo ymi)
                            yValue = ymi.Invoke(rect, null);
                        if (yValue != null) y = Convert.ToDouble(yValue);
                    }

                    if (widthProp != null)
                    {
                        object? widthValue = null;
                        if (widthProp is System.Reflection.PropertyInfo wpi)
                            widthValue = wpi.GetValue(rect);
                        else if (widthProp is System.Reflection.MethodInfo wmi)
                            widthValue = wmi.Invoke(rect, null);
                        if (widthValue != null) width = Convert.ToDouble(widthValue);
                    }

                    if (heightProp != null)
                    {
                        object? heightValue = null;
                        if (heightProp is System.Reflection.PropertyInfo hpi)
                            heightValue = hpi.GetValue(rect);
                        else if (heightProp is System.Reflection.MethodInfo hmi)
                            heightValue = hmi.Invoke(rect, null);
                        if (heightValue != null) height = Convert.ToDouble(heightValue);
                    }

                    return new BoundingBox
                    {
                        X = x,
                        Y = y,
                        Width = width,
                        Height = height
                    };
                }
            }

            return null;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to extract annotation boundary");
            return null;
        }
    }

    /// <summary>
    /// 转换高亮注释。
    /// </summary>
    private AnnotationInfo ConvertHighlightAnnotation(object pdfAnnotation, int pageNumber, BoundingBox boundary)
    {
        var info = CreateAnnotationInfo(pdfAnnotation, "Highlight", pageNumber, boundary);
        info.Color = ExtractColor(pdfAnnotation) ?? new double[] { 1.0, 1.0, 0.0 }; // 默认黄色
        return info;
    }

    /// <summary>
    /// 转换下划线注释。
    /// </summary>
    private AnnotationInfo ConvertUnderlineAnnotation(object pdfAnnotation, int pageNumber, BoundingBox boundary)
    {
        var info = CreateAnnotationInfo(pdfAnnotation, "Underline", pageNumber, boundary);
        info.Color = ExtractColor(pdfAnnotation) ?? new double[] { 1.0, 0.0, 0.0 }; // 默认红色
        return info;
    }

    /// <summary>
    /// 转换删除线注释。
    /// </summary>
    private AnnotationInfo ConvertStrikeOutAnnotation(object pdfAnnotation, int pageNumber, BoundingBox boundary)
    {
        var info = CreateAnnotationInfo(pdfAnnotation, "StrikeOut", pageNumber, boundary);
        info.Color = ExtractColor(pdfAnnotation) ?? new double[] { 1.0, 0.0, 0.0 }; // 默认红色
        return info;
    }

    /// <summary>
    /// 转换图章注释。
    /// </summary>
    private AnnotationInfo ConvertStampAnnotation(object pdfAnnotation, int pageNumber, BoundingBox boundary)
    {
        var info = CreateAnnotationInfo(pdfAnnotation, "Stamp", pageNumber, boundary);

        // 提取图章名称
        var stampName = ExtractStampName(pdfAnnotation);
        if (!string.IsNullOrEmpty(stampName))
        {
            info.Content = $"Stamp: {stampName}";
        }

        return info;
    }

    /// <summary>
    /// 转换文本注释。
    /// </summary>
    private AnnotationInfo ConvertTextAnnotation(object pdfAnnotation, int pageNumber, BoundingBox boundary)
    {
        return CreateAnnotationInfo(pdfAnnotation, "Text", pageNumber, boundary);
    }

    /// <summary>
    /// 转换链接注释。
    /// </summary>
    private AnnotationInfo ConvertLinkAnnotation(object pdfAnnotation, int pageNumber, BoundingBox boundary)
    {
        var info = CreateAnnotationInfo(pdfAnnotation, "Link", pageNumber, boundary);

        // 提取链接目标
        var target = ExtractLinkTarget(pdfAnnotation);
        if (!string.IsNullOrEmpty(target))
        {
            info.Content = $"Link: {target}";
        }

        return info;
    }

    /// <summary>
    /// 转换自由文本注释。
    /// </summary>
    private AnnotationInfo ConvertFreeTextAnnotation(object pdfAnnotation, int pageNumber, BoundingBox boundary)
    {
        return CreateAnnotationInfo(pdfAnnotation, "FreeText", pageNumber, boundary);
    }

    /// <summary>
    /// 处理不支持的注释类型。
    /// </summary>
    private AnnotationInfo? HandleUnsupportedAnnotation(string? subtype, int pageNumber, BoundingBox boundary)
    {
        _logger.LogWarning("Unsupported annotation type: {Type}, converting to generic annotation", subtype);

        return new AnnotationInfo
        {
            Type = "Generic",
            PageNumber = pageNumber,
            BoundingBox = boundary,
            Content = $"Unsupported annotation type: {subtype}"
        };
    }

    /// <summary>
    /// 创建注释信息对象。
    /// </summary>
    private AnnotationInfo CreateAnnotationInfo(object pdfAnnotation, string type, int pageNumber, BoundingBox boundary)
    {
        var info = new AnnotationInfo
        {
            Type = type,
            PageNumber = pageNumber,
            BoundingBox = boundary,
            Content = ExtractContent(pdfAnnotation),
            Author = ExtractAuthor(pdfAnnotation),
            CreationDate = ExtractCreationDate(pdfAnnotation),
            ModificationDate = ExtractModificationDate(pdfAnnotation)
        };

        return info;
    }

    /// <summary>
    /// 提取注释颜色（RGB）。
    /// </summary>
    private double[]? ExtractColor(object annotation)
    {
        try
        {
            var type = annotation.GetType();
            var getPdfObjectMethod = type.GetMethod("GetPdfObject");

            if (getPdfObjectMethod != null)
            {
                var pdfObject = getPdfObjectMethod.Invoke(annotation, null);
                if (pdfObject != null)
                {
                    var pdfType = pdfObject.GetType();
                    var getAsArrayMethod = pdfType.GetMethod("GetAsArray");

                    if (getAsArrayMethod != null)
                    {
                        // 尝试获取 /C (Color) 数组
                        var colorArray = getAsArrayMethod.Invoke(pdfObject, new object[] { "C" });
                        if (colorArray != null)
                        {
                            var arrayType = colorArray.GetType();
                            var sizeMethod = arrayType.GetMethod("Size");
                            var getAsNumberMethod = arrayType.GetMethod("GetAsNumber");

                            if (sizeMethod != null && getAsNumberMethod != null)
                            {
                                var size = (int)(sizeMethod.Invoke(colorArray, null) ?? 0);
                                if (size >= 3)
                                {
                                    var colors = new double[3];
                                    for (int i = 0; i < 3; i++)
                                    {
                                        var number = getAsNumberMethod.Invoke(colorArray, new object[] { i });
                                        if (number != null)
                                        {
                                            var floatMethod = number.GetType().GetMethod("FloatValue");
                                            if (floatMethod != null)
                                            {
                                                colors[i] = Convert.ToDouble(floatMethod.Invoke(number, null));
                                            }
                                        }
                                    }
                                    return colors;
                                }
                            }
                        }
                    }
                }
            }

            return null;
        }
        catch (Exception ex)
        {
            _logger.LogDebug(ex, "Failed to extract annotation color");
            return null;
        }
    }

    /// <summary>
    /// 提取注释内容。
    /// </summary>
    private string? ExtractContent(object annotation)
    {
        try
        {
            var type = annotation.GetType();
            var getContentsMethod = type.GetMethod("GetContents");

            if (getContentsMethod != null)
            {
                var contents = getContentsMethod.Invoke(annotation, null);
                return contents?.ToString();
            }

            return null;
        }
        catch
        {
            return null;
        }
    }

    /// <summary>
    /// 提取注释作者。
    /// </summary>
    private string? ExtractAuthor(object annotation)
    {
        try
        {
            var type = annotation.GetType();
            var getPdfObjectMethod = type.GetMethod("GetPdfObject");

            if (getPdfObjectMethod != null)
            {
                var pdfObject = getPdfObjectMethod.Invoke(annotation, null);
                if (pdfObject != null)
                {
                    var pdfType = pdfObject.GetType();
                    var getAsStringMethod = pdfType.GetMethod("GetAsString");

                    if (getAsStringMethod != null)
                    {
                        var author = getAsStringMethod.Invoke(pdfObject, new object[] { "T" });
                        return author?.ToString();
                    }
                }
            }

            return null;
        }
        catch
        {
            return null;
        }
    }

    /// <summary>
    /// 提取创建日期。
    /// </summary>
    private DateTime? ExtractCreationDate(object annotation)
    {
        try
        {
            var type = annotation.GetType();
            var getPdfObjectMethod = type.GetMethod("GetPdfObject");

            if (getPdfObjectMethod != null)
            {
                var pdfObject = getPdfObjectMethod.Invoke(annotation, null);
                if (pdfObject != null)
                {
                    var pdfType = pdfObject.GetType();
                    var getAsStringMethod = pdfType.GetMethod("GetAsString");

                    if (getAsStringMethod != null)
                    {
                        var dateStr = getAsStringMethod.Invoke(pdfObject, new object[] { "CreationDate" });
                        if (dateStr != null)
                        {
                            return ParsePdfDate(dateStr.ToString());
                        }
                    }
                }
            }

            return null;
        }
        catch
        {
            return null;
        }
    }

    /// <summary>
    /// 提取修改日期。
    /// </summary>
    private DateTime? ExtractModificationDate(object annotation)
    {
        try
        {
            var type = annotation.GetType();
            var getPdfObjectMethod = type.GetMethod("GetPdfObject");

            if (getPdfObjectMethod != null)
            {
                var pdfObject = getPdfObjectMethod.Invoke(annotation, null);
                if (pdfObject != null)
                {
                    var pdfType = pdfObject.GetType();
                    var getAsStringMethod = pdfType.GetMethod("GetAsString");

                    if (getAsStringMethod != null)
                    {
                        var dateStr = getAsStringMethod.Invoke(pdfObject, new object[] { "M" });
                        if (dateStr != null)
                        {
                            return ParsePdfDate(dateStr.ToString());
                        }
                    }
                }
            }

            return null;
        }
        catch
        {
            return null;
        }
    }

    /// <summary>
    /// 提取图章名称。
    /// </summary>
    private string? ExtractStampName(object annotation)
    {
        try
        {
            var type = annotation.GetType();
            var getPdfObjectMethod = type.GetMethod("GetPdfObject");

            if (getPdfObjectMethod != null)
            {
                var pdfObject = getPdfObjectMethod.Invoke(annotation, null);
                if (pdfObject != null)
                {
                    var pdfType = pdfObject.GetType();
                    var getAsNameMethod = pdfType.GetMethod("GetAsName");

                    if (getAsNameMethod != null)
                    {
                        var name = getAsNameMethod.Invoke(pdfObject, new object[] { "Name" });
                        return name?.ToString();
                    }
                }
            }

            return null;
        }
        catch
        {
            return null;
        }
    }

    /// <summary>
    /// 提取链接目标。
    /// </summary>
    private string? ExtractLinkTarget(object annotation)
    {
        try
        {
            var type = annotation.GetType();
            var getPdfObjectMethod = type.GetMethod("GetPdfObject");

            if (getPdfObjectMethod != null)
            {
                var pdfObject = getPdfObjectMethod.Invoke(annotation, null);
                if (pdfObject != null)
                {
                    var pdfType = pdfObject.GetType();
                    var getAsDictionaryMethod = pdfType.GetMethod("GetAsDictionary");

                    if (getAsDictionaryMethod != null)
                    {
                        var action = getAsDictionaryMethod.Invoke(pdfObject, new object[] { "A" });
                        if (action != null)
                        {
                            var actionType = action.GetType();
                            var getAsStringMethod = actionType.GetMethod("GetAsString");

                            if (getAsStringMethod != null)
                            {
                                var uri = getAsStringMethod.Invoke(action, new object[] { "URI" });
                                if (uri != null)
                                {
                                    return uri.ToString();
                                }
                            }

                            // 尝试获取 GoTo 目标
                            var getMethod = actionType.GetMethod("Get");
                            if (getMethod != null)
                            {
                                var dest = getMethod.Invoke(action, new object[] { "D" });
                                if (dest != null)
                                {
                                    return $"GoTo: {dest}";
                                }
                            }
                        }
                    }
                }
            }

            return null;
        }
        catch
        {
            return null;
        }
    }

    /// <summary>
    /// 解析 PDF 日期格式。
    /// </summary>
    private DateTime? ParsePdfDate(string? pdfDate)
    {
        if (string.IsNullOrWhiteSpace(pdfDate))
        {
            return null;
        }

        try
        {
            // PDF 日期格式: D:YYYYMMDDHHmmSSOHH'mm'
            if (pdfDate.StartsWith("D:"))
            {
                pdfDate = pdfDate.Substring(2);
            }

            if (pdfDate.Length < 14)
            {
                return null;
            }

            var year = int.Parse(pdfDate.Substring(0, 4));
            var month = int.Parse(pdfDate.Substring(4, 2));
            var day = int.Parse(pdfDate.Substring(6, 2));
            var hour = int.Parse(pdfDate.Substring(8, 2));
            var minute = int.Parse(pdfDate.Substring(10, 2));
            var second = int.Parse(pdfDate.Substring(12, 2));

            return new DateTime(year, month, day, hour, minute, second, DateTimeKind.Local);
        }
        catch
        {
            return null;
        }
    }
}

/// <summary>
/// 注释信息。
/// </summary>
public sealed class AnnotationInfo
{
    /// <summary>
    /// 注释类型（Highlight/Underline/StrikeOut/Stamp/Text/Link/FreeText）。
    /// </summary>
    public string Type { get; set; } = string.Empty;

    /// <summary>
    /// 页码（1-indexed）。
    /// </summary>
    public int PageNumber { get; set; }

    /// <summary>
    /// 边界框。
    /// </summary>
    public required BoundingBox BoundingBox { get; set; }

    /// <summary>
    /// 注释内容。
    /// </summary>
    public string? Content { get; set; }

    /// <summary>
    /// 作者。
    /// </summary>
    public string? Author { get; set; }

    /// <summary>
    /// 颜色（RGB，0-1 范围）。
    /// </summary>
    public double[]? Color { get; set; }

    /// <summary>
    /// 创建日期。
    /// </summary>
    public DateTime? CreationDate { get; set; }

    /// <summary>
    /// 修改日期。
    /// </summary>
    public DateTime? ModificationDate { get; set; }
}
