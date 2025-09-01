using System.Collections.Generic;
using System.Linq;
using OfdrwNet.Core;
using OfdrwNet.Core.BasicStructure.Doc;
using OfdrwNet.Core.BasicStructure.PageTree;
using OfdrwNet.Core.BasicType;
using OfdrwNet.Layout.Element;

namespace OfdrwNet.Layout.Engine;

/// <summary>
/// 虚拟页面解析引擎
///
/// 解析虚拟页面转换为OFD页面，放入文档容器中
///
/// 对应 Java 版本的 org.ofdrw.layout.engine.VPageParseEngine
/// </summary>
public class VPageParseEngine
{
    private int _maxUnitId;
    private Pages? _pages;
    private PageLayout _pageLayout;
    private ResManager _resManager;
    private static readonly Dictionary<string, IProcessor> RegisteredProcessors = new();
    private IVPageHandler? _beforePageParseHandler;

    static VPageParseEngine()
    {
        Register("Img", new ImgProcessor());
        Register("Paragraph", new ParagraphProcessor());
        Register("Canvas", new CanvasProcessor());
        Register("AreaHolderBlock", new AreaHolderBlockProcessor());
    }

    public static void Register(string elementType, IProcessor processor) => RegisteredProcessors[elementType] = processor;

    public VPageParseEngine(PageLayout pageLayout, Document document, ResManager resManager, int maxUnitId = 1)
    {
        _pageLayout = pageLayout ?? throw new ArgumentNullException(nameof(pageLayout));
        _resManager = resManager ?? throw new ArgumentNullException(nameof(resManager));
        _maxUnitId = maxUnitId;
        _pages = document.GetPages() ?? new Pages();
        document.SetPages(_pages);
    }

    public void SetBeforePageParseHandler(IVPageHandler handler) => _beforePageParseHandler = handler;

    public void Process(List<VirtualPage> vPageList)
    {
        if (vPageList == null || vPageList.Count == 0) return;
        var queue = new Queue<VirtualPage>(vPageList);
        while (queue.Count > 0)
        {
            var virtualPage = queue.Dequeue();
            if (virtualPage == null) continue;
            _beforePageParseHandler?.Handle(virtualPage);
            var pageDir = CreateNewPage();
            var pageLoc = $"Pages/Page_{pageDir.PageId}/Content.xml"; // 仅用于调试
            if (virtualPage.PageNum == null) virtualPage.PageNum = _pages!.GetSize() + 1;
            ConvertPageContent(pageLoc, virtualPage, pageDir);
        }
    }

    private PageDir CreateNewPage()
    {
        var pageId = new StId(++_maxUnitId);
        var pageDir = new PageDir(pageId);
        var pageTreeNode = new Page(pageId, StLoc.Parse($"Pages/Page_{pageId}/Content.xml"));
        _pages!.AddPage(pageTreeNode);
        return pageDir;
    }

    private void ConvertPageContent(string pageLoc, VirtualPage vPage, PageDir pageDir)
    {
        var page = new Core.BasicStructure.PageObj.Page();
        var vPageStyle = vPage.Style;
        if (!_pageLayout.Equals(vPageStyle)) page.SetArea(vPageStyle.GetPageArea());
        pageDir.SetContent(page);
        if (vPage.Content.Count == 0) return;
        var content = new Core.BasicStructure.PageObj.Content();
        var layer = new Core.BasicStructure.PageObj.Layer.CtLayer();
        layer.SetType((Core.BasicStructure.PageObj.Layer.LayerType)0);
        foreach (var element in vPage.Content)
        {
            ProcessElement(element, layer);
        }
        content.AddLayer(layer);
        page.SetContent(content);
    }

    private void ProcessElement(IElement element, Core.BasicStructure.PageObj.Layer.CtLayer layer)
    {
        if (element == null) return;
        var typeName = element.GetType().Name;
        if (RegisteredProcessors.TryGetValue(typeName, out var processor))
        {
            processor.Process(element, layer, _resManager);
        }
    }

    public int GetMaxUnitId() => _maxUnitId;
}

public class PageDir
{
    public StId PageId { get; }
    public Core.BasicStructure.PageObj.Page? Content { get; private set; }
    public PageDir(StId pageId) => PageId = pageId;
    public void SetContent(Core.BasicStructure.PageObj.Page content) => Content = content;
}

/// <summary>
/// 原始处理器接口（Process 签名）恢复，便于调试符号匹配。
/// </summary>
public interface IProcessor
{
    void Process(IElement element, Core.BasicStructure.PageObj.Layer.CtLayer layer, ResManager resManager);
}

public interface IVPageHandler { void Handle(VirtualPage page); }

public class DivProcessor : IProcessor { public void Process(IElement element, Core.BasicStructure.PageObj.Layer.CtLayer layer, ResManager resManager) { } }
public class ImgProcessor : IProcessor { public void Process(IElement element, Core.BasicStructure.PageObj.Layer.CtLayer layer, ResManager resManager) { } }
public class CanvasProcessor : IProcessor { public void Process(IElement element, Core.BasicStructure.PageObj.Layer.CtLayer layer, ResManager resManager) { } }
public class AreaHolderBlockProcessor : IProcessor { public void Process(IElement element, Core.BasicStructure.PageObj.Layer.CtLayer layer, ResManager resManager) { } }

/// <summary>
/// 段落处理器（可断点命中）
/// </summary>
public class ParagraphProcessor : IProcessor
{
    private const double SingleCharLineRatioThreshold = 0.7;

    public void Process(IElement element, Core.BasicStructure.PageObj.Layer.CtLayer layer, ResManager resManager)
    {
        System.Diagnostics.Debug.WriteLine("[PP] ENTER"); // 断点放这里
        if (element is not Paragraph p) { System.Diagnostics.Debug.WriteLine("[PP] Not Paragraph"); return; }
        try
        {
            double fontSize = p.DefaultFontSize ?? 4.0;
            double lineHeight = fontSize * (p.LineSpace > 0 ? p.LineSpace : 1.4);
            double width = p.Width ?? (210 - 31.7 * 2); // A4 内容宽兜底
            if (width < fontSize * 2) width = fontSize * 40 * 0.6;

            var raw = string.Join(string.Empty, p.Contents.Select(s => s.Text)).Replace('\r', '\n');
            if (string.IsNullOrWhiteSpace(raw)) return;
            var logical = raw.Split('\n');

            double charWidth = EstimateCharWidth(raw, fontSize);
            if (charWidth <= 0) charWidth = fontSize * 0.6;

            var lines = new List<string>();
            foreach (var ln in logical) Rewrap(ln, width, charWidth, lines);
            if (lines.Count == 0) return;
            int single = lines.Count(l => l.Length == 1);
            if (lines.Count > 3 && (double)single / lines.Count >= SingleCharLineRatioThreshold)
            {
                // 合并降级
                var merged = raw.Replace("\n", string.Empty);
                lines.Clear();
                Rewrap(merged, width, charWidth, lines);
            }

            // 再次检测仍单字 → 逐字符定位
            if (lines.Count > 0 && lines.Count(l => l.Length == 1) >= lines.Count * 0.7)
            {
                RenderCharByChar(p, layer, fontSize, lineHeight, width, raw.Replace("\n", string.Empty), charWidth);
                return;
            }

            double baseX = p.X ?? 0;
            double baseY = p.Y ?? 0;
            var textObject = new Core.Text.TextObject(DateTime.UtcNow.Ticks % 1_000_000);
            textObject.SetSize(fontSize).SetFont(new StRefId(1));
            textObject.SetBoundary(new StBox(baseX, baseY, width, lines.Count * lineHeight));

            double y = baseY + fontSize;
            foreach (var line in lines)
            {
                var tc = new Core.Text.TextCode();
                tc.SetContent(line);
                tc.SetX(baseX);
                tc.SetY(y);
                if (line.Length > 1)
                {
                    var dx = new double[line.Length - 1];
                    for (int i = 0; i < dx.Length; i++) dx[i] = charWidth;
                    tc.SetDeltaX(dx);
                }
                textObject.AddTextCode(tc);
                y += lineHeight;
            }
            layer.AddPageObject(textObject);
            System.Diagnostics.Debug.WriteLine($"[PP] DONE lines={lines.Count} rawLen={raw.Length} width={width}");
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"[PP] ERR {ex.Message}");
        }
    }

    private static double EstimateCharWidth(string text, double fontSize)
    {
        int sample = Math.Min(16, text.Length);
        int cjk = 0; for (int i = 0; i < sample; i++) if (IsCJK(text[i])) cjk++;
        double ratio = (double)cjk / Math.Max(1, sample);
        return fontSize * (ratio * 0.6 + (1 - ratio) * 0.5);
    }

    private static void Rewrap(string line, double maxWidth, double charWidth, List<string> outLines)
    {
        if (string.IsNullOrEmpty(line)) { outLines.Add(string.Empty); return; }
        if (line.Length * charWidth <= maxWidth) { outLines.Add(line); return; }
        var sb = new System.Text.StringBuilder(); double w = 0;
        foreach (var ch in line)
        {
            sb.Append(ch); w += charWidth;
            if (w + charWidth > maxWidth || IsSentenceEnding(ch)) { outLines.Add(sb.ToString()); sb.Clear(); w = 0; }
        }
        if (sb.Length > 0) outLines.Add(sb.ToString());
        // 如果全是单字且原行较长，回退整行
        if (line.Length > 4)
        {
            // 检测当前此行拆分结果是否全为单字符
            bool allSingle = true;
            int recentLen = 0;
            for (int i = outLines.Count - 1; i >= 0; i--)
            {
                var seg = outLines[i];
                recentLen += seg.Length;
                if (seg.Length != 1) { allSingle = false; break; }
                if (recentLen >= line.Length) break;
            }
            if (allSingle && recentLen >= line.Length)
            {
                outLines.Clear();
                outLines.Add(line);
            }
        }
    }

    private static void RenderCharByChar(Paragraph p, Core.BasicStructure.PageObj.Layer.CtLayer layer, double fontSize, double lineHeight, double width, string text, double charWidth)
    {
        double baseX = p.X ?? 0, baseY = p.Y ?? 0;
        var textObject = new Core.Text.TextObject((DateTime.UtcNow.Ticks % 1_000_000) + 500000);
        textObject.SetSize(fontSize).SetFont(new StRefId(1));
        int cols = (int)Math.Max(1, Math.Floor(width / charWidth));
        int line = 0, col = 0;
        foreach (var ch in text)
        {
            var tc = new Core.Text.TextCode();
            tc.SetContent(ch.ToString());
            tc.SetX(baseX + col * charWidth);
            tc.SetY(baseY + fontSize + line * lineHeight);
            textObject.AddTextCode(tc);
            col++; if (col >= cols) { col = 0; line++; }
        }
        textObject.SetBoundary(new StBox(baseX, baseY, width, (line + 1) * lineHeight));
        layer.AddPageObject(textObject);
        System.Diagnostics.Debug.WriteLine($"[PP] CharByChar cols={cols} lines={line + 1}");
    }

    private static bool IsSentenceEnding(char ch) => "。！？!?;；：:,.，、)）]】>》\"'".IndexOf(ch) >= 0;
    private static bool IsCJK(char ch) => (ch >= 0x4E00 && ch <= 0x9FFF) || (ch >= 0x3400 && ch <= 0x4DBF);
}