using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Text;

using iText.Kernel.Geom;
using iText.Kernel.Pdf;
using iText.Kernel.Pdf.Canvas.Parser;
using iText.Kernel.Pdf.Canvas.Parser.Data;
using iText.Kernel.Pdf.Canvas.Parser.Listener;

using Microsoft.Extensions.Logging;

using OfdrwNet.Abstractions;

namespace OfdrwNet.Converter.Refactor;

/// <summary>
/// 从 PDF 中提取文本并写入 OFD 的实现（包含并行或顺序处理）
/// </summary>
internal class TextExtractor : IPdfContentExtractor
{
    /// <summary>
    /// 主入口：根据选项提取 PDF 文本并通过 IOfdDocWriter 写入 OFD
    /// 支持并行处理与单页处理两种模式
    /// </summary>
    public Task ExtractAsync(PdfDocument pdfDoc, IOfdDocWriter ofd, ConvertHelper.PdfToOfdOptions options, ILogger? logger, System.Threading.CancellationToken token)
    {
        if (!options.ExtractText)
        {
            // 如果禁用文本提取，则直接返回
            logger?.LogDebug("[PDF2OFD][Text] ExtractText=false 跳过文本提取");
            return Task.CompletedTask;
        }

        int totalPages = pdfDoc.GetNumberOfPages();
        logger?.LogDebug("[PDF2OFD][Text] PDF总页数: {TotalPages}", totalPages);

        // 顺序处理（或并行度设置为 1 时）
        if (options.MaxDegreeOfParallelism <= 1)
        {
            for (int i = 1; i <= totalPages; i++)
            {
                token.ThrowIfCancellationRequested();
                if (options.PageFilter != null && !options.PageFilter(i))
                {
                    logger?.LogDebug("[PDF2OFD][Text] Page {P} 被过滤", i);
                    continue;
                }
                ProcessSinglePage(pdfDoc, ofd, options, logger, i);
            }
        }
        else
        {
            // 并行处理每一页，收集每页的文本块，最后按页序写入 OFD
            logger?.LogInformation("[PDF2OFD][Text] 使用并行处理，最大并行度: {Parallel}", options.MaxDegreeOfParallelism);
            var pages = Enumerable.Range(1, totalPages).Where(p => options.PageFilter == null || options.PageFilter(p)).ToList();
            var parallelOptions = new ParallelOptions { MaxDegreeOfParallelism = options.MaxDegreeOfParallelism, CancellationToken = token };
            var textBlocksPerPage = new System.Collections.Concurrent.ConcurrentDictionary<int, List<OfdText>>();
            Parallel.ForEach(pages, parallelOptions, i =>
            {
                try
                {
                    var page = pdfDoc.GetPage(i);
                    var strategy = new TextRenderListener(i, page.GetPageSize(), options, logger);
                    new PdfCanvasProcessor(strategy).ProcessPageContent(page);
                    strategy.FlushPendingWord();
                    List<OfdText> blocks;
                    if (options.PerGlyphPositioning)
                    {
                        blocks = strategy.TextBlocks;
                    }
                    else if (options.SplitTextBySpace)
                    {
                        blocks = TextAggregationHelper.AggregateWords(strategy.TextBlocks, logger, i, options);
                    }
                    else
                    {
                        blocks = TextAggregationHelper.Aggregate(strategy.TextBlocks, logger, i, options);
                    }
                    textBlocksPerPage[i] = blocks;
                }
                catch (iText.IO.Exceptions.IOException ex) when ((ex.Message.Contains("CMap") || ex.Message.Contains("UniGB")) && options.IgnoreCMapErrors)
                {
                    // 常见的中文字体 CMap 错误，根据选项决定是否忽略
                    logger?.LogWarning("[PDF2OFD][Text] Page {Page} 中文字体 CMap 错误，跳过: {Err}", i, ex.Message);
                    textBlocksPerPage[i] = new List<OfdText>();
                }
                catch (Exception ex)
                {
                    // 捕获其它异常并跳过该页
                    logger?.LogWarning(ex, "[PDF2OFD][Text] Page {Page} 提取失败，跳过", i);
                    textBlocksPerPage[i] = new List<OfdText>();
                }
            });

            // 按页序将文本块写入 OFD
            foreach (var kv in textBlocksPerPage.OrderBy(k => k.Key))
            {
                foreach (var blk in kv.Value)
                {
                    (ofd as OfdWriter)?.AddText(blk);
                }
            }
        }

        return Task.CompletedTask;
    }

    /// <summary>
    /// 单页处理逻辑：提取该页文本并立即写入 OFD（用于顺序模式）
    /// </summary>
    private static void ProcessSinglePage(PdfDocument pdfDoc, IOfdDocWriter ofd, ConvertHelper.PdfToOfdOptions options, ILogger? logger, int pageIndex)
    {
        try
        {
            var page = pdfDoc.GetPage(pageIndex);
            var strategy = new TextRenderListener(pageIndex, page.GetPageSize(), options, logger);
            new PdfCanvasProcessor(strategy).ProcessPageContent(page);
            strategy.FlushPendingWord();
            List<OfdText> blocks = options.PerGlyphPositioning
                ? strategy.TextBlocks
                : (options.SplitTextBySpace
                    ? TextAggregationHelper.AggregateWords(strategy.TextBlocks, logger, pageIndex, options)
                    : TextAggregationHelper.Aggregate(strategy.TextBlocks, logger, pageIndex, options));
            if (blocks.Count > 0)
                foreach (var blk in blocks)
                    (ofd as OfdWriter)?.AddText(blk);
        }
        catch (iText.IO.Exceptions.IOException ex) when ((ex.Message.Contains("CMap") || ex.Message.Contains("UniGB")) && options.IgnoreCMapErrors)
        {
            // 忽略字体 CMap 错误
            logger?.LogWarning("[PDF2OFD][Text] Page {Page} 中文字体 CMap 错误，跳过文本: {Err}", pageIndex, ex.Message);
        }
        catch (Exception ex)
        {
            // 捕获其它异常并记录
            logger?.LogWarning(ex, "[PDF2OFD][Text] Page {Page} 文本提取失败，跳过", pageIndex);
        }
    }

    // 以下聚合与监听器从 ConvertHelper 迁移
    /// <summary>
    /// 文本渲染监听器：从 iText 的 RenderText 事件中收集每个文本片段的信息（坐标、字体、大小等）
    /// 最终生成 OfdText 对象列表
    /// </summary>
    private class TextRenderListener : IEventListener
    {
        private readonly int _pageNum;
        private readonly Rectangle _pageSize;
        private readonly ConvertHelper.PdfToOfdOptions _options;
        private readonly ILogger? _logger;

        private readonly List<TextRenderInfo> _pendingInfos = new();
        private readonly StringBuilder _pendingBuffer = new();

        private static readonly HashSet<char> _sentenceSeparators = new()
        {
            '。','，','；','：','！','？','（','）','、','：','；','。','“','”','《','》','『','』'
        };

        private const float _baselineTolerancePt = 20f;

        // 收集的原始文本块（每个 renderInfo 对应一条）
        public List<OfdText> TextBlocks { get; } = new();

        public TextRenderListener(int pageNum, Rectangle pageSize, ConvertHelper.PdfToOfdOptions options, ILogger? logger)
        {
            _pageNum = pageNum;
            _pageSize = pageSize;
            _options = options;
            _logger = logger;
        }

        /// <summary>
        /// 事件处理：收集 RENDER_TEXT 事件并按基线/顺序聚合
        /// </summary>
        public void EventOccurred(IEventData data, EventType type)
        {
            if (type != EventType.RENDER_TEXT)
                return;

            if (data is not TextRenderInfo renderInfo)
                return;

            renderInfo.PreserveGraphicsState();
            var normalized = NormalizeRenderableText(renderInfo.GetText());
            if (string.IsNullOrEmpty(normalized))
                return;

            if (_pendingInfos.Count > 0 && RequiresBreak(renderInfo))
            {
                EmitPendingWord();
            }

            _pendingInfos.Add(renderInfo);
            _pendingBuffer.Append(normalized);

            if (ShouldFlush(normalized))
            {
                EmitPendingWord();
            }
        }

        public void FlushPendingWord() => EmitPendingWord();

        private bool ShouldFlush(string text)
        {
            foreach (var ch in text)
            {
                if (char.IsWhiteSpace(ch) || _sentenceSeparators.Contains(ch))
                    return true;
            }
            return false;
        }

        private bool RequiresBreak(TextRenderInfo nextInfo)
        {
            var lastInfo = _pendingInfos[^1];
            var lastBaseline = lastInfo.GetBaseline().GetStartPoint();
            var nextBaseline = nextInfo.GetBaseline().GetStartPoint();

            if (Math.Abs(nextBaseline.Get(1) - lastBaseline.Get(1)) > _baselineTolerancePt)
                return true;

            if (nextBaseline.Get(0) < lastBaseline.Get(0) - _baselineTolerancePt)
                return true;

            return false;
        }

        private void EmitPendingWord()
        {
            if (_pendingInfos.Count == 0)
            {
                _pendingBuffer.Clear();
                return;
            }

            var glyphs = CollectGlyphs(_pendingInfos);
            if (glyphs.Count == 0)
            {
                _pendingInfos.Clear();
                _pendingBuffer.Clear();
                return;
            }

            var trimmed = TrimGlyphs(glyphs);
            if (trimmed.Count == 0)
            {
                _pendingInfos.Clear();
                _pendingBuffer.Clear();
                return;
            }

            var ofdText = BuildOfdText(trimmed, _pendingInfos);
            if (ofdText != null)
            {
                TextBlocks.Add(ofdText);
                if (_options.EnableDebugWordLayout && _logger != null)
                {
                    _logger.LogDebug("[PDF2OFD][Text][Word] Page {Page} Text='{Text}' X={X:F2} Y={Y:F2} W={W:F2}",
                        _pageNum, ofdText.Text, ofdText.X, ofdText.Y, ofdText.Width);
                }
            }

            _pendingInfos.Clear();
            _pendingBuffer.Clear();
        }

        private List<GlyphMetrics> CollectGlyphs(List<TextRenderInfo> infos)
        {
            var glyphs = new List<GlyphMetrics>();
            foreach (var info in infos)
            {
                CollectGlyphsFromRenderInfo(info, glyphs);
            }
            return glyphs;
        }

        private void CollectGlyphsFromRenderInfo(TextRenderInfo renderInfo, List<GlyphMetrics> glyphs)
        {
            var text = NormalizeRenderableText(renderInfo.GetText());
            if (string.IsNullOrEmpty(text))
                return;

            var charInfos = renderInfo.GetCharacterRenderInfos();
            if (charInfos != null && charInfos.Count > 0)
            {
                foreach (var charInfo in charInfos)
                {
                    var charText = NormalizeRenderableText(charInfo.GetText());
                    if (string.IsNullOrEmpty(charText))
                        continue;

                    foreach (var ch in charText)
                    {
                        glyphs.Add(CreateGlyphMetrics(charInfo, ch));
                    }
                }
            }
            else
            {
                AppendFallbackGlyphs(renderInfo, text, glyphs);
            }
        }

        private GlyphMetrics CreateGlyphMetrics(TextRenderInfo info, char ch)
        {
            var baseline = info.GetBaseline();
            var startPt = baseline.GetStartPoint();
            var endPt = baseline.GetEndPoint();
            double startXmm = startPt.Get(0) * ConvertHelper.Pt2Mm;
            double endXmm = endPt.Get(0) * ConvertHelper.Pt2Mm;
            double widthMm = Math.Abs(endXmm - startXmm);
            if (widthMm <= 1e-6)
            {
                widthMm = info.GetFontSize() * ConvertHelper.Pt2Mm * 0.55d;
            }

            double baselineMm = ToOfdY(baseline.GetStartPoint().Get(1));
            var ascent = info.GetAscentLine();
            var descent = info.GetDescentLine();
            double topMm = Math.Min(ToOfdY(ascent.GetStartPoint().Get(1)), ToOfdY(ascent.GetEndPoint().Get(1)));
            double bottomMm = Math.Max(ToOfdY(descent.GetStartPoint().Get(1)), ToOfdY(descent.GetEndPoint().Get(1)));

            return new GlyphMetrics(ch, startXmm, Math.Max(0.01d, widthMm), topMm, bottomMm, baselineMm);
        }

        private void AppendFallbackGlyphs(TextRenderInfo info, string text, List<GlyphMetrics> glyphs)
        {
            if (string.IsNullOrEmpty(text))
                return;

            var baseline = info.GetBaseline();
            var startPt = baseline.GetStartPoint();
            var endPt = baseline.GetEndPoint();
            double startXmm = startPt.Get(0) * ConvertHelper.Pt2Mm;
            double endXmm = endPt.Get(0) * ConvertHelper.Pt2Mm;
            double totalWidthMm = Math.Abs(endXmm - startXmm);
            double direction = Math.Sign(endXmm - startXmm);
            if (direction == 0)
                direction = 1;

            double perWidthMm = totalWidthMm > 1e-6 ? totalWidthMm / text.Length : info.GetFontSize() * ConvertHelper.Pt2Mm * 0.55d;
            double topMm = ComputeRunTop(info);
            double bottomMm = ComputeRunBottom(info);
            double baselineMm = ToOfdY(baseline.GetStartPoint().Get(1));

            for (int i = 0; i < text.Length; i++)
            {
                double charStart = startXmm + direction * perWidthMm * i;
                glyphs.Add(new GlyphMetrics(text[i], charStart, Math.Max(0.01d, Math.Abs(perWidthMm)), topMm, bottomMm, baselineMm));
            }
        }

        private static List<GlyphMetrics> TrimGlyphs(List<GlyphMetrics> glyphs)
        {
            int start = 0;
            while (start < glyphs.Count && char.IsWhiteSpace(glyphs[start].Character))
                start++;

            int end = glyphs.Count - 1;
            while (end >= start && char.IsWhiteSpace(glyphs[end].Character))
                end--;

            if (start > end)
                return new List<GlyphMetrics>();

            return glyphs.GetRange(start, end - start + 1);
        }

        private OfdText? BuildOfdText(List<GlyphMetrics> glyphs, List<TextRenderInfo> infos)
        {
            if (glyphs.Count == 0)
                return null;

            double pt2mm = ConvertHelper.Pt2Mm;

            double minXGlyph = glyphs.Min(g => g.StartX);
            double maxXGlyph = glyphs.Max(g => g.EndX);
            double minYGlyph = glyphs.Min(g => g.TopY);
            double maxYGlyph = glyphs.Max(g => g.BottomY);

            double minBBoxXPt = double.PositiveInfinity;
            double maxBBoxXPt = double.NegativeInfinity;
            double minBBoxYPt = double.PositiveInfinity;
            double maxBBoxYPt = double.NegativeInfinity;

            foreach (var info in infos)
            {
                var ascentRect = info.GetAscentLine().GetBoundingRectangle();
                minBBoxXPt = Math.Min(minBBoxXPt, ascentRect.GetLeft());
                maxBBoxXPt = Math.Max(maxBBoxXPt, ascentRect.GetRight());
                minBBoxYPt = Math.Min(minBBoxYPt, ascentRect.GetBottom());
                maxBBoxYPt = Math.Max(maxBBoxYPt, ascentRect.GetTop());

                var descentRect = info.GetDescentLine().GetBoundingRectangle();
                minBBoxXPt = Math.Min(minBBoxXPt, descentRect.GetLeft());
                maxBBoxXPt = Math.Max(maxBBoxXPt, descentRect.GetRight());
                minBBoxYPt = Math.Min(minBBoxYPt, descentRect.GetBottom());
                maxBBoxYPt = Math.Max(maxBBoxYPt, descentRect.GetTop());
            }

            if (!double.IsFinite(minBBoxXPt) || !double.IsFinite(maxBBoxXPt))
            {
                minBBoxXPt = minXGlyph / pt2mm;
                maxBBoxXPt = maxXGlyph / pt2mm;
            }

            if (!double.IsFinite(minBBoxYPt) || !double.IsFinite(maxBBoxYPt))
            {
                minBBoxYPt = (_pageSize.GetHeight() - maxYGlyph / pt2mm);
                maxBBoxYPt = (_pageSize.GetHeight() - minYGlyph / pt2mm);
            }

            double boundaryX = minBBoxXPt * pt2mm;
            double boundaryWidth = Math.Max(0.1d, (maxBBoxXPt - minBBoxXPt) * pt2mm);
            double boundaryTop = (_pageSize.GetHeight() - maxBBoxYPt) * pt2mm;
            double boundaryHeight = Math.Max(0.1d, (maxBBoxYPt - minBBoxYPt) * pt2mm);
            double boundaryBottom = boundaryTop + boundaryHeight;

            var textChars = glyphs.Select(g => g.Character).ToArray();
            string text = new string(textChars);
            if (string.IsNullOrWhiteSpace(text))
                return null;

            var starts = glyphs.Select(g => g.StartX).ToArray();
            var advances = glyphs.Select(g => g.Width).ToArray();
            double avgAdvance = advances.Average();
            double? spaceAdvance = null;
            foreach (var glyph in glyphs)
            {
                if (glyph.Character == ' ')
                {
                    spaceAdvance = glyph.Width;
                    break;
                }
            }

            float[]? deltas = null;
            if (_options.EnableDeltaX && glyphs.Count > 1)
            {
                deltas = new float[glyphs.Count];
                deltas[0] = 0f;
                for (int i = 1; i < glyphs.Count; i++)
                {
                    double diff = glyphs[i].StartX - glyphs[i - 1].StartX;
                    if (diff < 0 && Math.Abs(diff) <= _options.MaxNegativeKerningAbsorbMm)
                        diff = 0;
                    deltas[i] = (float)Math.Max(0d, diff);
                }
            }

            var firstInfo = infos[0];
            double baseline = ToOfdY(firstInfo.GetBaseline().GetStartPoint().Get(1));
            double fallbackBaseline = ComputeBaseline(glyphs);
            if (double.IsNaN(baseline) && !double.IsNaN(fallbackBaseline))
            {
                baseline = fallbackBaseline;
            }

            double textCodeX = glyphs.Count > 0 ? glyphs[0].StartX - boundaryX : 0d;
            if (Math.Abs(textCodeX) < 1e-6)
            {
                textCodeX = 0d;
            }
            double? textCodeY = double.IsNaN(baseline) ? null : baseline - boundaryTop;

            double fontSizeMm = firstInfo.GetFontSize() * ConvertHelper.Pt2Mm;
            string fontFamily = ExtractFontFamily(firstInfo);
            int[]? glyphCodes = ExtractGlyphCodes(firstInfo, text);

            double[]? ctmArray = null;
            try
            {
                var ctm = firstInfo.GetGraphicsState()?.GetCtm();
                if (ctm != null)
                {
                    ctmArray = new double[6];
                    for (int i = 0; i < 6; i++)
                    {
                        ctmArray[i] = ctm.Get(i) * pt2mm;
                    }
                }
            }
            catch
            {
                ctmArray = null;
            }

            if (starts.Length > 0)
            {
                double offset = boundaryX - starts[0];
                if (Math.Abs(offset) > 1e-6)
                {
                    for (int i = 0; i < starts.Length; i++)
                    {
                        starts[i] += offset;
                    }
                }
            }

            return new OfdText
            {
                Page = _pageNum,
                Text = text,
                X = boundaryX,
                Y = boundaryTop,
                Width = boundaryWidth,
                Height = boundaryHeight,
                FontFamily = fontFamily,
                FontSize = fontSizeMm,
                TopY = boundaryTop,
                BottomY = boundaryBottom,
                CharStarts = starts,
                CharAdvances = advances,
                BaselineY = baseline,
                DeltaX = deltas,
                AvgAdvance = avgAdvance,
                SpaceAdvance = spaceAdvance,
                Glyphs = glyphCodes,
                CTM = ctmArray,
                TextCodeX = textCodeX,
                TextCodeY = textCodeY
            };
        }

        private double ComputeRunTop(TextRenderInfo info)
        {
            var ascent = info.GetAscentLine();
            return Math.Min(ToOfdY(ascent.GetStartPoint().Get(1)), ToOfdY(ascent.GetEndPoint().Get(1)));
        }

        private double ComputeRunBottom(TextRenderInfo info)
        {
            var descent = info.GetDescentLine();
            return Math.Max(ToOfdY(descent.GetStartPoint().Get(1)), ToOfdY(descent.GetEndPoint().Get(1)));
        }

        private double ToOfdY(double valuePt)
        {
            return (_pageSize.GetHeight() - valuePt) * ConvertHelper.Pt2Mm;
        }

        private static double ComputeBaseline(List<GlyphMetrics> glyphs)
        {
            var ordered = glyphs.Select(g => g.BaselineY).Where(double.IsFinite).OrderBy(v => v).ToArray();
            if (ordered.Length == 0)
                return double.NaN;
            int mid = ordered.Length / 2;
            return (ordered.Length % 2 == 1) ? ordered[mid] : (ordered[mid - 1] + ordered[mid]) / 2d;
        }

        private string ExtractFontFamily(TextRenderInfo info)
        {
            try
            {
                var fontProgram = info.GetFont().GetFontProgram();
                if (fontProgram != null)
                {
                    var names = fontProgram.GetFontNames();
                    var family = names?.GetFontName() ?? names?.GetFamilyName()?.ToString();
                    if (!string.IsNullOrEmpty(family))
                        return ConvertHelper.NormalizeLogicalFontName(family);
                }
            }
            catch
            {
                // 忽略字体名称获取失败
            }

            var fallback = info.GetFont()?.GetFontProgram()?.GetFontNames()?.GetFontName();
            if (!string.IsNullOrEmpty(fallback))
                return ConvertHelper.NormalizeLogicalFontName(fallback);

            return "DefaultFont";
        }

        private static int[]? ExtractGlyphCodes(TextRenderInfo info, string text)
        {
            try
            {
                var fontProgram = info.GetFont().GetFontProgram();
                if (fontProgram == null)
                    return null;

                var codes = new int[text.Length];
                for (int i = 0; i < text.Length; i++)
                {
                    var glyph = fontProgram.GetGlyph(text[i]);
                    codes[i] = glyph?.GetCode() ?? text[i];
                }
                return codes;
            }
            catch
            {
                return null;
            }
        }

        private readonly struct GlyphMetrics
        {
            public GlyphMetrics(char character, double startX, double width, double topY, double bottomY, double baselineY)
            {
                Character = character;
                StartX = startX;
                Width = width;
                TopY = topY;
                BottomY = bottomY;
                BaselineY = baselineY;
            }

            public char Character { get; }
            public double StartX { get; }
            public double Width { get; }
            public double TopY { get; }
            public double BottomY { get; }
            public double BaselineY { get; }
            public double EndX => StartX + Width;
        }

        public ICollection<EventType> GetSupportedEvents() => new[] { EventType.RENDER_TEXT };

        private static string NormalizeRenderableText(string text)
        {
            if (string.IsNullOrEmpty(text))
                return string.Empty;

            bool needsNormalization = false;
            for (int i = 0; i < text.Length; i++)
            {
                char ch = text[i];
                if (ch is '\r' or '\n' or '\t' or '\f' or '\v' or '\0')
                {
                    needsNormalization = true;
                    break;
                }
            }

            if (!needsNormalization)
                return text;

            var buffer = text.ToCharArray();
            for (int i = 0; i < buffer.Length; i++)
            {
                buffer[i] = buffer[i] switch
                {
                    '\r' or '\n' or '\t' or '\f' or '\v' or '\0' => ' ',
                    _ => buffer[i]
                };
            }

            return new string(buffer);
        }
    }

    /// <summary>
    /// 文本聚合辅助：将 raw 列表按行合并，尝试恢复原始文本行（合并相近 Y 的片段并在适当处添加空格）
    /// 该算法基于纵向容差与水平间距启发式判断
    /// </summary>
    private static class TextAggregationHelper
    {
        public static List<OfdText> Aggregate(List<OfdText> raw, ILogger? logger, int page, ConvertHelper.PdfToOfdOptions opt)
        {
            return AggregateInternal(raw, logger, page, opt, splitIntoWords: false);
        }

        public static List<OfdText> AggregateWords(List<OfdText> raw, ILogger? logger, int page, ConvertHelper.PdfToOfdOptions opt)
        {
            return AggregateInternal(raw, logger, page, opt, splitIntoWords: true);
        }

        private static List<OfdText> AggregateInternal(List<OfdText> raw, ILogger? logger, int page, ConvertHelper.PdfToOfdOptions opt, bool splitIntoWords)
        {
            if (raw == null || raw.Count == 0)
                return raw ?? new List<OfdText>();

            var lineBuckets = BuildLineBuckets(raw);
            var result = new List<OfdText>();

            foreach (var bucket in lineBuckets)
            {
                var segs = bucket.OrderBy(s => s.X).ToList();
                if (segs.Count == 0)
                    continue;

                double avgFontSize = segs.Average(s => s.FontSize > 0 ? s.FontSize : 12d);
                double lineTop = segs.Min(GetTop);
                double lineBottom = segs.Max(GetBottom);
                double lineHeight = Math.Max(0.1d, lineBottom - lineTop);
                double baselineY = ComputeLineBaseline(segs, lineTop, lineBottom);
                if (double.IsNaN(baselineY))
                {
                    baselineY = lineBottom - (lineHeight * 0.2d);
                }

                bool lineMostlyCjk = IsLineMostlyCjk(segs);
                var chars = BuildCharacterStream(segs, opt, avgFontSize, lineMostlyCjk, logger, page);
                if (chars.Count == 0)
                    continue;

                bool splitThisLine = splitIntoWords;
                if (splitIntoWords && opt.OnlySplitLatinWords)
                {
                    bool hasCjk = segs.Any(s => s.Text.Any(IsCjk));
                    bool hasLatinOrDigit = segs.Any(s => s.Text.Any(ch => (ch >= 'A' && ch <= 'Z') || (ch >= 'a' && ch <= 'z') || char.IsDigit(ch)));
                    bool hasAsciiSpace = segs.Any(s => s.Text.Contains(' '));
                    if (hasCjk && !hasLatinOrDigit && !hasAsciiSpace)
                    {
                        splitThisLine = false;
                    }
                }

                if (splitThisLine)
                {
                    EmitWords(result, segs, chars, avgFontSize, lineHeight, baselineY, logger, page, opt);
                }
                else
                {
                    EmitRun(result, segs, chars, 0, chars.Count - 1, avgFontSize, lineHeight, baselineY, logger, page, opt);
                }
            }

            logger?.LogDebug("[PDF2OFD][Text][Aggregate:{Mode}] Page {Page} Raw={Raw} Lines={Lines} Output={Out}",
                splitIntoWords ? "Words" : "Lines", page, raw.Count, lineBuckets.Count, result.Count);
            return result;
        }

        private static List<List<OfdText>> BuildLineBuckets(List<OfdText> raw)
        {
            var ordered = raw.OrderBy(t => GetTop(t)).ThenBy(t => t.X).ToList();
            var buckets = new List<List<OfdText>>();

            foreach (var blk in ordered)
            {
                double blkTop = GetTop(blk);
                double blkBottom = GetBottom(blk);
                double blkHeight = Math.Max(0.1d, blkBottom - blkTop);
                double blkBaseline = EstimateBaseline(blk);

                bool placed = false;
                foreach (var line in buckets)
                {
                    double lineTop = line.Min(GetTop);
                    double lineBottom = line.Max(GetBottom);
                    double lineHeight = Math.Max(0.1d, lineBottom - lineTop);
                    double lineBaseline = ComputeLineBaseline(line, lineTop, lineBottom);

                    double overlap = Math.Min(lineBottom, blkBottom) - Math.Max(lineTop, blkTop);
                    double minHeight = Math.Min(lineHeight, blkHeight);
                    double allowedGap = Math.Max(0.2d, minHeight * 0.25d);
                    double baselineGap = Math.Max(0.2d, minHeight * 0.35d);

                    if (overlap >= -allowedGap && Math.Abs(lineBaseline - blkBaseline) <= baselineGap)
                    {
                        line.Add(blk);
                        placed = true;
                        break;
                    }
                }

                if (!placed)
                {
                    buckets.Add(new List<OfdText> { blk });
                }
            }

            return buckets;
        }

        private static double GetTop(OfdText text)
        {
            if (text.TopY.HasValue)
                return text.TopY.Value;

            if (text.BaselineY.HasValue && text.Height > 0)
                return text.BaselineY.Value - text.Height;

            if (text.BaselineY.HasValue && text.FontSize > 0)
                return text.BaselineY.Value - (text.FontSize * 1.15d);

            if (text.Height > 0)
                return text.Y;

            if (text.FontSize > 0)
                return text.Y;

            return text.Y;
        }

        private static double GetBottom(OfdText text)
        {
            if (text.BottomY.HasValue)
                return text.BottomY.Value;

            if (text.BaselineY.HasValue)
                return text.BaselineY.Value;

            double top = GetTop(text);
            double height = text.Height > 0 ? text.Height : (text.FontSize > 0 ? text.FontSize * 1.2d : 0d);
            if (height <= 0)
                height = 2d;
            return top + height;
        }

        private static double EstimateBaseline(OfdText text)
        {
            if (text.BaselineY.HasValue)
                return text.BaselineY.Value;

            if (text.BottomY.HasValue)
                return text.BottomY.Value;

            double top = GetTop(text);
            double height = text.Height > 0 ? text.Height : (text.FontSize > 0 ? text.FontSize * 1.2d : 0d);
            if (height <= 0)
                height = 2d;
            return top + height;
        }

        private static double ComputeLineBaseline(IReadOnlyList<OfdText> segments, double lineTop, double lineBottom)
        {
            var baselines = new List<double>();
            foreach (var seg in segments)
            {
                if (seg.BaselineY.HasValue)
                    baselines.Add(seg.BaselineY.Value);
                else if (seg.BottomY.HasValue)
                    baselines.Add(seg.BottomY.Value);
            }

            if (baselines.Count > 0)
            {
                baselines.Sort();
                int mid = baselines.Count / 2;
                if ((baselines.Count & 1) == 1)
                    return baselines[mid];
                return (baselines[mid - 1] + baselines[mid]) / 2d;
            }

            double lineHeight = Math.Max(0.1d, lineBottom - lineTop);
            return lineBottom - Math.Max(0.1d, lineHeight * 0.2d);
        }

        private static bool IsLineMostlyCjk(IEnumerable<OfdText> segments)
        {
            int cjk = 0;
            int ascii = 0;

            foreach (var seg in segments)
            {
                foreach (var ch in seg.Text)
                {
                    if (IsCjk(ch))
                        cjk++;
                    else if (!char.IsControl(ch))
                        ascii++;
                }
            }

            return cjk > 0 && cjk >= ascii;
        }

        private static bool IsCjk(char ch) => ch >= '\u4E00' && ch <= '\u9FFF';

        private static List<LineChar> BuildCharacterStream(List<OfdText> segs, ConvertHelper.PdfToOfdOptions opt, double avgFontSize, bool lineMostlyCjk, ILogger? logger, int page)
        {
            var chars = new List<LineChar>();
            double cursorRight = double.NaN;
            OfdText? prevSeg = null;

            foreach (var seg in segs)
            {
                var segChars = ExtractSegmentChars(seg, avgFontSize);
                if (segChars.Count == 0)
                {
                    prevSeg = seg;
                    continue;
                }

                double segFirstStart = segChars[0].Start;
                if (!double.IsNaN(cursorRight))
                {
                    double gap = segFirstStart - cursorRight;
                    double baseAdvance = DetermineBaseAdvance(prevSeg, seg, avgFontSize);
                    bool treatAsCjk = lineMostlyCjk || seg.Text.Any(IsCjk);

                    if (gap < 0 && Math.Abs(gap) <= opt.MaxNegativeKerningAbsorbMm)
                    {
                        gap = 0;
                    }

                    if (ShouldInsertSyntheticSpace(gap, prevSeg, seg, baseAdvance, treatAsCjk, opt))
                    {
                        double baseSpace = seg.SpaceAdvance ?? (seg.AvgAdvance ?? baseAdvance);
                        if (baseSpace <= 0)
                            baseSpace = baseAdvance;
                        int spaceCount = (int)Math.Max(1d, Math.Min(opt.MaxSyntheticSpacesPerGap, Math.Round(gap / baseSpace)));
                        double spaceWidth = gap / spaceCount;
                        double spaceX = cursorRight;
                        for (int i = 0; i < spaceCount; i++)
                        {
                            var synthetic = new LineChar(' ', spaceX, spaceWidth, true);
                            chars.Add(synthetic);
                            if (opt.EnableDebugWordLayout && logger != null)
                            {
                                logger.LogDebug("[PDF2OFD][Text][SynthSpace] Page {Page} X={X:F2} W={W:F2}", page, synthetic.Start, synthetic.Width);
                            }
                            spaceX += spaceWidth;
                        }
                        cursorRight += gap;
                    }
                    else if (gap < 0)
                    {
                        double shift = cursorRight - segFirstStart;
                        if (shift > 0)
                        {
                            segChars = ShiftCharacters(segChars, shift);
                        }
                    }
                }

                foreach (var ch in segChars)
                {
                    chars.Add(ch);
                    cursorRight = double.IsNaN(cursorRight) ? ch.End : Math.Max(cursorRight, ch.End);
                    if (opt.EnableDebugWordLayout && logger != null)
                    {
                        logger.LogDebug("[PDF2OFD][Text][Char] Page {Page} Ch='{Ch}' X={X:F2} W={W:F2} Synthetic={Synth}",
                            page, ch.Char, ch.Start, ch.Width, ch.Synthetic);
                    }
                }

                prevSeg = seg;
            }

            return chars;
        }

        private static double DetermineBaseAdvance(OfdText? prev, OfdText cur, double avgFontSize)
        {
            if (cur.SpaceAdvance.HasValue && cur.SpaceAdvance.Value > 0)
                return cur.SpaceAdvance.Value;
            if (prev?.SpaceAdvance.HasValue == true && prev.SpaceAdvance.Value > 0)
                return prev.SpaceAdvance.Value;
            if (cur.AvgAdvance.HasValue && cur.AvgAdvance.Value > 0)
                return cur.AvgAdvance.Value;
            if (prev?.AvgAdvance.HasValue == true && prev.AvgAdvance.Value > 0)
                return prev.AvgAdvance.Value;
            double size = cur.FontSize > 0 ? cur.FontSize : avgFontSize;
            return size * 0.55d;
        }

        private static double ComputeGapThreshold(double baseAdvance, bool treatAsCjk, ConvertHelper.PdfToOfdOptions opt)
        {
            double ratio = treatAsCjk ? Math.Min(opt.CjkGapTriggerRatio, 0.3d) : opt.GapSpaceTriggerRatio;
            return Math.Max(opt.MinGapForSyntheticSpaceMm, baseAdvance * ratio);
        }

        private static bool ShouldInsertSyntheticSpace(double gap, OfdText? prevSeg, OfdText curSeg, double baseAdvance, bool treatAsCjk, ConvertHelper.PdfToOfdOptions opt)
        {
            if (gap <= 0)
                return false;

            double threshold = ComputeGapThreshold(baseAdvance, treatAsCjk, opt);

            double prevAdvance = GetTailAdvance(prevSeg, baseAdvance);
            double curAdvance = GetHeadAdvance(curSeg, baseAdvance);
            double localAdvance = Math.Max(prevAdvance, curAdvance);
            if (localAdvance > 0)
            {
                double guard = Math.Max(opt.MinGapForSyntheticSpaceMm, localAdvance * 0.6d);
                if (gap <= guard)
                    return false;
                threshold = Math.Max(threshold, guard);
            }

            if (prevSeg != null && IsNumericFragment(prevSeg.Text) && IsNumericFragment(curSeg.Text))
            {
                double numericAdvance = Math.Max(localAdvance, baseAdvance);
                double numericThreshold = Math.Max(opt.NumericMinGapMm, numericAdvance * opt.NumericGapMultiplier);
                if (gap <= numericThreshold)
                    return false;
                threshold = Math.Max(threshold, numericThreshold);
            }

            return gap > threshold;
        }

        private static double GetHeadAdvance(OfdText seg, double fallback)
        {
            if (seg.CharAdvances != null && seg.CharAdvances.Length > 0)
            {
                var head = seg.CharAdvances[0];
                if (double.IsFinite(head) && head > 0)
                    return head;
            }

            if (seg.AvgAdvance.HasValue && seg.AvgAdvance.Value > 0)
                return seg.AvgAdvance.Value;

            return fallback;
        }

        private static double GetTailAdvance(OfdText? seg, double fallback)
        {
            if (seg == null)
                return fallback;

            if (seg.CharAdvances != null && seg.CharAdvances.Length > 0)
            {
                var tail = seg.CharAdvances[seg.CharAdvances.Length - 1];
                if (double.IsFinite(tail) && tail > 0)
                    return tail;
            }

            if (seg.AvgAdvance.HasValue && seg.AvgAdvance.Value > 0)
                return seg.AvgAdvance.Value;

            return fallback;
        }

        private static List<LineChar> ExtractSegmentChars(OfdText seg, double avgFontSize)
        {
            var list = new List<LineChar>(seg.Text.Length);
            if (seg.Text.Length == 0)
                return list;

            if (seg.CharStarts != null && seg.CharAdvances != null &&
                seg.CharStarts.Length == seg.Text.Length && seg.CharAdvances.Length == seg.Text.Length)
            {
                for (int i = 0; i < seg.Text.Length; i++)
                {
                    double width = seg.CharAdvances[i];
                    if (!double.IsFinite(width) || width <= 0)
                    {
                        width = seg.AvgAdvance ?? (avgFontSize * 0.55d);
                    }
                    list.Add(new LineChar(seg.Text[i], seg.CharStarts[i], Math.Max(0.01d, width), false));
                }
                return list;
            }

            if (seg.DeltaX != null && seg.DeltaX.Length == seg.Text.Length)
            {
                double start = seg.X;
                var starts = new double[seg.Text.Length];
                double current = start;
                for (int i = 0; i < seg.Text.Length; i++)
                {
                    if (i == 0)
                    {
                        starts[i] = current;
                    }
                    else
                    {
                        current += seg.DeltaX[i];
                        starts[i] = current;
                    }
                }

                double segWidth = seg.Width > 0 ? seg.Width : (seg.AvgAdvance ?? (avgFontSize * 0.55d)) * seg.Text.Length;
                for (int i = 0; i < seg.Text.Length; i++)
                {
                    double width;
                    if (i < seg.Text.Length - 1)
                    {
                        width = starts[i + 1] - starts[i];
                    }
                    else
                    {
                        width = segWidth - (starts[i] - start);
                    }

                    if (!double.IsFinite(width) || width <= 0)
                    {
                        width = seg.AvgAdvance ?? (avgFontSize * 0.55d);
                    }

                    list.Add(new LineChar(seg.Text[i], starts[i], Math.Max(0.01d, width), false));
                }
                return list;
            }

            double fallbackWidth = seg.Width > 0 ? seg.Width : (seg.AvgAdvance ?? (avgFontSize * 0.55d)) * Math.Max(1, seg.Text.Length);
            double perChar = fallbackWidth / Math.Max(1, seg.Text.Length);
            for (int i = 0; i < seg.Text.Length; i++)
            {
                double start = seg.X + perChar * i;
                list.Add(new LineChar(seg.Text[i], start, Math.Max(0.01d, perChar), false));
            }
            return list;
        }

        private static List<LineChar> ShiftCharacters(List<LineChar> source, double shift)
        {
            if (Math.Abs(shift) <= 1e-6d)
                return source;
            var shifted = new List<LineChar>(source.Count);
            foreach (var ch in source)
            {
                shifted.Add(new LineChar(ch.Char, ch.Start + shift, ch.Width, ch.Synthetic));
            }
            return shifted;
        }

        private static bool IsNumericFragment(string text)
        {
            if (string.IsNullOrEmpty(text))
                return false;
            for (int i = 0; i < text.Length; i++)
            {
                char ch = text[i];
                if (char.IsDigit(ch))
                    continue;
                if (ch is '-' or '/' or ':' or '年' or '月' or '日' or '.')
                    continue; // allow common date separators
                if (char.IsWhiteSpace(ch))
                    continue;
                return false;
            }
            return true;
        }

        private static void EmitWords(List<OfdText> result, List<OfdText> segs, List<LineChar> chars, double avgFontSize, double lineHeight, double baselineY, ILogger? logger, int page, ConvertHelper.PdfToOfdOptions opt)
        {
            int index = 0;
            while (index < chars.Count)
            {
                while (index < chars.Count && chars[index].Char == ' ')
                    index++;
                if (index >= chars.Count)
                    break;

                int end = index;
                while (end + 1 < chars.Count && chars[end + 1].Char != ' ')
                    end++;

                EmitRun(result, segs, chars, index, end, avgFontSize, lineHeight, baselineY, logger, page, opt);
                index = end + 1;
            }
        }

        private static void EmitRun(List<OfdText> result, List<OfdText> segs, List<LineChar> chars, int startIndex, int endIndex, double avgFontSize, double lineHeight, double baselineY, ILogger? logger, int page, ConvertHelper.PdfToOfdOptions opt)
        {
            if (endIndex < startIndex)
                return;

            int length = endIndex - startIndex + 1;
            var textBuffer = new char[length];
            var starts = new double[length];
            var advances = new double[length];
            var deltas = new float[length];

            double prevStart = 0d;
            for (int i = 0; i < length; i++)
            {
                var glyph = chars[startIndex + i];
                textBuffer[i] = glyph.Char;
                starts[i] = glyph.Start;
                advances[i] = glyph.Width;
                if (i == 0)
                {
                    deltas[i] = 0f;
                    prevStart = glyph.Start;
                }
                else
                {
                    double diff = glyph.Start - prevStart;
                    if (diff < 0 && Math.Abs(diff) <= opt.MaxNegativeKerningAbsorbMm)
                    {
                        diff = 0;
                    }
                    deltas[i] = (float)Math.Max(0d, diff);
                    prevStart = glyph.Start;
                }
            }

            string text = new string(textBuffer);
            if (string.IsNullOrWhiteSpace(text))
                return;

            double minX = starts[0];
            double maxR = starts[length - 1] + advances[length - 1];
            double avgAdvance = advances.Average();
            double? spaceAdvance = null;
            for (int i = 0; i < length; i++)
            {
                if (textBuffer[i] == ' ')
                {
                    spaceAdvance = advances[i];
                    break;
                }
            }

            double topY = segs.Min(s => s.Y);
            double textCodeX = starts.Length > 0 ? starts[0] - minX : 0d;
            if (Math.Abs(textCodeX) < 1e-6)
            {
                textCodeX = 0d;
            }
            double? textCodeY = double.IsNaN(baselineY) ? null : baselineY - topY;

            var ofdText = new OfdText
            {
                Page = segs[0].Page,
                Text = text,
                X = minX,
                Y = topY,
                Width = Math.Max(0.1d, maxR - minX),
                Height = lineHeight <= 0 ? avgFontSize * 1.2d : lineHeight,
                FontFamily = SelectDominantFont(segs),
                FontSize = avgFontSize,
                CTM = PickCtm(segs),
                DeltaX = (opt.EnableDeltaX && length > 1) ? deltas : null,
                AvgAdvance = avgAdvance,
                SpaceAdvance = spaceAdvance,
                CharStarts = starts,
                CharAdvances = advances,
                BaselineY = baselineY,
                TextCodeX = textCodeX,
                TextCodeY = textCodeY
            };

            result.Add(ofdText);

            if (opt.EnableDebugWordLayout && logger != null)
            {
                logger.LogDebug("[PDF2OFD][Text][Emit] Page {Page} Text='{Text}' X={X:F2} W={W:F2} Len={Len}",
                    page, text, ofdText.X, ofdText.Width, text.Length);
            }
        }

        private static string SelectDominantFont(List<OfdText> segs)
        {
            var group = segs
                .GroupBy(s => s.FontFamily)
                .OrderByDescending(g => g.Count())
                .ThenByDescending(g => g.Sum(x => x.Text.Length))
                .FirstOrDefault();
            return group?.Key ?? segs[0].FontFamily;
        }

        private static double[]? PickCtm(List<OfdText> segs)
        {
            foreach (var seg in segs)
            {
                if (seg.CTM != null && seg.CTM.Length == 6)
                    return seg.CTM;
            }
            return null;
        }

        private readonly struct LineChar
        {
            public LineChar(char ch, double start, double width, bool synthetic)
            {
                Char = ch;
                Start = start;
                Width = width;
                Synthetic = synthetic;
            }

            public char Char
            {
                get;
            }
            public double Start
            {
                get;
            }
            public double Width
            {
                get;
            }
            public bool Synthetic
            {
                get;
            }
            public double End => Start + Width;
        }
    }
}
