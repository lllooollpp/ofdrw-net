using System;

namespace OfdrwNet.Text.Pdf;

/// <summary>
/// PDF 文本提取相关的选项，供 PDF→OFD 流水线与公共文本组件共享。
/// </summary>
public sealed class PdfTextExtractionOptions
{
    /// <summary>是否执行文本提取。</summary>
    public bool ExtractText { get; set; } = true;

    /// <summary>按页号过滤，返回 true 表示保留该页。</summary>
    public Func<int, bool>? PageFilter { get; set; }

    /// <summary>是否启用逐字定位。</summary>
    public bool PerGlyphPositioning { get; set; } = true;

    /// <summary>当 CJK 文本宽度估算过窄时是否扩展。</summary>
    public bool ExpandCjkWidth { get; set; } = true;

    /// <summary>CJK 宽度扩展时额外增加的余量比例。</summary>
    public double CjkExtraAdvanceRatio { get; set; } = 0.12d;

    /// <summary>是否启用 DeltaX 输出。</summary>
    public bool EnableDeltaX { get; set; } = true;

    /// <summary>是否启用 gap→空格 的分词逻辑。</summary>
    public bool SplitTextBySpace { get; set; } = true;

    /// <summary>仅对拉丁文本执行按空格分词。</summary>
    public bool OnlySplitLatinWords { get; set; } = true;

    /// <summary>gap 触发空格的比例（相对字号）。</summary>
    public double GapSpaceTriggerRatio { get; set; } = 0.55d;

    /// <summary>gap 合成空格数量上限。</summary>
    public int MaxSyntheticSpacesPerGap { get; set; } = 4;

    /// <summary>合成空格的最小 gap 宽度（mm）。</summary>
    public double MinGapForSyntheticSpaceMm { get; set; } = 0.45d;

    /// <summary>可吸收的最大负 kerning（mm）。</summary>
    public double MaxNegativeKerningAbsorbMm { get; set; } = 0.25d;

    /// <summary>数字片段的 gap 触发放大倍数。</summary>
    public double NumericGapMultiplier { get; set; } = 1.3d;

    /// <summary>数字段最小 gap（mm）。</summary>
    public double NumericMinGapMm { get; set; } = 1.0d;

    /// <summary>CJK 文本 gap 触发比例。</summary>
    public double CjkGapTriggerRatio { get; set; } = 0.45d;

    /// <summary>是否输出词级调试信息。</summary>
    public bool EnableDebugWordLayout { get; set; } = false;
}
