using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using iText.Kernel.Pdf;
using Microsoft.Extensions.Logging;
using OfdrwNet.Abstractions;

namespace OfdrwNet.Converter.Refactor;

/// <summary>
/// 统一调度 PDF -> OFD 内容提取的 Orchestrator。
/// 负责根据选项顺序调用各提取器，后续可扩展为可配置执行顺序 / 依赖管理。
/// </summary>
internal class PdfToOfdOrchestrator
{
    private readonly List<IPdfContentExtractor> _extractors = new();

    public PdfToOfdOrchestrator(Addon? addon = null)
    {
        // 默认顺序：Text -> Image -> Vector -> Annotation -> Form （字体在外部单独先行处理）
        // 允许通过 addon 注入 / 替换（预留扩展）
        _extractors.Add(new TextExtractor());
    _extractors.Add(new PdfImageExtractor());
        _extractors.Add(new VectorExtractor());
        _extractors.Add(new AnnotationExtractor());
        _extractors.Add(new FormExtractor());

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

    public async Task ExecuteAsync(PdfDocument pdfDoc, IOfdDocWriter ofd, ConvertHelper.PdfToOfdOptions options, ILogger? logger, CancellationToken token)
    {
        foreach (var extractor in _extractors)
        {
            token.ThrowIfCancellationRequested();
            switch (extractor)
            {
                case TextExtractor when !options.ExtractText: continue;
                case PdfImageExtractor when !options.ExtractImage: continue;
                case AnnotationExtractor when !options.ExtractAnnotations: continue;
                case FormExtractor when !options.ExtractForms: continue;
            }

            var name = extractor.GetType().Name.Replace("Extractor", string.Empty);
            logger?.LogInformation("[PDF2OFD] Orchestrator 执行 {Stage}", name);
            await extractor.ExtractAsync(pdfDoc, ofd, options, logger, token);
        }
    }
}
