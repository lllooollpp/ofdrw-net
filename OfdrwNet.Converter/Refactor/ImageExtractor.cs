using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Imaging;
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

namespace OfdrwNet.Converter.Refactor
{
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
            if (options.MaxDegreeOfParallelism <= 1)
            {
                for (int i = 1; i <= totalPages; i++)
                {
                    token.ThrowIfCancellationRequested();
                    if (options.PageFilter != null && !options.PageFilter(i)) continue;
                    ProcessSinglePage(pdfDoc, ofd, options, logger, i);
                }
            }
            else
            {
                var pages = Enumerable.Range(1, totalPages).Where(p => options.PageFilter == null || options.PageFilter(p)).ToList();
                var po = new ParallelOptions { MaxDegreeOfParallelism = options.MaxDegreeOfParallelism, CancellationToken = token };
                var bag = new System.Collections.Concurrent.ConcurrentDictionary<int, List<OfdImage>>();
                Parallel.ForEach(pages, po, p =>
                {
                    var page = pdfDoc.GetPage(p);
                    var listener = new InternalImageRenderListener(p, page.GetPageSize(), options, logger);
                    new PdfCanvasProcessor(listener).ProcessPageContent(page);
                    bag[p] = listener.Images;
                });
                foreach (var kv in bag.OrderBy(k => k.Key)) foreach (var img in kv.Value) (ofd as OfdWriter)?.AddImage(img);
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
                if (imageObject == null) return;
                byte[]? imageBytes = null;
                try
                {
                    try { imageBytes = imageObject.GetImageBytes(); }
                    catch { try { imageBytes = imageObject.GetImageBytes(true); } catch { return; } }
                    if (imageBytes == null) return;

                    // T074: 颜色空间转换 (如果启用ColorConverter)
                    if (_options.ColorConverter != null && _options.EnableColorValidation)
                    {
                        try
                        {
                            // 注意: 这里是示意集成点,实际颜色转换需要分析PDF图像的ColorSpace
                            // 当前iText API限制,暂时记录日志作为集成占位
                            _logger?.LogDebug("[PDF2OFD][Image][ColorConversion] Page {Page} ColorConverter可用,待实现CMYK→sRGB转换", _pageNum);

                            // TODO: 实际实现需要:
                            // 1. 从imageObject获取ColorSpace信息
                            // 2. 如果是CMYK,调用ColorConverter.ConvertAsync
                            // 3. 验证ΔE是否超过阈值
                            // 4. 替换imageBytes
                        }
                        catch (Exception exColor)
                        {
                            _logger?.LogWarning(exColor, "[PDF2OFD][Image][ColorConversion] Page {Page} 颜色转换失败,使用原图", _pageNum);
                        }
                    }

                    bool alphaChanged = false; string? newFormat = null;
                    if (_options.MakeWhiteBackgroundTransparent)
                    {
                        try
                        {
                            // 检测是否为 TIFF 格式
                            var imageExt = imageObject.IdentifyImageFileExtension();
                            bool isTiff = imageExt?.ToLowerInvariant().Contains("tif") == true;

                            var processed = SimpleWhiteToTransparent(imageBytes, _options.WhiteThreshold, isTiff, out bool changed);
                            if (changed)
                            {
                                imageBytes = processed; alphaChanged = true; newFormat = "PNG";
                                _logger?.LogDebug("[PDF2OFD][Image][Alpha] Page {Page} Success 白底->透明{TiffNote}", _pageNum, isTiff ? " (TIFF反相)" : "");
                            }
                            else
                            {
                                _logger?.LogDebug("[PDF2OFD][Image][Alpha] Page {Page} NoChange", _pageNum);
                            }
                        }
                        catch (Exception exAlpha) { _logger?.LogWarning(exAlpha, "[PDF2OFD][Image][Alpha] Page {Page} 处理失败忽略", _pageNum); }
                    }

                    var matrix = renderInfo.GetImageCtm();
                    var mmScale = ConvertHelper.Pt2Mm;

                    double rawW = matrix.Get(Matrix.I11);
                    double rawH = matrix.Get(Matrix.I22);
                    double rawX = matrix.Get(Matrix.I31);
                    double rawY = matrix.Get(Matrix.I32);

                    if (rawW < 0) { rawX += rawW; rawW = -rawW; }
                    if (rawH < 0) { rawY += rawH; rawH = -rawH; }

                    double pageHeightPt = _pageSize.GetHeight();
                    double topLeftYPt = pageHeightPt - (rawY + rawH);

                    var boundaryX = (float)(rawX * mmScale);
                    var boundaryY = (float)(topLeftYPt * mmScale);
                    var widthMm = (float)(rawW * mmScale);
                    var heightMm = (float)(rawH * mmScale);

                    double[]? ctm = null;
                    try
                    {
                        var a = matrix.Get(Matrix.I11) * mmScale;
                        var b = matrix.Get(Matrix.I12) * mmScale;
                        var c = matrix.Get(Matrix.I21) * mmScale;
                        var d = matrix.Get(Matrix.I22) * mmScale;

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
                            var rawTranslateX = matrix.Get(Matrix.I31) * mmScale;
                            var rawTranslateY = matrix.Get(Matrix.I32) * mmScale;
                            var pageHeightMm = pageHeightPt * mmScale;
                            var topLeftY = pageHeightMm - (rawTranslateY + d);
                            translateX = rawTranslateX - boundaryX;
                            translateY = topLeftY - boundaryY;
                        }

                        ctm = new double[] { a, b, c, d, translateX, translateY };
                    }
                    catch { }

                    double posX = boundaryX;
                    double posY = boundaryY;
                    var width = widthMm;
                    var height = heightMm;

                    var originalExt = imageObject.IdentifyImageFileExtension();
                    var finalFormat = alphaChanged ? (newFormat ?? "PNG") : originalExt;
                    if (!string.IsNullOrEmpty(finalFormat)) finalFormat = finalFormat.TrimStart('.');
                    Images.Add(new OfdImage
                    {
                        Page = _pageNum,
                        X = (float)posX,
                        Y = (float)posY,
                        Width = width,
                        Height = height,
                        ImageData = imageBytes,
                        Format = finalFormat,
                        CTM = ctm
                    });
                }
                catch (Exception ex) { _logger?.LogWarning(ex, "[PDF2OFD][Image] Page {Page} 异常", _pageNum); }
            }

            public ICollection<EventType> GetSupportedEvents() => new[] { EventType.RENDER_IMAGE };

            private static byte[] SimpleWhiteToTransparent(byte[] bytes, byte threshold, bool isTiff, out bool changed)
            {
                changed = false;
                using var ms = new MemoryStream(bytes);
                using var originalImage = Image.FromStream(ms);
                using var bitmap = new Bitmap(originalImage.Width, originalImage.Height, PixelFormat.Format32bppArgb);

                // 复制原图像到支持透明的位图
                using (var g = System.Drawing.Graphics.FromImage(bitmap))
                {
                    g.DrawImage(originalImage, 0, 0);
                }

                bool localChanged = false;
                int w = bitmap.Width;
                int h = bitmap.Height;

                for (int y = 0; y < h; y++)
                {
                    for (int x = 0; x < w; x++)
                    {
                        Color pixel = bitmap.GetPixel(x, y);

                        // TIFF 特殊处理：先反相颜色
                        if (isTiff)
                        {
                            pixel = Color.FromArgb(pixel.A, (byte)(255 - pixel.R), (byte)(255 - pixel.G), (byte)(255 - pixel.B));
                            bitmap.SetPixel(x, y, pixel);
                        }

                        // 白底转透明逻辑
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

                using var outputMs = new MemoryStream();
                bitmap.Save(outputMs, ImageFormat.Png);
                return outputMs.ToArray();
            }
        }
    }
}
