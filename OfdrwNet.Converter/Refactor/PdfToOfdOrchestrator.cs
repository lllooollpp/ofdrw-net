using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using iText.Kernel.Pdf;
using Microsoft.Extensions.Logging;
using OfdrwNet.Converter.Options;
using OfdrwNet.Abstractions;

namespace OfdrwNet.Converter.Refactor;

/// <summary>
/// 统一调度 PDF -> OFD 内容提取的 Orchestrator。
/// 负责根据选项顺序调用各提取器，后续可扩展为可配置执行顺序 / 依赖管理。
/// T073: 集成服务依赖
/// </summary>
internal class PdfToOfdOrchestrator
{
    private readonly List<IPdfContentExtractor> _extractors = new();

    public PdfToOfdOrchestrator(PdfToOfdOptions? options = null, Addon? addon = null)
    {
        // 默认顺序：Text -> Image -> Vector -> Annotation -> Form （字体在外部单独先行处理）
        // T073: 传递服务依赖到各提取器
        _extractors.Add(new TextExtractor());
        _extractors.Add(new PdfImageExtractor());
        _extractors.Add(new VectorExtractor());

        // T076: AnnotationExtractor 集成 BookmarkConverter 和 ActionMapper
        _extractors.Add(new AnnotationExtractor(
            options?.BookmarkConverter,
            options?.ActionMapper));

        // T075: FormExtractor 集成表单服务
        _extractors.Add(new FormExtractor(
            options?.FormMapper,
            options?.XfaDetector,
            options?.XfaHintWriter,
            options?.JavaScriptScanner));

        addon?.Configure(_extractors);
    }

    /// <summary>
    /// 允许外部自定义扩展提取器集合。
    /// </summary>
    internal class Addon
    {
        private readonly System.Action<List<IPdfContentExtractor>> _configure;
        public Addon(System.Action<List<IPdfContentExtractor>> configure) => _configure = configure;
        public void Configure(List<IPdfContentExtractor> list) => _configure(list);
    }

    public async Task ExecuteAsync(PdfDocument pdfDoc, IOfdDocWriter ofd, PdfToOfdOptions options, ILogger? logger, CancellationToken token)
    {
        // 确保本地有一个非 null 的选项副本以避免可空引用警告
        var runExtractors = _extractors;
        var opts = options ?? new PdfToOfdOptions();

        // 如果启用自动检测（AutoDetectImageOnly）并且用户未显式要求 ExportPageImagesOnly，则先快速扫描文档判断是否仅包含图片。
        var localForceImageOnly = false;
        if (opts.AutoDetectImageOnly && !opts.ExportPageImagesOnly)
        {
            bool foundTextOrPath = false;
            int totalPages = pdfDoc.GetNumberOfPages();
            var pagesToScan = Enumerable.Range(1, totalPages).Where(p => opts.PageFilter == null || opts.PageFilter(p));
            foreach (var p in pagesToScan)
            {
                token.ThrowIfCancellationRequested();
                var page = pdfDoc.GetPage(p);
                // 轻量 listener: 一旦看到文本或路径事件就标记并停止
                var listener = new QuickDetectListener();
                new iText.Kernel.Pdf.Canvas.Parser.PdfCanvasProcessor(listener).ProcessPageContent(page);
                if (listener.FoundTextOrPath)
                {
                    foundTextOrPath = true; break;
                }
            }

            if (!foundTextOrPath) localForceImageOnly = true;
        }

        // 如果用户请求仅导出页面图片（或自动检测判断为仅图片），则仅运行图片提取器。
        if (opts.ExportPageImagesOnly || localForceImageOnly)
        {
            runExtractors = new List<IPdfContentExtractor>();
            // 找到第一个 PdfImageExtractor 并仅运行它
            foreach (var ex in _extractors)
            {
                if (ex is PdfImageExtractor)
                {
                    runExtractors.Add(ex);
                    break;
                }
            }
        }

        foreach (var extractor in runExtractors)
        {
            token.ThrowIfCancellationRequested();
            // 当 ExportPageImagesOnly=true 时，我们忽略 ExtractImage 标志，强制运行图片提取器；否则保留原有过滤逻辑。
            if (!opts.ExportPageImagesOnly)
            {
                switch (extractor)
                {
                    case TextExtractor when !opts.ExtractText: continue;
                    case PdfImageExtractor when !opts.ExtractImage: continue;
                    case VectorExtractor when !opts.ExtractVector: continue;
                    case AnnotationExtractor when !opts.ExtractAnnotations: continue;
                    case FormExtractor when !opts.ExtractForms: continue;
                }
            }

            var name = extractor.GetType().Name.Replace("Extractor", string.Empty);
            logger?.LogInformation("[PDF2OFD] Orchestrator 执行 {Stage}", name);
            await extractor.ExtractAsync(pdfDoc, ofd, opts, logger, token);
        }
    }

    /// <summary>
    /// 轻量检测监听器：只关心是否出现文本或路径事件，用于快速判断是否为仅图片的 PDF。
    /// </summary>
    private class QuickDetectListener : iText.Kernel.Pdf.Canvas.Parser.Listener.IEventListener
    {
        public bool FoundTextOrPath { get; private set; } = false;

        public void EventOccurred(iText.Kernel.Pdf.Canvas.Parser.Data.IEventData data, iText.Kernel.Pdf.Canvas.Parser.EventType type)
        {
            if (FoundTextOrPath) return;
            if (type == iText.Kernel.Pdf.Canvas.Parser.EventType.RENDER_TEXT || type == iText.Kernel.Pdf.Canvas.Parser.EventType.RENDER_PATH)
                FoundTextOrPath = true;
        }

        public System.Collections.Generic.ICollection<iText.Kernel.Pdf.Canvas.Parser.EventType> GetSupportedEvents()
        {
            return new[] { iText.Kernel.Pdf.Canvas.Parser.EventType.RENDER_TEXT, iText.Kernel.Pdf.Canvas.Parser.EventType.RENDER_PATH };
        }
    }
}
