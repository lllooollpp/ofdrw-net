using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using OfdrwNet.Reader;
using OfdrwNet.Converter.Export;
using System.Collections.Generic; // 新增：页面过滤
//using OfdrwNet; // 移除直接引用以避免循环
using iText.Kernel.Pdf.Canvas.Parser;
using iText.Kernel.Pdf.Canvas.Parser.Listener;
using iText.Kernel.Pdf.Canvas.Parser.Data;
using iText.Kernel.Geom;
using Path = System.IO.Path; // 解决与 iText.Kernel.Geom.Path 冲突

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
    }

    public static void ToOfd(string pdfPath, string ofdOutputPath, PdfToOfdOptions? options = null) => ToOfdAsync(pdfPath, ofdOutputPath, options).GetAwaiter().GetResult();

    public static async Task ToOfdAsync(string pdfPath, string ofdOutputPath, PdfToOfdOptions? options = null)
    {
        if (string.IsNullOrWhiteSpace(pdfPath) || !File.Exists(pdfPath)) throw new FileNotFoundException("PDF不存在", pdfPath);
        if (string.IsNullOrWhiteSpace(ofdOutputPath)) throw new ArgumentException("输出路径不能为空", nameof(ofdOutputPath));
        options ??= new PdfToOfdOptions();

        using var reader = new iText.Kernel.Pdf.PdfReader(pdfPath);
        using var pdfDoc = new iText.Kernel.Pdf.PdfDocument(reader);

        // 1. 字体抽取
        var fontFileTempMap = new Dictionary<string,string?>();
        var visited = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        int totalPages = pdfDoc.GetNumberOfPages();
        int processed = 0;
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
                    var baseName = fontDict.GetAsName(iText.Kernel.Pdf.PdfName.BaseFont)?.GetValue() ?? fontName.GetValue();
                    if (!visited.Add(baseName)) continue;
                    if (!options.ExtractAndEmbedFonts) continue;
                    try
                    {
                        var descriptor = fontDict.GetAsDictionary(iText.Kernel.Pdf.PdfName.FontDescriptor);
                        iText.Kernel.Pdf.PdfStream? ff = descriptor?.GetAsStream(iText.Kernel.Pdf.PdfName.FontFile3) ?? descriptor?.GetAsStream(iText.Kernel.Pdf.PdfName.FontFile2) ?? descriptor?.GetAsStream(iText.Kernel.Pdf.PdfName.FontFile);
                        if (ff != null)
                        {
                            var bytes = ff.GetBytes();
                            // 简单类型判断
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

        // 2. 创建 OFD 文档 (通过反射，避免直接项目引用造成循环)
        var ofdType = Type.GetType("OfdrwNet.OFDDoc, OfdrwNet", throwOnError: false);
        if (ofdType == null) throw new InvalidOperationException("无法加载 OfdrwNet.OFDDoc 类型，请确认 OfdrwNet 程序集引用");
        using var ofd = (IDisposable)Activator.CreateInstance(ofdType, ofdOutputPath)!;
        // AddExternalEmbeddedFont 调用
        var addFontMethod = ofdType.GetMethod("AddExternalEmbeddedFont");
        var addGlyphRunMethod = ofdType.GetMethod("AddRawTextGlyphRun");
        foreach (var kv in fontFileTempMap)
        {
            if (kv.Value != null && addFontMethod != null)
            {
                try { addFontMethod.Invoke(ofd, new object?[] { kv.Key, kv.Value }); } catch { }
            }
        }
        // 3. 逐字定位解析（可选）
        if (options.PerGlyphPositioning && addGlyphRunMethod != null)
        {
            ExtractGlyphRuns(pdfDoc, ofd, ofdType, addGlyphRunMethod, options);
        }
        var closeAsync = ofdType.GetMethod("CloseAsync");
        if (closeAsync != null)
        {
            var task = (Task)closeAsync.Invoke(ofd, null)!;
            await task.ConfigureAwait(false);
        }
        else
        {
            // 退化：尝试 Close()
            ofdType.GetMethod("Close")?.Invoke(ofd, null);
        }

        foreach (var val in fontFileTempMap.Values)
        {
            try { if (val != null && File.Exists(val)) File.Delete(val); } catch { }
        }
    }

    private static void ExtractGlyphRuns(iText.Kernel.Pdf.PdfDocument pdfDoc, IDisposable ofdInstance, Type ofdType, System.Reflection.MethodInfo addGlyphRunMethod, PdfToOfdOptions options)
    {
        double mmPerUnit = 25.4 / 72.0; // PDF 用户单位 -> mm
        int total = pdfDoc.GetNumberOfPages();
        for (int pageIndex = 1; pageIndex <= total; pageIndex++)
        {
            options.CancellationToken.ThrowIfCancellationRequested();
            var page = pdfDoc.GetPage(pageIndex);
            var pageSize = page.GetPageSize();
            double pageHeightMm = pageSize.GetHeight() * mmPerUnit;
            var listener = new GlyphCollectListener((fontName, fontSizeUser, chars, xsUser, ysUser) =>
            {
                if (string.IsNullOrEmpty(chars)) return;
                double fontSizeMm = fontSizeUser * mmPerUnit;
                if (xsUser.Count == 0) return;
                double baseXmm = xsUser[0] * mmPerUnit;
                double baseYmmBaseline = ysUser[0] * mmPerUnit;
                double topYmm = pageHeightMm - (baseYmmBaseline + fontSizeMm * 0.2);
                var deltaX = new double[Math.Max(0, xsUser.Count - 1)];
                for (int i = 1; i < xsUser.Count; i++)
                {
                    deltaX[i - 1] = (xsUser[i] - xsUser[i - 1]) * mmPerUnit;
                }
                try
                {
                    // 签名: AddRawTextGlyphRun(string fontName, double fontSizeMm, double x, double yTop, string text, double[]? deltaX, double[]? deltaY, int pageIndex)
                    addGlyphRunMethod.Invoke(ofdInstance, new object?[] { fontName, fontSizeMm, baseXmm, topYmm, chars, deltaX, null, pageIndex });
                }
                catch { }
            });
            var processor = new PdfCanvasProcessor(listener);
            processor.ProcessPageContent(page);
        }
    }

    private class GlyphCollectListener : IEventListener
    {
        private readonly Action<string,double,string,List<float>,List<float>> _onRun;
        public GlyphCollectListener(Action<string,double,string,List<float>,List<float>> onRun) { _onRun = onRun; }
        public void EventOccurred(IEventData data, EventType type)
        {
            if (type != EventType.RENDER_TEXT) return;
            if (data is not TextRenderInfo tri) return;
            try
            {
                var font = tri.GetFont();
                string fontName = font?.GetFontProgram()?.GetFontNames()?.GetFontName() ?? font?.GetFontProgram()?.ToString() ?? "Unknown";
                float fs = tri.GetFontSize();
                var charInfos = tri.GetCharacterRenderInfos();
                if (charInfos == null || charInfos.Count == 0) return;
                // 排除旋转（暂不支持）：判断文本矩阵是否纯水平
                var m = tri.GetTextMatrix();
                if (Math.Abs(m.Get(1)) > 0.0001 || Math.Abs(m.Get(2)) > 0.0001) return; // 有旋转/斜切
                var xs = new List<float>();
                var ys = new List<float>();
                var sb = new System.Text.StringBuilder();
                foreach (var ci in charInfos)
                {
                    var baseline = ci.GetBaseline();
                    var sp = baseline.GetStartPoint();
                    xs.Add(sp.Get(0));
                    ys.Add(sp.Get(1));
                    sb.Append(ci.GetText());
                }
                _onRun(fontName, fs, sb.ToString(), xs, ys);
            }
            catch { }
        }
        public ICollection<EventType> GetSupportedEvents() => new[] { EventType.RENDER_TEXT };
    }

    #endregion
}
