using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using System.IO.Compression;
using System.Text;
using System.Linq;
using OfdrwNet.Extensions;
using OfdrwNet.Core.BasicStructure.Ofd.DocInfo;
using Microsoft.Extensions.Logging; // 新增
using OfdrwNet.Abstractions; // 新增

namespace OfdrwNet;

/// <summary>
/// OFD文档主要API类
/// </summary>
public class OfdWriter : IDisposable, IOfdDocWriter
{
    #region 字段
    private readonly string? _outPath;
    private readonly Stream? _outStream;
    private int _maxUnitID;
    private PageLayout _pageLayout = PageLayout.A4();
    private bool _disposed = false;
    private readonly List<object> _streamQueue = new();
    private readonly List<VirtualPage> _virtualPageList = new();
    private Dictionary<string, int> _fontMap = new();
    private string? _publicResRelativePath; // 更新：PublicRes.xml
    private string? _documentResRelativePath; // 新增 DocumentRes.xml
    private readonly Dictionary<string, string> _externalFontFiles = new(); // FontName -> temp file
    private readonly ILogger? _logger; // 新增：可选日志器

    private class RawGlyphRun
    {
        public string FontName { get; set; } = "SimSun";
        public double FontSizeMm { get; set; }
        public double X { get; set; }
        public double Y { get; set; }
        public string Text { get; set; } = string.Empty;
        public double[]? DeltaX { get; set; }
        public double[]? DeltaY { get; set; }
        public int Page { get; set; } = 1;
    }
    private class RawImage
    {
        public string Format { get; set; } = "PNG"; // PNG/JPG/GIF/BMP/TIFF/WEBP
        public double X { get; set; }
        public double Y { get; set; } // 顶部Y
        public double Width { get; set; }
        public double Height { get; set; }
        public byte[] Data { get; set; } = Array.Empty<byte>();
        public int Page { get; set; } = 1;
        public int ResourceID { get; set; }
        public string Hash { get; set; } = string.Empty; // SHA256
        public bool IsFirstResource { get; set; } // 是否资源首实例
    }
    private readonly List<RawImage> _rawImages = new();
    private readonly Dictionary<string, RawImage> _imageHashFirst = new(); // hash->首图
    private int _dedupImageCount = 0;
    public int DedupImageCount => _dedupImageCount;
    private List<(int PageNumber, List<object> Items)>? _pageGroups; // 预计算分页
    #endregion

    #region 构造
    public OfdWriter(string outPath, ILogger? logger = null)
    {
        if (string.IsNullOrEmpty(outPath)) throw new ArgumentException("OFD文件存储路径不能为空", nameof(outPath));
        var directory = Path.GetDirectoryName(Path.GetFullPath(outPath));
        if (!string.IsNullOrEmpty(directory) && !Directory.Exists(directory)) throw new ArgumentException($"OFD文件存储路径的上级目录不存在: {directory}");
        _outPath = outPath;
        _logger = logger;
        InitializeContainer();
    }
    public OfdWriter(Stream outStream, ILogger? logger = null)
    {
        _outStream = outStream ?? throw new ArgumentNullException(nameof(outStream));
        _logger = logger;
        InitializeContainer();
    }
    private void InitializeContainer() => _maxUnitID = 0;
    #endregion

    #region 属性
    public PageLayout PageLayout => _pageLayout.Clone();
    public int MaxUnitID => _maxUnitID;
    public ILogger? Logger => _logger; // 接口要求
    #endregion

    #region 添加元素
    public OfdWriter SetDefaultPageLayout(PageLayout pageLayout)
    { if (pageLayout != null) _pageLayout = pageLayout; return this; }

    public OfdWriter Add(OfdrwNet.Layout.Element.Div item)
    { if (_streamQueue.Contains(item)) throw new ArgumentException("元素已经存在"); _streamQueue.Add(item); return this; }

    public OfdWriter Add(OfdrwNet.Layout.Element.Paragraph paragraph)
    { if (_streamQueue.Contains(paragraph)) throw new ArgumentException("元素已经存在"); _streamQueue.Add(paragraph); return this; }

    public OfdWriter AddVirtualPage(VirtualPage virtualPage)
    { _virtualPageList.Add(virtualPage); return this; }

    public OfdWriter AddExternalEmbeddedFont(string fontName, string fontFilePath)
    {
        if (string.IsNullOrWhiteSpace(fontName)) throw new ArgumentNullException(nameof(fontName));
        if (string.IsNullOrWhiteSpace(fontFilePath) || !File.Exists(fontFilePath)) throw new FileNotFoundException("字体文件不存在", fontFilePath);
        _externalFontFiles[fontName] = fontFilePath;
        return this; // 确保链式
    }
    IOfdDocWriter IOfdDocWriter.AddExternalEmbeddedFont(string fontName, string fontFilePath) => AddExternalEmbeddedFont(fontName, fontFilePath);

    public OfdWriter AddRawTextGlyphRun(string fontName, double fontSizeMm, double originX, double originY, string text, double[]? deltaX = null, double[]? deltaY = null, int page = 1)
    {
        _streamQueue.Add(new RawGlyphRun
        {
            FontName = string.IsNullOrWhiteSpace(fontName) ? "SimSun" : fontName,
            FontSizeMm = fontSizeMm,
            X = originX,
            Y = originY,
            Text = text ?? string.Empty,
            DeltaX = deltaX,
            DeltaY = deltaY,
            Page = page < 1 ? 1 : page
        });
        return this; // 链式
    }
    IOfdDocWriter IOfdDocWriter.AddRawTextGlyphRun(string fontName, double fontSizeMm, double originX, double originY, string text, double[]? deltaX, double[]? deltaY, int page) => AddRawTextGlyphRun(fontName, fontSizeMm, originX, originY, text, deltaX, deltaY, page);

    public OfdWriter AddRawImage(string format, double x, double y, double width, double height, byte[] data, int page = 1)
    {
        if (data == null || data.Length == 0) { _logger?.LogDebug("[OFDDoc][Image] Skip empty image data Page={Page}", page); return this; }
        string hash;
        try { using var sha = System.Security.Cryptography.SHA256.Create(); hash = Convert.ToHexString(sha.ComputeHash(data)); }
        catch { hash = Guid.NewGuid().ToString("N"); }
        bool first = !_imageHashFirst.ContainsKey(hash);
        if (!first) _dedupImageCount++;
        var img = new RawImage
        {
            Format = string.IsNullOrWhiteSpace(format) ? "PNG" : format.ToUpperInvariant(),
            X = x,
            Y = y < 0 ? 0 : y,
            Width = width <= 0 ? 1 : width,
            Height = height <= 0 ? 1 : height,
            Data = first ? data : Array.Empty<byte>(),
            Page = page < 1 ? 1 : page,
            Hash = hash,
            IsFirstResource = first
        };
        _rawImages.Add(img);
        if (first) { _imageHashFirst[hash] = img; _logger?.LogDebug("[OFDDoc][Image] Page={Page} NewImage Hash={Hash} {W}x{H}mm Format={Fmt}", img.Page, hash[..Math.Min(12,hash.Length)], img.Width.ToString("0.##"), img.Height.ToString("0.##"), img.Format); }
        else { _logger?.LogDebug("[OFDDoc][Image] Page={Page} DupImage Hash={Hash}", img.Page, hash[..Math.Min(12,hash.Length)]); }
        return this;
    }
    IOfdDocWriter IOfdDocWriter.AddRawImage(string format, double x, double y, double width, double height, byte[] data, int page) => AddRawImage(format, x, y, width, height, data, page);
    public OfdWriter AddImagePublic(string format, double x, double y, double width, double height, byte[] data, int page = 1) => AddRawImage(format, x, y, width, height, data, page);
    #endregion

    #region ID
    public int GetNextID() => Interlocked.Increment(ref _maxUnitID);
    #endregion

    #region 生成/关闭
    public async Task CloseAsync()
    {
        if (_disposed) return;
        try
        {
            if (_streamQueue.Count > 0) await ProcessStreamLayoutAsync();
            if (_virtualPageList.Count > 0) await ProcessVirtualPagesAsync();
            if (_streamQueue.Count == 0 && _virtualPageList.Count == 0) _virtualPageList.Add(new VirtualPage());
            await GenerateDocumentAsync();
        }
        finally { _disposed = true; }
    }
    private Task ProcessStreamLayoutAsync() => Task.CompletedTask; // 占位
    private Task ProcessVirtualPagesAsync() => Task.CompletedTask; // 占位

    private async Task GenerateDocumentAsync()
    {
        if (!string.IsNullOrEmpty(_outPath))
        { await GenerateOfdContentAsync(_outPath); }
        else if (_outStream != null)
        { await GenerateOfdContentAsync(_outStream); }
        else throw new InvalidOperationException("未设置输出");
    }

    private async Task GenerateOfdContentAsync(string outputPath)
    {
        var tempDir = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());
        Directory.CreateDirectory(tempDir);
        try
        {
            await CreateOfdStructureWithNewClassesAsync(tempDir);
            ZipFile.CreateFromDirectory(tempDir, outputPath);
        }
        finally { if (Directory.Exists(tempDir)) Directory.Delete(tempDir, true); }
    }

    private async Task GenerateOfdContentAsync(Stream outputStream)
    {
        var tempDir = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());
        Directory.CreateDirectory(tempDir);
        try
        {
            await CreateOfdStructureAsync(tempDir);
            using var archive = new ZipArchive(outputStream, ZipArchiveMode.Create, true);
            foreach (var file in Directory.GetFiles(tempDir, "*", SearchOption.AllDirectories))
            {
                var entryName = Path.GetRelativePath(tempDir, file).Replace('\\', '/');
                var entry = archive.CreateEntry(entryName);
                using var entryStream = entry.Open();
                using var fileStream = File.OpenRead(file);
                await fileStream.CopyToAsync(entryStream);
            }
        }
        finally { if (Directory.Exists(tempDir)) Directory.Delete(tempDir, true); }
    }

    // 旧结构创建（流输出路径）
    private async Task CreateOfdStructureAsync(string baseDir)
    {
        _pageGroups = ComputePageGroups();
        _logger?.LogDebug("[OFD][CreateStructure] Pages={PageCount}", _pageGroups?.Count);
        var ofdXml = "<?xml version=\"1.0\" encoding=\"UTF-8\"?>\n" +
                     "<ofd:OFD xmlns:ofd=\"http://www.ofdspec.org/2016\" DocType=\"OFD\" Version=\"1.2\">\n" +
                     "    <ofd:DocBody>\n" +
                     "        <ofd:DocInfo DocID=\"1\">\n" +
                     "            <ofd:DocRoot>Doc/Document.xml</ofd:DocRoot>\n" +
                     "        </ofd:DocInfo>\n" +
                     "    </ofd:DocBody>\n" +
                     "</ofd:OFD>";
        await File.WriteAllTextAsync(Path.Combine(baseDir, "OFD.xml"), ofdXml, Encoding.UTF8);
        var docDir = Path.Combine(baseDir, "Doc"); Directory.CreateDirectory(docDir);
        await GenerateResourcesAsync(docDir);
        var documentXml = await GenerateDocumentXmlAsync();
        await File.WriteAllTextAsync(Path.Combine(docDir, "Document.xml"), documentXml, Encoding.UTF8);
        var pagesDir = Path.Combine(docDir, "Pages"); Directory.CreateDirectory(pagesDir);
        await GeneratePageFilesAsync(pagesDir);
    }

    // 使用新基础结构类（文件输出）
    private async Task CreateOfdStructureWithNewClassesAsync(string baseDir)
    {
        _pageGroups = ComputePageGroups();
        var docInfo = new CtDocInfo();
        docInfo.RandomDocID().SetTitle("由OfdrwNet生成的OFD文档").SetCreator("OfdrwNet").SetCreatorVersion("1.0").SetCreationDate(DateTime.Now).SetModDate(DateTime.Now);
        var docBody = new OfdrwNet.Core.BasicStructure.Ofd.DocBody();
        docBody.SetDocInfo(docInfo).SetDocRoot(new OfdrwNet.Core.BasicType.StLoc("Doc/Document.xml"));
        var ofd = new OfdrwNet.Core.BasicStructure.Ofd.OFD(); ofd.AddDocBody(docBody);
        await File.WriteAllTextAsync(Path.Combine(baseDir, "OFD.xml"), ofd.ToXml(), Encoding.UTF8);
        var docDir = Path.Combine(baseDir, "Doc"); Directory.CreateDirectory(docDir);
        await GenerateResourcesAsync(docDir);
        var documentXml = await GenerateDocumentXmlAsync();
        await File.WriteAllTextAsync(Path.Combine(docDir, "Document.xml"), documentXml, Encoding.UTF8);
        var pagesDir = Path.Combine(docDir, "Pages"); Directory.CreateDirectory(pagesDir);
        await GeneratePageFilesAsync(pagesDir);
    }

    private async Task<string> GenerateDocumentXmlAsync()
    {
        if (_pageGroups == null) _pageGroups = ComputePageGroups();
        var pagesXml = new StringBuilder();
        foreach (var pg in _pageGroups.OrderBy(p => p.PageNumber)) pagesXml.AppendLine($"        <ofd:Page ID=\"{pg.PageNumber}\" BaseLoc=\"Pages/Page_{pg.PageNumber}.xml\"/>");
        var sb = new StringBuilder();
        sb.AppendLine("<?xml version=\"1.0\" encoding=\"UTF-8\"?>");
        sb.AppendLine("<ofd:Document xmlns:ofd=\"http://www.ofdspec.org/2016\">");
        sb.AppendLine("    <ofd:CommonData>");
        sb.AppendLine("        <ofd:PageArea>");
        sb.AppendLine($"            <ofd:PhysicalBox>0 0 {_pageLayout.Width} {_pageLayout.Height}</ofd:PhysicalBox>");
        sb.AppendLine("        </ofd:PageArea>");
        if (!string.IsNullOrEmpty(_publicResRelativePath)) sb.AppendLine($"        <ofd:PublicRes>{_publicResRelativePath}</ofd:PublicRes>");
        if (!string.IsNullOrEmpty(_documentResRelativePath)) sb.AppendLine($"        <ofd:DocumentRes>{_documentResRelativePath}</ofd:DocumentRes>");
        sb.AppendLine("    </ofd:CommonData>");
        sb.AppendLine("    <ofd:Pages>");
        sb.Append(pagesXml.ToString());
        sb.AppendLine("    </ofd:Pages>");
        sb.AppendLine("</ofd:Document>");
        await Task.CompletedTask; return sb.ToString();
    }

    private async Task GeneratePageFilesAsync(string pagesDir)
    { if (_fontMap.Count == 0) BuildFontMap(); if (_pageGroups == null) _pageGroups = ComputePageGroups(); foreach (var pg in _pageGroups) await CreatePageFileAsync(pagesDir, pg.PageNumber, pg.Items); }

    private async Task CreatePageFileAsync(string pagesDir, int pageNumber, List<object> items)
    {
        var contentXml = new StringBuilder();
        var imagesOnPageList = _rawImages.Where(i => i.Page == pageNumber).ToList();
        if (imagesOnPageList.Count > 0)
        { _logger?.LogDebug("[OFDDoc][PageWrite] Page={Page} ImageCount={Count} RIDs={RIDs}", pageNumber, imagesOnPageList.Count, string.Join(',', imagesOnPageList.Select(i=>i.ResourceID))); }
        else
        { _logger?.LogDebug("[OFDDoc][PageWrite] Page={Page} ImageCount=0", pageNumber); }
        // 先输出图片
        foreach (var img in imagesOnPageList)
        {
            if (img.ResourceID == 0) _logger?.LogWarning("[OFDDoc][ImageDiag] Page={Page} Image Hash={Hash} ResourceID=0 (未分配)", pageNumber, img.Hash[..Math.Min(12,img.Hash.Length)]);
            string boundary = $"{img.X:0.###} {img.Y:0.###} {img.Width:0.###} {img.Height:0.###}";
            contentXml.AppendLine($"        <ofd:ImageObject ID=\"{GetNextID()}\" ResourceID=\"{img.ResourceID}\" Boundary=\"{boundary}\" CTM=\"1 0 0 1 0 0\"/>");
        }
        double currentY = _pageLayout.Margins.Top;
        foreach (var item in items)
        {
            if (item is RawGlyphRun run)
            {
                var fontName = run.FontName; if (!_fontMap.TryGetValue(fontName, out var fontId)) fontId = 1;
                var fontSize = run.FontSizeMm; var textContent = System.Security.SecurityElement.Escape(run.Text ?? "");
                double ascentMm = 0, descentMm = 0; if (run.DeltaY != null && run.DeltaY.Length >= 2) { ascentMm = run.DeltaY[0]; descentMm = run.DeltaY[1]; }
                if (ascentMm <= 0) ascentMm = fontSize * 0.88; if (descentMm <= 0) descentMm = fontSize * 0.12;
                // A方案：精简边界，不再加入额外 padding，Boundary 高度 = ascent+descent，顶部 = run.Y
                double topY = run.Y; if (topY < 0) topY = 0; // clamp
                double height = ascentMm + descentMm; if (height <= 0) height = fontSize; // 兜底
                double width;
                if (run.DeltaX != null && run.DeltaX.Length > 0)
                {
                    width = run.DeltaX.Sum() + fontSize * 0.5; // 末尾补一个字宽的 0.5 估计
                }
                else
                {
                    bool hasCjk = run.Text.Any(ch => ch >= '\u4E00' && ch <= '\u9FFF');
                    double perChar = hasCjk ? fontSize : fontSize * 0.6;
                    width = perChar * run.Text.Length;
                }
                if (width < fontSize * 0.4) width = fontSize * 0.4; // 最小宽度兜底
                string boundary = $"{run.X:0.###} {topY:0.###} {width:0.###} {height:0.###}";
                double baselineLocalY = ascentMm; // TextCode 内部Y=ascent，表示基线位置
                string textCode;
                if (run.DeltaX != null && run.DeltaX.Length > 0)
                {
                    var dxStr = string.Join(" ", run.DeltaX.Select(v => v.ToString("0.###")));
                    textCode = $"<ofd:TextCode X=\"0\" Y=\"{baselineLocalY:0.##}\" DeltaX=\"{dxStr}\">{textContent}</ofd:TextCode>";
                }
                else
                {
                    textCode = $"<ofd:TextCode X=\"0\" Y=\"{baselineLocalY:0.##}\">{textContent}</ofd:TextCode>";
                }
                bool embedded = _externalFontFiles.ContainsKey(fontName);
                _logger?.LogDebug("[PDF2OFD][GlyphRunSimplified] Page={Page} Font={Font} Embedded={Embedded} FontId={FontId} Size={Size} TextLen={TextLen} Width={Width} Ascent={Ascent} Descent={Descent} TopY={TopY} H={H}", pageNumber, fontName, embedded, fontId, fontSize, run.Text.Length, width, ascentMm, descentMm, topY, height);
                contentXml.AppendLine($"        <ofd:TextObject ID=\"{GetNextID()}\" Font=\"{fontId}\" Size=\"{fontSize:0.##}\" Boundary=\"{boundary}\" CTM=\"1 0 0 1 0 0\">{textCode}</ofd:TextObject>");
                continue;
            }
            if (item is OfdrwNet.Layout.Element.Paragraph p)
            {
                var textContent = System.Security.SecurityElement.Escape(p.GetText() ?? ""); var fontName = p.GetFontName() ?? "SimSun"; if (!_fontMap.TryGetValue(fontName, out var fontId)) fontId = 1; double fontSize = p.GetFontSize(); double lineHeight = p.GetLineHeight(); double estimatedHeight = fontSize * lineHeight; double estimatedWidth = textContent.Length * fontSize * 0.6; double x = _pageLayout.Margins.Left; double y = currentY; string boundary = $"{x:0.###} {y:0.###} {estimatedWidth:0.###} {estimatedHeight:0.###}"; contentXml.AppendLine($"        <ofd:TextObject ID=\"{GetNextID()}\" Font=\"{fontId}\" Size=\"{fontSize:0.##}\" Boundary=\"{boundary}\" CTM=\"1 0 0 1 0 0\"><ofd:TextCode X=\"0\" Y=\"{fontSize:0.##}\">{textContent}</ofd:TextCode></ofd:TextObject>"); currentY += estimatedHeight + 2;
            }
        }
        var pageXml = new StringBuilder(); pageXml.AppendLine("<?xml version=\"1.0\" encoding=\"UTF-8\"?>"); pageXml.AppendLine("<ofd:Page xmlns:ofd=\"http://www.ofdspec.org/2016\">"); pageXml.AppendLine("    <ofd:Area>"); pageXml.AppendLine($"        <ofd:PhysicalBox>0 0 {_pageLayout.Width} {_pageLayout.Height}</ofd:PhysicalBox>"); pageXml.AppendLine("    </ofd:Area>"); pageXml.AppendLine("    <ofd:Content>"); pageXml.AppendLine("        <ofd:Layer ID=\"Layer1\">"); pageXml.Append(contentXml.ToString()); pageXml.AppendLine("        </ofd:Layer>"); pageXml.AppendLine("    </ofd:Content>"); pageXml.AppendLine("</ofd:Page>");
        await File.WriteAllTextAsync(Path.Combine(pagesDir, $"Page_{pageNumber}.xml"), pageXml.ToString(), Encoding.UTF8);
    }

    private void BuildFontMap()
    {
        _fontMap.Clear(); int id = 1;
        foreach (var item in _streamQueue)
        {
            if (item is OfdrwNet.Layout.Element.Paragraph p)
            { var fn = string.IsNullOrWhiteSpace(p.GetFontName()) ? "SimSun" : p.GetFontName()!.Trim(); if (!_fontMap.ContainsKey(fn)) _fontMap[fn] = id++; }
            else if (item is RawGlyphRun rg)
            { var fn = string.IsNullOrWhiteSpace(rg.FontName) ? "SimSun" : rg.FontName.Trim(); if (!_fontMap.ContainsKey(fn)) _fontMap[fn] = id++; }
        }
        foreach (var kv in _externalFontFiles.Keys) if (!_fontMap.ContainsKey(kv)) _fontMap[kv] = id++;
        if (_fontMap.Count == 0) _fontMap["SimSun"] = 1;
    }

    private async Task GenerateResourcesAsync(string docDir)
    {
        BuildFontMap();
        var resDir = Path.Combine(docDir, "Res"); Directory.CreateDirectory(resDir);
        _publicResRelativePath = "PublicRes.xml";
        _documentResRelativePath = "DocumentRes.xml";

        // PublicRes (Fonts)
        var fontRes = new StringBuilder();
        fontRes.Append("<?xml version=\"1.0\" encoding=\"UTF-8\"?>");
        fontRes.Append("<ofd:Res xmlns:ofd=\"http://www.ofdspec.org/2016\" BaseLoc=\"Res\"><ofd:Fonts>");
        foreach (var kv in _fontMap)
        {
            var fontName = kv.Key; var id = kv.Value; var fontNameEsc = System.Security.SecurityElement.Escape(fontName); string? fileName = null;
            if (_externalFontFiles.TryGetValue(fontName, out var srcPath))
            {
                try
                {
                    var ext = Path.GetExtension(srcPath); fileName = $"font_{id}_{id}{ext}"; var dest = Path.Combine(resDir, fileName);
                    if (!File.Exists(dest)) File.Copy(srcPath, dest, true);
                }
                catch (Exception ex) { _logger?.LogWarning(ex, "[OFDDoc][Font] Copy Fail Font={Font}", fontName); }
            }
            if (fileName != null) fontRes.Append($"<ofd:Font ID=\"{id}\" FontName=\"{fontNameEsc}\"><ofd:FontFile>{fileName}</ofd:FontFile></ofd:Font>");
            else fontRes.Append($"<ofd:Font ID=\"{id}\" FontName=\"{fontNameEsc}\"/>");
        }
        fontRes.Append("</ofd:Fonts></ofd:Res>");
        await File.WriteAllTextAsync(Path.Combine(docDir, "PublicRes.xml"), fontRes.ToString(), Encoding.UTF8);

        // DocumentRes (Images)
        var imgRes = new StringBuilder();
        imgRes.Append("<?xml version=\"1.0\" encoding=\"UTF-8\"?>");
        imgRes.Append("<ofd:Res xmlns:ofd=\"http://www.ofdspec.org/2016\" BaseLoc=\"Res\">");
        if (_rawImages.Count > 0)
        {
            int maxFontId = _fontMap.Values.DefaultIfEmpty(0).Max(); int nextId = maxFontId + 1;
            foreach (var firstImg in _rawImages.Where(r => r.IsFirstResource)) if (firstImg.ResourceID == 0) firstImg.ResourceID = nextId++;
            var hashToId = _rawImages.Where(r => r.IsFirstResource).ToDictionary(r => r.Hash, r => r.ResourceID);
            foreach (var inst in _rawImages.Where(r => !r.IsFirstResource)) if (hashToId.TryGetValue(inst.Hash, out var rid)) inst.ResourceID = rid;
            imgRes.Append("<ofd:MultiMedias>");
            foreach (var img in _rawImages.Where(r => r.IsFirstResource))
            {
                string ext = img.Format switch { "JPG" => ".jpg", "JPEG" => ".jpg", "PNG" => ".png", "GIF" => ".gif", "BMP" => ".bmp", "TIFF" => ".tiff", "WEBP" => ".webp", _ => ".bin" };
                string fileName = $"image_{img.ResourceID}{ext}"; var destPath = Path.Combine(resDir, fileName);
                try { if (!File.Exists(destPath)) File.WriteAllBytes(destPath, img.Data); } catch (Exception ex) { _logger?.LogWarning(ex, "[OFDDoc][Image] Write fail Hash={Hash}", img.Hash[..Math.Min(12,img.Hash.Length)]); }
                imgRes.Append($"<ofd:MultiMedia ID=\"{img.ResourceID}\" Type=\"Image\" Format=\"{img.Format}\"><ofd:MediaFile>{fileName}</ofd:MediaFile></ofd:MultiMedia>");
            }
            imgRes.Append("</ofd:MultiMedias>");
            _logger?.LogInformation("[OFDDoc][Image] DocumentRes Images={Count} Dedup={Dedup}", _rawImages.Where(r=>r.IsFirstResource).Count(), _dedupImageCount);
        }
        imgRes.Append("</ofd:Res>");
        await File.WriteAllTextAsync(Path.Combine(docDir, "DocumentRes.xml"), imgRes.ToString(), Encoding.UTF8);
    }

    // 合并 RawGlyphRun: 同页、字体、字号、基线Y近似，且都有DeltaX
    private List<RawGlyphRun> MergeGlyphRuns(IEnumerable<RawGlyphRun> runs)
    {
        const double yEps = 0.01; var result = new List<RawGlyphRun>(); RawGlyphRun? current = null;
        foreach (var r in runs.OrderBy(r => r.Page).ThenBy(r => r.Y).ThenBy(r => r.X))
        {
            if (current == null) { current = r; continue; }
            bool compatible = r.Page == current.Page && r.FontName == current.FontName && Math.Abs(r.FontSizeMm - current.FontSizeMm) < 1e-6 && Math.Abs(r.Y - current.Y) <= yEps && current.DeltaX != null && r.DeltaX != null;
            if (!compatible) { result.Add(current); current = r; continue; }
            try
            {
                var merged = current.Text + r.Text; var list = new List<double>(current.DeltaX!);
                double interGap = r.X - current.X - current.DeltaX.Sum();
                if (double.IsNaN(interGap) || interGap < 0) interGap = 0; // A方案：不再用字号 *0.1 增扩
                list.Add(interGap); list.AddRange(r.DeltaX!);
                current.Text = merged; current.DeltaX = list.ToArray();
            }
            catch { result.Add(current); current = r; }
        }
        if (current != null) result.Add(current); return result;
    }

    private List<(int PageNumber, List<object> Items)> ComputePageGroups()
    {
        // 使用具名元组，后续可以通过 PageNumber / Items 访问
        var groups = new List<(int PageNumber, List<object> Items)>();
        var imagePages = new HashSet<int>(_rawImages.Select(i => i.Page));
        _logger?.LogDebug("[OFD][PageGroups] Start RawImages={ImageCount} ImagePages=[{Pages}] StreamCount={StreamCount}", _rawImages.Count, string.Join(',', imagePages.OrderBy(p=>p)), _streamQueue.Count);

        if (_streamQueue.Count == 0)
        {
            if (imagePages.Count == 0)
            {
                groups.Add((1, new List<object>()));
                return groups;
            }
            foreach (var p in imagePages.OrderBy(x => x)) groups.Add((p, new List<object>()));
            return groups;
        }

        if (_streamQueue.Any(o => o is RawGlyphRun))
        {
            var rawGroups = _streamQueue.Where(o => o is RawGlyphRun).Cast<RawGlyphRun>().GroupBy(r => r.Page).OrderBy(g => g.Key);
            foreach (var g in rawGroups)
            {
                var merged = MergeGlyphRuns(g.ToList());
                groups.Add((g.Key, merged.Cast<object>().ToList()));
                _logger?.LogDebug("[OFD][PageGroups] Page={Page} GlyphRuns Raw={Raw} Merged={Merged}", g.Key, g.Count(), merged.Count);
            }
            var existing = new HashSet<int>(groups.Select(g => g.PageNumber));
            foreach (var p in imagePages.Except(existing).OrderBy(x => x)) groups.Add((p, new List<object>()));
            return groups.OrderBy(g => g.PageNumber).ToList();
        }

        int pageNo = 1; var pageItems = new List<object>();
        foreach (var item in _streamQueue)
        {
            pageItems.Add(item);
            if (pageItems.Count >= 20) { groups.Add((pageNo++, pageItems)); pageItems = new List<object>(); }
        }
        if (pageItems.Count > 0) groups.Add((pageNo, pageItems));

        var existingPages = new HashSet<int>(groups.Select(g => g.PageNumber));
        foreach (var p in imagePages.OrderBy(x => x)) if (!existingPages.Contains(p)) groups.Add((p, new List<object>()));

        return groups.OrderBy(g => g.PageNumber).ToList();
    }
    #endregion

    #region IDisposable
    public void Dispose() { Dispose(true); GC.SuppressFinalize(this); }
    protected virtual void Dispose(bool disposing)
    { if (!_disposed && disposing) { if (!_disposed) { try { Task.Run(async () => await CloseAsync()).Wait(); } catch { } } } }
    #endregion
}

public class PageLayout
{
    public double Width { get; set; }
    public double Height { get; set; }
    public Margins Margins { get; set; } = new();
    public static PageLayout A4() => new() { Width = 210.0, Height = 297.0, Margins = new Margins { Top = 25.4, Bottom = 25.4, Left = 31.7, Right = 31.7 } };
    public PageLayout Clone() => new() { Width = Width, Height = Height, Margins = Margins.Clone() };
}
public class Margins { public double Top { get; set; } public double Bottom { get; set; } public double Left { get; set; } public double Right { get; set; } public Margins Clone() => new() { Top = Top, Bottom = Bottom, Left = Left, Right = Right }; }
public class VirtualPage { }
public class TextParagraph : OfdrwNet.Layout.Element.Div { public string Text { get; set; } public double FontSize { get; set; } public string FontFamily { get; set; } = "SimSun"; public Position Position { get; set; } = new(); public TextParagraph(string text) { Text = text; } }
public class Position { public double X { get; set; } public double Y { get; set; } public Position() { } public Position(double x, double y) { X = x; Y = y; } }

// 兼容类型别名：保留旧的 OFDDoc 名称作为过渡包装（避免一次性破坏外部引用）
[Obsolete("Use OfdWriter instead. This shim will be removed in future releases.")]
public class OFDDoc : OfdWriter
{
    public OFDDoc(string outPath, ILogger? logger = null) : base(outPath, logger) { }
    public OFDDoc(Stream outStream, ILogger? logger = null) : base(outStream, logger) { }
}