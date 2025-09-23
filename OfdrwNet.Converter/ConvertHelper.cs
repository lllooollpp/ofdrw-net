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
using Path = System.IO.Path; // 解决与 iText.Kernel.Geom.Path 冲突
using Microsoft.Extensions.Logging; // 新增
using SkiaSharp; // 用于跨平台图片解码和重编码
using SixLabors.ImageSharp; // 新增：用于更强大的图片解码
using SixLabors.ImageSharp.Formats.Png; // 新增：用于PNG编码
using OfdrwNet.Abstractions;
using OfdrwNet; // 直接引用
using System.Linq;
using OfdrwNet.Layout; // 新增: 设置页面尺寸
using iText.Kernel.Geom; // 用于Matrix等几何类型
using iTextRectangle = iText.Kernel.Geom.Rectangle; // 起别名避免冲突

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
    public const double Pt2Mm = 25.4 / 72.0; // 改为 public 供监听器访问

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
        /// <summary>
        /// 是否提取真实图片
        /// </summary>
        public bool RealImageEmbedding { get; set; } = true;

        /// <summary>
        /// 日志记录器
        /// </summary>
        public ILogger? Logger { get; set; }
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

    #region PDF -> OFD 转换方法

    /// <summary>
    /// 使用 Playwright + PDF.js 将 PDF 转换为 OFD（推荐方法）
    /// 这种方法使用浏览器技术栈，在复杂 PDF 处理上更稳定
    /// </summary>
    /// <param name="pdfPath">输入 PDF 文件路径</param>
    /// <param name="ofdOutputPath">输出 OFD 文件路径</param>
    /// <param name="options">转换选项</param>
    public static async Task PdfToOfdByPlaywrightAsync(string pdfPath, string ofdOutputPath, PlaywrightConvertOptions? options = null)
    {
        using var converter = new PlaywrightPdfConverter();
        await converter.InitializeAsync();
        await converter.ConvertPdfToOfdAsync(pdfPath, ofdOutputPath, options);
    }

    /// <summary>
    /// 使用 Playwright + PDF.js 将 PDF 转换为 OFD（同步版本）
    /// </summary>
    public static void PdfToOfdByPlaywright(string pdfPath, string ofdOutputPath, PlaywrightConvertOptions? options = null)
    {
        PdfToOfdByPlaywrightAsync(pdfPath, ofdOutputPath, options).GetAwaiter().GetResult();
    }

    #endregion

    #region PDF -> OFD 初始骨架（字体嵌入阶段）

    public class PdfToOfdOptions
    {
        public bool ExtractAndEmbedFonts { get; set; } = true; // 第1阶段目标
        public bool ExtractText { get; set; } = true;
        public bool ExtractImage { get; set; } = true;
        public bool ExtractAnnotations { get; set; } = true; // 提取注释/批注
        public bool ExtractForms { get; set; } = true; // 提取表单
        public bool PerGlyphPositioning { get; set; } = false; // 预留第2阶段
        public IProgress<(int done, int total)>? Progress { get; set; }
        public CancellationToken CancellationToken { get; set; }
        public bool NormalizeSubsetFontName { get; set; } = true;
        public bool EnableDeltaX { get; set; } = true;
        public ILogger? Logger { get; set; }
        public bool RealImageEmbedding { get; set; } = true; // 是否输出真实图片资源
        public Func<int, bool>? PageFilter { get; set; }
        public string? Password { get; set; } // 密码参数支持
        public int MaxDegreeOfParallelism { get; set; } = 1; // 并行度，1表示顺序处理，>1表示并行处理
        public bool IgnoreCMapErrors { get; set; } = true; // 忽略中文字体 CMap 错误，默认启用

        // 新增：为兼容性添加的属性
        public bool EnableImageExtraction { get { return ExtractImage; } set { ExtractImage = value; } }
        public bool EnableAnnotationExtraction { get { return ExtractAnnotations; } set { ExtractAnnotations = value; } }
        public bool EnableFormExtraction { get { return ExtractForms; } set { ExtractForms = value; } }
    }

    // 新增：字体乱码到系统字体名映射 + 系统字体候选文件
    private static readonly Dictionary<string, string> FontNameFallbackMap = new(StringComparer.OrdinalIgnoreCase)
    {
        ["ËÎÌå"] = "SimSun",          // 宋体
        ["Î¢ÈíÑÅºÚ"] = "Microsoft YaHei", // 微软雅黑
        ["ºÚÌå"] = "SimHei",          // 黑体
        ["¿¬Ìå"] = "KaiTi",           // 楷体(可能)
        ["KaiTi_GB2312"] = "KaiTi",   // 另一种写法
    };
    private static readonly Dictionary<string, string[]> SystemFontCandidates = new(StringComparer.OrdinalIgnoreCase)
    {
        ["SimSun"] = new[]{"simsun.ttc","SimSun.ttc"},
        ["Microsoft YaHei"] = new[]{"msyh.ttc","msyh.ttf"},
        ["SimHei"] = new[]{"simhei.ttf"},
        ["KaiTi"] = new[]{"simkai.ttf","kaiti.ttf"}
    };
    // 可供文本监听器调用，需改为 internal
    internal static string NormalizeLogicalFontName(string baseName)
    {
        // 去除常见后缀 _GB2312-WinCharSet... 或 -WinCharSet...
        var cleaned = baseName;
        int idx = cleaned.IndexOf("-WinCharSet", StringComparison.OrdinalIgnoreCase);
        if (idx > 0) cleaned = cleaned[..idx];
        idx = cleaned.IndexOf("_GB2312", StringComparison.OrdinalIgnoreCase);
        if (idx > 0) cleaned = cleaned[..idx];
        foreach (var kv in FontNameFallbackMap)
        {
            if (cleaned.Contains(kv.Key, StringComparison.OrdinalIgnoreCase)) return kv.Value;
        }
        return cleaned;
    }
    private static string? FindSystemFontPath(string logical)
    {
        if (!SystemFontCandidates.TryGetValue(logical, out var candidates)) return null;
        string fontDir = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Windows), "Fonts");
        foreach (var c in candidates)
        {
            var p = Path.Combine(fontDir, c);
            if (File.Exists(p)) return p;
        }
        return null;
    }

    public static void PdfToOfd(string pdfPath, string ofdOutputDir, PdfToOfdOptions? options = null)
    {
        PdfToOfdAsync(pdfPath, ofdOutputDir, options).GetAwaiter().GetResult();
    }

    private static readonly System.Text.RegularExpressions.Regex subsetPrefixRegex = new System.Text.RegularExpressions.Regex(@"^[A-Z]{6}\+");

    public static async Task PdfToOfdAsync(string pdfPath, string ofdOutputDir, PdfToOfdOptions? options = null)
    {
        options ??= new PdfToOfdOptions();
        ILogger? logger = options.Logger;
        if (logger == null)
        {
            // 如果未提供日志记录器，创建一个临时的
            var lf = LoggerFactory.Create(b => {
                b.AddConsole();
                b.SetMinimumLevel(Microsoft.Extensions.Logging.LogLevel.Debug);
            });
            logger = lf.CreateLogger("PDF2OFD");
            logger.LogInformation("[PDF2OFD] 未提供外部Logger，已启用内部临时Logger");
        }

        logger.LogInformation("[PDF2OFD] 开始转换 PDF -> OFD 输入={Pdf} 输出={Ofd}", pdfPath, ofdOutputDir);

        if (!File.Exists(pdfPath))
        {
            logger.LogError("[PDF2OFD] PDF文件不存在: {Pdf}", pdfPath);
            throw new FileNotFoundException("PDF文件不存在", pdfPath);
        }

        var fontFileTempMap = new Dictionary<string, string?>();
        // 创建PdfReader，支持密码
        iText.Kernel.Pdf.PdfReader pdfReader;
        if (!string.IsNullOrEmpty(options.Password))
        {
            pdfReader = new iText.Kernel.Pdf.PdfReader(pdfPath, new iText.Kernel.Pdf.ReaderProperties().SetPassword(System.Text.Encoding.UTF8.GetBytes(options.Password)));
            logger.LogInformation("[PDF2OFD] 使用密码打开PDF文件");
        }
        else
        {
            pdfReader = new iText.Kernel.Pdf.PdfReader(pdfPath);
        }
        using var pdfDoc = new iText.Kernel.Pdf.PdfDocument(pdfReader);
        // 预读取第一页尺寸，后续用于设置页面尺寸
        iText.Kernel.Geom.Rectangle? firstPageSize = null;
        if (pdfDoc.GetNumberOfPages() > 0)
        {
            firstPageSize = pdfDoc.GetPage(1).GetPageSize();
        }

        // 1. 字体抽取
        if (options.ExtractAndEmbedFonts)
        {
            var visited = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            int totalPages = pdfDoc.GetNumberOfPages();
            logger.LogInformation("[PDF2OFD] PDF总页数: {TotalPages}，开始提取字体...", totalPages);

            try
            {
                for (int i = 1; i <= totalPages; i++)
                {
                    options.CancellationToken.ThrowIfCancellationRequested();
                    var page = pdfDoc.GetPage(i);
                    var resources = page.GetResources();
                    if (resources == null) continue;

                    foreach (var fontName in resources.GetResourceNames(iText.Kernel.Pdf.PdfName.Font) ?? Array.Empty<iText.Kernel.Pdf.PdfName>())
                    {
                        var fontObj = resources.GetResource(iText.Kernel.Pdf.PdfName.Font).Get(fontName);
                        if (fontObj is not iText.Kernel.Pdf.PdfDictionary fontDict) continue;

                        var baseNameRaw = fontDict.GetAsName(iText.Kernel.Pdf.PdfName.BaseFont)?.GetValue() ?? fontName.GetValue();
                        var baseName = options.NormalizeSubsetFontName ? subsetPrefixRegex.Replace(baseNameRaw, string.Empty) : baseNameRaw;

                        if (!visited.Add(baseName)) continue;

                        try
                        {
                            var descriptor = fontDict.GetAsDictionary(iText.Kernel.Pdf.PdfName.FontDescriptor);
                            iText.Kernel.Pdf.PdfStream? ff = descriptor?.GetAsStream(iText.Kernel.Pdf.PdfName.FontFile3)
                                                        ?? descriptor?.GetAsStream(iText.Kernel.Pdf.PdfName.FontFile2)
                                                        ?? descriptor?.GetAsStream(iText.Kernel.Pdf.PdfName.FontFile);

                            // 新增：逻辑字体名归一（解决乱码导致的无法匹配系统字体问题）
                            var logicalName = NormalizeLogicalFontName(baseName);

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
                                await File.WriteAllBytesAsync(tmp, bytes, options.CancellationToken);
                                fontFileTempMap[logicalName] = tmp; // 使用归一后的名称
                                logger.LogDebug("[PDF2OFD] 提取并暂存字体 '{FontName}' -> {TempPath}", logicalName, tmp);
                            }
                            else
                            {
                                // 尝试系统字体回退
                                var sys = FindSystemFontPath(logicalName);
                                if (sys != null)
                                {
                                    string ext = Path.GetExtension(sys);
                                    string tmp = Path.Combine(Path.GetTempPath(), $"pdf_sysfont_{Guid.NewGuid():N}{ext}");
                                    try { File.Copy(sys, tmp, true); fontFileTempMap[logicalName] = tmp; logger.LogInformation("[PDF2OFD] 使用系统字体回退 '{Font}' -> {Path}", logicalName, sys); }
                                    catch (Exception copyEx) { fontFileTempMap[logicalName] = null; logger.LogWarning(copyEx, "[PDF2OFD] 系统字体 '{Font}' 复制失败", logicalName); }
                                }
                                else
                                {
                                    fontFileTempMap[logicalName] = null; // 标记但未提取
                                    logger.LogDebug("[PDF2OFD] 字体 '{FontName}' 未嵌入且未找到系统回退", logicalName);
                                }
                            }
                        }
                        catch (Exception ex)
                        {
                            fontFileTempMap[baseName] = null;
                            logger.LogWarning(ex, "[PDF2OFD] 提取字体 '{FontName}' 时发生异常", baseName);
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "[PDF2OFD] 字体提取过程中发生严重异常");
                throw;
            }
            logger.LogInformation("[PDF2OFD] 字体提取完成，共处理 {FontCount} 种字体。", fontFileTempMap.Count);
        }


        // 2. 创建 OFD 文档
        logger.LogInformation("[PDF2OFD] 创建 OfdWriter, 输出目录: {OfdOutputDir}", ofdOutputDir);
        using IOfdDocWriter ofd = new OfdWriter(ofdOutputDir, logger);
        // 根据PDF第一页实际尺寸动态设置页面布局(缺省A4时避免变形)
        if (firstPageSize != null)
        {
            var pw = firstPageSize.GetWidth() * Pt2Mm;
            var ph = firstPageSize.GetHeight() * Pt2Mm;
            (ofd as OfdWriter)?.SetDefaultPageLayout(new PageLayout(pw, ph));
            logger.LogInformation("[PDF2OFD] 已设置OFD页面尺寸 {W:0.##}mm x {H:0.##}mm", pw, ph);
        }
        try
        {
            foreach (var kv in fontFileTempMap)
            {
                if (kv.Value != null)
                {
                    try
                    {
                        (ofd as OfdWriter)?.AddExternalEmbeddedFont(kv.Key, kv.Value);
                        logger.LogDebug("[PDF2OFD] 添加外部字体到OFD: {FontName} from {Path}", kv.Key, kv.Value);
                    }
                    catch (Exception ex)
                    {
                        logger.LogWarning(ex, "[PDF2OFD] 添加外部字体 {FontName} 到OFD失败", kv.Key);
                    }
                }
            }

            // 3. 提取内容并添加到 OFD
            if (options.ExtractText)
            {
                logger.LogInformation("[PDF2OFD] 开始提取文本");
                ExtractText(pdfDoc, ofd, options, logger);
            }

            if (options.ExtractImage)
            {
                if (options.RealImageEmbedding)
                {
                    logger.LogInformation("[PDF2OFD][Image] 开始提取图片 (RealImageEmbedding=true)");
                    ExtractImages(pdfDoc, ofd, options, logger);
                }
                else
                {
                    logger.LogWarning("[PDF2OFD][Image] 提取图片时未启用真实图片嵌入（RealImageEmbedding），将使用占位符");
                    ExtractImages(pdfDoc, ofd, options, logger);
                }
            }

            // 新增：向量路径提取
            logger.LogInformation("[PDF2OFD] 开始提取向量路径");
            ExtractVectors(pdfDoc, ofd, options, logger);

            // 新增：注释提取
            if (options.ExtractAnnotations)
            {
                logger.LogInformation("[PDF2OFD] 开始提取注释");
                ExtractAnnotations(pdfDoc, ofd, options, logger);
            }

            // 新增：表单提取
            if (options.ExtractForms)
            {
                logger.LogInformation("[PDF2OFD] 开始提取表单");
                ExtractForms(pdfDoc, ofd, options, logger);
            }

            await ofd.CloseAsync().ConfigureAwait(false);
            logger.LogInformation("[PDF2OFD] OFD文档异步关闭完成");
        }
        finally
        {
            // 清理临时字体文件
            foreach (var path in fontFileTempMap.Values)
            {
                if (path != null) SafeDelete(path);
            }
            logger.LogInformation("[PDF2OFD] 临时字体文件清理完毕");
        }
    }

    // 新增：别名方法以兼容测试程序
    public static Task ConvertPdfToOfdAsync(string pdfPath, string ofdOutputDir, PdfToOfdOptions? options = null)
    {
        return PdfToOfdAsync(pdfPath, ofdOutputDir, options);
    }

    private static void ExtractImages(iText.Kernel.Pdf.PdfDocument pdfDoc, IOfdDocWriter ofd, PdfToOfdOptions options, ILogger? logger)
    {
        int totalPages = pdfDoc.GetNumberOfPages();
        logger?.LogDebug("[PDF2OFD][Image] PDF总页数: {TotalPages}", totalPages);

        if (options.MaxDegreeOfParallelism <= 1)
        {
            // 顺序处理
            for (int i = 1; i <= totalPages; i++)
            {
                if (options.PageFilter != null && !options.PageFilter(i))
                {
                    logger?.LogDebug("[PDF2FD][Image] Page {PageNum} 被过滤", i);
                    continue;
                }

                var page = pdfDoc.GetPage(i);
                var listener = new ImageRenderListener(i, page.GetPageSize(), options, logger);
                new PdfCanvasProcessor(listener).ProcessPageContent(page);

                if (listener.Images.Count > 0)
                {
                    logger?.LogDebug("[PDF2OFD][Image] Page {PageNum} 发现 {ImageCount} 张图片", i, listener.Images.Count);
                    foreach (var img in listener.Images)
                    {
                        (ofd as OfdWriter)?.AddImage(img);
                    }
                }
                else
                {
                    logger?.LogDebug("[PDF2OFD][Image] Page {PageNum} 未发现图片", i);
                }
            }
        }
        else
        {
            // 并行处理
            logger?.LogInformation("[PDF2OFD][Image] 使用并行处理，最大并行度: {Parallelism}", options.MaxDegreeOfParallelism);

            var pagesToProcess = Enumerable.Range(1, totalPages)
                .Where(i => options.PageFilter == null || options.PageFilter(i))
                .ToList();

            var parallelOptions = new ParallelOptions
            {
                MaxDegreeOfParallelism = options.MaxDegreeOfParallelism,
                CancellationToken = options.CancellationToken
            };

            var imagesPerPage = new System.Collections.Concurrent.ConcurrentDictionary<int, List<OfdImage>>();

            Parallel.ForEach(pagesToProcess, parallelOptions, i =>
            {
                try
                {
                    var page = pdfDoc.GetPage(i);
                    var listener = new ImageRenderListener(i, page.GetPageSize(), options, logger);
                    new PdfCanvasProcessor(listener).ProcessPageContent(page);

                    imagesPerPage[i] = listener.Images;
                    logger?.LogDebug("[PDF2OFD][Image] Page {PageNum} 处理完成，发现 {ImageCount} 张图片", i, listener.Images.Count);
                }
                catch (Exception ex)
                {
                    logger?.LogError(ex, "[PDF2OFD][Image] Page {PageNum} 并行处理失败", i);
                    throw;
                }
            });

            // 按页码顺序添加图片
            foreach (var kvp in imagesPerPage.OrderBy(kvp => kvp.Key))
            {
                var images = kvp.Value;
                if (images.Count > 0)
                {
                    logger?.LogDebug("[PDF2OFD][Image] Page {PageNum} 输出 {ImageCount} 张图片", kvp.Key, images.Count);
                    foreach (var img in images)
                    {
                        (ofd as OfdWriter)?.AddImage(img);
                    }
                }
                else
                {
                    logger?.LogDebug("[PDF2OFD][Image] Page {PageNum} 未发现图片", kvp.Key);
                }
            }
        }
    }

    private static void ExtractVectors(iText.Kernel.Pdf.PdfDocument pdfDoc, IOfdDocWriter ofd, PdfToOfdOptions options, ILogger? logger)
    {
        int totalPages = pdfDoc.GetNumberOfPages();
        logger?.LogDebug("[PDF2OFD][Vector] PDF总页数: {TotalPages}", totalPages);

        if (options.MaxDegreeOfParallelism <= 1)
        {
            // 顺序处理
            for (int i = 1; i <= totalPages; i++)
            {
                if (options.PageFilter != null && !options.PageFilter(i))
                {
                    logger?.LogDebug("[PDF2OFD][Vector] Page {PageNum} 被过滤", i);
                    continue;
                }

                var page = pdfDoc.GetPage(i);
                var listener = new VectorPathListener(logger);
                new PdfCanvasProcessor(listener).ProcessPageContent(page);

                if (listener.GetPaths().Count > 0)
                {
                    logger?.LogDebug("[PDF2OFD][Vector] Page {PageNum} 发现 {PathCount} 个路径", i, listener.GetPaths().Count);
                    foreach (var path in listener.GetPaths())
                    {
                        path.Page = i; // 设置正确的页码
                        (ofd as OfdWriter)?.AddPath(path);
                    }
                }
                else
                {
                    logger?.LogDebug("[PDF2OFD][Vector] Page {PageNum} 未发现路径", i);
                }
            }
        }
        else
        {
            // 并行处理
            logger?.LogInformation("[PDF2OFD][Vector] 使用并行处理，最大并行度: {Parallelism}", options.MaxDegreeOfParallelism);

            var pagesToProcess = Enumerable.Range(1, totalPages)
                .Where(i => options.PageFilter == null || options.PageFilter(i))
                .ToList();

            var parallelOptions = new ParallelOptions
            {
                MaxDegreeOfParallelism = options.MaxDegreeOfParallelism,
                CancellationToken = options.CancellationToken
            };

            var pathsPerPage = new System.Collections.Concurrent.ConcurrentDictionary<int, List<OfdPath>>();

            Parallel.ForEach(pagesToProcess, parallelOptions, i =>
            {
                try
                {
                    var page = pdfDoc.GetPage(i);
                    var listener = new VectorPathListener(logger);
                    new PdfCanvasProcessor(listener).ProcessPageContent(page);

                    var paths = listener.GetPaths();
                    foreach (var path in paths)
                    {
                        path.Page = i; // 设置正确的页码
                    }
                    pathsPerPage[i] = paths;

                    logger?.LogDebug("[PDF2OFD][Vector] Page {PageNum} 处理完成，发现 {PathCount} 个路径", i, paths.Count);
                }
                catch (Exception ex)
                {
                    logger?.LogError(ex, "[PDF2OFD][Vector] Page {PageNum} 并行处理失败", i);
                    throw;
                }
            });

            // 按页码顺序添加路径
            foreach (var kvp in pathsPerPage.OrderBy(kvp => kvp.Key))
            {
                var paths = kvp.Value;
                if (paths.Count > 0)
                {
                    logger?.LogDebug("[PDF2OFD][Vector] Page {PageNum} 输出 {PathCount} 个路径", kvp.Key, paths.Count);
                    foreach (var path in paths)
                    {
                        (ofd as OfdWriter)?.AddPath(path);
                    }
                }
                else
                {
                    logger?.LogDebug("[PDF2OFD][Vector] Page {PageNum} 未发现路径", kvp.Key);
                }
            }
        }
    }

    private static void ExtractText(iText.Kernel.Pdf.PdfDocument pdfDoc, IOfdDocWriter ofd, PdfToOfdOptions options, ILogger? logger)
    {
        int totalPages = pdfDoc.GetNumberOfPages();
        logger?.LogDebug("[PDF2OFD][Text] PDF总页数: {TotalPages}", totalPages);

        if (options.MaxDegreeOfParallelism <= 1)
        {
            // 顺序处理
            for (int i = 1; i <= totalPages; i++)
            {
                if (options.PageFilter != null && !options.PageFilter(i))
                {
                    logger?.LogDebug("[PDF2OFD][Text] Page {PageNum} 被过滤", i);
                    continue;
                }

                try
                {
                    var page = pdfDoc.GetPage(i);
                    var strategy = new TextRenderListener(i, page.GetPageSize(), options, logger);
                    new PdfCanvasProcessor(strategy).ProcessPageContent(page);

                    // 新增：根据定位模式决定是否聚合
                    var blocks = options.PerGlyphPositioning ? strategy.TextBlocks : TextAggregationHelper.Aggregate(strategy.TextBlocks, logger, i);

                    if (blocks.Count > 0)
                    {
                        logger?.LogDebug("[PDF2OFD][Text] Page {PageNum} 输出 {TextBlockCount} 个文本块 (原始={RawCount})", i, blocks.Count, strategy.TextBlocks.Count);
                        foreach (var block in blocks)
                        {
                            (ofd as OfdWriter)?.AddText(block);
                        }
                    }
                    else
                    {
                        logger?.LogDebug("[PDF2OFD][Text] Page {PageNum} 未发现文本", i);
                    }
                }
                catch (iText.IO.Exceptions.IOException ex) when ((ex.Message.Contains("CMap") || ex.Message.Contains("UniGB")) && options.IgnoreCMapErrors)
                {
                    logger?.LogWarning("[PDF2OFD][Text] Page {PageNum} 中文字体 CMap 错误，跳过文本提取: {Error}", i, ex.Message);
                }
                catch (Exception ex)
                {
                    logger?.LogWarning(ex, "[PDF2OFD][Text] Page {PageNum} 文本提取失败，跳过: {Error}", i, ex.Message);
                }
            }
        }
        else
        {
            // 并行处理
            logger?.LogInformation("[PDF2OFD][Text] 使用并行处理，最大并行度: {Parallelism}", options.MaxDegreeOfParallelism);

            var pagesToProcess = Enumerable.Range(1, totalPages)
                .Where(i => options.PageFilter == null || options.PageFilter(i))
                .ToList();

            var parallelOptions = new ParallelOptions
            {
                MaxDegreeOfParallelism = options.MaxDegreeOfParallelism,
                CancellationToken = options.CancellationToken
            };

            var textBlocksPerPage = new System.Collections.Concurrent.ConcurrentDictionary<int, List<OfdText>>();

            Parallel.ForEach(pagesToProcess, parallelOptions, i =>
            {
                try
                {
                    var page = pdfDoc.GetPage(i);
                    var strategy = new TextRenderListener(i, page.GetPageSize(), options, logger);
                    new PdfCanvasProcessor(strategy).ProcessPageContent(page);

                    // 根据定位模式决定是否聚合
                    var blocks = options.PerGlyphPositioning ? strategy.TextBlocks : TextAggregationHelper.Aggregate(strategy.TextBlocks, logger, i);
                    textBlocksPerPage[i] = blocks;

                    logger?.LogDebug("[PDF2OFD][Text] Page {PageNum} 处理完成，发现 {TextBlockCount} 个文本块", i, blocks.Count);
                }
                catch (iText.IO.Exceptions.IOException ex) when ((ex.Message.Contains("CMap") || ex.Message.Contains("UniGB")) && options.IgnoreCMapErrors)
                {
                    logger?.LogWarning("[PDF2OFD][Text] Page {PageNum} 中文字体 CMap 错误，跳过文本提取: {Error}", i, ex.Message);
                    textBlocksPerPage[i] = new List<OfdText>(); // 添加空列表避免 KeyNotFound
                }
                catch (Exception ex)
                {
                    logger?.LogWarning(ex, "[PDF2OFD][Text] Page {PageNum} 文本提取失败，跳过: {Error}", i, ex.Message);
                    textBlocksPerPage[i] = new List<OfdText>(); // 添加空列表避免 KeyNotFound
                }
            });

            // 按页码顺序添加文本块
            foreach (var kvp in textBlocksPerPage.OrderBy(kvp => kvp.Key))
            {
                var blocks = kvp.Value;
                if (blocks.Count > 0)
                {
                    logger?.LogDebug("[PDF2OFD][Text] Page {PageNum} 输出 {TextBlockCount} 个文本块", kvp.Key, blocks.Count);
                    foreach (var block in blocks)
                    {
                        (ofd as OfdWriter)?.AddText(block);
                    }
                }
                else
                {
                    logger?.LogDebug("[PDF2OFD][Text] Page {PageNum} 未发现文本", kvp.Key);
                }
            }
        }
    }

    private static void ExtractAnnotations(iText.Kernel.Pdf.PdfDocument pdfDoc, IOfdDocWriter ofd, PdfToOfdOptions options, ILogger? logger)
    {
        int totalPages = pdfDoc.GetNumberOfPages();
        logger?.LogDebug("[PDF2OFD][Annotation] PDF总页数: {TotalPages}", totalPages);

        for (int i = 1; i <= totalPages; i++)
        {
            if (options.PageFilter != null && !options.PageFilter(i))
            {
                logger?.LogDebug("[PDF2OFD][Annotation] Page {PageNum} 被过滤", i);
                continue;
            }

            var page = pdfDoc.GetPage(i);
            var annotations = page.GetAnnotations();

            if (annotations != null && annotations.Count > 0)
            {
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
            else
            {
                logger?.LogDebug("[PDF2OFD][Annotation] Page {PageNum} 未发现注释", i);
            }
        }
    }

    private static void ExtractForms(iText.Kernel.Pdf.PdfDocument pdfDoc, IOfdDocWriter ofd, PdfToOfdOptions options, ILogger? logger)
    {
        // 暂时跳过表单处理，稍后实现
        logger?.LogDebug("[PDF2OFD][Form] 表单处理暂未实现，跳过");
        return;

        /*
        var acroForm = iText.Forms.PdfAcroForm.GetAcroForm(pdfDoc, false);
        if (acroForm == null)
        {
            logger?.LogDebug("[PDF2OFD][Form] PDF中未发现表单");
            return;
        }

        var fields = acroForm.GetFields();
        if (fields == null || fields.Count == 0)
        {
            logger?.LogDebug("[PDF2OFD][Form] PDF中未发现表单字段");
            return;
        }

        logger?.LogInformation("[PDF2OFD][Form] 发现 {FieldCount} 个表单字段", fields.Count);

        foreach (var field in fields)
        {
            try
            {
                var ofdFormField = ConvertPdfFormFieldToOfd(field, logger);
                if (ofdFormField != null)
                {
                    (ofd as OfdWriter)?.AddFormField(ofdFormField);
                }
            }
            catch (Exception ex)
            {
                logger?.LogWarning(ex, "[PDF2OFD][Form] 转换表单字段 '{FieldName}' 失败", field.GetFieldName()?.ToString());
            }
        }
        */
    }

    private static object? ConvertPdfAnnotationToOfd(iText.Kernel.Pdf.Annot.PdfAnnotation pdfAnnotation, int pageIndex, ILogger? logger)
    {
        if (pdfAnnotation == null) return null;

        try
        {
            var subtype = pdfAnnotation.GetSubtype();
            logger?.LogDebug("[PDF2OFD][Annotation] 转换注释类型: {Type}", subtype?.ToString());

            // 获取注释的边界框
            var rect = pdfAnnotation.GetRectangle();
            if (rect == null)
            {
                logger?.LogWarning("[PDF2OFD][Annotation] 注释缺少边界框，跳过");
                return null;
            }

            // 转换坐标系：PDF坐标(pt) -> OFD坐标(mm)
            // rect是PdfArray，需要转换为Rectangle
            var rectangle = rect.ToRectangle();
            double x = rectangle.GetX() * Pt2Mm;
            double y = rectangle.GetY() * Pt2Mm;
            double width = rectangle.GetWidth() * Pt2Mm;
            double height = rectangle.GetHeight() * Pt2Mm;

            // 创建边界框
            var boundary = new OfdrwNet.Core.BasicType.StBox(x, y, width, height);

            // 生成注释ID
            var annotationId = new OfdrwNet.Core.BasicType.StId(GetNextAnnotationId());
            var pageId = new OfdrwNet.Core.BasicType.StId(pageIndex);

            // 根据注释类型进行转换
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
                        return null;
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

    private static object? ConvertHighlightAnnotation(iText.Kernel.Pdf.Annot.PdfAnnotation pdfAnnotation,
        OfdrwNet.Core.BasicType.StId annotationId, OfdrwNet.Core.BasicType.StId pageId,
        OfdrwNet.Core.BasicType.StBox boundary, ILogger? logger)
    {
        try
        {
            // 创建RGB颜色空间
            var rgbColorSpace = new OfdrwNet.Core.Resource.ColorSpace(new OfdrwNet.Core.BasicType.StId(1), OfdrwNet.Core.Resource.ColorSpaceType.RGB);

            // 默认高亮颜色为黄色
            var color = new OfdrwNet.Core.Resource.Color(new OfdrwNet.Core.BasicType.StId(1), rgbColorSpace)
            {
                Components = new double[] { 1.0, 1.0, 0.0 } // 黄色
            };

            // 尝试从注释中提取颜色信息
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

            var highlightAnnotation = new OfdrwNet.Core.Annotation.HighlightAnnotation(annotationId, pageId, boundary, color);

            // 获取注释内容
            var contents = pdfAnnotation.GetContents();
            if (contents != null && !string.IsNullOrEmpty(contents.ToString()))
            {
                highlightAnnotation.Content = contents.ToString();
            }

            // 获取创建者
            var author = pdfAnnotation.GetPdfObject().GetAsString(iText.Kernel.Pdf.PdfName.T);
            if (author != null)
            {
                highlightAnnotation.Creator = author.ToString();
            }

            // 获取标题
            var title = pdfAnnotation.GetPdfObject().GetAsString(iText.Kernel.Pdf.PdfName.Subj);
            if (title != null)
            {
                highlightAnnotation.Title = title.ToString();
            }

            // 设置创建时间
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

            // 添加高亮区域（使用整个边界框作为高亮区域）
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

    private static object? ConvertTextAnnotation(iText.Kernel.Pdf.Annot.PdfAnnotation pdfAnnotation,
        OfdrwNet.Core.BasicType.StId annotationId, OfdrwNet.Core.BasicType.StId pageId,
        OfdrwNet.Core.BasicType.StBox boundary, ILogger? logger)
    {
        // 文本注释转换为高亮注释（简化处理）
        return ConvertHighlightAnnotation(pdfAnnotation, annotationId, pageId, boundary, logger);
    }

    private static object? ConvertLinkAnnotation(iText.Kernel.Pdf.Annot.PdfAnnotation pdfAnnotation,
        OfdrwNet.Core.BasicType.StId annotationId, OfdrwNet.Core.BasicType.StId pageId,
        OfdrwNet.Core.BasicType.StBox boundary, ILogger? logger)
    {
        try
        {
            // 默认链接类型和目标
            var linkType = OfdrwNet.Core.Annotation.LinkType.Url;
            var target = "#";

            // 尝试获取链接目标
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
                            linkType = OfdrwNet.Core.Annotation.LinkType.Url;
                            target = uri.ToString();
                        }
                    }
                    else if (actionTypeStr == "/GoTo")
                    {
                        // 处理页面跳转
                        var dest = action.Get(iText.Kernel.Pdf.PdfName.D);
                        if (dest != null)
                        {
                            linkType = OfdrwNet.Core.Annotation.LinkType.Page;
                            target = dest.ToString();
                        }
                    }
                }
            }

            var linkAnnotation = new OfdrwNet.Core.Annotation.LinkAnnotation(annotationId, pageId, boundary, linkType, target ?? "#");

            // 获取注释内容
            var contents = pdfAnnotation.GetContents();
            if (contents != null && !string.IsNullOrEmpty(contents.ToString()))
            {
                linkAnnotation.Content = contents.ToString();
            }

            // 获取创建者
            var author = pdfAnnotation.GetPdfObject().GetAsString(iText.Kernel.Pdf.PdfName.T);
            if (author != null)
            {
                linkAnnotation.Creator = author.ToString();
            }

            // 设置创建时间
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

    private static object? ConvertStampAnnotation(iText.Kernel.Pdf.Annot.PdfAnnotation pdfAnnotation,
        OfdrwNet.Core.BasicType.StId annotationId, OfdrwNet.Core.BasicType.StId pageId,
        OfdrwNet.Core.BasicType.StBox boundary, ILogger? logger)
    {
        // 图章注释暂时转换为高亮注释（简化处理）
        logger?.LogDebug("[PDF2OFD][Annotation] 图章注释转换为高亮注释: {Id}", annotationId);
        return ConvertHighlightAnnotation(pdfAnnotation, annotationId, pageId, boundary, logger);
    }

    private static DateTime ParsePdfDate(string pdfDate)
    {
        // PDF日期格式: D:YYYYMMDDHHMMSS+TZ 或 D:YYYYMMDDHHMMSS
        if (string.IsNullOrEmpty(pdfDate) || !pdfDate.StartsWith("D:"))
            throw new ArgumentException("Invalid PDF date format");

        var dateStr = pdfDate.Substring(2);
        if (dateStr.Length < 14)
            throw new ArgumentException("PDF date string too short");

        var year = int.Parse(dateStr.Substring(0, 4));
        var month = int.Parse(dateStr.Substring(4, 2));
        var day = int.Parse(dateStr.Substring(6, 2));
        var hour = int.Parse(dateStr.Substring(8, 2));
        var minute = int.Parse(dateStr.Substring(10, 2));
        var second = int.Parse(dateStr.Substring(12, 2));

        return new DateTime(year, month, day, hour, minute, second, DateTimeKind.Local);
    }

    private static int _nextAnnotationId = 1;
    private static int GetNextAnnotationId()
    {
        return System.Threading.Interlocked.Increment(ref _nextAnnotationId);
    }

    private static object? ConvertPdfFormFieldToOfd(iText.Forms.Fields.PdfFormField pdfField, ILogger? logger)
    {
        // 暂时返回null，稍后实现具体的表单字段转换逻辑
        logger?.LogDebug("[PDF2OFD][Form] 表单字段类型: {Type}", pdfField.GetType().Name);
        return null;
    }

    // OfdImage 和 OfdText 已移至 OfdrwNet.Abstractions
    #endregion
}

internal class ImageRenderListener : IEventListener
{
    private readonly int _pageNum;
    private readonly iTextRectangle _pageSize;
    private readonly ConvertHelper.PdfToOfdOptions _options;
    private readonly ILogger? _logger;

    public List<OfdImage> Images { get; } = new List<OfdImage>();

    public ImageRenderListener(int pageNum, iTextRectangle pageSize, ConvertHelper.PdfToOfdOptions options, ILogger? logger)
    {
        _pageNum = pageNum;
        _pageSize = pageSize;
        _options = options;
        _logger = logger;
    }

    public void EventOccurred(IEventData data, EventType type)
    {
        if (type == EventType.RENDER_IMAGE)
        {
            var renderInfo = (ImageRenderInfo)data;
            var imageObject = renderInfo.GetImage();
            if (imageObject == null)
            {
                _logger?.LogWarning("[PDF2OFD][Image] Page {PageNum} 发现空图片对象", _pageNum);
                return;
            }

            try
            {
                byte[] imageBytes;
                try
                {
                    imageBytes = imageObject.GetImageBytes();
                }
                catch (iText.IO.Exceptions.IOException ex) when (ex.Message.Contains("color space") && ex.Message.Contains("not supported"))
                {
                    _logger?.LogWarning(ex, "[PDF2OFD][Image] Page {Page} 色彩空间不支持，尝试原始数据提取", _pageNum);
                    try
                    {
                        var pdfStream = imageObject.GetPdfObject();
                        var rawBytes = pdfStream.GetBytes(true);
                        if (rawBytes != null && rawBytes.Length > 0)
                        {
                            try
                            {
                                // 方案2: 尝试使用 SixLabors.ImageSharp 解码
                                using var image = SixLabors.ImageSharp.Image.Load(rawBytes);
                                using var ms = new MemoryStream();
                                image.Save(ms, new PngEncoder());
                                imageBytes = ms.ToArray();
                                _logger?.LogInformation("[PDF2OFD][Image] Page {Page} 使用 SixLabors.ImageSharp 解码成功", _pageNum);
                            }
                            catch (Exception exSharp)
                            {
                                _logger?.LogWarning(exSharp, "[PDF2OFD][Image] Page {Page} SixLabors.ImageSharp 解码失败，尝试 SkiaSharp", _pageNum);
                                // 方案3: 尝试使用 SkiaSharp 解码
                                using var skBitmap = SKBitmap.Decode(rawBytes);
                                if (skBitmap != null)
                                {
                                    using var image = SKImage.FromBitmap(skBitmap);
                                    using var pngData = image.Encode(SKEncodedImageFormat.Png, 100);
                                    imageBytes = pngData.ToArray();
                                }
                                else
                                {
                                    // 解码失败，尝试强制解码
                                    imageBytes = imageObject.GetImageBytes(true);
                                }
                            }
                        }
                        else
                        {
                            // 原始数据为空，则尝试强制解码
                            imageBytes = imageObject.GetImageBytes(true);
                        }
                    }
                    catch (Exception ex2)
                    {
                        _logger?.LogError(ex2, "[PDF2OFD][Image] Page {Page} 所有备选解码方案均失败，使用透明占位符", _pageNum);
                        // 所有解码都失败了，使用一个1x1的透明PNG作为占位符
                        imageBytes = new byte[] {
                            0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A, 0x00, 0x00, 0x00, 0x0D, 0x49, 0x48, 0x44, 0x52,
                            0x00, 0x00, 0x00, 0x01, 0x00, 0x00, 0x00, 0x01, 0x08, 0x06, 0x00, 0x00, 0x00, 0x1F, 0x15, 0xC4,
                            0x89, 0x00, 0x00, 0x00, 0x0A, 0x49, 0x44, 0x41, 0x54, 0x78, 0x9C, 0x63, 0x00, 0x01, 0x00, 0x00,
                            0x05, 0x00, 0x01, 0x0D, 0x0A, 0x2D, 0xB4, 0x00, 0x00, 0x00, 0x00, 0x49, 0x45, 0x4E, 0x44, 0xAE,
                            0x42, 0x60, 0x82
                        };
                    }
                }
                catch (Exception ex)
                {
                    _logger?.LogWarning(ex, "[PDF2OFD][Image] Page {Page} 初次解码失败，尝试强制解码", _pageNum);
                    try { imageBytes = imageObject.GetImageBytes(true); }
                    catch (Exception ex2) { _logger?.LogError(ex2, "[PDF2OFD][Image] Page {Page} 强制解码失败，跳过图片", _pageNum); return; }
                }

                var matrix = renderInfo.GetImageCtm();
                float width = matrix.Get(Matrix.I11);
                float height = matrix.Get(Matrix.I22);
                float x = matrix.Get(Matrix.I31);
                float y = matrix.Get(Matrix.I32);

                // 处理翻转：若宽/高为负，平移修正后取绝对值
                if (width < 0) { x += width; width = -width; }
                if (height < 0) { y += height; height = -height; }
                // 坐标系转换
                y = _pageSize.GetHeight() - (y + height);
                // 统一单位: pt -> mm
                x = (float)(x * ConvertHelper.Pt2Mm);
                y = (float)(y * ConvertHelper.Pt2Mm);
                width = (float)(width * ConvertHelper.Pt2Mm);
                height = (float)(height * ConvertHelper.Pt2Mm);

                // Prepare CTM array (a b c d e f) converted to mm and aligned to OFD coord system
                try
                {
                    var a = matrix.Get(Matrix.I11);
                    var b = matrix.Get(Matrix.I12);
                    var c = matrix.Get(Matrix.I21);
                    var d = matrix.Get(Matrix.I22);
                    var e = matrix.Get(Matrix.I31);
                    var f = matrix.Get(Matrix.I32);
                    // Use the computed top-left x/y (after flip) for translation components to keep CTM aligned with bounding box
                    double[] ctm = new double[]
                    {
                        a * ConvertHelper.Pt2Mm,
                        b * ConvertHelper.Pt2Mm,
                        c * ConvertHelper.Pt2Mm,
                        d * ConvertHelper.Pt2Mm,
                        x, // already converted to mm
                        y  // already converted to mm
                    };

                    _logger?.LogDebug("[PDF2OFD][Image] Page {PageNum} 提取到图片: X={X}, Y={Y}, W={W}, H={H}, Size={Size} bytes", _pageNum, x, y, width, height, imageBytes.Length);

                    Images.Add(new OfdImage
                    {
                        Page = _pageNum,
                        X = x,
                        Y = y,
                        Width = width,
                        Height = height,
                        ImageData = imageBytes,
                        Format = imageObject.IdentifyImageFileExtension(),
                        CTM = ctm
                    });
                }
                catch
                {
                    // 回退：若任何CTM计算失败，仍输出基本属性
                    _logger?.LogDebug("[PDF2OFD][Image] Page {PageNum} CTM 计算失败，降级输出无CTM的图片", _pageNum);
                    Images.Add(new OfdImage
                    {
                        Page = _pageNum,
                        X = x,
                        Y = y,
                        Width = width,
                        Height = height,
                        ImageData = imageBytes,
                        Format = imageObject.IdentifyImageFileExtension()
                    });
                }
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "[PDF2OFD][Image] Page {PageNum} 提取图片数据时发生异常", _pageNum);
            }
        }
    }

    public ICollection<EventType> GetSupportedEvents() => new[] { EventType.RENDER_IMAGE };
}

// 文本聚合辅助
internal static class TextAggregationHelper
{
    public static List<OfdText> Aggregate(List<OfdText> raw, ILogger? logger, int page)
    {
        if (raw.Count == 0) return raw;
        var orderedAll = raw.OrderBy(t => t.Y).ToList();
        var lineBuckets = new List<List<OfdText>>();
        foreach (var blk in orderedAll)
        {
            bool placed = false;
            foreach (var line in lineBuckets)
            {
                double refFont = line[0].FontSize <= 0 ? 12d : line[0].FontSize;
                double tolerance = Math.Max(1.5d, refFont * 0.8d);
                if (Math.Abs(line[0].Y - blk.Y) < tolerance)
                {
                    line.Add(blk);
                    placed = true;
                    break;
                }
            }
            if (!placed)
            {
                lineBuckets.Add(new List<OfdText> { blk });
            }
        }

        var result = new List<OfdText>(lineBuckets.Count);
        foreach (var line in lineBuckets)
        {
            var segs = line.OrderBy(s => s.X).ToList();
            if (segs.Count == 0) continue;
            var fontGroup = segs.GroupBy(s => s.FontFamily).OrderByDescending(g => g.Count()).First();
            string lineFont = fontGroup.Key;
            double avgSize = segs.Average(s => (double)s.FontSize);
            double minX = segs.Min(s => (double)s.X);
            double EstimateWidth(OfdText t)
                => t.Width > 0 ? t.Width : (float)((t.FontSize > 0 ? t.FontSize : avgSize) * 0.6d * Math.Max(1, t.Text.Length));

            var sb = new System.Text.StringBuilder();
            var first = segs[0];
            sb.Append(first.Text);
            double cursorRight = first.X + EstimateWidth(first);
            double maxH = first.Height > 0 ? first.Height : (first.FontSize > 0 ? first.FontSize * 1.2d : avgSize * 1.2d);

            for (int i = 1; i < segs.Count; i++)
            {
                var cur = segs[i];
                double curEstW = EstimateWidth(cur);
                double gap = cur.X - cursorRight;
                double refSize = cur.FontSize > 0 ? cur.FontSize : avgSize;
                if (gap > refSize * 0.55d)
                {
                    sb.Append(' ');
                }
                sb.Append(cur.Text);
                cursorRight = Math.Max(cursorRight, cur.X + curEstW);
                if (cur.Height > 0) maxH = Math.Max(maxH, cur.Height);
            }

            string merged = sb.ToString();
            if (string.IsNullOrWhiteSpace(merged)) continue;

            result.Add(new OfdText
            {
                Page = segs[0].Page,
                Text = merged,
                X = (float)minX,
                Y = segs.Min(s => s.Y),
                Width = (float)(cursorRight - minX),
                Height = (float)(maxH <= 0 ? avgSize * 1.2d : maxH),
                FontFamily = lineFont,
                FontSize = (float)avgSize
            });
        }
        logger?.LogDebug("[PDF2OFD][Text][Aggregate] Page {Page} 原始={Raw} 行数={Lines} 聚合后块={Agg}", page, raw.Count, lineBuckets.Count, result.Count);
        return result;
    }
}

internal class TextRenderListener : IEventListener
{
    private readonly int _pageNum;
    private readonly iTextRectangle _pageSize;
    private readonly ConvertHelper.PdfToOfdOptions _options;
    private readonly ILogger? _logger;

    public List<OfdText> TextBlocks { get; } = new List<OfdText>();

    public TextRenderListener(int pageNum, iTextRectangle pageSize, ConvertHelper.PdfToOfdOptions options, ILogger? logger)
    {
        _pageNum = pageNum;
        _pageSize = pageSize;
        _options = options;
        _logger = logger;
    }

    public void EventOccurred(IEventData data, EventType type)
    {
        if (type == EventType.RENDER_TEXT)
        {
            var renderInfo = (TextRenderInfo)data;
            var text = renderInfo.GetText();
            if (string.IsNullOrWhiteSpace(text)) return;

            var ascentLine = renderInfo.GetAscentLine();
            var descentLine = renderInfo.GetDescentLine();

            var x = descentLine.GetStartPoint().Get(0);
            var yBase = descentLine.GetStartPoint().Get(1);
            var width = descentLine.GetEndPoint().Get(0) - x;
            var heightRaw = ascentLine.GetStartPoint().Get(1) - yBase;
            if (heightRaw < 0) heightRaw = Math.Abs(heightRaw);
            var pageHeight = _pageSize.GetHeight();
            var y = pageHeight - yBase - heightRaw; // 仍是pt
            var fontProgram = renderInfo.GetFont().GetFontProgram();
            var fontNames = fontProgram.GetFontNames();
            var fontFamily = ConvertHelper.NormalizeLogicalFontName(fontNames.GetFontName() ?? fontNames.GetFamilyName()?.ToString() ?? "DefaultFont");
            var fontSizePt = renderInfo.GetFontSize();
            // 宽高/坐标/字号统一转换为mm
            double xMm = x * ConvertHelper.Pt2Mm;
            double yMm = y * ConvertHelper.Pt2Mm;
            double wMm = width <= 0 ? fontSizePt * Math.Max(1, text.Length * 0.6) : width;
            wMm *= ConvertHelper.Pt2Mm;
            double hMm = heightRaw <= 0 ? fontSizePt * 1.2 : heightRaw;
            hMm *= ConvertHelper.Pt2Mm;
            double fontSizeMm = fontSizePt * ConvertHelper.Pt2Mm;
            _logger?.LogTrace("[PDF2OFD][Text] Page {PageNum} 文本: '{Text}' X={X}mm Y={Y}mm W={W}mm H={H}mm Font={Font} Size={Size}mm", _pageNum, text, xMm, yMm, wMm, hMm, fontFamily, fontSizeMm);

            // 尝试捕获 CTM 与 每字形 DeltaX（以 mm 为单位）以便在 OFD 中输出更精确的定位
            double[]? ctmArray = null;
            float[]? deltaXArray = null;
            try
            {
                // 获取文本矩阵（6 参数）
                var textMatrix = renderInfo.GetTextMatrix();
                var a = textMatrix.Get(Matrix.I11);
                var b = textMatrix.Get(Matrix.I12);
                var c = textMatrix.Get(Matrix.I21);
                var d = textMatrix.Get(Matrix.I22);
                var e = textMatrix.Get(Matrix.I31);
                var f = textMatrix.Get(Matrix.I32);

                // 将 a/b/c/d 按比例换算为 mm（缩放分量），将 e/f 转换为 OFD 坐标系下的 mm 平移量
                double txMm = xMm; // 使用先前计算的基准左上坐标作为翻译分量
                double tyMm = yMm;
                ctmArray = new double[] { a * ConvertHelper.Pt2Mm, b * ConvertHelper.Pt2Mm, c * ConvertHelper.Pt2Mm, d * ConvertHelper.Pt2Mm, txMm, tyMm };

                if (_options.EnableDeltaX)
                {
                    var charInfos = renderInfo.GetCharacterRenderInfos();
                    if (charInfos != null && charInfos.Count > 0)
                    {
                        var deltas = new List<float>(charInfos.Count);
                        // 以基线起点 X 位置计算相邻字符的 X 增量
                        float? prevX = null;
                        foreach (var ci in charInfos)
                        {
                            try
                            {
                                var ds = ci.GetDescentLine();
                                var cx = ds.GetStartPoint().Get(0);
                                if (prevX is null)
                                {
                                    deltas.Add(0f); // 首字形增量设为0
                                }
                                else
                                {
                                    var dxPt = cx - prevX.Value;
                                    deltas.Add((float)(dxPt * ConvertHelper.Pt2Mm));
                                }
                                prevX = cx;
                            }
                            catch
                            {
                                deltas.Add(0f);
                            }
                        }
                        deltaXArray = deltas.ToArray();
                    }
                }
            }
            catch (Exception ex)
            {
                _logger?.LogDebug(ex, "[PDF2OFD][Text] Page {PageNum} 捕获 CTM/DeltaX 失败，降级为简单边界输出", _pageNum);
                ctmArray = null;
                deltaXArray = null;
            }

            TextBlocks.Add(new OfdText
            {
                Page = _pageNum,
                Text = text,
                X = (float)xMm,
                Y = (float)yMm,
                Width = (float)wMm,
                Height = (float)hMm,
                FontFamily = fontFamily,
                FontSize = (float)fontSizeMm,
                CTM = ctmArray,
                DeltaX = deltaXArray
            });
        }
    }

    public ICollection<EventType> GetSupportedEvents() => new[] { EventType.RENDER_TEXT };
}
