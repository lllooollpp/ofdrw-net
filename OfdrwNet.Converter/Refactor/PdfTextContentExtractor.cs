using System.Threading;
using System.Threading.Tasks;
using iText.Kernel.Pdf;
using Microsoft.Extensions.Logging;
using OfdrwNet.Abstractions;
using OfdrwNet.Converter.Options;
using OfdrwNet.Text.Pdf;

namespace OfdrwNet.Converter.Refactor;

/// <summary>
/// 适配器：将公共文本组件接入转换器的统一提取器接口。
/// </summary>
internal sealed class PdfTextContentExtractor : IPdfContentExtractor
{
    private readonly PdfTextExtractor _impl = new();

    public Task ExtractAsync(PdfDocument pdfDoc, IOfdDocWriter ofd, PdfToOfdOptions options, ILogger? logger, CancellationToken token)
    {
        var effectiveOptions = options ?? new PdfToOfdOptions();
        if (!effectiveOptions.ExtractText)
        {
            return Task.CompletedTask;
        }

        var textOptions = effectiveOptions.ToTextExtractionOptions();
        return _impl.ExtractAsync(pdfDoc, ofd, textOptions, logger, token);
    }
}

internal static class PdfToOfdTextOptionExtensions
{
    public static PdfTextExtractionOptions ToTextExtractionOptions(this PdfToOfdOptions options)
    {
        return new PdfTextExtractionOptions
        {
            ExtractText = options.ExtractText,
            PageFilter = options.PageFilter,
            PerGlyphPositioning = options.PerGlyphPositioning,
            ExpandCjkWidth = options.ExpandCjkWidth,
            CjkExtraAdvanceRatio = options.CjkExtraAdvanceRatio,
            EnableDeltaX = options.EnableDeltaX,
            SplitTextBySpace = options.SplitTextBySpace,
            OnlySplitLatinWords = options.OnlySplitLatinWords,
            GapSpaceTriggerRatio = options.GapSpaceTriggerRatio,
            MaxSyntheticSpacesPerGap = options.MaxSyntheticSpacesPerGap,
            MinGapForSyntheticSpaceMm = options.MinGapForSyntheticSpaceMm,
            MaxNegativeKerningAbsorbMm = options.MaxNegativeKerningAbsorbMm,
            NumericGapMultiplier = options.NumericGapMultiplier,
            NumericMinGapMm = options.NumericMinGapMm,
            CjkGapTriggerRatio = options.CjkGapTriggerRatio,
            EnableDebugWordLayout = options.EnableDebugWordLayout
        };
    }
}
