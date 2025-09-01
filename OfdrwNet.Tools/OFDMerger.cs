using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using System.Xml.Linq;
using Microsoft.Extensions.Logging;
using OfdrwNet.Core.BasicType;
using OfdrwNet.Packaging.Container;
using OfdrwNet.Reader;
using System.Security.Cryptography;

namespace OfdrwNet.Tools;

/// <summary>
/// 合并结果接口（暴露统计）。
/// </summary>
public interface IMergeResult { OFDMerger.MergeStats Stats { get; } }

/// <summary>
/// 合并器接口。
/// </summary>
public interface IOfdMerger : IDisposable
{
    IOfdMerger Add(string filePath, params int[] pageIndexes);
    IOfdMerger AddMix(string dstDocFilePath, int dstPageIndex, string tbMixDocFilePath, int tbMixPageIndex);
    Task<IMergeResult> MergeAsync(CancellationToken cancellationToken = default);
    Task<IMergeResult> MergeAsync(IProgress<OFDMerger.MergeProgress>? progress, CancellationToken cancellationToken = default);
    OFDMerger.MergeStats GetStats();
}

/// <summary>
/// OFD 文档合并工具（重写修复版本）。
/// 当前实现：页面复制 + 基础结构生成 + 简单资源占位迁移统计 + 裁剪风险检测 + 进度/取消/统计输出。
/// TODO: 真正资源解析与去重、混合页面、PublicRes XElement 构建细化、字体/图片嵌入策略接口等。
/// </summary>
public class OFDMerger : IDisposable, IOfdMerger, IMergeResult
{
    #region 公共/接口相关
    public List<PageEntry> PageArray { get; } = new();
    public MergeStats GetStats() => _stats;
    MergeStats IMergeResult.Stats => _stats;
    // 兼容旧版 API
    public int GetPageCount() => PageArray.Count;
    public int GetDocumentCount() => _docContextMap.Count;
    #endregion

    #region 字段
    private readonly Dictionary<string, DocContext> _docContextMap = new();
    private readonly string _destinationPath;
    private readonly ILogger? _logger;
    private readonly string? _statsJsonPath;
    private VirtualContainer? _ofdContainer;
    private bool _disposed;

    private readonly MergeStats _stats = new();

    private double _pageWidth = 210; // A4 默认 mm
    private double _pageHeight = 297;
    private bool _pageSizeDetected;

    private int _clipRiskCount;
    private readonly List<ClipRiskDetail> _clipRiskDetails = new();
    private readonly bool _enableClipRiskDetails = true;

    // 资源占位迁移统计
    private readonly Dictionary<string, long> _resourceMigrationCache = new();
    private long _nextResourceId = 1000;
    private long _nextObjectId = 1; // 页面内对象重新分配 ID 种子
    private bool _objectIdSeedInitialized = false;

    private int _resourceFileCounter = 1;
    private int _migratedFontCount = 0;
    private int _migratedImageCount = 0;
    private int _migratedGenericResourceCount = 0;
    private readonly List<MigratedResource> _migratedResources = new();

    // 去重/缺失统计
    private int _missingFontFileCount = 0;
    private int _missingImageFileCount = 0;
    private int _dedupFontCount = 0;
    private int _dedupImageCount = 0;
    private long _savedBytes = 0;
    private readonly List<string> _warnings = new();

    // 资源散列去重
    private readonly Dictionary<string, long> _resourceHashToId = new();
    private readonly Dictionary<string, string> _resourceHashToRelPath = new();

    // 每个文档资源缓存
    private readonly Dictionary<DocContext, DocResources> _docResources = new();
    private class DocResources
    {
        public bool Loaded;
        public Dictionary<long, string> FontFiles { get; } = new(); // ID -> 绝对路径
        public Dictionary<long, string> ImageFiles { get; } = new(); // ID -> 绝对路径
        public string? PublicResDirAbs; // PublicRes.xml 所在目录
    }
    // 资源ID映射缓存 (源 DocContext + 资源类型 + 原ID -> 新ID)
    private readonly Dictionary<(DocContext Ctx, string Type, string OldId), long> _resourceIdMap = new();
    #endregion

    #region 进度
    public enum MergePhase { Start, DetectPageSize, InitObjectIdSeed, CreateStructure, MergePage, MigrateResources, UpdatePublicRes, Package, Finish, Canceled }
    public record MergeProgress(MergePhase Phase, int CurrentPage, int TotalPages, double Percent, string Message);
    private static void Report(IProgress<MergeProgress>? progress, MergeProgress mp) => progress?.Report(mp);
    #endregion

    #region 构造
    public OFDMerger(string destinationPath, ILogger? logger = null, string? statsJsonPath = null, bool enableClipRiskDetails = true)
    {
        if (string.IsNullOrWhiteSpace(destinationPath)) throw new ArgumentException("合并结果路径不能为空", nameof(destinationPath));
        _destinationPath = Path.GetFullPath(destinationPath);
        _logger = logger;
        _statsJsonPath = string.IsNullOrWhiteSpace(statsJsonPath) ? null : Path.GetFullPath(statsJsonPath);
        _enableClipRiskDetails = enableClipRiskDetails;
        var outDir = Path.GetDirectoryName(_destinationPath);
        if (!string.IsNullOrEmpty(outDir) && !Directory.Exists(outDir)) Directory.CreateDirectory(outDir!);
        if (_statsJsonPath != null)
        {
            var statsDir = Path.GetDirectoryName(_statsJsonPath);
            if (!string.IsNullOrEmpty(statsDir) && !Directory.Exists(statsDir)) Directory.CreateDirectory(statsDir!);
        }
    }
    #endregion

    #region 添加页面
    public OFDMerger Add(string filePath, params int[] pageIndexes)
    {
        if (string.IsNullOrWhiteSpace(filePath)) return this;
        if (!File.Exists(filePath)) throw new FileNotFoundException($"文件不存在: {filePath}");
        var key = Path.GetFileName(filePath);
        if (!_docContextMap.TryGetValue(key, out var ctx)) { ctx = new DocContext(filePath); _docContextMap[key] = ctx; }
        if (pageIndexes == null || pageIndexes.Length == 0)
        {
            int n = ctx.GetNumberOfPages();
            pageIndexes = Enumerable.Range(1, n).ToArray();
        }
        foreach (var p in pageIndexes)
        {
            if (ctx.IsValidPageIndex(p)) PageArray.Add(new PageEntry(p, ctx));
            else _logger?.LogWarning("页面索引 {Index} 超出文档 {Doc} 范围 (1-{Max})", p, key, ctx.GetNumberOfPages());
        }
        return this;
    }
    IOfdMerger IOfdMerger.Add(string filePath, params int[] pageIndexes) => Add(filePath, pageIndexes);

    public OFDMerger AddMix(string dstDocFilePath, int dstPageIndex, string tbMixDocFilePath, int tbMixPageIndex)
    {
        // 简化：仅添加第一个页面，留待真正混合实现
        return Add(dstDocFilePath, dstPageIndex);
    }
    IOfdMerger IOfdMerger.AddMix(string dstDocFilePath, int dstPageIndex, string tbMixDocFilePath, int tbMixPageIndex) => AddMix(dstDocFilePath, dstPageIndex, tbMixDocFilePath, tbMixPageIndex);
    #endregion

    #region Merge 主流程
    public async Task<IMergeResult> MergeAsync(CancellationToken cancellationToken = default) => await MergeAsync(null, cancellationToken);

    [Obsolete("请使用支持取消/进度的 MergeAsync 重载。")]
    public Task MergeAsync() => MergeAsync(CancellationToken.None);

    public async Task<IMergeResult> MergeAsync(IProgress<MergeProgress>? progress, CancellationToken cancellationToken = default)
    {
        if (PageArray.Count == 0) throw new InvalidOperationException("没有页面可以合并");
        var sw = Stopwatch.StartNew();
        _stats.PageCount = PageArray.Count;
        _stats.SourceDocCount = _docContextMap.Count;
        _stats.OutputPath = _destinationPath;
        _stats.Canceled = false;
        Report(progress, new MergeProgress(MergePhase.Start, 0, PageArray.Count, 0, "开始"));
        try
        {
            _logger?.LogInformation("合并开始 Pages={Pages} Dest={Dest}", PageArray.Count, _destinationPath);
            // 创建临时工作目录
            var workDir = Path.Combine(Path.GetTempPath(), $"OfdrwNet_Merge_{Guid.NewGuid():N}");
            Directory.CreateDirectory(workDir);
            _ofdContainer = new VirtualContainer(workDir);
            cancellationToken.ThrowIfCancellationRequested();

            DetectPageSizeIfPossible();
            Report(progress, new MergeProgress(MergePhase.DetectPageSize, 0, PageArray.Count, 2, "探测页面尺寸"));

            InitializeObjectIdSeed();
            Report(progress, new MergeProgress(MergePhase.InitObjectIdSeed, 0, PageArray.Count, 3, "ID 种子初始化"));

            CreateBasicStructure();
            Report(progress, new MergeProgress(MergePhase.CreateStructure, 0, PageArray.Count, 5, "结构创建"));

            await MergePagesInternal(progress, cancellationToken);

            cancellationToken.ThrowIfCancellationRequested();
            UpdatePublicRes();
            Report(progress, new MergeProgress(MergePhase.UpdatePublicRes, PageArray.Count, PageArray.Count, 95, "公共资源更新"));

            PackageDocument();
            Report(progress, new MergeProgress(MergePhase.Package, PageArray.Count, PageArray.Count, 97, "打包"));

            sw.Stop();
            _stats.DurationMs = sw.ElapsedMilliseconds;
            _stats.FontsMigrated = _migratedFontCount;
            _stats.ImagesMigrated = _migratedImageCount;
            _stats.GenericResourcesMigrated = _migratedGenericResourceCount;
            _stats.ClipRiskCount = _clipRiskCount;
            _stats.FontFiles = _migratedResources.Where(r => r.Type == "Font").Select(r => r.RelativePath ?? "").ToList();
            _stats.ImageFiles = _migratedResources.Where(r => r.Type == "Image").Select(r => r.RelativePath ?? "").ToList();
            _stats.ClipRiskDetails = _enableClipRiskDetails ? _clipRiskDetails : null;
            _stats.MissingFontFileCount = _missingFontFileCount;
            _stats.MissingImageFileCount = _missingImageFileCount;
            _stats.DedupFontCount = _dedupFontCount;
            _stats.DedupImageCount = _dedupImageCount;
            _stats.SavedBytes = _savedBytes;
            _stats.Warnings = _warnings;

            if (_statsJsonPath != null)
            {
                var json = JsonSerializer.Serialize(_stats, new JsonSerializerOptions { WriteIndented = true });
                File.WriteAllText(_statsJsonPath, json);
                _logger?.LogInformation("统计输出 {Path}", _statsJsonPath);
            }

            _logger?.LogInformation("合并完成 Ms={Ms}", _stats.DurationMs);
            Report(progress, new MergeProgress(MergePhase.Finish, PageArray.Count, PageArray.Count, 100, "完成"));
            return this;
        }
        catch (OperationCanceledException)
        {
            sw.Stop();
            _stats.DurationMs = sw.ElapsedMilliseconds;
            _stats.Canceled = true;
            _logger?.LogWarning("合并已取消 Ms={Ms}", sw.ElapsedMilliseconds);
            Report(progress, new MergeProgress(MergePhase.Canceled, 0, PageArray.Count, 0, "已取消"));
            throw;
        }
        catch (Exception ex)
        {
            sw.Stop();
            _stats.DurationMs = sw.ElapsedMilliseconds;
            _logger?.LogError(ex, "合并失败 Ms={Ms}", sw.ElapsedMilliseconds);
            throw new InvalidOperationException("文档合并失败", ex);
        }
    }
    #endregion

    #region 页面尺寸探测
    private void DetectPageSizeIfPossible()
    {
        if (_pageSizeDetected || PageArray.Count == 0) return;
        try
        {
            var first = PageArray[0];
            var baseDir = first.DocContext.Container!.GetSysAbsPath();
            var candidates = new[]
            {
                Path.Combine(baseDir, "Doc", "Document.xml"),
                Path.Combine(baseDir, "Doc_0", "Document.xml")
            };
            foreach (var docXml in candidates)
            {
                if (!File.Exists(docXml)) continue;
                var xdoc = XDocument.Load(docXml);
                var phys = xdoc.Descendants().FirstOrDefault(e => e.Name.LocalName == "PhysicalBox");
                if (phys == null) continue;
                var parts = phys.Value.Split(new[] { ' ', '\t' }, StringSplitOptions.RemoveEmptyEntries);
                if (parts.Length == 4 && double.TryParse(parts[2], NumberStyles.Float, CultureInfo.InvariantCulture, out var w) && double.TryParse(parts[3], NumberStyles.Float, CultureInfo.InvariantCulture, out var h) && w > 0 && h > 0)
                {
                    _pageWidth = w; _pageHeight = h; _pageSizeDetected = true;
                    _logger?.LogInformation("页面尺寸 {W}x{H}", w, h);
                    break;
                }
            }
        }
        catch (Exception ex)
        {
            _logger?.LogDebug(ex, "页面尺寸探测失败 使用默认");
        }
    }
    #endregion

    #region 对象ID种子初始化
    private void InitializeObjectIdSeed()
    {
        if (_objectIdSeedInitialized) return;
        long maxId = 0;
        try
        {
            foreach (var ctx in _docContextMap.Values)
            {
                var baseDir = ctx.Container!.GetSysAbsPath();
                var candidates = new[]
                {
                    Path.Combine(baseDir, "Doc", "Document.xml"),
                    Path.Combine(baseDir, "Doc_0", "Document.xml")
                };
                foreach (var p in candidates)
                {
                    if (!File.Exists(p)) continue;
                    var text = File.ReadAllText(p);
                    foreach (Match m in Regex.Matches(text, "ID=\"(\\d+)\""))
                    {
                        if (long.TryParse(m.Groups[1].Value, out var id) && id > maxId) maxId = id;
                    }
                }
            }
        }
        catch (Exception ex)
        {
            _logger?.LogDebug(ex, "对象ID扫描失败 使用默认");
        }
        _nextObjectId = Math.Max(_nextObjectId, maxId + 1);
        _objectIdSeedInitialized = true;
        _logger?.LogDebug("对象ID种子初始化 maxSourceId={Max} nextStart={Start}", maxId, _nextObjectId);
    }
    #endregion

    #region 结构创建
    private void CreateBasicStructure()
    {
        if (_ofdContainer == null) throw new InvalidOperationException("容器未初始化");
        // OFD.xml Version=1.2
        var ofdXml = "<?xml version=\"1.0\" encoding=\"UTF-8\"?>\n" +
                     "<ofd:OFD xmlns:ofd=\"http://www.ofdspec.org/2016\" DocType=\"OFD\" Version=\"1.2\">\n" +
                     "  <ofd:DocBody>\n" +
                     "    <ofd:DocInfo>\n" +
                     "      <ofd:DocRoot>Doc_0/Document.xml</ofd:DocRoot>\n" +
                     $"      <ofd:CreationDate>{DateTime.Now:yyyy-MM-ddTHH:mm:ss}</ofd:CreationDate>\n" +
                     "    </ofd:DocInfo>\n" +
                     "  </ofd:DocBody>\n" +
                     "</ofd:OFD>";
        _ofdContainer.PutObj("OFD.xml", ofdXml);

        var doc0 = _ofdContainer.ObtainContainer("Doc_0", () => new VirtualContainer(Path.Combine(_ofdContainer.GetSysAbsPath(), "Doc_0")));
        doc0.PutObj("Document.xml", CreateDocumentXml());
        var res = doc0.ObtainContainer("Res", () => new VirtualContainer(Path.Combine(doc0.GetSysAbsPath(), "Res")));
        // 初始占位 PublicRes
        var publicRes = "<?xml version=\"1.0\" encoding=\"UTF-8\"?>\n<ofd:Res xmlns:ofd=\"http://www.ofdspec.org/2016\"></ofd:Res>";
        res.PutObj("PublicRes.xml", publicRes);
    }

    private string CreateDocumentXml()
    {
        var pw = _pageWidth.ToString(CultureInfo.InvariantCulture);
        var ph = _pageHeight.ToString(CultureInfo.InvariantCulture);
        var sb = new StringBuilder();
        sb.AppendLine("<?xml version=\"1.0\" encoding=\"UTF-8\"?>");
        sb.AppendLine("<ofd:Document xmlns:ofd=\"http://www.ofdspec.org/2016\">");
        sb.AppendLine("  <ofd:CommonData>");
        sb.AppendLine("    <ofd:PageArea>");
        sb.AppendLine($"      <ofd:PhysicalBox>0 0 {pw} {ph}</ofd:PhysicalBox>");
        sb.AppendLine("    </ofd:PageArea>");
        sb.AppendLine("    <ofd:PublicRes>Res/PublicRes.xml</ofd:PublicRes>");
        sb.AppendLine("  </ofd:CommonData>");
        sb.AppendLine("  <ofd:Pages>");
        for (int i = 0; i < PageArray.Count; i++)
        {
            int pageNo = i + 1;
            sb.AppendLine($"    <ofd:Page ID=\"{pageNo}\" BaseLoc=\"Pages/Page_{pageNo}/Content.xml\"/>");
        }
        sb.AppendLine("  </ofd:Pages>");
        sb.AppendLine("</ofd:Document>");
        return sb.ToString();
    }
    #endregion

    #region 页面合并
    private async Task MergePagesInternal(IProgress<MergeProgress>? progress, CancellationToken ct)
    {
        if (_ofdContainer == null) throw new InvalidOperationException("容器未初始化");
        var doc0 = _ofdContainer.ObtainContainer("Doc_0", () => new VirtualContainer(Path.Combine(_ofdContainer.GetSysAbsPath(), "Doc_0")));
        var pages = doc0.ObtainContainer("Pages", () => new VirtualContainer(Path.Combine(doc0.GetSysAbsPath(), "Pages")));

        // 进度分段： 5%~85% 分配给页面循环（含资源迁移）
        const double phaseBase = 5; const double phaseSpan = 80;

        for (int i = 0; i < PageArray.Count; i++)
        {
            ct.ThrowIfCancellationRequested();
            var entry = PageArray[i]; int pageNo = i + 1;
            _logger?.LogDebug("页面 {Index}/{Total} Src={Doc}", pageNo, PageArray.Count, entry.DocContext.GetFileName());
            var pageContainer = pages.ObtainContainer($"Page_{pageNo}", () => new VirtualContainer(Path.Combine(pages.GetSysAbsPath(), $"Page_{pageNo}")));
            var srcInfo = entry.GetPageInfo();
            var pageContent = srcInfo.Obj; // XElement
            var xml = pageContent.ToString();
            await File.WriteAllTextAsync(Path.Combine(pageContainer.GetSysAbsPath(), "Content.xml"), xml, ct);

            _clipRiskCount += DetectClipRisk(pageContent, pageNo);

            await MigratePageResources(pageContent, entry.DocContext.Reader, progress, pageNo, ct);

            double percent = phaseBase + ((double)pageNo / PageArray.Count) * phaseSpan;
            Report(progress, new MergeProgress(MergePhase.MergePage, pageNo, PageArray.Count, percent, $"页面 {pageNo} 完成"));
        }
    }
    #endregion

    #region 资源迁移（占位实现）
    private async Task MigratePageResources(XElement pageContent, OfdReader sourceReader, IProgress<MergeProgress>? progress, int pageNo, CancellationToken ct)
    {
        // 简化：扫描属性名集合，模拟迁移计数；后续替换为真实资源解析。
        var resourceAttributes = new Dictionary<string, string[]>
        {
            { "Font", new[] { "Font" } },
            { "ResourceID", new[] { "ResourceID" } },
            { "Substitution", new[] { "Substitution" } },
            { "ImageMask", new[] { "ImageMask" } },
            { "Thumbnail", new[] { "Thumbnail" } },
            { "DrawParam", new[] { "DrawParam" } },
            { "ColorSpace", new[] { "ColorSpace" } }
        };
        int processed = 0; int total = resourceAttributes.Sum(kv => kv.Value.Length);
        foreach (var kv in resourceAttributes)
        {
            foreach (var attr in kv.Value)
            {
                ct.ThrowIfCancellationRequested();
                try { await MigrateResourcesByAttribute(pageContent, attr, sourceReader); }
                catch (OperationCanceledException) { throw; }
                catch (Exception ex) { _logger?.LogError(ex, "属性资源迁移错误 {Attr}", attr); }
                processed++;
                double subPercent = (double)processed / Math.Max(1, total);
                // 将资源迁移子进度映射到当前页所在整体阶段：粗略增量（不再额外分段，避免百分比跳动过大）
                var basePercent = 5 + ((double)(pageNo - 1) / PageArray.Count) * 80;
                var nextBase = 5 + ((double)pageNo / PageArray.Count) * 80;
                var mergedPercent = basePercent + (nextBase - basePercent) * subPercent;
                Report(progress, new MergeProgress(MergePhase.MigrateResources, pageNo, PageArray.Count, mergedPercent, $"资源迁移 {attr}"));
            }
        }
    }

    private async Task<long> MigrateFontResource(object? fontRes, OfdReader reader)
    {
        try
        {
            var ctx = GetDocContextByReader(reader);
            if (ctx != null) EnsureDocResources(ctx);
            long newId = await RealMigrateResourceFromAttributeCache(reader, isFont: true);
            return newId;
        }
        catch { return 0L; }
    }

    private async Task<long> MigrateMediaResource(object? mediaRes, OfdReader reader)
    {
        try { return await RealMigrateResourceFromAttributeCache(reader, isFont: false); } catch { return 0L; }
    }

    private Task<long> MigrateDrawParamResource(object? drawParam) => Task.FromResult(0L); // 占位
    private Task<long> MigrateColorSpaceResource(object? cs, OfdReader reader) => Task.FromResult(0L); // 占位
    private Task<long> MigrateGenericResource(object? res, OfdReader reader) => Task.FromResult(0L); // 占位
    // 移除无主体前向声明，直接提供私有帮助方法（如果后续已有实现将不再重复）
    private string GuessByExtensionInternal(string? path)
    {
        if (string.IsNullOrEmpty(path)) return "Unknown";
        var ext = Path.GetExtension(path).ToLowerInvariant();
        return ext switch
        {
            ".png" => "PNG",
            ".jpg" or ".jpeg" or ".jfif" => "JPG",
            ".bmp" => "BMP",
            ".gif" => "GIF",
            ".tif" or ".tiff" => "TIFF",
            ".webp" => "WEBP",
            _ => "Unknown"
        };
    }
    // 如果后面已有 GuessByExtension/GuessImageFormat 将继续使用后者；此内部方法仅兜底未找到文件时调用
    #endregion

    #region PublicRes 重写
    private void UpdatePublicRes()
    {
        try
        {
            if (_ofdContainer == null) return;
            var doc0 = _ofdContainer.ObtainContainer("Doc_0", () => new VirtualContainer(Path.Combine(_ofdContainer.GetSysAbsPath(), "Doc_0")));
            var res = doc0.ObtainContainer("Res", () => new VirtualContainer(Path.Combine(doc0.GetSysAbsPath(), "Res")));
            if (_migratedResources.Count == 0) return;
            XNamespace ofd = "http://www.ofdspec.org/2016";
            var root = new XElement(ofd + "Res");
            var fonts = _migratedResources.Where(r => r.Type == "Font").ToList();
            if (fonts.Count > 0)
            {
                var fontsEl = new XElement(ofd + "Fonts");
                foreach (var f in fonts)
                {
                    var name = $"Font{f.Id}";
                    var fontEl = new XElement(ofd + "Font",
                        new XAttribute("ID", f.Id),
                        new XAttribute("FontName", name),
                        new XAttribute("FamilyName", name),
                        new XAttribute("Charset", "unicode"));
                    if (!string.IsNullOrEmpty(f.RelativePath)) fontEl.Add(new XAttribute("FontFile", f.RelativePath));
                    fontsEl.Add(fontEl);
                }
                root.Add(fontsEl);
            }
            var images = _migratedResources.Where(r => r.Type == "Image").ToList();
            if (images.Count > 0)
            {
                var mmEl = new XElement(ofd + "MultiMedias");
                foreach (var img in images)
                {
                    var format = GuessImageFormat(img.RelativePath);
                    var media = new XElement(ofd + "MultiMedia",
                        new XAttribute("ID", img.Id),
                        new XAttribute("Type", "Image"),
                        new XAttribute("Format", format));
                    if (!string.IsNullOrEmpty(img.RelativePath)) media.Add(new XAttribute("MediaFile", img.RelativePath));
                    mmEl.Add(media);
                }
                root.Add(mmEl);
            }
            var generics = _migratedResources.Where(r => r.Type == "Generic").ToList();
            if (generics.Count > 0)
            {
                var extEl = new XElement(ofd + "Extensions");
                foreach (var g in generics)
                {
                    var e = new XElement(ofd + "Extension",
                        new XAttribute("ID", g.Id),
                        new XAttribute("Type", "GenericRes"));
                    if (!string.IsNullOrEmpty(g.RelativePath)) e.Add(new XAttribute("File", g.RelativePath));
                    extEl.Add(e);
                }
                root.Add(extEl);
            }
            var xdoc = new XDocument(new XDeclaration("1.0", "UTF-8", null), root);
            res.PutObj("PublicRes.xml", xdoc.ToString());
            _logger?.LogDebug("PublicRes 更新 Fonts={F} Images={I} Generic={G}", fonts.Count, images.Count, generics.Count);
        }
        catch (Exception ex)
        {
            _logger?.LogWarning(ex, "PublicRes 更新失败(保持占位) ");
        }
    }
    #endregion

    #region 打包 & 释放
    private void PackageDocument()
    {
        if (_ofdContainer == null) throw new InvalidOperationException("容器未初始化");
        _ofdContainer.Flush();
        var workDir = _ofdContainer.GetSysAbsPath();
        if (File.Exists(_destinationPath)) File.Delete(_destinationPath);
        ZipFile.CreateFromDirectory(workDir, _destinationPath);
        try { Directory.Delete(workDir, true); } catch { /* ignore */ }
    }

    public void Dispose()
    {
        if (_disposed) return;
        foreach (var ctx in _docContextMap.Values) ctx.Dispose();
        _docContextMap.Clear();
        _ofdContainer?.Dispose();
        _disposed = true;
    }
    #endregion

    #region 统计/结构
    public class MergeStats
    {
        public string StatsVersion { get; set; } = "1.1";
        public int PageCount { get; set; }
        public int SourceDocCount { get; set; }
        public long DurationMs { get; set; }
        public int FontsMigrated { get; set; }
        public int ImagesMigrated { get; set; }
        public int GenericResourcesMigrated { get; set; }
        public int ClipRiskCount { get; set; }
        public bool Canceled { get; set; }
        public string? OutputPath { get; set; }
        public List<string>? FontFiles { get; set; }
        public List<string>? ImageFiles { get; set; }
        public List<ClipRiskDetail>? ClipRiskDetails { get; set; }
        public int MissingFontFileCount { get; set; }
        public int MissingImageFileCount { get; set; }
        public int DedupFontCount { get; set; }
        public int DedupImageCount { get; set; }
        public long SavedBytes { get; set; }
        public List<string>? Warnings { get; set; }
        // 别名
        public List<string>? Fonts { get => FontFiles; set => FontFiles = value; }
        public List<string>? Images { get => ImageFiles; set => ImageFiles = value; }
    }

    public class ClipRiskDetail
    {
        public int Page { get; set; }
        public string Boundary { get; set; } = string.Empty;
        public bool Left { get; set; }
        public bool Top { get; set; }
        public bool Right { get; set; }
        public bool Bottom { get; set; }
    }

    private class MigratedResource
    {
        public long Id { get; set; }
        public string Type { get; set; } = string.Empty;
        public string? RelativePath { get; set; }
    }
    #endregion

    #region ClipRisk 检测
    private int DetectClipRisk(XElement pageContent, int pageNo)
    {
        int count = 0;
        try
        {
            foreach (var el in pageContent.Descendants())
            {
                var attr = el.Attribute("Boundary");
                if (attr == null) continue;
                var nums = attr.Value.Split(new[] { ' ', '\t' }, StringSplitOptions.RemoveEmptyEntries);
                if (nums.Length < 4) continue;
                if (!double.TryParse(nums[0], NumberStyles.Float, CultureInfo.InvariantCulture, out var x)) continue;
                if (!double.TryParse(nums[1], NumberStyles.Float, CultureInfo.InvariantCulture, out var y)) continue;
                if (!double.TryParse(nums[2], NumberStyles.Float, CultureInfo.InvariantCulture, out var w)) continue;
                if (!double.TryParse(nums[3], NumberStyles.Float, CultureInfo.InvariantCulture, out var h)) continue;
                if (w <= 0 || h <= 0) continue;
                bool left = x < 0; bool top = y < 0; bool right = x + w > _pageWidth + 0.01; bool bottom = y + h > _pageHeight + 0.01;
                if (left || top || right || bottom)
                {
                    count++;
                    if (_enableClipRiskDetails)
                    {
                        _clipRiskDetails.Add(new ClipRiskDetail
                        {
                            Page = pageNo,
                            Boundary = $"{x} {y} {w} {h}",
                            Left = left,
                            Top = top,
                            Right = right,
                            Bottom = bottom
                        });
                    }
                }
            }
        }
        catch (Exception ex)
        {
            _logger?.LogDebug(ex, "ClipRisk 检测失败");
        }
        return count;
    }
    #endregion

    #region 若尚未定义 GuessImageFormat，则提供实现
    private string GuessImageFormat(string? path)
    {
        if (string.IsNullOrEmpty(path) || !File.Exists(path)) return GuessByExtensionInternal(path);
        try
        {
            using var fs = File.OpenRead(path);
            Span<byte> buf = stackalloc byte[16];
            var read = fs.Read(buf);
            if (read >= 8 && buf[0]==0x89 && buf[1]==0x50 && buf[2]==0x4E && buf[3]==0x47 && buf[4]==0x0D && buf[5]==0x0A && buf[6]==0x1A && buf[7]==0x0A) return "PNG";
            if (read >= 3 && buf[0]==0xFF && buf[1]==0xD8 && buf[2]==0xFF) return "JPG";
            if (read >= 6 && buf[0]=='G' && buf[1]=='I' && buf[2]=='F' && buf[3]=='8' && (buf[4]=='7'||buf[4]=='9') && buf[5]=='a') return "GIF";
            if (read >= 2 && buf[0]=='B' && buf[1]=='M') return "BMP";
            if (read >= 4 && ((buf[0]=='I'&&buf[1]=='I'&&buf[2]==0x2A&&buf[3]==0x00)||(buf[0]=='M'&&buf[1]=='M'&&buf[2]==0x00&&buf[3]==0x2A))) return "TIFF";
            if (read >= 12 && buf[0]=='R'&&buf[1]=='I'&&buf[2]=='F'&&buf[3]=='F' && buf[8]=='W'&&buf[9]=='E'&&buf[10]=='B'&&buf[11]=='P') return "WEBP";
            return GuessByExtensionInternal(path);
        }
        catch { return GuessByExtensionInternal(path); }
    }
    #endregion

    #region 帮助方法
    private DocContext? GetDocContextByReader(OfdReader reader)
    {
        foreach (var kv in _docContextMap)
        {
            if (kv.Value.Reader == reader) return kv.Value;
        }
        return null;
    }

    private void EnsureDocResources(DocContext ctx)
    {
        if (_docResources.TryGetValue(ctx, out var cache) && cache.Loaded) return;
        cache = cache ?? new DocResources();
        var baseDir = ctx.Container!.GetSysAbsPath();
        var candidates = new[]
        {
            Path.Combine(baseDir, "Doc", "Res", "PublicRes.xml"),
            Path.Combine(baseDir, "Doc_0", "Res", "PublicRes.xml"),
            Path.Combine(baseDir, "Res", "PublicRes.xml")
        };
        var found = candidates.FirstOrDefault(File.Exists);
        if (found == null) { cache.Loaded = true; _docResources[ctx] = cache; return; }
        cache.PublicResDirAbs = Path.GetDirectoryName(found);
        try
        {
            var xdoc = XDocument.Load(found);
            foreach (var font in xdoc.Descendants().Where(e=>e.Name.LocalName=="Font"))
            {
                if (long.TryParse(font.Attribute("ID")?.Value, out var fid))
                {
                    var fontFile = font.Attribute("FontFile")?.Value;
                    if (!string.IsNullOrEmpty(fontFile))
                    {
                        var abs = Path.GetFullPath(Path.Combine(cache.PublicResDirAbs!, fontFile));
                        cache.FontFiles[fid] = abs;
                    }
                }
            }
            foreach (var mm in xdoc.Descendants().Where(e=>e.Name.LocalName=="MultiMedia"))
            {
                if (!string.Equals(mm.Attribute("Type")?.Value, "Image", StringComparison.OrdinalIgnoreCase)) continue;
                if (long.TryParse(mm.Attribute("ID")?.Value, out var mid))
                {
                    var mediaFile = mm.Attribute("MediaFile")?.Value;
                    if (!string.IsNullOrEmpty(mediaFile))
                    {
                        var abs = Path.GetFullPath(Path.Combine(cache.PublicResDirAbs!, mediaFile));
                        cache.ImageFiles[mid] = abs;
                    }
                }
            }
        }
        catch (Exception ex)
        {
            _logger?.LogDebug(ex, "解析 PublicRes 失败 {File}", found);
        }
        cache.Loaded = true;
        _docResources[ctx] = cache;
    }

    [ThreadStatic] private static string? _currentOldResourceId; // 已存在上方若重复则可删除
    private async Task<long> RealMigrateResourceFromAttributeCache(OfdReader reader, bool isFont)
    {
        var oldIdStr = _currentOldResourceId;
        if (string.IsNullOrWhiteSpace(oldIdStr)) return 0;
        if (!long.TryParse(oldIdStr, out var oldId)) return 0;
        var ctx = GetDocContextByReader(reader); if (ctx == null) return 0; EnsureDocResources(ctx);
        var key = (Ctx: ctx, Type: isFont ? "Font" : "Image", OldId: oldIdStr);
        if (_resourceIdMap.TryGetValue(key, out var mapped))
        {
            return mapped;
        }
        if (!_docResources.TryGetValue(ctx, out var docRes)) return 0;
        var dict = isFont ? docRes.FontFiles : docRes.ImageFiles;
        dict.TryGetValue(oldId, out var absPath);
        bool placeholder = string.IsNullOrEmpty(absPath) || !File.Exists(absPath!);
        long newId;
        if (placeholder)
        {
            newId = _nextResourceId++;
            _migratedResources.Add(new MigratedResource { Id = newId, Type = isFont ? "Font" : "Image", RelativePath = null });
            if (isFont) { _migratedFontCount++; _missingFontFileCount++; _warnings.Add($"Missing font file Doc={ctx.GetFileName()} OldID={oldIdStr}"); }
            else { _migratedImageCount++; _missingImageFileCount++; _warnings.Add($"Missing image file Doc={ctx.GetFileName()} OldID={oldIdStr}"); }
            _resourceIdMap[key] = newId;
            return newId;
        }
        string hash; long fileSize;
        using (var fs = File.OpenRead(absPath!))
        {
            fileSize = fs.Length;
            var sha = SHA256.Create();
            hash = Convert.ToHexString(await sha.ComputeHashAsync(fs));
        }
        if (_resourceHashToId.TryGetValue(hash, out var existing))
        {
            _resourceIdMap[key] = existing;
            if (!_migratedResources.Any(r => r.Id == existing))
            {
                _migratedResources.Add(new MigratedResource { Id = existing, Type = isFont ? "Font" : "Image", RelativePath = _resourceHashToRelPath[hash] });
            }
            if (isFont) _dedupFontCount++; else _dedupImageCount++;
            _savedBytes += fileSize;
            return existing;
        }
        newId = _nextResourceId++;
        var resDir = Path.Combine(_ofdContainer!.GetSysAbsPath(), "Doc_0", "Res"); Directory.CreateDirectory(resDir);
        var ext = Path.GetExtension(absPath);
        var typeDir = isFont ? "fonts" : "images";
        var targetName = $"{typeDir}_{newId}{ext}";
        var targetAbs = Path.Combine(resDir, targetName);
        File.Copy(absPath!, targetAbs, true);
        var rel = $"Res/{targetName}";
        _resourceHashToId[hash] = newId;
        _resourceHashToRelPath[hash] = rel;
        _migratedResources.Add(new MigratedResource { Id = newId, Type = isFont ? "Font" : "Image", RelativePath = rel });
        if (isFont) _migratedFontCount++; else _migratedImageCount++;
        _resourceIdMap[key] = newId;
        return newId;
    }

    private async Task MigrateResourcesByAttribute(XElement pageContent, string attributeName, OfdReader sourceReader)
    {
        var list = pageContent.Descendants().Where(e=>e.Attribute(attributeName)!=null).ToList();
        var ctx = GetDocContextByReader(sourceReader);
        foreach (var el in list)
        {
            var old = el.Attribute(attributeName)!.Value; if (string.IsNullOrWhiteSpace(old)) continue;
            _currentOldResourceId = old;
            long newId = attributeName switch
            {
                "Font" => await RealMigrateResourceFromAttributeCache(sourceReader, true),
                "ResourceID" => await RealMigrateResourceFromAttributeCache(sourceReader, false),
                "DrawParam" => await MigrateDrawParamResource(null),
                "ColorSpace" => await MigrateColorSpaceResource(null, sourceReader),
                _ => await MigrateGenericResource(null, sourceReader)
            };
            if (newId > 0)
            {
                el.SetAttributeValue(attributeName, newId.ToString());
            }
        }
        ReassignObjectIds(pageContent);
    }

    private void ReassignObjectIds(XElement root)
    {
        try
        {
            var els = root.Descendants().Where(e=>e.Attribute("ID")!=null).ToList();
            foreach (var el in els)
            {
                el.SetAttributeValue("ID", (_nextObjectId++).ToString());
            }
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "重新分配对象ID失败");
        }
    }
    #endregion
}