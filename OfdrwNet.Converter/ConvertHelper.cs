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
using OfdrwNet.Core.BasicStructure.Ofd.DocInfo;
// T075/T076: 服务命名空间
using OfdrwNet.Abstractions.Forms;
using OfdrwNet.Converter.Forms;
using OfdrwNet.Converter.Scripting;
using OfdrwNet.Converter.Interaction;

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
        public bool PerGlyphPositioning { get; set; } = true; // 预留第2阶段
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

        /// <summary>
        /// 是否在聚合后再按空格拆分文本块（用于需要保留词级定位的场景）。
        /// 默认 false。
        /// </summary>
        public bool SplitTextBySpace { get; set; } = true;

        /// <summary>
        /// 仅对主要由拉丁字母/数字组成的行进行按空格词级拆分；
        /// 如果检测到行内包含 CJK (\u4E00-\u9FFF) 则回退为整行聚合。
        /// 默认 true 以避免中文被错误拆分。
        /// </summary>
        public bool OnlySplitLatinWords { get; set; } = true;

        /// <summary>
        /// 触发将 gap 视为“至少一个空格”的水平距离阈值比例（相对于参考字体大小）。
        /// 默认 0.55 (与原启发式保持一致)。
        /// </summary>
        public double GapSpaceTriggerRatio { get; set; } = 0.55d;

        /// <summary>
        /// 单个 gap 允许合成的最大空格数量上限，避免超大 gap 生成过多占位。
        /// 默认 4。
        /// </summary>
        public int MaxSyntheticSpacesPerGap { get; set; } = 4;

    /// <summary>
    /// 合成空格时必须达到的最小间隙（mm）。用于避免数字等窄字符被错误拆分。
    /// 默认 0.45mm。
    /// </summary>
    public double MinGapForSyntheticSpaceMm { get; set; } = 0.45d;

    /// <summary>
    /// 允许吸收的最大负间距（mm）。用于忽略 PDF 中的轻微负 kerning，避免错误地回填空格。
    /// 默认 0.25mm。
    /// </summary>
    public double MaxNegativeKerningAbsorbMm { get; set; } = 0.25d;

    /// <summary>
    /// 当 gap 发生在主要由数字、连字符等组成的片段之间时，额外放大的触发系数。
    /// 数值越大，越不容易在数字间合成空格。默认 1.3。
    /// </summary>
    public double NumericGapMultiplier { get; set; } = 1.3d;

    /// <summary>
    /// 数字段之间触发合成空格所需的最小实际间距（mm）。默认 1.0mm。
    /// </summary>
    public double NumericMinGapMm { get; set; } = 1.0d;

        /// <summary>
        /// 启用后输出词级调试：包含字符起点/宽度、gap 判定与最终词矩形。
        /// 默认 false。
        /// </summary>
        public bool EnableDebugWordLayout { get; set; } = false;

        /// <summary>
        /// 对主要为 CJK 的文本，将宽度强制扩展到 字数 * 字号（避免 PDF 原 descent 线估算偏小导致截断）。默认 true。
        /// </summary>
        public bool ExpandCjkWidth { get; set; } = true;

        /// <summary>
        /// 在扩展 CJK 宽度时额外增加的右侧余量比例 (相对单字宽)，默认 0.12。
        /// 例如 8 个字，字号 16pt，则附加 = 16pt * 0.12。
        /// </summary>
        public double CjkExtraAdvanceRatio { get; set; } = 0.12d;

        /// <summary>
        /// CJK 主体行（检测到大量中文且 ASCII 字母比例低）用于合成半角空格的 gap 触发比例（相对参考空格宽 baseRef）。
        /// 设为 0.45 表示 gap > baseRef * 0.45 即认为需要插入一个空格。默认 0.45。
        /// 该值用于修正中文等宽字体的 AvgAdvance 较大导致原通用阈值过高、空隙未被判定为空格的问题。
        /// </summary>
        public double CjkGapTriggerRatio { get; set; } = 0.45d;

        /// <summary>
        /// 图片叠放顺序策略（默认 Sequence：后添加覆盖前添加）。
        /// 可选：Sequence / YAscending / YDescending。
        /// </summary>
        public string ImageOrdering { get; set; } = "Sequence"; // 与 OfdWriter 中策略匹配

        /// <summary>
        /// 将接近白色(#FFFFFF)背景像素转换为透明。默认 false 不处理。
        /// </summary>
        public bool MakeWhiteBackgroundTransparent { get; set; } = true;

        /// <summary>
        /// 认为是“白色”的阈值(0-255)。像素 R/G/B 全部 >= 此值则视为白。默认 250。
        /// </summary>
        public byte WhiteThreshold { get; set; } = 250;

        /// <summary>
        /// 透明化后若整体透明像素比例 >= 此值(0-1) 且原图无 Alpha，则自动保留一层最外框 1px 边界不透明（防止全透明消失）。默认 0.98。
        /// </summary>
        public double PreserveBorderIfAlmostAllTransparentRatio { get; set; } = 0.98;

        /// <summary>
        /// 仅当图片本身无 Alpha 通道时才尝试转换；否则如果已有 Alpha 则不再二次抹白。默认 true。
        /// </summary>
        public bool OnlyIfOpaque { get; set; } = true;

        /// <summary>
        /// 若图像像素格式包含 Alpha 通道，但所有像素 A 均为 255（即“形式上有 Alpha，实际上完全不透明”），
        /// 且设置了 OnlyIfOpaque=true，则可将其视为“无 Alpha”继续做白底转透明。
        /// 默认 true 以处理常见库（例如某些解码自动给 RGBA）的情况。
        /// </summary>
        public bool TreatFullAlphaAsOpaque { get; set; } = true;

        /// <summary>
        /// 调试：在资源写出后重新读取图片并统计透明像素比例，输出日志。[默认 false]
        /// 仅用于定位透明丢失问题，会增加 I/O 开销。
        /// </summary>
        public bool DebugVerifyOutputImageAlpha { get; set; } = false;

        // 新增：为兼容性添加的属性
        public bool EnableImageExtraction { get { return ExtractImage; } set { ExtractImage = value; } }
        public bool EnableAnnotationExtraction { get { return ExtractAnnotations; } set { ExtractAnnotations = value; } }
        public bool EnableFormExtraction { get { return ExtractForms; } set { ExtractForms = value; } }

        /// <summary>
        /// 输出 OFD 文档的版本号（默认 null 使用系统默认值）。
        /// </summary>
        public string? TargetOfdVersion { get; set; }

        /// <summary>
        /// 允许调用方注入 DocInfo 配置。
        /// </summary>
        public Action<CtDocInfo>? ConfigureDocInfo { get; set; }

        /// <summary>
        /// 是否自动生成 DocID（默认 true）。
        /// </summary>
        public bool AutoGenerateDocId { get; set; } = true;

        /// <summary>
        /// 显式覆盖 DocID（为空时不覆盖）。
        /// </summary>
        public string? OverrideDocId { get; set; }

        /// <summary>
        /// 当为 true 时，在写入 DocInfo 前移除现有 DocID（与 AutoGenerateDocId=false 配合使用）。
        /// </summary>
        public bool RemoveDocId { get; set; }

        /// <summary>
        /// 覆盖文档标题（DocInfo/Title）。
        /// </summary>
        public string? DocTitle { get; set; }

        /// <summary>
        /// 覆盖作者（DocInfo/Author）。
        /// </summary>
        public string? DocAuthor { get; set; }

        /// <summary>
        /// 覆盖主题（DocInfo/Subject）。
        /// </summary>
        public string? DocSubject { get; set; }

        /// <summary>
        /// 覆盖关键词（DocInfo/Keywords 原始文本）。
        /// </summary>
        public string? DocKeywords { get; set; }

        /// <summary>
        /// 覆盖创建应用程序（DocInfo/Creator）。
        /// </summary>
        public string? DocCreator { get; set; }

        /// <summary>
        /// 覆盖创建应用程序版本（DocInfo/CreatorVersion）。
        /// </summary>
        public string? DocCreatorVersion { get; set; }

        /// <summary>
        /// 直接设置 DocInfo/CreationDate 的原始字符串（例如 PDF 的 D: 格式）。
        /// </summary>
        public string? DocCreationDateRaw { get; set; }

        /// <summary>
        /// 直接设置 DocInfo/ModDate 的原始字符串。
        /// </summary>
        public string? DocModDateRaw { get; set; }

        // ============================================
        // 高级转换特性选项 (Phase 3.4 Integration - T073)
        // ============================================

        /// <summary>
        /// 启用表格识别（默认 false）。
        /// </summary>
        public bool EnableTableRecognition { get; set; } = false;

        /// <summary>
        /// 启用公式识别（默认 false）。
        /// </summary>
        public bool EnableFormulaRecognition { get; set; } = false;

        /// <summary>
        /// 启用颜色精度验证（ΔE检查，默认 false）。
        /// </summary>
        public bool EnableColorValidation { get; set; } = false;

        /// <summary>
        /// 转换后验证OFD结构（默认 false）。
        /// </summary>
        public bool EnableValidation { get; set; } = false;

        /// <summary>
        /// RGB颜色精度阈值（ΔE，默认 2.0）。
        /// </summary>
        public double DeltaEThreshold { get; set; } = 2.0;

        /// <summary>
        /// CMYK颜色精度阈值（ΔE，默认 5.0）。
        /// </summary>
        public double CmykDeltaEThreshold { get; set; } = 5.0;

        /// <summary>
        /// 兼容性配置文件名称（可选，例如 "Suwell 9.x"）。
        /// </summary>
        public string? CompatibilityProfile { get; set; }

        /// <summary>
        /// 输出转换报告路径（可选，JSON格式）。
        /// </summary>
        public string? ReportPath { get; set; }

        /// <summary>
        /// 启用版本控制（默认 false）。
        /// </summary>
        public bool EnableVersioning { get; set; } = false;

        /// <summary>
        /// 内存警告阈值（MB，默认 2000）。
        /// </summary>
        public double MemoryWarningThresholdMB { get; set; } = 2000;

        /// <summary>
        /// 内存严重阈值（MB，默认 3000）。
        /// </summary>
        public double MemoryCriticalThresholdMB { get; set; } = 3000;

        // ==== 服务注入 (T073集成) ====

        /// <summary>
        /// 颜色空间转换器 (可选)。用于RGB/CMYK → sRGB转换并验证色差(ΔE)
        /// </summary>
        public ColorManagement.ColorSpaceConverter? ColorConverter { get; set; }

        /// <summary>
        /// 表格识别器 (可选)。用于从文本中识别表格结构
        /// </summary>
        public Recognition.RuleBasedTableRecognizer? TableRecognizer { get; set; }

        /// <summary>
        /// 公式识别器 (可选)。用于识别数学公式
        /// </summary>
        public Recognition.BasicFormulaRecognizer? FormulaRecognizer { get; set; }

        /// <summary>
        /// 内存监控器 (可选)。用于在转换过程中监控内存使用
        /// </summary>
        public Batch.MemoryGuard? MemoryGuard { get; set; }

        /// <summary>
        /// 验证引擎 (可选)。用于对生成的OFD进行验证
        /// </summary>
        public Validation.CompositeValidationEngine? Validator { get; set; }

        /// <summary>
        /// 错误报告构建器 (可选)。用于生成验证报告
        /// </summary>
        public Reporting.ErrorReportBuilder? ReportBuilder { get; set; }

        // ==== T075: 表单服务注入 ====

        /// <summary>
        /// 表单字段映射器 (可选)。用于PDF表单字段到OFD的映射
        /// </summary>
        public IFormFieldMapper? FormMapper { get; set; }

        /// <summary>
        /// XFA检测器 (可选)。用于检测和处理XFA表单
        /// </summary>
        public XfaDetector? XfaDetector { get; set; }

        /// <summary>
        /// XFA提示写入器 (可选)。用于写入XFA降级提示
        /// </summary>
        public XfaHintWriter? XfaHintWriter { get; set; }

        /// <summary>
        /// JavaScript扫描器 (可选)。用于扫描表单中的JavaScript
        /// </summary>
        public JavaScriptScanner? JavaScriptScanner { get; set; }

        // ==== T076: 注释/交互服务注入 ====

        /// <summary>
        /// 书签转换器 (可选)。用于PDF书签转OFD书签
        /// </summary>
        public BookmarkConverter? BookmarkConverter { get; set; }

        /// <summary>
        /// 动作映射器 (可选)。用于PDF动作到OFD动作的映射
        /// </summary>
        public ActionMapper? ActionMapper { get; set; }
    }

    // 字体归一逻辑已迁移到 Refactor.Utils.FontUtils（保留向后兼容的内部代理，后续可删除）
    internal static string NormalizeLogicalFontName(string baseName) => Refactor.Utils.FontUtils.NormalizeLogicalFontName(baseName);
    private static string? FindSystemFontPath(string logical) => Refactor.Utils.FontUtils.FindSystemFontPath(logical);

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
            // 如果未提供日志记录器,创建一个临时的
            var lf = LoggerFactory.Create(b =>
            {
                b.AddConsole();
                b.SetMinimumLevel(Microsoft.Extensions.Logging.LogLevel.Debug);
            });
            logger = lf.CreateLogger("PDF2OFD");
            logger.LogInformation("[PDF2OFD] 未提供外部Logger,已启用内部临时Logger");
        }

        logger.LogInformation("[PDF2OFD] 开始转换 PDF -> OFD 输入={Pdf} 输出={Ofd}", pdfPath, ofdOutputDir);

        // T073: 内存检查初始化 (如果启用)
        var memoryGuard = options.MemoryGuard;
        if (memoryGuard != null)
        {
            logger.LogInformation("[PDF2OFD] 内存监控已启用 警告阈值={Warning}MB 关键阈值={Critical}MB",
                options.MemoryWarningThresholdMB, options.MemoryCriticalThresholdMB);
            var snapshot = memoryGuard.CheckMemory();
            if (snapshot.Action == Domain.MemoryAction.Abort)
            {
                logger.LogError("[PDF2OFD] 内存不足,转换中止 当前使用={Current}MB", snapshot.AllocatedMB);
                throw new OutOfMemoryException("转换前内存检查失败");
            }
        }

        if (!File.Exists(pdfPath))
        {
            logger.LogError("[PDF2OFD] PDF文件不存在: {Pdf}", pdfPath);
            throw new FileNotFoundException("PDF文件不存在", pdfPath);
        }

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

        // 1. 字体抽取（重构：委派 FontExtractor）
        var fontExtractor = new Refactor.FontExtractor();


        // 2. 创建 OFD 文档
        logger.LogInformation("[PDF2OFD] 创建 OfdWriter, 输出目录: {OfdOutputDir}", ofdOutputDir);
        using IOfdDocWriter ofd = new OfdWriter(ofdOutputDir, logger);
        if (ofd is OfdWriter writer)
        {
            var autoDocId = options.AutoGenerateDocId && !options.RemoveDocId;
            writer.SetAutoGenerateDocId(autoDocId);

            var shouldRemoveDocId = options.RemoveDocId || (!autoDocId && string.IsNullOrWhiteSpace(options.OverrideDocId));
            if (shouldRemoveDocId)
            {
                writer.ConfigureDocInfo(info => info.RemoveOfdElementsByNames("DocID"));
            }

            ApplyPdfMetadataToOfd(pdfDoc, writer, options, logger);

            ApplyDocInfoOverrides(writer, options);

            if (!string.IsNullOrWhiteSpace(options.OverrideDocId))
            {
                writer.ConfigureDocInfo(info => info.SetOfdEntity("DocID", options.OverrideDocId));
            }

            if (!string.IsNullOrWhiteSpace(options.TargetOfdVersion))
            {
                writer.SetOfdVersion(options.TargetOfdVersion);
            }

            if (options.ConfigureDocInfo != null)
            {
                writer.ConfigureDocInfo(options.ConfigureDocInfo!);
            }
        }
        // 在创建 writer 之后执行字体提取，以便 FontExtractor 直接注册字体
        await fontExtractor.ExtractAsync(pdfDoc, ofd, options, logger, options.CancellationToken);
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
            // 字体注册已经在 FontExtractor 中完成

            // 3. Orchestrator 统一调度剩余内容提取（字体已在前）
            // T073: 传递 options 以支持服务依赖注入
            var orchestrator = new Refactor.PdfToOfdOrchestrator(options);
            await orchestrator.ExecuteAsync(pdfDoc, ofd, options, logger, options.CancellationToken);

            await ofd.CloseAsync().ConfigureAwait(false);
            logger.LogInformation("[PDF2OFD] OFD文档异步关闭完成");

            // T073: 转换后验证和报告生成 (如果启用)
            if (options.EnableValidation && options.Validator != null)
            {
                logger.LogInformation("[PDF2OFD] 开始验证生成的OFD文件");
                var validationResult = options.Validator.Validate(ofdOutputDir);

                var allErrors = validationResult.SchemaErrors.Concat(validationResult.SemanticErrors).ToList();

                if (allErrors.Count > 0)
                {
                    logger.LogWarning("[PDF2OFD] 发现 {Count} 个验证问题", allErrors.Count);

                    // 如果配置了报告生成器,生成报告
                    if (options.ReportBuilder != null && !string.IsNullOrWhiteSpace(options.ReportPath))
                    {
                        options.ReportBuilder
                            .WithJob(System.Guid.NewGuid().ToString(), pdfPath, ofdOutputDir)
                            .WithStartTime(DateTime.UtcNow)
                            .AddErrors(allErrors)
                            .BuildToFile(options.ReportPath, indented: true);

                        logger.LogInformation("[PDF2OFD] 验证报告已保存到: {Path}", options.ReportPath);
                    }
                }
                else
                {
                    logger.LogInformation("[PDF2OFD] OFD验证通过,无问题发现");
                }
            }
        }
        finally
        {
            // 清理临时字体文件（与原逻辑相同的生命周期）
            fontExtractor.CleanupTempFiles(logger);
            logger.LogInformation("[PDF2OFD] 临时字体文件清理完毕(由 FontExtractor)");
        }
    }

    private static void ApplyPdfMetadataToOfd(iText.Kernel.Pdf.PdfDocument pdfDoc, OfdWriter writer, PdfToOfdOptions options, ILogger? logger)
    {
        _ = logger;
        var pdfInfo = pdfDoc.GetDocumentInfo();
        if (pdfInfo == null)
        {
            return;
        }

        writer.ConfigureDocInfo(docInfo =>
        {
            var title = pdfInfo.GetTitle();
            if (!string.IsNullOrWhiteSpace(title))
            {
                docInfo.SetTitle(title);
            }

            var author = pdfInfo.GetAuthor();
            if (!string.IsNullOrWhiteSpace(author))
            {
                docInfo.SetAuthor(author);
            }

            var subject = pdfInfo.GetSubject();
            if (!string.IsNullOrWhiteSpace(subject))
            {
                docInfo.SetSubject(subject);
            }

            var creator = pdfInfo.GetCreator();
            if (!string.IsNullOrWhiteSpace(creator))
            {
                docInfo.SetCreator(creator);
            }

            var producer = pdfInfo.GetProducer();
            if (!string.IsNullOrWhiteSpace(producer))
            {
                docInfo.SetCreatorVersion(producer);
            }

            var keywords = pdfInfo.GetKeywords();
            if (!string.IsNullOrWhiteSpace(keywords))
            {
                docInfo.SetOfdEntity("Keywords", keywords);
            }

            var creationRaw = GetPdfInfoValue(pdfInfo, "CreationDate");
            var normalizedCreation = NormalizePdfDateString(creationRaw);
            if (!string.IsNullOrEmpty(normalizedCreation))
            {
                docInfo.SetOfdEntity("CreationDate", normalizedCreation);
            }
            else
            {
                var creationDate = TryParsePdfDate(creationRaw);
                if (creationDate.HasValue)
                {
                    docInfo.SetCreationDate(creationDate.Value);
                }
            }

            var modRaw = GetPdfInfoValue(pdfInfo, "ModDate");
            var normalizedMod = NormalizePdfDateString(modRaw);
            if (!string.IsNullOrEmpty(normalizedMod))
            {
                docInfo.SetOfdEntity("ModDate", normalizedMod);
            }
            else
            {
                var modDate = TryParsePdfDate(modRaw);
                if (modDate.HasValue)
                {
                    docInfo.SetModDate(modDate.Value);
                }
            }
        });
    }

    private static void ApplyDocInfoOverrides(OfdWriter writer, PdfToOfdOptions options)
    {
        if (writer == null || options == null)
        {
            return;
        }

        bool hasOverrides = options.RemoveDocId
            || !string.IsNullOrWhiteSpace(options.DocTitle)
            || !string.IsNullOrWhiteSpace(options.DocAuthor)
            || !string.IsNullOrWhiteSpace(options.DocSubject)
            || !string.IsNullOrWhiteSpace(options.DocKeywords)
            || !string.IsNullOrWhiteSpace(options.DocCreator)
            || !string.IsNullOrWhiteSpace(options.DocCreatorVersion)
            || !string.IsNullOrWhiteSpace(options.DocCreationDateRaw)
            || !string.IsNullOrWhiteSpace(options.DocModDateRaw);

        if (!hasOverrides)
        {
            return;
        }

        writer.ConfigureDocInfo(docInfo =>
        {
            if (options.RemoveDocId)
            {
                docInfo.RemoveOfdElementsByNames("DocID");
            }

            if (!string.IsNullOrWhiteSpace(options.DocTitle))
            {
                docInfo.SetTitle(options.DocTitle);
            }

            if (!string.IsNullOrWhiteSpace(options.DocAuthor))
            {
                docInfo.SetAuthor(options.DocAuthor);
            }

            if (!string.IsNullOrWhiteSpace(options.DocSubject))
            {
                docInfo.SetSubject(options.DocSubject);
            }

            if (!string.IsNullOrWhiteSpace(options.DocKeywords))
            {
                docInfo.SetOfdEntity("Keywords", options.DocKeywords);
            }

            if (!string.IsNullOrWhiteSpace(options.DocCreator))
            {
                docInfo.SetCreator(options.DocCreator);
            }

            if (!string.IsNullOrWhiteSpace(options.DocCreatorVersion))
            {
                docInfo.SetCreatorVersion(options.DocCreatorVersion);
            }

            if (!string.IsNullOrWhiteSpace(options.DocCreationDateRaw))
            {
                docInfo.SetOfdEntity("CreationDate", options.DocCreationDateRaw);
            }

            if (!string.IsNullOrWhiteSpace(options.DocModDateRaw))
            {
                docInfo.SetOfdEntity("ModDate", options.DocModDateRaw);
            }
        });
    }

    private static string? GetPdfInfoValue(iText.Kernel.Pdf.PdfDocumentInfo info, string key)
    {
        var value = info.GetMoreInfo(key);
        if (!string.IsNullOrWhiteSpace(value))
        {
            return value;
        }

        var lower = key.ToLowerInvariant();
        if (!string.Equals(lower, key, StringComparison.Ordinal))
        {
            value = info.GetMoreInfo(lower);
            if (!string.IsNullOrWhiteSpace(value))
            {
                return value;
            }
        }

        return null;
    }

    private static string? NormalizePdfDateString(string? raw)
    {
        if (string.IsNullOrWhiteSpace(raw))
        {
            return null;
        }

        var trimmed = raw.Trim();
        if (!trimmed.StartsWith("D:", StringComparison.OrdinalIgnoreCase))
        {
            return trimmed;
        }

        // 标准化前缀为大写 D:
    return trimmed.Length > 2 ? "D:" + trimmed.Substring(2) : "D:";
    }

    private static DateTime? TryParsePdfDate(string? raw)
    {
        if (string.IsNullOrWhiteSpace(raw))
        {
            return null;
        }

        var trimmed = raw.Trim();
        try
        {
            return iText.Kernel.Pdf.PdfDate.Decode(trimmed);
        }
        catch
        {
            if (DateTime.TryParse(trimmed, out var dt))
            {
                return dt;
            }
        }
        return null;
    }

    // 新增：别名方法以兼容测试程序
    public static Task ConvertPdfToOfdAsync(string pdfPath, string ofdOutputDir, PdfToOfdOptions? options = null)
    {
        return PdfToOfdAsync(pdfPath, ofdOutputDir, options);
    }

    // VectorExtractor 已迁出

    // 原表单转换方法已移除，未来实现由 FormExtractor 负责

    // OfdImage 和 OfdText 已移至 OfdrwNet.Abstractions
    #endregion
}

/// <summary>
/// Listens for image rendering events and extracts image data from PDF pages for conversion to OFD format.
/// </summary>
/// <remarks>This class is intended for internal use during PDF to OFD conversion processes. It collects image
/// objects encountered during rendering and makes them available for further processing or embedding in the output
/// document. The class is not thread-safe.</remarks>
    // ImageExtractor 已迁出

// （TextExtractor 已迁出，此处删除旧文本聚合残留）
