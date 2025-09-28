using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
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
                if (options.PageFilter != null && !options.PageFilter(i)) { logger?.LogDebug("[PDF2OFD][Text] Page {P} 被过滤", i); continue; }
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
                        blocks = TextAggregationHelper.Aggregate(strategy.TextBlocks, logger, i);
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
            List<OfdText> blocks = options.PerGlyphPositioning
                ? strategy.TextBlocks
                : (options.SplitTextBySpace
                    ? TextAggregationHelper.AggregateWords(strategy.TextBlocks, logger, pageIndex, options)
                    : TextAggregationHelper.Aggregate(strategy.TextBlocks, logger, pageIndex));
            if (blocks.Count > 0) foreach (var blk in blocks) (ofd as OfdWriter)?.AddText(blk);
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

        // 收集的原始文本块（每个 renderInfo 对应一条）
        public List<OfdText> TextBlocks { get; } = new();

        public TextRenderListener(int pageNum, Rectangle pageSize, ConvertHelper.PdfToOfdOptions options, ILogger? logger)
        { _pageNum = pageNum; _pageSize = pageSize; _options = options; _logger = logger; }

        /// <summary>
        /// 事件处理：只处理 RENDER_TEXT 类型的事件
        /// 将 iText 的坐标系与字体信息转换为 OFD 所需的 OfdText
        /// </summary>
        public void EventOccurred(IEventData data, EventType type)
        {
            if (type != EventType.RENDER_TEXT) return;
            var renderInfo = (TextRenderInfo)data;
            var text = renderInfo.GetText();
            if (string.IsNullOrWhiteSpace(text)) return;

            // 原始几何数据（与旧逻辑保持一致用于基线/定位）
            var ascent = renderInfo.GetAscentLine();
            var descent = renderInfo.GetDescentLine();
            var x = descent.GetStartPoint().Get(0);
            var yBase = descent.GetStartPoint().Get(1);
            var rawWidth = descent.GetEndPoint().Get(0) - x; // 仅作回退参考
            var heightRaw = ascent.GetStartPoint().Get(1) - yBase;
            if (heightRaw < 0) heightRaw = Math.Abs(heightRaw);
            var pageHeight = _pageSize.GetHeight();
            var y = pageHeight - yBase - heightRaw;
            double fontSizePt = renderInfo.GetFontSize();

            double xMm = x * ConvertHelper.Pt2Mm;
            double yMm = y * ConvertHelper.Pt2Mm;
            Refactor.Utils.FontMetricsHelper.RunMetrics metrics;
            try
            {
                metrics = Refactor.Utils.FontMetricsHelper.ComputeRunMetrics(renderInfo, renderInfo.GetFont());
            }
            catch
            {
                // 回退：保持最初逻辑
                double fallbackWidthPt = rawWidth > 0 ? rawWidth : fontSizePt * Math.Max(1, text.Length * 0.6);
                metrics = new Refactor.Utils.FontMetricsHelper.RunMetrics(
                    fallbackWidthPt,
                    new double[text.Length],
                    fontSizePt * 0.6,
                    fontSizePt * 0.5,
                    text.Any(c=>c>='\u4E00'&&c<='\u9FFF'),
                    fontSizePt*1.2,
                    0);
            }
            // Sanitize 宽度
            double finalWidthPt = Refactor.Utils.FontMetricsHelper.SanitizeWidthPt(metrics.RunAdvancePt, text.Length, metrics.IsCjk, fontSizePt, 1.0, metrics.AvgAdvancePt);
            double wMm = finalWidthPt * ConvertHelper.Pt2Mm;
            double hMm = metrics.LineHeightPt * ConvertHelper.Pt2Mm;
            double fontSizeMm = fontSizePt * ConvertHelper.Pt2Mm;

            // DeltaX：Step 模式（相邻字符 advance）
            double[]? ctmArray = null; float[]? deltaXArray = null;
            try
            {
                // 去掉 CTM 避免双重缩放（坐标已转换为mm）
                // var tm = renderInfo.GetTextMatrix();
                // var a = tm.Get(Matrix.I11); var b = tm.Get(Matrix.I12); var c = tm.Get(Matrix.I21); var d = tm.Get(Matrix.I22);
                // ctmArray = new double[] { a * ConvertHelper.Pt2Mm, b * ConvertHelper.Pt2Mm, c * ConvertHelper.Pt2Mm, d * ConvertHelper.Pt2Mm, xMm, yMm };
                ctmArray = null; // 暂时移除CTM避免重复缩放
                if (_options.EnableDeltaX && metrics.CharAdvancesPt.Length == text.Length && text.Length>1)
                {
                    // DeltaX 长度 = n-1 (字符数-1)
                    deltaXArray = new float[text.Length - 1];
                    for (int i=0;i<text.Length-1;i++)
                    {
                        double advPt = metrics.CharAdvancesPt[i];
                        deltaXArray[i] = (float)(advPt * ConvertHelper.Pt2Mm);
                    }
                }
            }
            catch (Exception ex)
            { _logger?.LogDebug(ex, "[PDF2OFD][Text] Page {Page} 捕获 CTM/DeltaX 失败", _pageNum); }

            // 尝试获取字形编码
            int[]? glyphCodes = null;
            try
            {
                var fp = renderInfo.GetFont().GetFontProgram();
                if (fp != null)
                {
                    var codes = new List<int>();
                    for (int i = 0; i < text.Length; i++)
                    {
                        var glyph = fp.GetGlyph(text[i]);
                        if (glyph != null)
                        {
                            codes.Add(glyph.GetCode());
                        }
                        else
                        {
                            // 无法获取字形时使用 Unicode 值
                            codes.Add((int)text[i]);
                        }
                    }
                    glyphCodes = codes.ToArray();
                }
            }
            catch { /* 忽略字形获取失败 */ }

            // 将转换后的文本块加入集合
            var fontProgram = renderInfo.GetFont().GetFontProgram();
            var fontNames = fontProgram.GetFontNames();
            var fontFamily = ConvertHelper.NormalizeLogicalFontName(fontNames.GetFontName() ?? fontNames.GetFamilyName()?.ToString() ?? "DefaultFont");
            TextBlocks.Add(new OfdText {
                Page = _pageNum,
                Text = text,
                X = (float)xMm,
                Y = (float)yMm,
                Width = (float)wMm,
                Height = (float)hMm,
                FontFamily = fontFamily,
                FontSize = (float)fontSizeMm,
                CTM = ctmArray,
                DeltaX = deltaXArray,
                AvgAdvance = metrics.AvgAdvancePt * ConvertHelper.Pt2Mm,
                SpaceAdvance = metrics.SpaceWidthPt * ConvertHelper.Pt2Mm,
                DeltaXMode = _options.EnableDeltaX ? "Step" : null,
                Glyphs = glyphCodes
            });
            return;
        }

        public ICollection<EventType> GetSupportedEvents() => new[] { EventType.RENDER_TEXT };
    }

    /// <summary>
    /// 文本聚合辅助：将 raw 列表按行合并，尝试恢复原始文本行（合并相近 Y 的片段并在适当处添加空格）
    /// 该算法基于纵向容差与水平间距启发式判断
    /// </summary>
    private static class TextAggregationHelper
    {
        public static List<OfdText> Aggregate(List<OfdText> raw, ILogger? logger, int page)
        {
            if (raw.Count == 0) return raw;

            // 先按 Y 排序，方便行分组（注意：Y 值越小通常在页面越靠上，视具体坐标系而定）
            var orderedAll = raw.OrderBy(t => t.Y).ToList();

            // 行桶：将 Y 值接近的文本块分为同一行
            var lineBuckets = new List<List<OfdText>>();
            foreach (var blk in orderedAll)
            {
                bool placed = false;
                foreach (var line in lineBuckets)
                {
                    double refFont = line[0].FontSize <= 0 ? 12d : line[0].FontSize;
                    double tolerance = Math.Max(1.5d, refFont * 0.8d); // 垂直容差，根据字体大小调整
                    if (Math.Abs(line[0].Y - blk.Y) < tolerance) { line.Add(blk); placed = true; break; }
                }
                if (!placed) lineBuckets.Add(new List<OfdText> { blk });
            }

            var result = new List<OfdText>(lineBuckets.Count);

            // 对每一行内部按 X 排序并合并成字符串，估计间距决定是否插入空格
            foreach (var line in lineBuckets)
            {
                var segs = line.OrderBy(s => s.X).ToList(); if (segs.Count == 0) continue;
                var fontGroup = segs.GroupBy(s => s.FontFamily).OrderByDescending(g => g.Count()).First();
                string lineFont = fontGroup.Key; double avgSize = segs.Average(s => (double)s.FontSize);
                double minX = segs.Min(s => (double)s.X);

                double EstimateWidth(OfdText t)
                {
                    if (t.Width > 0) return t.Width;
                    // 回退：使用 AvgAdvance * 字符数
                    if (t.AvgAdvance.HasValue) return t.AvgAdvance.Value * Math.Max(1, t.Text.Length);
                    // 最后兜底使用字体大小 * 0.55
                    return (t.FontSize > 0 ? t.FontSize : avgSize) * 0.55 * Math.Max(1, t.Text.Length);
                }

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
                    double spaceRef = 0d;
                    if (first.SpaceAdvance.HasValue && first.SpaceAdvance.Value > 0) spaceRef = first.SpaceAdvance.Value;
                    else if (first.AvgAdvance.HasValue) spaceRef = first.AvgAdvance.Value;
                    else spaceRef = (first.FontSize > 0 ? first.FontSize : avgSize) * 0.55; // pt->mm 已在 earlier? 注意：这里变量单位都是 mm （字体 size 是 mm）
                    bool lineMostlyCjk = segs.Count(s => s.Text.Any(ch => ch >= '\u4E00' && ch <= '\u9FFF')) >= segs.Count * 0.5;
                    double triggerRatio = 0.85d;
                    if (lineMostlyCjk)
                    {
                        // 中文等宽时 spaceRef 较大，使用更低触发阈值；参考选项 CjkGapTriggerRatio（若可获取）
                        // 这里无法直接访问 options，只能基于经验值 0.45。
                        triggerRatio = 0.45d; // 与 PdfToOfdOptions.CjkGapTriggerRatio 默认保持一致
                        // 进一步压缩参考宽度，防止阈值过大
                        spaceRef = Math.Min(spaceRef, (first.FontSize > 0 ? first.FontSize : avgSize) * 0.7);
                    }
                    if (gap > spaceRef * triggerRatio) sb.Append(' ');
                    sb.Append(cur.Text);
                    cursorRight = Math.Max(cursorRight, cur.X + curEstW);
                    if (cur.Height > 0) maxH = Math.Max(maxH, cur.Height);
                }

                string merged = sb.ToString(); if (string.IsNullOrWhiteSpace(merged)) continue;

                // 构建合并后的 OfdText，并加入结果
                result.Add(new OfdText { Page = segs[0].Page, Text = merged, X = (float)minX, Y = segs.Min(s => s.Y), Width = (float)(cursorRight - minX), Height = (float)(maxH <= 0 ? avgSize * 1.2d : maxH), FontFamily = lineFont, FontSize = (float)avgSize });
            }

            // 日志：显示原始片段数、行数与聚合后块数，便于调试
            logger?.LogDebug("[PDF2OFD][Text][Aggregate] Page {Page} 原始={Raw} 行数={Lines} 聚合后块={Agg}", page, raw.Count, lineBuckets.Count, result.Count);
            return result;
        }

        /// <summary>
        /// <summary>
        /// 直接从原始片段进行“行 + 词”两级聚合，按空格 & 水平间隙划分词。
        /// 与 Aggregate 不同：不会先构造整行再二次均分，避免词宽不准确。
        /// 逻辑：
        /// 1. 先行分组（同 Aggregate）。
        /// 2. 行内按 X 排序，基于 EstimateWidth 计算片段右边界。
        /// 3. 构造一个字符流：片段文本直接串接；遇到需要补空格的 gap 插入一个占位空格（只用于分词，不入输出）。
        /// 4. 遍历字符流，记录每个字符的起始 X 与宽度（对补空格使用 gap 宽度，对真实字符按 (segmentWidth/segmentTextLength) 均分）。
        /// 5. 以空格分隔成词，词的 X/Width = 覆盖字符的最小X和最大( X+Width )。
        /// 这样对多段组合成一行中被补的空格能体现真实 gap 宽度。
        /// </summary>
        public static List<OfdText> AggregateWords(List<OfdText> raw, ILogger? logger, int page, ConvertHelper.PdfToOfdOptions opt)
        {
            if (raw.Count == 0) return raw;
            // 复用行分组逻辑
            var orderedAll = raw.OrderBy(t => t.Y).ToList();
            var lineBuckets = new List<List<OfdText>>();
            foreach (var blk in orderedAll)
            {
                bool placed = false;
                foreach (var line in lineBuckets)
                {
                    double refFont = line[0].FontSize <= 0 ? 12d : line[0].FontSize;
                    double tolerance = Math.Max(1.5d, refFont * 0.8d);
                    if (Math.Abs(line[0].Y - blk.Y) < tolerance) { line.Add(blk); placed = true; break; }
                }
                if (!placed) lineBuckets.Add(new List<OfdText> { blk });
            }

            var result = new List<OfdText>();
            foreach (var line in lineBuckets)
            {
                var segs = line.OrderBy(s => s.X).ToList(); if (segs.Count == 0) continue;
                var fontGroup = segs.GroupBy(s => s.FontFamily).OrderByDescending(g => g.Count()).First();
                string lineFont = fontGroup.Key; double avgSize = segs.Average(s => (double)s.FontSize);
                double maxH = segs.Max(s => (double)(s.Height > 0 ? s.Height : (s.FontSize > 0 ? s.FontSize * 1.2d : avgSize * 1.2d)));

                // 仅拉丁行分词：如启用 OnlySplitLatinWords 且检测到 CJK 则直接行级合并
                if (opt.OnlySplitLatinWords)
                {
                    bool hasCjk = segs.Any(s => s.Text.Any(ch => ch >= '\u4E00' && ch <= '\u9FFF'));
                    bool hasAsciiLetterOrDigit = segs.Any(s => s.Text.Any(ch => (ch >= 'A' && ch <= 'Z') || (ch >= 'a' && ch <= 'z') || char.IsDigit(ch)));
                    bool hasAsciiSpace = segs.Any(s => s.Text.Contains(' '));
                    // 只有纯 CJK 且没有 ASCII 空格/字母/数字才回退
                    if (hasCjk && !hasAsciiLetterOrDigit && !hasAsciiSpace)
                    {
                        var mergedLine = Aggregate(segs, logger, page);
                        result.AddRange(mergedLine);
                        continue;
                    }
                }

                double EstimateWidth(OfdText t)
                {
                    if (t.Width > 0) return t.Width;
                    if (t.AvgAdvance.HasValue) return t.AvgAdvance.Value * Math.Max(1, t.Text.Length);
                    return (t.FontSize > 0 ? t.FontSize : avgSize) * 0.55 * Math.Max(1, t.Text.Length);
                }

                // 构建字符级列表
                var chars = new List<(char ch, double x, double w, bool isGap, string src)>();
                double cursorRight = segs[0].X + EstimateWidth(segs[0]);

                void AppendSegmentChars(OfdText seg)
                {
                    // 利用 DeltaX（字符间横向 advance），约定：DeltaX[0]=0，DeltaX[i]= 前后字符起点差。
                    if (seg.DeltaX != null && seg.DeltaX.Length == seg.Text.Length)
                    {
                        double segStart = seg.X;
                        double segEstimatedRight = seg.X + EstimateWidth(seg);
                        // 构造字符起点数组
                        double running = 0d;
                        var starts = new double[seg.Text.Length];
                        for (int ci = 0; ci < seg.Text.Length; ci++)
                        {
                            if (ci == 0) running = 0d; else running += seg.DeltaX[ci];
                            starts[ci] = segStart + running;
                        }
                        for (int ci = 0; ci < seg.Text.Length; ci++)
                        {
                            double charX = starts[ci];
                            double charW;
                            if (ci < seg.Text.Length - 1)
                            {
                                charW = Math.Max(0.1, starts[ci + 1] - starts[ci]);
                            }
                            else
                            {
                                // 最后一个字符宽度 = 段估算右边界 - 最后起点；如异常则回退平均宽度
                                charW = Math.Max(0.1, segEstimatedRight - charX);
                                if (charW > (segEstimatedRight - segStart) * 2)
                                {
                                    double avg = (segEstimatedRight - segStart) / Math.Max(1, seg.Text.Length);
                                    charW = avg;
                                }
                            }
                            chars.Add((seg.Text[ci], charX, charW, false, "delta"));
                            if (opt.EnableDebugWordLayout && logger != null)
                            {
                                logger.LogDebug("[PDF2OFD][Text][DeltaChar] Page {Page} Ch='{Ch}' X={X:F2} W={W:F2}", page, seg.Text[ci], charX, charW);
                            }
                        }
                    }
                    else if (seg.DeltaX != null && seg.DeltaX.Length > 0)
                    {
                        // 长度不匹配，降级平均
                        double segW = EstimateWidth(seg);
                        double perChar = segW / Math.Max(1, seg.Text.Length);
                        for (int ci = 0; ci < seg.Text.Length; ci++)
                        {
                            double cx = seg.X + perChar * ci;
                            chars.Add((seg.Text[ci], cx, perChar, false, "avg-fallback"));
                        }
                    }
                    else
                    {
                        double segW = EstimateWidth(seg);
                        double perChar = segW / Math.Max(1, seg.Text.Length);
                        for (int ci = 0; ci < seg.Text.Length; ci++)
                        {
                            double cx = seg.X + perChar * ci;
                            chars.Add((seg.Text[ci], cx, perChar, false, "avg"));
                        }
                    }
                }

                // 第一个段
                AppendSegmentChars(segs[0]);

                for (int si = 1; si < segs.Count; si++)
                {
                    var seg = segs[si];
                    double segW = EstimateWidth(seg);
                    double gap = seg.X - cursorRight;
                    double spaceRef = seg.SpaceAdvance ?? seg.AvgAdvance ?? ((seg.FontSize > 0 ? seg.FontSize : avgSize) * 0.55);
                    bool lineMostlyCjk = segs.Count(s => s.Text.Any(ch => ch >= '\u4E00' && ch <= '\u9FFF')) >= segs.Count * 0.5;
                    double triggerRatio = opt.GapSpaceTriggerRatio;
                    if (lineMostlyCjk)
                    {
                        triggerRatio = opt.CjkGapTriggerRatio;
                        // 中文行把参考 spaceRef 压缩，避免字宽导致阈值偏大
                        spaceRef = Math.Min(spaceRef, (seg.FontSize > 0 ? seg.FontSize : avgSize) * 0.7);
                    }
                    if (gap > spaceRef * triggerRatio)
                    {
                        double baseSpace = seg.SpaceAdvance ?? (seg.AvgAdvance ?? spaceRef);
                        if (baseSpace <= 0) baseSpace = spaceRef;
                        int spaceCount = (int)Math.Max(1, Math.Min(opt.MaxSyntheticSpacesPerGap, Math.Round(gap / baseSpace)));
                        double spaceWidth = gap / spaceCount;
                        double spaceX = cursorRight;
                        for (int sp = 0; sp < spaceCount; sp++)
                        {
                            chars.Add((' ', spaceX, spaceWidth, true, "gap"));
                            spaceX += spaceWidth;
                        }
                    }
                    AppendSegmentChars(seg);
                    cursorRight = Math.Max(cursorRight, seg.X + segW);
                }

                // 基于空格拆词
                int wordStart = -1;
                for (int i = 0; i < chars.Count; i++)
                {
                    var ch = chars[i];
                    bool isSpace = ch.ch == ' ';
                    if (isSpace)
                    {
                        if (wordStart >= 0)
                        {
                            EmitWord(chars, wordStart, i - 1);
                            wordStart = -1;
                        }
                        continue;
                    }
                    if (wordStart < 0) wordStart = i;
                }
                if (wordStart >= 0)
                {
                    EmitWord(chars, wordStart, chars.Count - 1);
                }

                void EmitWord(List<(char ch, double x, double w, bool isGap, string src)> c, int start, int end)
                {
                    if (end < start) return;
                    // 过滤掉全 gap 的情况（理论不会发生）
                    if (c.Skip(start).Take(end - start + 1).All(t => t.isGap)) return;
                    double minX = c[start].x;
                    double maxR = c[start].x + c[start].w;
                    var sb = new System.Text.StringBuilder();
                    for (int j = start; j <= end; j++)
                    {
                        var cc = c[j];
                        if (!cc.isGap) sb.Append(cc.ch);
                        maxR = Math.Max(maxR, cc.x + cc.w);
                    }
                    string word = sb.ToString();
                    if (string.IsNullOrWhiteSpace(word)) return;
                    // 继承首段 CTM（如果各段CTM不同，后续可考虑做平均或放弃）
                    double[]? ctm = segs[0].CTM;
                    float[]? wordDelta = null;
                    if (opt.EnableDeltaX)
                    {
                        // 构造 DeltaX：长度 = 字符数，首项0，元素含义=当前字符起点 - 前一个字符起点（典型 Step 模式）
                        var wordChars = c.Skip(start).Take(end - start + 1).Where(cc => !cc.isGap).ToList();
                        if (wordChars.Count > 0)
                        {
                            var deltas = new List<float>(wordChars.Count);
                            double prev = wordChars[0].x;
                            deltas.Add(0f);
                            for (int k = 1; k < wordChars.Count; k++)
                            {
                                double adv = wordChars[k].x - prev;
                                if (adv < 0) adv = 0; // 防御性
                                deltas.Add((float)adv);
                                prev = wordChars[k].x;
                            }
                            wordDelta = deltas.ToArray();
                        }
                    }

                    result.Add(new OfdText
                    {
                        Page = segs[0].Page,
                        Text = word,
                        X = (float)minX,
                        Y = segs.Min(s => s.Y),
                        Width = (float)(maxR - minX),
                        Height = (float)maxH,
                        FontFamily = lineFont,
                        FontSize = (float)avgSize,
                        CTM = ctm,
                        DeltaX = wordDelta
                    });

                    if (opt.EnableDebugWordLayout && logger != null)
                    {
                        logger.LogDebug("[PDF2OFD][Text][WordDbg] Page {Page} Word='{Word}' X={X:F2} W={W:F2} Chars={Chars} Src=[{Src}]", page, word, minX, (maxR - minX), end - start + 1,
                            string.Join(',', c.Skip(start).Take(end - start + 1).Select(t => t.src).Distinct()));
                    }
                }

                if (opt.EnableDebugWordLayout && logger != null)
                {
                    logger.LogDebug("[PDF2OFD][Text][WordDbg] Page {Page} 行完成 字符总数={Cnt} 词数={Words}", page, chars.Count, result.Count);
                }
            }
            logger?.LogDebug("[PDF2OFD][Text][AggregateWords] Page {Page} 原始片段={Raw} 行数={Lines} 词级块={Words}", page, raw.Count, lineBuckets.Count, result.Count);
            return result;
        }
    }
}
