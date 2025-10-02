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
using OfdrwNet.Converter.Refactor.Utils;

namespace OfdrwNet.Converter.Refactor
{
    /// <summary>
    /// 按基线与水平间距分组文字片段并调用 IOfdDocWriter.AddRawTextGlyphRun
    /// 目标：替换复杂冗余实现，使用明确的单位转换和最小断行规则。
    /// </summary>
    internal class TextExtractor : IPdfContentExtractor
    {
        public async Task ExtractAsync(PdfDocument pdfDoc, IOfdDocWriter ofd, ConvertHelper.PdfToOfdOptions options, ILogger? logger, System.Threading.CancellationToken token)
        {
            if (!options.ExtractText)
            {
                logger?.LogDebug("[PDF2OFD][Text] ExtractText=false 跳过文本提取");
                return;
            }

            int pages = pdfDoc.GetNumberOfPages();
            for (int p = 1; p <= pages; p++)
            {
                token.ThrowIfCancellationRequested();
                if (options.PageFilter != null && !options.PageFilter(p))
                    continue;

                var page = pdfDoc.GetPage(p);
                var pageHeightPt = page.GetPageSize().GetHeight();  // 获取页面高度用于坐标转换
                var strat = new SimpleGroupingStrategy(page.GetPageSize());
                var processor = new PdfCanvasProcessor(strat);
                processor.ProcessPageContent(page);
                strat.Flush();

                var writer = ofd;
                foreach (var group in strat.Groups)
                {
                    if (group.RenderInfos.Count == 0)
                        continue;
                    var first = group.RenderInfos[0];
                    var pdfCtm = first.GetGraphicsState().GetCtm();
                    double[] ctm = GeometryUtils.BuildOfdCtmFromPdf(pdfCtm);

                    // 如果 CTM 仅是单位从 pt -> mm 的等比例缩放（a≈d≈0.352778，b,c≈0），
                    // 则可以忽略它，避免与我们已转换到 mm 的 Boundary / FontSize 重复缩放导致视觉变小。
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
                                // 仅保留平移(如果有)；此处平移通常也已在坐标换算中体现，可直接移除整个 CTM。
                                ctm = Array.Empty<double>(); // 用空数组表示无需设置 CTM
                                logger?.LogDebug("[PDF2OFD][Text] 规范化 CTM: 去除纯 pt->mm 缩放，避免双重缩放");
                            }
                        }
                    }
                    catch
                    {

                        logger?.LogWarning("[PDF2OFD][Text] 获取 CTM 失败，保持原 CTM");

                    }

                    // 检查 CTM 中的缩放因子（a=水平缩放, d=垂直缩放）
                    double ctmScaleX = Math.Abs(pdfCtm.Get(iText.Kernel.Geom.Matrix.I11));
                    double ctmScaleY = Math.Abs(pdfCtm.Get(iText.Kernel.Geom.Matrix.I22));

                    double baselinePt = first.GetBaseline().GetStartPoint().Get(1);
                    double baselineYmm = GeometryUtils.PtToMm(baselinePt);

                    double minX = double.MaxValue, maxX = double.MinValue, minY = double.MaxValue, maxY = double.MinValue;
                    foreach (var ri in group.RenderInfos)
                    {
                        var r = ri.GetAscentLine().GetBoundingRectangle();
                        minX = Math.Min(minX, r.GetLeft());
                        maxX = Math.Max(maxX, r.GetRight());
                        minY = Math.Min(minY, r.GetBottom());
                        maxY = Math.Max(maxY, r.GetTop());
                    }

                    // 参照 Program.cs: BoundaryY = pageHeight - maxY (OFD 坐标系 Y 轴向下)
                    double originXmm = GeometryUtils.PtToMm(minX);
                    double originYmm = GeometryUtils.PtToMm(pageHeightPt - maxY);  // Y 轴翻转
                    double widthMm = GeometryUtils.PtToMm(Math.Max(0.1, maxX - minX));
                    double heightMm = GeometryUtils.PtToMm(Math.Max(0.1, maxY - minY));

                    double[]? charStarts = null;
                    double[]? charAdvances = null;
                    if (options.PerGlyphPositioning)
                    {
                        var starts = new List<double>();
                        var advs = new List<double>();
                        foreach (var ri in group.RenderInfos)
                        {
                            var s = ri.GetBaseline().GetStartPoint().Get(0);
                            starts.Add(GeometryUtils.PtToMm(s));
                            advs.Add(GeometryUtils.PtToMm(ri.GetAscentLine().GetBoundingRectangle().GetWidth()));
                        }
                        charStarts = starts.ToArray();
                        charAdvances = advs.ToArray();
                    }

                    string text = string.Concat(group.RenderInfos.Select(r => r.GetText()));

                    // 获取 PDF 字体大小并转换为 mm
                    double pdfFontSizePt = first.GetFontSize();
                    double fontSizeMm = GeometryUtils.PtToMm(pdfFontSizePt);

                    // 🔧 关键修复：如果 CTM 包含非标准缩放（不是 1 或 -1），需要应用到字体大小
                    // PDF 规范：字体大小受 CTM 垂直缩放影响
                    if (Math.Abs(ctmScaleY) > 0.001 && Math.Abs(Math.Abs(ctmScaleY) - 1.0) > 0.001)
                    {
                        // CTM 有非标准垂直缩放，应用到字体大小
                        fontSizeMm *= ctmScaleY;
                        logger?.LogDebug("[PDF2OFD][Text] CTM Y缩放 {Scale}, 调整字体大小: {Original}pt -> {Adjusted}mm",
                            ctmScaleY, pdfFontSizePt, fontSizeMm);
                    }

                    // 改进字体名称提取：尝试获取 PostScript 名称或字体家族名
                    string fontName = "SimSun"; // 默认宋体
                    try
                    {
                        var pdfFont = first.GetFont();
                        if (pdfFont != null)
                        {
                            var fontProgram = pdfFont.GetFontProgram();
                            if (fontProgram != null)
                            {
                                var fontNames = fontProgram.GetFontNames();
                                // 优先使用 PostScript 名称，其次字体家族名
                                fontName = fontNames.GetFontName()
                                    ?? fontNames.GetFamilyName()?[0]?[3]  // Windows平台英文名
                                    ?? fontNames.GetFamilyName()?[0]?[1]  // Mac平台英文名
                                    ?? "SimSun";

                                // 清理字体名称中的子集前缀 (如 "ABCDEF+SimSun" -> "SimSun")
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

                    double[]? deltaX = null;
                    if (group.DeltaXs != null && group.DeltaXs.Count > 0)
                        deltaX = group.DeltaXs.Select(v => GeometryUtils.PtToMm(v)).ToArray();

                    // 传递翻转后的 Y 坐标和 pageHeight，让 PageContentWriter 计算 baseline offset
                    double pageHeightMm = GeometryUtils.PtToMm(pageHeightPt);
                    // 如果 ctm 被规范化为空数组，则传 null，PageContentWriter 将不会设置 CTM
                    writer.AddRawTextGlyphRun(fontName, fontSizeMm, originXmm, originYmm, widthMm, heightMm, text, deltaX, null, null, p,
                        (ctm.Length == 0 ? null : ctm), baselineY: pageHeightMm - baselineYmm, charStarts: charStarts, charAdvances: charAdvances);
                }
            }

            await Task.CompletedTask;
        }

        private class SimpleGroupingStrategy : IEventListener
        {
            private readonly Rectangle _pageSize;
            private readonly List<Group> _groups = new();
            private readonly List<TextRenderInfo> _current = new();
            private readonly List<float> _deltas = new();

            public SimpleGroupingStrategy(Rectangle pageSize)
            {
                _pageSize = pageSize;
            }

            public IReadOnlyList<Group> Groups => _groups;

            public void EventOccurred(IEventData data, EventType type)
            {
                if (type != EventType.RENDER_TEXT)
                    return;
                var ri = (TextRenderInfo)data;
                try
                {
                    // Ensure graphics state is preserved for later access (ctm/font/etc.)
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

                    if (Math.Abs(curY - lastY) > 6.0f || curX < lastX - 6.0f)
                    {
                        FlushCurrent();
                    }
                    else
                    {
                        _deltas.Add(curX - lastX);
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
                public List<float>? DeltaXs
                {
                    get; set;
                }
            }
        }
    }
}
