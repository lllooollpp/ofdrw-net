using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Linq;
using ImageMagick;
using iText.Kernel.Pdf;
using iText.Kernel.Pdf.Canvas.Parser;
using iText.Kernel.Pdf.Canvas.Parser.Data;
using iText.Kernel.Pdf.Canvas.Parser.Listener;
using iText.Kernel.Pdf.Xobject;
using Microsoft.Extensions.Logging;
using PdfRectangle = iText.Kernel.Geom.Rectangle;
using PdfMatrix = iText.Kernel.Geom.Matrix;

namespace OfdrwNet.Image;

/// <summary>
/// PDF 图像渲染事件监听器，用于从 PDF 中提取图像
/// </summary>
public sealed class ImageRenderEventListener : IEventListener
{
    private readonly List<OfdImageData> _images = new();
    private readonly int _pageNum;
    private readonly PdfRectangle _pageSize;
    private readonly ImageProcessingOptions _options;
    private readonly ILogger? _logger;

    public ImageRenderEventListener(int pageNum, PdfRectangle pageSize, ImageProcessingOptions options, ILogger? logger)
    {
        _pageNum = pageNum;
        _pageSize = pageSize;
        _options = options;
        _logger = logger;
    }

    public List<OfdImageData> Images => _images;

    public void EventOccurred(IEventData data, EventType type)
    {
        if (type != EventType.RENDER_IMAGE) return;

        var renderInfo = (ImageRenderInfo)data;
        var imageObject = renderInfo.GetImage();
        if (imageObject == null) return;

        try
        {
            byte[] imageBytes = ReadImageBytes(imageObject);
            var originalFormatHint = NormalizeFormat(imageObject.IdentifyImageFileExtension());
            string? formatHint = null;
            bool hasAlpha = false;
            bool whiteTransparencyApplied = false;

            var magickResult = ProcessWithMagick(imageObject, imageBytes, originalFormatHint);
            if (magickResult.Success)
            {
                imageBytes = magickResult.Bytes;
                formatHint = magickResult.Format;
                hasAlpha = magickResult.HasAlpha;

                if (magickResult.ColorConverted)
                {
                    _logger?.LogDebug("[PDF2OFD][Image][Magick] Page {Page} 颜色空间转换完成", _pageNum);
                }

                if (magickResult.MaskApplied)
                {
                    _logger?.LogDebug("[PDF2OFD][Image][Magick] Page {Page} SoftMask 已合成", _pageNum);
                }
            }
            else if (!string.IsNullOrEmpty(magickResult.FallbackReason))
            {
                _logger?.LogWarning("[PDF2OFD][Image][Magick] Page {Page} 回退到原始图像: {Reason}", _pageNum, magickResult.FallbackReason);
            }

            bool isTiff = string.Equals(originalFormatHint, "TIFF", StringComparison.OrdinalIgnoreCase) ||
                           string.Equals(originalFormatHint, "TIF", StringComparison.OrdinalIgnoreCase);

            if (_options.MakeWhiteBackgroundTransparent)
            {
                if (!hasAlpha)
                {
                    try
                    {
                        var processed = SimpleWhiteToTransparent(imageBytes, _options.WhiteThreshold, isTiff, out bool changed);
                        if (changed)
                        {
                            imageBytes = processed;
                            whiteTransparencyApplied = true;
                            hasAlpha = true;
                            _logger?.LogDebug("[PDF2OFD][Image][Alpha] Page {Page} 白底->透明成功{TiffNote}", _pageNum, isTiff ? " (TIFF 反相)" : string.Empty);
                        }
                        else
                        {
                            _logger?.LogDebug("[PDF2OFD][Image][Alpha] Page {Page} 白底处理无变化", _pageNum);
                        }
                    }
                    catch (Exception exAlpha)
                    {
                        _logger?.LogWarning(exAlpha, "[PDF2OFD][Image][Alpha] Page {Page} 白底处理失败，忽略", _pageNum);
                    }
                }
                else
                {
                    _logger?.LogDebug("[PDF2OFD][Image][Alpha] Page {Page} 原图已有 Alpha，跳过白底处理", _pageNum);
                }
            }

            var matrix = renderInfo.GetImageCtm();
            var mmScale = 0.352777777777778; // ConvertHelper.Pt2Mm equivalent

            double rawW = matrix.Get(PdfMatrix.I11);
            double rawH = matrix.Get(PdfMatrix.I22);
            double rawX = matrix.Get(PdfMatrix.I31);
            double rawY = matrix.Get(PdfMatrix.I32);

            if (rawW < 0) { rawX += rawW; rawW = -rawW; }
            if (rawH < 0) { rawY += rawH; rawH = -rawH; }

            double pageHeightPt = _pageSize.GetHeight();
            double topLeftYPt = pageHeightPt - (rawY + rawH);

            var boundaryX = rawX * mmScale;
            var boundaryY = topLeftYPt * mmScale;
            var widthMm = rawW * mmScale;
            var heightMm = rawH * mmScale;

            double[]? ctm = null;
            try
            {
                var a = matrix.Get(PdfMatrix.I11) * mmScale;
                var b = matrix.Get(PdfMatrix.I12) * mmScale;
                var c = matrix.Get(PdfMatrix.I21) * mmScale;
                var d = matrix.Get(PdfMatrix.I22) * mmScale;

                var hasRotation = Math.Abs(b) > 1e-6 || Math.Abs(c) > 1e-6;
                double translateX;
                double translateY;

                if (!hasRotation)
                {
                    translateX = 0d;
                    translateY = d < 0 ? -d : 0d;
                }
                else
                {
                    var rawTranslateX = matrix.Get(PdfMatrix.I31) * mmScale;
                    var rawTranslateY = matrix.Get(PdfMatrix.I32) * mmScale;
                    var pageHeightMm = pageHeightPt * mmScale;
                    var topLeftY = pageHeightMm - (rawTranslateY + d);
                    translateX = rawTranslateX - boundaryX;
                    translateY = topLeftY - boundaryY;
                }

                ctm = new[] { a, b, c, d, translateX, translateY };
            }
            catch
            {
                // 保持默认定位
            }

            var finalFormat = ResolveFinalFormat(formatHint, whiteTransparencyApplied, hasAlpha, originalFormatHint);

            _images.Add(new OfdImageData
            {
                Page = _pageNum,
                X = boundaryX,
                Y = boundaryY,
                Width = widthMm,
                Height = heightMm,
                ImageData = imageBytes,
                Format = finalFormat,
                CTM = ctm
            });
        }
        catch (Exception ex)
        {
            _logger?.LogWarning(ex, "[PDF2OFD][Image] Page {Page} 处理异常", _pageNum);
        }
    }

    public ICollection<EventType> GetSupportedEvents() => new[] { EventType.RENDER_IMAGE };

    private static byte[] ReadImageBytes(PdfImageXObject imageObject)
    {
        try
        {
            return imageObject.GetImageBytes(true);
        }
        catch
        {
            var stream = imageObject.GetPdfObject();
            return stream.GetBytes(true) ?? Array.Empty<byte>();
        }
    }

    private static string NormalizeFormat(string? ext)
    {
        if (string.IsNullOrWhiteSpace(ext)) return "PNG";
        var trimmed = ext.Trim().Trim('.');
        if (trimmed.Length == 0) return "PNG";

        return trimmed.ToUpperInvariant() switch
        {
            "JPEG" => "JPG",
            _ => trimmed.ToUpperInvariant()
        };
    }

    private static MagickProcessingResult ProcessWithMagick(PdfImageXObject imageObject, byte[] originalBytes, string? originalFormat)
    {
        try
        {
            using var image = new MagickImage(originalBytes);

            bool colorConverted = false;
            if (image.ColorSpace != ColorSpace.RGB && image.ColorSpace != ColorSpace.sRGB)
            {
                image.ColorSpace = ColorSpace.sRGB;
                colorConverted = true;
            }

            bool maskApplied = false;
            bool hasAlpha = image.HasAlpha;

            var smaskStream = imageObject.GetPdfObject().GetAsStream(PdfName.SMask);
            if (smaskStream != null)
            {
                var smaskObject = new PdfImageXObject(smaskStream);
                var maskBytes = ReadImageBytes(smaskObject);
                if (maskBytes.Length > 0)
                {
                    using var maskImage = new MagickImage(maskBytes);
                    if (maskImage.ColorSpace != ColorSpace.Gray)
                    {
                        maskImage.ColorSpace = ColorSpace.Gray;
                    }

                    if (maskImage.Width != image.Width || maskImage.Height != image.Height)
                    {
                        maskImage.Resize(image.Width, image.Height);
                    }

                    image.Alpha(AlphaOption.Set);
                    image.Composite(maskImage, CompositeOperator.CopyAlpha);
                    maskApplied = true;
                    hasAlpha = true;
                }
            }

            var targetFormat = ResolveMagickFormat(originalFormat, hasAlpha);
            image.Format = targetFormat;

            using var ms = new MemoryStream();
            image.Write(ms);
            return new MagickProcessingResult(true, ms.ToArray(), ToFormatString(targetFormat), hasAlpha, colorConverted, maskApplied, null);
        }
        catch (Exception ex)
        {
            return new MagickProcessingResult(false, originalBytes, NormalizeFormat(originalFormat), false, false, false, ex.Message);
        }
    }

    private static MagickFormat ResolveMagickFormat(string? originalFormat, bool hasAlpha)
    {
        if (hasAlpha) return MagickFormat.Png;

        return NormalizeFormat(originalFormat) switch
        {
            "JPG" => MagickFormat.Jpeg,
            "JP2" => MagickFormat.Jp2,
            "TIFF" => MagickFormat.Tiff,
            "BMP" => MagickFormat.Bmp,
            _ => MagickFormat.Png
        };
    }

    private static string ResolveFinalFormat(string? magickFormat, bool whiteTransparencyApplied, bool hasAlpha, string? originalFormatHint)
    {
        if (whiteTransparencyApplied || hasAlpha) return "PNG";
        if (!string.IsNullOrWhiteSpace(magickFormat)) return magickFormat!;
        var normalized = NormalizeFormat(originalFormatHint);
        return string.IsNullOrWhiteSpace(normalized) ? "PNG" : normalized;
    }

    private static byte[] SimpleWhiteToTransparent(byte[] bytes, byte threshold, bool isTiff, out bool changed)
    {
        changed = false;
        using var ms = new MemoryStream(bytes);
        using var originalImage = System.Drawing.Image.FromStream(ms);
        using var bitmap = new Bitmap(originalImage.Width, originalImage.Height, PixelFormat.Format32bppArgb);

        using (var g = System.Drawing.Graphics.FromImage(bitmap))
        {
            g.DrawImage(originalImage, 0, 0);
        }

        bool localChanged = false;
        int width = bitmap.Width;
        int height = bitmap.Height;

        for (int y = 0; y < height; y++)
        {
            for (int x = 0; x < width; x++)
            {
                var pixel = bitmap.GetPixel(x, y);

                if (isTiff)
                {
                    pixel = Color.FromArgb(pixel.A, (byte)(255 - pixel.R), (byte)(255 - pixel.G), (byte)(255 - pixel.B));
                    bitmap.SetPixel(x, y, pixel);
                }

                if (pixel.R >= threshold && pixel.G >= threshold && pixel.B >= threshold)
                {
                    if (pixel.A != 0)
                    {
                        bitmap.SetPixel(x, y, Color.FromArgb(0, pixel.R, pixel.G, pixel.B));
                        localChanged = true;
                    }
                }
            }
        }

        if (!localChanged) return bytes;
        changed = true;

        using var output = new MemoryStream();
        bitmap.Save(output, ImageFormat.Png);
        return output.ToArray();
    }

    private static string ToFormatString(MagickFormat format) => format switch
    {
        MagickFormat.Jpeg => "JPG",
        MagickFormat.Jp2 => "JP2",
        MagickFormat.Tiff => "TIFF",
        MagickFormat.Bmp => "BMP",
        MagickFormat.Png => "PNG",
        _ => format.ToString().ToUpperInvariant()
    };

    private readonly record struct MagickProcessingResult(
        bool Success,
        byte[] Bytes,
        string Format,
        bool HasAlpha,
        bool ColorConverted,
        bool MaskApplied,
        string? FallbackReason);
}

/// <summary>
/// 图像处理选项
/// </summary>
public class ImageProcessingOptions
{
    /// <summary>
    /// 是否将白色背景转为透明
    /// </summary>
    public bool MakeWhiteBackgroundTransparent { get; set; }

    /// <summary>
    /// 白色阈值（0-255）
    /// </summary>
    public byte WhiteThreshold { get; set; } = 240;
}

/// <summary>
/// OFD 图像数据
/// </summary>
public class OfdImageData
{
    /// <summary>
    /// 页码
    /// </summary>
    public int Page { get; set; }

    /// <summary>
    /// X坐标（毫米）
    /// </summary>
    public double X { get; set; }

    /// <summary>
    /// Y坐标（毫米）
    /// </summary>
    public double Y { get; set; }

    /// <summary>
    /// 宽度（毫米）
    /// </summary>
    public double Width { get; set; }

    /// <summary>
    /// 高度（毫米）
    /// </summary>
    public double Height { get; set; }

    /// <summary>
    /// 图像数据
    /// </summary>
    public byte[] ImageData { get; set; } = Array.Empty<byte>();

    /// <summary>
    /// 图像格式
    /// </summary>
    public string Format { get; set; } = "PNG";

    /// <summary>
    /// 变换矩阵
    /// </summary>
    public double[]? CTM { get; set; }
}

/// <summary>
/// PDF 图像提取器
/// </summary>
public static class PdfImageExtractor
{
    /// <summary>
    /// 排序图像
    /// </summary>
    /// <param name="images">图像列表</param>
    /// <param name="ordering">排序方式</param>
    /// <returns>排序后的图像列表</returns>
    public static IReadOnlyList<OfdImageData> OrderImages(List<OfdImageData> images, string? ordering)
    {
        if (images.Count <= 1) return images;

        switch (ordering?.Trim()?.ToUpperInvariant())
        {
            case "YASCENDING":
                return images.OrderBy(i => i.Y).ToList();
            case "YDESCENDING":
                return images.OrderByDescending(i => i.Y).ToList();
            default:
                return images;
        }
    }
}
