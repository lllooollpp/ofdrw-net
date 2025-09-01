using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using OfdrwNet.Reader;
using OfdrwNet.Converter.Export;
using System.Collections.Generic; // 新增：页面过滤
using iText.Kernel.Pdf.Canvas.Parser;
using iText.Kernel.Pdf.Canvas.Parser.Listener;
using iText.Kernel.Pdf.Canvas.Parser.Data;
using iText.Kernel.Geom;
using Path = System.IO.Path; // 解决与 iText.Kernel.Geom.Path 冲突
using Microsoft.Extensions.Logging; // 新增
using OfdrwNet.Abstractions;
using OfdrwNet; // 直接引用

namespace OfdrwNet.Converter;

/// <summary>
/// 转换工具类（C#版）
/// 对应 Java org.ofdrw.converter.ConvertHelper
/// 提供 OFD -> PDF (及预留 HTML) 多种输入输出形式的便捷静态方法。
/// 当前实现：统一通过 <see cref="PDFExporter"/>（iText7 实现）完成导出；
/// Java 里的 ItextMaker / PdfboxMaker 区别此处暂未细分，仅保留枚举作未来扩展。
/// </summary>
public static class ConvertHelper
{
    /// <summary>
    /// PDF 导出附加选项
    /// </summary>
    public class PdfExportOptions
    {
        /// <summary>DPI（>=72）。默认 150。</summary>
        public float Dpi { get; set; } = 150f;
        /// <summary>起始页(1-based，可空)。</summary>
        public int? StartPage { get; set; }
        /// <summary>结束页(1-based，可空)。</summary>
        public int? EndPage { get; set; }
        /// <summary>进度回调（已转换页数, 总页数）。</summary>
        public IProgress<(int done, int total)>? Progress { get; set; }
        /// <summary>是否保留版式（绝对定位）。</summary>
        public bool PreserveLayout { get; set; }
        /// <summary>统计信息输出 JSON 文件路径（可空：不输出）。</summary>
        public string? StatsJsonPath { get; set; }
        /// <summary>字体名称映射回调：参数为 OFD 中字体名，返回 PDF 可用字体名（null 则使用默认）。</summary>
        public Func<string, string?>? FontMapper { get; set; }
        /// <summary>是否尝试嵌入映射字体（占位，当前未实现实际嵌入）。</summary>
        public bool EmbedFonts { get; set; }
        /// <summary>页面过滤器（1-based 页码）。返回 true 表示需要导出。</summary>
        public Func<int, bool>? PageFilter { get; set; }
        /// <summary>是否尝试真实图片嵌入（占位，当前仍为占位符）。</summary>
        public bool RealImageEmbedding { get; set; }
    }

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

    #region 公共入口（与 Java ofd2pdf(Object,Object) 语义对应）

    /// <summary>
    /// OFD 转 PDF 通用入口（不建议直接使用，建议调用具体重载）
    /// 支持输入：Stream, string(文件路径)
    /// 支持输出：Stream, string(文件路径)
    /// </summary>
    [Obsolete("请使用 ToPdf/ToPdfAsync 强类型重载。")]
    public static void Ofd2Pdf(object input, object output)
    {
        // 统一同步包装
        try
        {
            // 1. 规范化输入 -> OFD 文件路径
            var (ofdFilePath, tempInputFile) = NormalizeInputToTempOfd(input);

            bool outputIsStream = output is Stream;
            string? targetPdfPath = outputIsStream ? Path.GetTempFileName() : NormalizeOutputPath(output);
            if (targetPdfPath == null)
            {
                throw new ArgumentException("不支持的输出格式(output)，仅支持 Stream、string 文件路径");
            }

            RunPdfExportAsync(ofdFilePath, targetPdfPath, null, CancellationToken.None).GetAwaiter().GetResult();

            if (outputIsStream)
            {
                using var fs = File.OpenRead(targetPdfPath);
                fs.CopyTo((Stream)output);
            }

            SafeDelete(tempInputFile);
            if (outputIsStream) SafeDelete(targetPdfPath);
        }
        catch (GeneralConvertException)
        {
            throw;
        }
        catch (Exception ex)
        {
            throw new GeneralConvertException("convert to pdf failed", ex);
        }
    }

    #endregion

    #region 强类型同步重载

    public static void ToPdf(Stream input, Stream output, PdfExportOptions? options = null) => ToPdfAsync(input, output, options).GetAwaiter().GetResult();
    public static void ToPdf(Stream input, string outputPath, PdfExportOptions? options = null) => ToPdfAsync(input, outputPath, options).GetAwaiter().GetResult();
    public static void ToPdf(string inputPath, Stream output, PdfExportOptions? options = null) => ToPdfAsync(inputPath, output, options).GetAwaiter().GetResult();
    public static void ToPdf(string inputPath, string outputPath, PdfExportOptions? options = null) => ToPdfAsync(inputPath, outputPath, options).GetAwaiter().GetResult();

    /// <summary>
    /// 已解压目录 -> PDF
    /// </summary>
    public static void ToPdfFromUnzipped(string unzippedPathRoot, string outputPath, bool deleteOnClose, PdfExportOptions? options = null)
        => ToPdfFromUnzippedAsync(unzippedPathRoot, outputPath, deleteOnClose, options).GetAwaiter().GetResult();

    #endregion

    #region 强类型异步重载

    public static Task ToPdfAsync(Stream input, Stream output, PdfExportOptions? options = null, CancellationToken token = default) => Ofd2PdfAsyncInternal(input, output, options, token);
    public static Task ToPdfAsync(Stream input, string outputPath, PdfExportOptions? options = null, CancellationToken token = default) => Ofd2PdfAsyncInternal(input, outputPath, options, token);
    public static Task ToPdfAsync(string inputPath, Stream output, PdfExportOptions? options = null, CancellationToken token = default) => Ofd2PdfAsyncInternal(inputPath, output, options, token);
    public static Task ToPdfAsync(string inputPath, string outputPath, PdfExportOptions? options = null, CancellationToken token = default) => Ofd2PdfAsyncInternal(inputPath, outputPath, options, token);

    public static async Task ToPdfFromUnzippedAsync(string unzippedPathRoot, string outputPath, bool deleteOnClose, PdfExportOptions? options = null, CancellationToken token = default)
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
            SafeDelete(ofdPath);
            if (deleteOnClose)
            {
                try { Directory.Delete(unzippedPathRoot, true); } catch { /* ignore */ }
            }
        }
    }

    #endregion

    #region HTML 导出占位

    public static void ToHtml(OfdReader reader, string outputPath, int screenWidth)
    {
        throw new NotImplementedException("HTML 导出尚未在 .NET 版本实现");
    }

    public static void ToHtml(string ofdPath, string htmlOutputPath, int screenWidth)
    {
        throw new NotImplementedException("HTML 导出尚未在 .NET 版本实现");
    }

    #endregion

    #region 内部统一实现

    private static Task Ofd2PdfAsyncInternal(object input, object output, PdfExportOptions? options, CancellationToken token)
    {
        return Task.Run(async () =>
        {
            try
            {
                var (ofdFilePath, tempInputFile) = NormalizeInputToTempOfd(input);

                bool outputIsStream = output is Stream;
                string? targetPdfPath = outputIsStream ? Path.GetTempFileName() : NormalizeOutputPath(output);
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

                SafeDelete(tempInputFile);
                if (outputIsStream) SafeDelete(targetPdfPath);
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

    private static async Task RunPdfExportAsync(string ofdFilePath, string pdfOutputPath, PdfExportOptions? options, CancellationToken token)
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
            if (s > e) (s, e) = (e, s);
            pages = new List<int>();
            for (int i = s; i <= e; i++)
            {
                if (options?.PageFilter == null || options.PageFilter(i)) pages.Add(i);
            }
            if (pages.Count == 0) throw new ArgumentException("页面过滤后无可导出页面");
        }

        int totalExport = pages.Count;
        int done = 0;
        foreach (var page1Based in pages)
        {
            token.ThrowIfCancellationRequested();
            // Exporter 使用 0-based 区间调用：这里逐页调用 ExportPageAsync 会重复初始化，所以直接内部一次性：改为批量方式。
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

    private static (string ofdPath, string? tempFile) NormalizeInputToTempOfd(object input)
    {
        switch (input)
        {
            case string path when File.Exists(path):
                return (path, null);
            case Stream stream:
                string tempOfd = Path.ChangeExtension(Path.GetTempFileName(), ".ofd");
                using (var fs = File.Create(tempOfd))
                {
                    stream.CopyTo(fs);
                }
                return (tempOfd, tempOfd);
            default:
                throw new ArgumentException("不支持的输入格式(input)，仅支持 Stream、string 文件路径");
        }
    }

    private static string? NormalizeOutputPath(object output)
    {
        if (output is string s) return s;
        return null; // Stream 情况由调用处处理
    }

    private static void SafeDelete(string? path)
    {
        if (string.IsNullOrEmpty(path)) return;
        try { if (File.Exists(path)) File.Delete(path); } catch { /* ignore */ }
    }

    #endregion

    #region PDF -> OFD 初始骨架（字体嵌入阶段）

    public class PdfToOfdOptions
    {
        public bool ExtractAndEmbedFonts { get; set; } = true; // 第1阶段目标
        public bool PerGlyphPositioning { get; set; } = false; // 预留第2阶段
        public IProgress<(int done, int total)>? Progress { get; set; }
        public CancellationToken CancellationToken { get; set; }
        // 新增：是否去掉 6位大写+"+" 的子集前缀
        public bool NormalizeSubsetFontName { get; set; } = true;
        // 新增：是否输出 DeltaX（可用于调试关闭）
        public bool EnableDeltaX { get; set; } = true;
        // 新增：外部日志注入
        public ILogger? Logger { get; set; }
        public bool RealImageEmbedding { get; set; } = true; // 是否输出真实图片资源
    }

    public static void ToOfd(string pdfPath, string ofdOutputPath, PdfToOfdOptions? options = null) => ToOfdAsync(pdfPath, ofdOutputPath, options).GetAwaiter().GetResult();

    public static async Task ToOfdAsync(string pdfPath, string ofdOutputDir, PdfToOfdOptions? options = null, CancellationToken cancellationToken = default)
    {
        options ??= new PdfToOfdOptions();

        using var reader = new iText.Kernel.Pdf.PdfReader(pdfPath);
        using var pdfDoc = new iText.Kernel.Pdf.PdfDocument(reader);

        ILogger? logger = options.Logger;
        if (logger == null)
        {
            try
            {
                var lf = LoggerFactory.Create(b => { }); // 无扩展，保持最小依赖
                logger = lf.CreateLogger("PDF2OFD");
                logger.LogInformation("[PDF2OFD][Image] 未提供外部Logger，已启用内部临时Logger");
            }
            catch { }
        }
        logger?.LogInformation("[PDF2OFD] 开始转换 PDF -> OFD 输入={Pdf} 输出={Ofd}", pdfPath, ofdOutputDir);

        // 1. 字体抽取
        var fontFileTempMap = new Dictionary<string,string?>();
        var visited = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        int totalPages = pdfDoc.GetNumberOfPages();
        int processed = 0;
        var subsetPrefixRegex = new System.Text.RegularExpressions.Regex("^[A-Z]{6}\\+", System.Text.RegularExpressions.RegexOptions.Compiled);
        for (int i = 1; i <= totalPages; i++)
        {
            options.CancellationToken.ThrowIfCancellationRequested();
            var page = pdfDoc.GetPage(i);
            var resources = page.GetResources();
            if (resources != null)
            {
                foreach (var fontName in resources.GetResourceNames(iText.Kernel.Pdf.PdfName.Font) ?? Array.Empty<iText.Kernel.Pdf.PdfName>())
                {
                    var fontObj = resources.GetResource(iText.Kernel.Pdf.PdfName.Font).Get(fontName);
                    var fontDict = fontObj as iText.Kernel.Pdf.PdfDictionary;
                    if (fontDict == null) continue;
                    var baseNameRaw = fontDict.GetAsName(iText.Kernel.Pdf.PdfName.BaseFont)?.GetValue() ?? fontName.GetValue();
                    var baseName = options.NormalizeSubsetFontName ? subsetPrefixRegex.Replace(baseNameRaw, string.Empty) : baseNameRaw;
                    if (!visited.Add(baseName)) continue;
                    if (!options.ExtractAndEmbedFonts) continue;
                    try
                    {
                        var descriptor = fontDict.GetAsDictionary(iText.Kernel.Pdf.PdfName.FontDescriptor);
                        iText.Kernel.Pdf.PdfStream? ff = descriptor?.GetAsStream(iText.Kernel.Pdf.PdfName.FontFile3) ?? descriptor?.GetAsStream(iText.Kernel.Pdf.PdfName.FontFile2) ?? descriptor?.GetAsStream(iText.Kernel.Pdf.PdfName.FontFile);
                        if (ff != null)
                        {
                            var bytes = ff.GetBytes();
                            string ext = ".font";
                            var subType = fontDict.GetAsName(iText.Kernel.Pdf.PdfName.Subtype)?.GetValue();
                            if (subType != null)
                            {
                                if (subType.Contains("TrueType", StringComparison.OrdinalIgnoreCase)) ext = ".ttf";
                                else if (subType.Contains("Type0", StringComparison.OrdinalIgnoreCase)) ext = ".otf";
                                else if (subType.Contains("Type1", StringComparison.OrdinalIgnoreCase)) ext = ".pfb";
                                else if (subType.Contains("CIDFont", StringComparison.OrdinalIgnoreCase)) ext = ".otf";
                            }
                            string tmp = Path.Combine(Path.GetTempPath(), $"pdf_font_{Guid.NewGuid():N}{ext}");
                            File.WriteAllBytes(tmp, bytes);
                            fontFileTempMap[baseName] = tmp;
                        }
                        else
                        {
                            fontFileTempMap[baseName] = null; // 标记但未提取
                        }
                    }
                    catch { fontFileTempMap[baseName] = null; }
                }
            }
            processed++;
            options.Progress?.Report((processed, totalPages));
        }

        // 2. 创建 OFD 文档：使用兼容 shim 名称 OFDDoc（继承自新的写入器），避免与 Layout.OFDDoc 冲突
        IOfdDocWriter ofd = new OfdWriter(ofdOutputDir);
        try
        {
            foreach (var kv in fontFileTempMap)
            {
                if (kv.Value != null)
                {
                    try { ofd.AddExternalEmbeddedFont(kv.Key, kv.Value); } catch { }
                }
            }
            if (options.PerGlyphPositioning)
            {
                ExtractGlyphRuns(pdfDoc, ofd, options, logger);
            }
            if (options.RealImageEmbedding)
            {
                logger?.LogInformation("[PDF2OFD][Image] 开始提取图片 (RealImageEmbedding=true)");
                ExtractImages(pdfDoc, ofd, options, logger);
            }
            await ofd.CloseAsync().ConfigureAwait(false);
        }
        finally
        {
            (ofd as IDisposable)?.Dispose();
        }

        foreach (var val in fontFileTempMap.Values)
        {
            try { if (val != null && File.Exists(val)) File.Delete(val); } catch { }
        }
        logger?.LogInformation("[PDF2OFD] 写出完成");
    }

    private static void ExtractImages(iText.Kernel.Pdf.PdfDocument pdfDoc, IOfdDocWriter ofd, PdfToOfdOptions options, ILogger? logger)
    {
        double mmPerUnit = 25.4 / 72.0;
        int total = pdfDoc.GetNumberOfPages();
        int globalCount = 0;
        logger?.LogDebug("[PDF2OFD][Image] Pages={Total}", total);
        for (int pageIndex = 1; pageIndex <= total; pageIndex++)
        {
            options.CancellationToken.ThrowIfCancellationRequested();
            var page = pdfDoc.GetPage(pageIndex);
            var mediaBox = page.GetMediaBox();
            var pageHeightUser = mediaBox.GetHeight();
            double pageHeightMm = pageHeightUser * mmPerUnit;
            int pageImageCount = 0;
            var listener = new ImageCollectListener(pageHeightUser, logger, (bytes, format, xUser, yUserBottom, wUser, hUser, angleDeg) =>
            {
                pageImageCount++; globalCount++;
                try
                {
                    double x = xUser * mmPerUnit;
                    double w = wUser * mmPerUnit;
                    double h = hUser * mmPerUnit;
                    double yTop = pageHeightMm - (yUserBottom + hUser) * mmPerUnit;
                    ofd.AddRawImage(format, x, yTop, w, h, bytes, pageIndex);
                }
                catch (Exception ex)
                { logger?.LogWarning(ex, "[PDF2OFD][Image] AddRawImage failed Page={Page}", pageIndex); }
            });
            var processor = new PdfCanvasProcessor(listener);
            try { processor.ProcessPageContent(page); }
            catch (Exception ex) { logger?.LogWarning(ex, "[PDF2OFD][Image] ProcessPage failed Page={Page}", pageIndex); }
            logger?.LogInformation("[PDF2OFD][Image] Page={Page} Images={Count}", pageIndex, pageImageCount);
        }
        if (globalCount == 0) logger?.LogWarning("[PDF2OFD][Image] No RENDER_IMAGE events – maybe vector-only / form-xobject not containing raster or listener issue");
        else logger?.LogInformation("[PDF2OFD][Image] TotalImages={Count}", globalCount);
    }

    private static void ExtractGlyphRuns(iText.Kernel.Pdf.PdfDocument pdfDoc, IOfdDocWriter ofd, PdfToOfdOptions options, ILogger? logger)
    {
        double mmPerUnit = 25.4 / 72.0;
        int total = pdfDoc.GetNumberOfPages();
        int globalCount = 0;
        logger?.LogDebug("[PDF2FD][Glyph] Pages={Total}", total);
        for (int pageIndex = 1; pageIndex <= total; pageIndex++)
        {
            options.CancellationToken.ThrowIfCancellationRequested();
            var page = pdfDoc.GetPage(pageIndex);
            var mediaBox = page.GetMediaBox();
            var pageHeightUser = mediaBox.GetHeight();
            double pageHeightMm = pageHeightUser * mmPerUnit;
            int pageTextCount = 0;

            var listener = new GlyphCollectListener((tri) =>
            {
                try
                {
                    if (tri == null) return;
                    string? txt = tri.GetText();
                    if (string.IsNullOrWhiteSpace(txt)) return;

                    // baseline start point
                    var baseline = tri.GetBaseline();
                    var startPt = baseline.GetStartPoint();
                    double xUser = startPt.Get(iText.Kernel.Geom.Vector.I1);
                    double yUser = startPt.Get(iText.Kernel.Geom.Vector.I2);

                    double originX = xUser * mmPerUnit;
                    double originY = pageHeightMm - yUser * mmPerUnit; // 转为以页面顶部为原点的 mm

                    // 字号（以 pt 为单位），转换为 mm
                    double fontSizePt = tri.GetFontSize();
                    double fontSizeMm = fontSizePt * mmPerUnit;

                    // 计算每字间距（deltaX/deltaY）——使用 character render infos 的基线起点差值
                    var charInfos = tri.GetCharacterRenderInfos();
                    double[]? deltaX = null;
                    double[]? deltaY = null;
                    try
                    {
                        if (charInfos != null && charInfos.Count > 1)
                        {
                            var dx = new List<double>();
                            var dy = new List<double>();
                            var prev = charInfos[0].GetBaseline().GetStartPoint();
                            double prevX = prev.Get(iText.Kernel.Geom.Vector.I1);
                            double prevY = prev.Get(iText.Kernel.Geom.Vector.I2);
                            for (int k = 1; k < charInfos.Count; k++)
                            {
                                var p = charInfos[k].GetBaseline().GetStartPoint();
                                double cx = p.Get(iText.Kernel.Geom.Vector.I1);
                                double cy = p.Get(iText.Kernel.Geom.Vector.I2);
                                dx.Add((cx - prevX) * mmPerUnit);
                                dy.Add((cy - prevY) * mmPerUnit);
                                prevX = cx; prevY = cy;
                            }
                            if (dx.Count > 0) deltaX = dx.ToArray();
                            if (dy.Count > 0) deltaY = dy.ToArray();
                        }
                    }
                    catch (Exception) { /* best-effort, ignore */ }

                    // 尝试提取字体名
                    string fontName = "SimSun";
                    try
                    {
                        var f = tri.GetFont();
                        if (f != null) fontName = f.ToString() ?? fontName;
                    }
                    catch { }

                    ofd.AddRawTextGlyphRun(fontName, fontSizeMm, originX, originY, txt, deltaX, deltaY, pageIndex);
                    pageTextCount++; globalCount++;
                }
                catch (Exception ex)
                {
                    logger?.LogWarning(ex, "[PDF2OFD][Glyph] Text event handling failed Page={Page}", pageIndex);
                }
            });

            var processor = new PdfCanvasProcessor(listener);
            try { processor.ProcessPageContent(page); }
            catch (Exception ex) { logger?.LogWarning(ex, "[PDF2OFD][Glyph] ProcessPage failed Page={Page}", pageIndex); }

            logger?.LogInformation("[PDF2FD][Glyph] Page={Page} TextRuns={Count}", pageIndex, pageTextCount);
        }
        if (globalCount == 0) logger?.LogWarning("[PDF2FD][Glyph] No RENDER_TEXT events – PDF may contain outlined text or extraction listener issue");
        else logger?.LogInformation("[PDF2FD][Glyph] TotalTextRuns={Count}", globalCount);
    }

    private class ImageCollectListener : IEventListener
    {
        private readonly Action<byte[], string, double, double, double, double, double> _onImage;
        private readonly double _pageHeightUser;
        private readonly ILogger? _logger;
        public ImageCollectListener(double pageHeightUser, ILogger? logger, Action<byte[], string, double, double, double, double, double> onImage)
        { _onImage = onImage; _pageHeightUser = pageHeightUser; _logger = logger; }
        public void EventOccurred(IEventData data, EventType type)
        {
            if (type != EventType.RENDER_IMAGE) return;
            try
            {
                if (data is not ImageRenderInfo iri) return;
                var pdfImage = iri.GetImage();
                if (pdfImage == null) { _logger?.LogDebug("[PDF2OFD][ImageDiag] ImageRenderInfo.GetImage() returned null"); return; }
                byte[] bytes; try { bytes = pdfImage.GetImageBytes(true); } catch { bytes = pdfImage.GetImageBytes(false); }
                if (bytes == null || bytes.Length == 0) { _logger?.LogDebug("[PDF2OFD][ImageDiag] Empty image bytes"); return; }
                string fmt = GuessFormat(bytes);
                var ctm = iri.GetImageCtm();
                double a = ctm.Get(iText.Kernel.Geom.Matrix.I11);
                double b = ctm.Get(iText.Kernel.Geom.Matrix.I12);
                double c = ctm.Get(iText.Kernel.Geom.Matrix.I21);
                double d = ctm.Get(iText.Kernel.Geom.Matrix.I22);
                double e = ctm.Get(iText.Kernel.Geom.Matrix.I31);
                double f = ctm.Get(iText.Kernel.Geom.Matrix.I32);
                double widthUser = Math.Sqrt(a * a + b * b);
                double heightUser = Math.Sqrt(c * c + d * d);
                double angleDeg = Math.Atan2(b, a) * 180.0 / Math.PI;
                _onImage(bytes, fmt, e, f, widthUser, heightUser, angleDeg);
            }
            catch (Exception ex)
            { _logger?.LogWarning(ex, "[PDF2OFD][ImageDiag] EventOccurred exception"); }
        }
        public ICollection<EventType> GetSupportedEvents() => new[] { EventType.RENDER_IMAGE };
        private static string GuessFormat(byte[] bytes)
        {
            if (bytes.Length >= 8 && bytes[0]==0x89 && bytes[1]==0x50 && bytes[2]==0x4E && bytes[3]==0x47) return "PNG";
            if (bytes.Length >= 2 && bytes[0]==0xFF && bytes[1]==0xD8) return "JPG";
            if (bytes.Length >= 6 && bytes[0]=='G' && bytes[1]=='I' && bytes[2]=='F') return "GIF";
            if (bytes.Length >= 4 && ((bytes[0]=='I' && bytes[1]=='I' && bytes[2]==0x2A && bytes[3]==0x00) || (bytes[0]=='M' && bytes[1]=='M' && bytes[2]==0x00 && bytes[3]==0x2A))) return "TIFF";
            if (bytes.Length >= 2 && bytes[0]=='B' && bytes[1]=='M') return "BMP";
            if (bytes.Length >= 12 && bytes[0]=='R' && bytes[1]=='I' && bytes[2]=='F' && bytes[3]=='F' && bytes[8]=='W' && bytes[9]=='E' && bytes[10]=='B' && bytes[11]=='P') return "WEBP";
            if (bytes.Length >= 8 && bytes[0]==0x97 && bytes[1]==0x4A && bytes[2]==0x42 && bytes[3]==0x32) return "JBIG2";
            if (bytes.Length >= 8 && bytes[4]==0x6A && bytes[5]==0x50 && bytes[6]==0x20 && bytes[7]==0x20) return "JPEG2000";
            return "PNG";
        }
    }

    private class GlyphCollectListener : IEventListener
    {
        private readonly Action<iText.Kernel.Pdf.Canvas.Parser.Data.TextRenderInfo> _onRun;
        public GlyphCollectListener(Action<iText.Kernel.Pdf.Canvas.Parser.Data.TextRenderInfo> onRun) { _onRun = onRun; }
        public void EventOccurred(IEventData data, EventType type)
        {
            if (type != EventType.RENDER_TEXT) return;
            try
            {
                if (data is not iText.Kernel.Pdf.Canvas.Parser.Data.TextRenderInfo tri) return;
                _onRun?.Invoke(tri);
            }
            catch { }
        }
        public ICollection<EventType> GetSupportedEvents() => new[] { EventType.RENDER_TEXT };
    }

    #endregion
}
