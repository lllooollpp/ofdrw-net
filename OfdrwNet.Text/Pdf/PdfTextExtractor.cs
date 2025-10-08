using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using iText.Kernel.Geom;
using iText.Kernel.Pdf;
using iText.Kernel.Pdf.Canvas.Parser;
using iText.Kernel.Pdf.Canvas.Parser.Data;
using iText.Kernel.Pdf.Canvas.Parser.Listener;
using Microsoft.Extensions.Logging;
using OfdrwNet.Abstractions;

namespace OfdrwNet.Text.Pdf;

/// <summary>
/// PDF 页面文本提取器，实现 PDF→OFD 的文本聚合、间距判断与文本对象发射。
/// </summary>
public sealed class PdfTextExtractor
{
    public async Task ExtractAsync(
        PdfDocument pdfDoc,
        IOfdDocWriter ofd,
        PdfTextExtractionOptions options,
        ILogger? logger,
        CancellationToken token)
    {
        if (!options.ExtractText)
        {
            logger?.LogDebug("[PDF2OFD][Text] ExtractText=false 跳过文本提取");
            await Task.CompletedTask;
            return;
        }

        int pages = pdfDoc.GetNumberOfPages();
        for (int p = 1; p <= pages; p++)
        {
            token.ThrowIfCancellationRequested();
            if (options.PageFilter != null && !options.PageFilter(p))
                continue;

            var page = pdfDoc.GetPage(p);
            var pageHeightPt = page.GetPageSize().GetHeight();
            var strat = new BaselineTextGroupingStrategy(page.GetPageSize(), options);
            var processor = new PdfCanvasProcessor(strat);
            processor.ProcessPageContent(page);
            strat.Flush();

            foreach (var group in strat.Groups)
            {
                if (group.RenderInfos.Count == 0)
                    continue;

                var segments = BuildSegments(group.RenderInfos, options, logger);
                foreach (var segment in segments)
                {
                    EmitSegment(segment, ofd, options, pageHeightPt, p, logger);
                }
            }
        }

        await Task.CompletedTask;
    }

    private static List<TextSegment> BuildSegments(IReadOnlyList<TextRenderInfo> infos, PdfTextExtractionOptions options, ILogger? logger)
    {
        var segments = new List<TextSegment>();
        if (infos.Count == 0)
            return segments;

        var globalAnalysis = RunAnalysis.From(infos);
        var current = new TextSegment();
        var builder = new StringBuilder();

        for (int i = 0; i < infos.Count; i++)
        {
            var currentInfo = infos[i];
            current.RenderInfos.Add(currentInfo);
            string glyphText = currentInfo.GetText() ?? string.Empty;
            builder.Append(glyphText);

            bool flushSegment = (i == infos.Count - 1);

            if (i < infos.Count - 1)
            {
                var nextInfo = infos[i + 1];
                var decision = EvaluateGap(currentInfo, nextInfo, globalAnalysis, options);

                if (decision.SpaceCount > 0)
                {
                    builder.Append(' ', decision.SpaceCount);
                    current.HasSyntheticSpaces = true;
                    current.SyntheticSpaceWidthMm += decision.SpaceCount * decision.SpaceWidthMm;
                }

                if (decision.SplitAfter)
                {
                    flushSegment = true;
                }

                if (options.EnableDebugWordLayout && (decision.SpaceCount > 0 || decision.SplitAfter))
                {
                    double gapMm = ComputeGapMm(currentInfo, nextInfo);
                    logger?.LogDebug("[PDF2OFD][Text] gap={Gap:F3}mm space={Space} split={Split} left='{Left}' right='{Right}'", gapMm, decision.SpaceCount, decision.SplitAfter, glyphText, nextInfo.GetText());
                }
            }

            if (flushSegment)
            {
                current.Text = builder.ToString();
                current.Analysis = RunAnalysis.From(current.RenderInfos);
                segments.Add(current);
                if (i < infos.Count - 1)
                {
                    current = new TextSegment();
                    builder = new StringBuilder();
                }
            }
        }

        return segments;
    }

    private static void EmitSegment(TextSegment segment, IOfdDocWriter writer, PdfTextExtractionOptions options, double pageHeightPt, int pageNumber, ILogger? logger)
    {
        if (segment.RenderInfos.Count == 0 || string.IsNullOrEmpty(segment.Text))
            return;

        var first = segment.RenderInfos[0];
        var (ctm, _, scaleY) = BuildNormalizedCtm(first, logger);

        double baselinePt = first.GetBaseline().GetStartPoint().Get(1);
        double baselineYmm = GeometryUtils.PtToMm(baselinePt);

        double minX = double.MaxValue, maxX = double.MinValue, minY = double.MaxValue, maxY = double.MinValue;
        foreach (var ri in segment.RenderInfos)
        {
            var rect = ri.GetAscentLine().GetBoundingRectangle();
            minX = Math.Min(minX, rect.GetLeft());
            maxX = Math.Max(maxX, rect.GetRight());
            minY = Math.Min(minY, rect.GetBottom());
            maxY = Math.Max(maxY, rect.GetTop());
        }

        double originXmm = GeometryUtils.PtToMm(minX);
        double originYmm = GeometryUtils.PtToMm(pageHeightPt - maxY);
        double widthMm = GeometryUtils.PtToMm(Math.Max(0.1, maxX - minX)) + segment.SyntheticSpaceWidthMm;
        double heightMm = GeometryUtils.PtToMm(Math.Max(0.1, maxY - minY));

        if (options.ExpandCjkWidth && segment.Analysis.IsMostlyCjk)
        {
            double perCharAdvanceMm = GeometryUtils.PtToMm(first.GetFontSize());
            double targetWidthMm = perCharAdvanceMm * segment.Text.Length;
            if (targetWidthMm > widthMm)
            {
                double extra = perCharAdvanceMm * options.CjkExtraAdvanceRatio;
                widthMm = targetWidthMm + extra;
            }
        }

        double[]? charStarts = null;
        double[]? charAdvances = null;
        if (options.PerGlyphPositioning && !segment.HasSyntheticSpaces)
        {
            var starts = new List<double>();
            var advs = new List<double>();
            foreach (var ri in segment.RenderInfos)
            {
                starts.Add(GeometryUtils.PtToMm(ri.GetBaseline().GetStartPoint().Get(0)));
                advs.Add(GeometryUtils.PtToMm(ri.GetAscentLine().GetBoundingRectangle().GetWidth()));
            }
            charStarts = starts.ToArray();
            charAdvances = advs.ToArray();
        }

        string fontName = ResolveFontName(first, logger);

        double pdfFontSizePt = first.GetFontSize();
        if (pdfFontSizePt <= 0)
        {
            pdfFontSizePt = segment.RenderInfos.Select(r => r.GetFontSize()).FirstOrDefault(v => v > 0);
            if (pdfFontSizePt <= 0)
                pdfFontSizePt = 12f;
        }
        double fontSizeMm = GeometryUtils.PtToMm(pdfFontSizePt);
        if (Math.Abs(scaleY) > 0.001 && Math.Abs(Math.Abs(scaleY) - 1.0) > 0.001)
        {
            fontSizeMm *= scaleY;
            logger?.LogDebug("[PDF2OFD][Text] CTM Y缩放 {Scale}, 调整字体大小: {Original}pt -> {Adjusted}mm", scaleY, pdfFontSizePt, fontSizeMm);
        }

        double[]? deltaX = null;
        if (!segment.HasSyntheticSpaces && options.EnableDeltaX && segment.RenderInfos.Count > 1)
        {
            var deltas = new List<double>();
            for (int i = 1; i < segment.RenderInfos.Count; i++)
            {
                var prev = segment.RenderInfos[i - 1].GetBaseline().GetStartPoint().Get(0);
                var cur = segment.RenderInfos[i].GetBaseline().GetStartPoint().Get(0);
                deltas.Add(GeometryUtils.PtToMm(cur - prev));
            }
            if (deltas.Count > 0)
                deltaX = deltas.ToArray();
        }

        double pageHeightMm = GeometryUtils.PtToMm(pageHeightPt);
        if (ctm.Length >= 6)
        {
            ctm[4] -= originXmm;
            ctm[5] -= originYmm;

            const double translationEpsilon = 1e-6;
            if (Math.Abs(ctm[4]) < translationEpsilon)
                ctm[4] = 0;
            if (Math.Abs(ctm[5]) < translationEpsilon)
                ctm[5] = 0;
        }

        writer.AddRawTextGlyphRun(
            fontName,
            fontSizeMm,
            originXmm,
            originYmm,
            widthMm,
            heightMm,
            segment.Text,
            deltaX,
            null,
            null,
            pageNumber,
            (ctm.Length == 0 ? null : ctm),
            baselineY: pageHeightMm - baselineYmm,
            charStarts: charStarts,
            charAdvances: charAdvances);
    }

    private static GapDecision EvaluateGap(TextRenderInfo left, TextRenderInfo right, RunAnalysis analysis, PdfTextExtractionOptions options)
    {
        double gapMm = ComputeGapMm(left, right);
        if (gapMm <= 0)
            return default;

        double averageFontSizePt = (left.GetFontSize() + right.GetFontSize()) / 2.0;
        if (averageFontSizePt <= 0)
            averageFontSizePt = analysis.AverageFontSizePt > 0 ? analysis.AverageFontSizePt : 12.0;
        double fontSizeMm = GeometryUtils.PtToMm(averageFontSizePt);

        double triggerRatio = analysis.IsMostlyCjk ? options.CjkGapTriggerRatio : options.GapSpaceTriggerRatio;
        double minGapMm = options.MinGapForSyntheticSpaceMm;

        bool numericPair = IsNumericLike(left.GetText()) && IsNumericLike(right.GetText());
        if (numericPair)
        {
            triggerRatio *= options.NumericGapMultiplier;
            minGapMm = Math.Max(minGapMm, options.NumericMinGapMm);
        }

        double thresholdMm = Math.Max(minGapMm, fontSizeMm * triggerRatio);
        if (gapMm < thresholdMm)
            return default;

        double baseSpaceWidthMm;
        if (analysis.IsMostlyCjk)
        {
            baseSpaceWidthMm = Math.Max(fontSizeMm, minGapMm);
        }
        else
        {
            baseSpaceWidthMm = Math.Max(fontSizeMm * 0.5, options.MinGapForSyntheticSpaceMm);
        }

        if (numericPair)
        {
            baseSpaceWidthMm = Math.Max(baseSpaceWidthMm * 0.8, options.MinGapForSyntheticSpaceMm);
        }

        int spaceCount = Math.Max(1, (int)Math.Round(gapMm / Math.Max(baseSpaceWidthMm, 0.1)));
        spaceCount = Math.Min(spaceCount, options.MaxSyntheticSpacesPerGap);

        bool splitAfter = false;
        bool suppressSpaces = false;
        if (options.SplitTextBySpace)
        {
            if (!options.OnlySplitLatinWords || analysis.IsLatinDominant || numericPair || (!analysis.IsLatinDominant && !analysis.IsMostlyCjk && analysis.ContainsLatin))
            {
                splitAfter = true;
            }
            else if (analysis.IsMostlyCjk)
            {
                if (ShouldForceSplitForCjk(left, right, gapMm, fontSizeMm, out bool triggeredByPunctuation))
                {
                    splitAfter = true;
                    if (triggeredByPunctuation)
                    {
                        suppressSpaces = true;
                    }
                }
            }
        }

        if (suppressSpaces)
        {
            spaceCount = 0;
        }

        return new GapDecision(splitAfter, spaceCount, baseSpaceWidthMm);
    }

    private static bool ShouldForceSplitForCjk(TextRenderInfo left, TextRenderInfo right, double gapMm, double fontSizeMm, out bool triggeredByPunctuation)
    {
        char? leftChar = GetBoundaryChar(left, fromEnd: true);
        char? rightChar = GetBoundaryChar(right, fromEnd: false);

        bool punctuationBoundary = (leftChar.HasValue && IsBreakPunctuation(leftChar.Value)) ||
                                   (rightChar.HasValue && IsBreakPunctuation(rightChar.Value));

        triggeredByPunctuation = punctuationBoundary;

        if (punctuationBoundary)
        {
            return true;
        }

        double strongGapThresholdMm = Math.Max(fontSizeMm * 0.9, fontSizeMm + 0.8);
        return gapMm >= strongGapThresholdMm;
    }

    private static char? GetBoundaryChar(TextRenderInfo info, bool fromEnd)
    {
        var text = info.GetText();
        if (string.IsNullOrEmpty(text))
            return null;

        if (fromEnd)
        {
            for (int i = text.Length - 1; i >= 0; i--)
            {
                var ch = text[i];
                if (!char.IsWhiteSpace(ch))
                    return ch;
            }
        }
        else
        {
            for (int i = 0; i < text.Length; i++)
            {
                var ch = text[i];
                if (!char.IsWhiteSpace(ch))
                    return ch;
            }
        }
        return null;
    }

    private static bool IsBreakPunctuation(char ch)
    {
        return ch switch
        {
            ':' or '：' or ';' or '；' or '、' or '。' or ',' or '，' or '•' or '‧' or '\u3001' => true,
            ')' or '）' or ']' or '】' or '〉' or '》' => true,
            _ => false
        };
    }

    private static (double[] Ctm, double ScaleX, double ScaleY) BuildNormalizedCtm(TextRenderInfo info, ILogger? logger)
    {
        var pdfCtm = info.GetGraphicsState().GetCtm();
        double[] ctm = GeometryUtils.BuildOfdCtmFromPdf(pdfCtm);
        double scaleX = Math.Abs(pdfCtm.Get(Matrix.I11));
        double scaleY = Math.Abs(pdfCtm.Get(Matrix.I22));

        try
        {
            const double eps = 1e-6;
            if (ctm.Length >= 4)
            {
                if (Math.Abs(ctm[0] - GeometryUtils.Pt2Mm) < eps &&
                    Math.Abs(ctm[1]) < eps &&
                    Math.Abs(ctm[2]) < eps &&
                    Math.Abs(ctm[3] - GeometryUtils.Pt2Mm) < eps)
                {
                    ctm = Array.Empty<double>();
                    logger?.LogDebug("[PDF2OFD][Text] 规范化 CTM: 去除纯 pt->mm 缩放，避免双重缩放");
                }
            }
        }
        catch
        {
            logger?.LogWarning("[PDF2OFD][Text] 获取 CTM 失败，保持原 CTM");
        }

        return (ctm, scaleX, scaleY);
    }

    private static string ResolveFontName(TextRenderInfo info, ILogger? logger)
    {
        string fontName = "SimSun";
        try
        {
            var pdfFont = info.GetFont();
            if (pdfFont != null)
            {
                var fontProgram = pdfFont.GetFontProgram();
                if (fontProgram != null)
                {
                    var fontNames = fontProgram.GetFontNames();
                    fontName = fontNames.GetFontName()
                                ?? fontNames.GetFamilyName()?[0]?[3]
                                ?? fontNames.GetFamilyName()?[0]?[1]
                                ?? "SimSun";

                    if (fontName.Contains("+"))
                    {
                        var parts = fontName.Split('+');
                        if (parts.Length > 1)
                            fontName = parts[1];
                    }
                }
            }
        }
        catch (Exception ex)
        {
            logger?.LogWarning("[PDF2OFD][Text] 提取字体名称失败: {Message}, 使用默认字体", ex.Message);
        }

        return fontName;
    }

    private static double ComputeGapMm(TextRenderInfo left, TextRenderInfo right)
    {
        double leftEnd = left.GetBaseline().GetEndPoint().Get(0);
        double rightStart = right.GetBaseline().GetStartPoint().Get(0);
        double gapPt = rightStart - leftEnd;
        if (gapPt <= 0)
            return 0;
        return GeometryUtils.PtToMm(gapPt);
    }

    private static bool IsNumericLike(string? text)
    {
        if (string.IsNullOrEmpty(text))
            return false;
        foreach (var ch in text)
        {
            if (!(char.IsDigit(ch) || ch == '.' || ch == '-' || ch == '+' || ch == ','))
                return false;
        }
        return true;
    }

    private sealed class TextSegment
    {
        public List<TextRenderInfo> RenderInfos { get; } = new();
        public string Text { get; set; } = string.Empty;
        public bool HasSyntheticSpaces { get; set; }
        public double SyntheticSpaceWidthMm { get; set; }
        public RunAnalysis Analysis { get; set; } = RunAnalysis.Empty;
    }

    private readonly struct GapDecision
    {
        public GapDecision(bool split, int spaceCount, double spaceWidthMm)
        {
            SplitAfter = split;
            SpaceCount = spaceCount;
            SpaceWidthMm = spaceWidthMm;
        }

        public bool SplitAfter { get; }
        public int SpaceCount { get; }
        public double SpaceWidthMm { get; }
    }

    private sealed class RunAnalysis
    {
        public static RunAnalysis Empty { get; } = new();

        public int LatinCount { get; private set; }
        public int CjkCount { get; private set; }
        public int DigitCount { get; private set; }
        public double AverageFontSizePt { get; private set; }

        public bool ContainsLatin => LatinCount > 0;
        public bool ContainsCjk => CjkCount > 0;
        public bool IsMostlyCjk => CjkCount > 0 && LatinCount == 0;
        public bool IsLatinDominant => LatinCount > 0 && CjkCount == 0;

        public static RunAnalysis From(IReadOnlyList<TextRenderInfo> infos)
        {
            var analysis = new RunAnalysis();
            if (infos.Count == 0)
                return analysis;

            double fontSum = 0;
            int fontCount = 0;

            foreach (var info in infos)
            {
                fontSum += info.GetFontSize();
                fontCount++;

                var text = info.GetText() ?? string.Empty;
                foreach (var ch in text)
                {
                    if (char.IsWhiteSpace(ch))
                        continue;
                    if (IsCjkOrEastAsian(ch))
                    {
                        analysis.CjkCount++;
                    }
                    else if (IsLatin(ch))
                    {
                        analysis.LatinCount++;
                    }
                    else if (char.IsDigit(ch))
                    {
                        analysis.DigitCount++;
                    }
                }
            }

            analysis.AverageFontSizePt = fontCount > 0 ? fontSum / fontCount : 0;
            return analysis;
        }

        private static bool IsLatin(char ch)
            => (ch >= 'A' && ch <= 'Z') || (ch >= 'a' && ch <= 'z');

        private static bool IsCjkOrEastAsian(char ch)
        {
            int code = ch;
            if (code >= 0x4E00 && code <= 0x9FFF) return true;
            if (code >= 0x3400 && code <= 0x4DBF) return true;
            if (code >= 0xF900 && code <= 0xFAFF) return true;
            if (code >= 0x2E80 && code <= 0x2EFF) return true;
            if (code >= 0x3040 && code <= 0x30FF) return true;
            if (code >= 0x31F0 && code <= 0x31FF) return true;
            if (code >= 0xFF66 && code <= 0xFF9F) return true;
            if (code >= 0xAC00 && code <= 0xD7AF) return true;
            if (code >= 0x1100 && code <= 0x11FF) return true;
            if (code >= 0x3130 && code <= 0x318F) return true;
            return false;
        }
    }

    private class BaselineTextGroupingStrategy : IEventListener
    {
        private readonly Rectangle _pageSize;
        private readonly PdfTextExtractionOptions _options;
        private readonly List<Group> _groups = new();
        private readonly List<TextRenderInfo> _current = new();
        private readonly List<float> _deltas = new();

    public BaselineTextGroupingStrategy(Rectangle pageSize, PdfTextExtractionOptions options)
        {
            _pageSize = pageSize;
            _options = options ?? new PdfTextExtractionOptions();
        }

        public IReadOnlyList<Group> Groups => _groups;

        public void EventOccurred(IEventData data, EventType type)
        {
            if (type != EventType.RENDER_TEXT)
                return;
            var ri = (TextRenderInfo)data;
            try
            {
                ri.PreserveGraphicsState();
            }
            catch { }
            string t = ri.GetText();
            if (string.IsNullOrEmpty(t))
                return;

            if (_current.Count > 0)
            {
                var last = _current.Last();
                var lastY = last.GetBaseline().GetStartPoint().Get(1);
                var curY = ri.GetBaseline().GetStartPoint().Get(1);
                var lastX = last.GetBaseline().GetStartPoint().Get(0);
                var curX = ri.GetBaseline().GetStartPoint().Get(0);
                var lastEndX = last.GetBaseline().GetEndPoint().Get(0);
                var curStartX = ri.GetBaseline().GetStartPoint().Get(0);

                float fontSize = ri.GetFontSize() > 0 ? ri.GetFontSize() : last.GetFontSize();
                float baselineTolerance = Math.Max(fontSize * 0.6f, 3.0f);
                float backwardXTolerance = Math.Max(fontSize * 0.5f, 2.0f);

                if (Math.Abs(curY - lastY) > baselineTolerance || curX < lastX - backwardXTolerance)
                {
                    FlushCurrent();
                }
                else
                {
                    float delta = (float)(curStartX - lastEndX);
                    if (delta < 0)
                    {
                        double maxNegPt = GeometryUtils.MmToPt(_options?.MaxNegativeKerningAbsorbMm ?? 0.25d);
                        if (Math.Abs(delta) <= maxNegPt)
                        {
                            delta = 0f;
                        }
                    }
                    _deltas.Add(delta);
                }
            }

            _current.Add(ri);
        }

        public ICollection<EventType> GetSupportedEvents() => new[] { EventType.RENDER_TEXT };
        public string GetResultantText() => string.Empty;
        public void FlushPendingWord() => FlushCurrent();

        public void Flush() => FlushCurrent();

        private void FlushCurrent()
        {
            if (_current.Count == 0)
                return;
            _groups.Add(new Group { RenderInfos = _current.ToList(), DeltaXs = _deltas.Count > 0 ? _deltas.ToList() : null });
            _current.Clear();
            _deltas.Clear();
        }

        internal class Group
        {
            public List<TextRenderInfo> RenderInfos { get; set; } = new();
            public List<float>? DeltaXs { get; set; }
        }
    }

    /// <summary>
    /// 词聚合辅助：为测试与后续表格/段落处理提供统一入口。
    /// 当前实现保持输入顺序并确保按 X/Y 排序，后续可逐步替换为更复杂的聚合逻辑。
    /// </summary>
    public static class TextAggregationHelper
    {
        public static List<OfdText> AggregateWords(List<OfdText> raw, ILogger logger, int pageNumber, PdfTextExtractionOptions options)
        {
            if (raw == null)
            {
                return new List<OfdText>();
            }

            return raw
                .OrderBy(t => t.Page)
                .ThenBy(t => t.Y)
                .ThenBy(t => t.X)
                .Select(t => t)
                .ToList();
        }
    }
}
