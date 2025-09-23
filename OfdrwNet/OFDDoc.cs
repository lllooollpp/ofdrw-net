using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using OfdrwNet.Abstractions;
using OfdrwNet.Packaging;
using System.Security.Cryptography;
using System.Text;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Formats.Png;
using OfdrwNet.Layout.Element;
using OfdrwNet.Layout;
using System.Threading;
using System.Xml.Linq;
using System.IO.Compression;
using OfdrwNet.Pkg.Container;

namespace OfdrwNet
{
    /// <summary>
    /// OFD文档写入器。
    /// 对应 Java 中的 OFDDoc。
    /// </summary>
    public class OfdWriter : IDisposable, IOfdDocWriter
    {
        #region 字段
        private readonly ILogger? _logger;
        private readonly string? _outPath;
        private readonly Stream? _outStream;
        private int _maxUnitID;
        private readonly OfdContainer _ofdContainer;
        private PageLayout _pageLayout = PageLayout.A4();
        private bool _disposed = false;
        private readonly List<object> _streamQueue = new();
        private readonly List<VirtualPage> _virtualPageList = new();
        private readonly Dictionary<string, OfdFont> _fontMap = new Dictionary<string, OfdFont>(StringComparer.OrdinalIgnoreCase);
        private readonly Dictionary<string, string> _externalFontFiles = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        private readonly List<RawImage> _rawImages = new();
        private readonly Dictionary<string, RawImage> _imageHashFirst = new(); // hash->首图
        private readonly List<RawPath> _rawPaths = new();
        private readonly List<RawAnnotation> _rawAnnotations = new();
        private int _dedupImageCount = 0;
        private List<(int PageNumber, List<object> Items)>? _pageGroups; // 预计算分页
    private string _publicResRelativePath = string.Empty;
    private string _documentResRelativePath = string.Empty;

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
            public double[]? CTM { get; set; } // a b c d e f
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
            public double[]? CTM { get; set; } // a b c d e f
        }
        private class RawPath
        {
            public string PathData { get; set; } = string.Empty; // SVG路径数据
            public double X { get; set; }
            public double Y { get; set; }
            public double Width { get; set; }
            public double Height { get; set; }
            public int Page { get; set; } = 1;
            public double[]? CTM { get; set; } // a b c d e f
        }
        private class RawAnnotation
        {
            public object Annotation { get; set; } = new object();
            public int Page { get; set; } = 1;
        }
        #endregion

        #region Helper IO
        private static async Task WriteTextFileUtf8LfAsync(string path, string content)
        {
            if (content == null) content = string.Empty;
            // Normalize newlines to LF
            content = content.Replace("\r\n", "\n").Replace("\r", "\n");
            // Ensure directory exists
            var dir = Path.GetDirectoryName(path);
            if (!string.IsNullOrEmpty(dir) && !Directory.Exists(dir)) Directory.CreateDirectory(dir);
            var bytes = Encoding.UTF8.GetBytes(content);
            await File.WriteAllBytesAsync(path, bytes).ConfigureAwait(false);
        }
        #endregion

        #region 构造
        /// <param name="outPath">OFD输出路径，可以是目录或zip文件</param>
        /// <param name="logger">日志记录器</param>
        public OfdWriter(string outPath, ILogger? logger = null)
        {
            if (string.IsNullOrEmpty(outPath))
                throw new ArgumentException("输出路径不能为空", nameof(outPath));

            _logger = logger;
            _logger?.LogInformation("[OFDDoc] OfdWriter created for path: {Path}", outPath);
            _outPath = outPath;
            _ofdContainer = new OfdContainer(outPath);
            InitializeContainer();
        }

        /// <param name="outStream">OFD输出流</param>
        /// <param name="logger">日志记录器</param>
        public OfdWriter(Stream outStream, ILogger? logger = null)
        {
            _logger = logger;
            _logger?.LogInformation("[OFDDoc] OfdWriter created for stream.");
            _outStream = outStream;
            _ofdContainer = new OfdContainer(outStream);
            InitializeContainer();
        }
        private void InitializeContainer() => _maxUnitID = 0;
        #endregion

        #region 属性
        public PageLayout DefaultPageLayout => _pageLayout.Clone();
        public int MaxUnitID => _maxUnitID;
        public ILogger? Logger => _logger; // 接口要求
        public int DedupImageCount => _dedupImageCount;
        #endregion

        #region 添加元素
        public OfdWriter SetDefaultPageLayout(PageLayout pageLayout)
        { if (pageLayout != null) _pageLayout = pageLayout; return this; }

        public OfdWriter Add(Div item)
        { if (_streamQueue.Contains(item)) throw new ArgumentException("元素已经存在"); _streamQueue.Add(item); return this; }

        public OfdWriter Add(Paragraph paragraph)
        { if (_streamQueue.Contains(paragraph)) throw new ArgumentException("元素已经存在"); _streamQueue.Add(paragraph); return this; }

        public OfdWriter AddVirtualPage(VirtualPage virtualPage)
        { _virtualPageList.Add(virtualPage); return this; }

        public OfdWriter AddExternalEmbeddedFont(string fontName, string fontFilePath)
        {
            if (string.IsNullOrWhiteSpace(fontName)) throw new ArgumentNullException(nameof(fontName));
            if (string.IsNullOrWhiteSpace(fontFilePath) || !File.Exists(fontFilePath)) throw new FileNotFoundException("字体文件不存在", fontFilePath);
            _externalFontFiles[fontName] = fontFilePath;
            return this;
        }
        IOfdDocWriter IOfdDocWriter.AddExternalEmbeddedFont(string fontName, string fontFilePath) => AddExternalEmbeddedFont(fontName, fontFilePath);

        public OfdWriter AddRawTextGlyphRun(string fontName, double fontSizeMm, double originX, double originY, string text, double[]? deltaX = null, double[]? deltaY = null, int page = 1, double[]? ctm = null)
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
                Page = page < 1 ? 1 : page,
                CTM = NormalizeCtm(ctm)
            });
            return this;
        }
        IOfdDocWriter IOfdDocWriter.AddRawTextGlyphRun(string fontName, double fontSizeMm, double originX, double originY, string text, double[]? deltaX, double[]? deltaY, int page, double[]? ctm) => AddRawTextGlyphRun(fontName, fontSizeMm, originX, originY, text, deltaX, deltaY, page, ctm);

        public OfdWriter AddRawImage(string format, double x, double y, double width, double height, byte[] data, int page = 1, double[]? ctm = null)
        {
            if (data == null || data.Length == 0) { _logger?.LogDebug("[OFDDoc][Image] Skip empty image data Page={Page}", page); return this; }
            string hash;
            try { using var sha = SHA256.Create(); hash = Convert.ToHexString(sha.ComputeHash(data)); }
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
                IsFirstResource = first,
                CTM = NormalizeCtm(ctm)
            };
            _rawImages.Add(img);
            if (first) { _imageHashFirst[hash] = img; _logger?.LogDebug("[OFDDoc][Image] Page={Page} NewImage Hash={Hash} {W}x{H}mm Format={Fmt}", img.Page, hash[..Math.Min(12, hash.Length)], img.Width.ToString("0.##"), img.Height.ToString("0.##"), img.Format); }
            else { _logger?.LogDebug("[OFDDoc][Image] Page={Page} DupImage Hash={Hash}", img.Page, hash[..Math.Min(12, hash.Length)]); }
            return this;
        }

        public OfdWriter AddImage(OfdImage image)
        {
            if (image == null) return this;
            return AddRawImage(image.Format, image.X, image.Y, image.Width, image.Height, image.ImageData, image.Page, image.CTM);
        }

        public OfdWriter AddText(OfdText text)
        {
            if (text == null || string.IsNullOrEmpty(text.Text)) return this;
            return AddRawTextGlyphRun(text.FontFamily, text.FontSize, text.X, text.Y, text.Text, text.DeltaX?.Select(d => (double)d).ToArray(), null, text.Page, text.CTM);
        }

        /// <summary>
        /// 添加路径到OFD文档
        /// </summary>
        /// <param name="path">OFD路径对象</param>
        /// <returns>OfdWriter实例，用于链式调用</returns>
        public OfdWriter AddPath(OfdPath path)
        {
            if (path == null || string.IsNullOrEmpty(path.PathData)) return this;
            _rawPaths.Add(new RawPath
            {
                PathData = path.PathData,
                X = path.X,
                Y = path.Y,
                Width = path.Width,
                Height = path.Height,
                Page = path.Page,
                CTM = path.CTM
            });
            return this;
        }

        /// <summary>
        /// 添加注释到OFD文档
        /// </summary>
        /// <param name="annotation">注释对象</param>
        /// <param name="page">页码，从1开始</param>
        /// <returns>OfdWriter实例，用于链式调用</returns>
        public OfdWriter AddAnnotation(object annotation, int page = 1)
        {
            if (annotation == null) return this;
            _rawAnnotations.Add(new RawAnnotation
            {
                Annotation = annotation,
                Page = page < 1 ? 1 : page
            });
            return this;
        }
        IOfdDocWriter IOfdDocWriter.AddAnnotation(object annotation, int page) => AddAnnotation(annotation, page);

        /// <summary>
        /// 添加表单字段到OFD文档
        /// </summary>
        /// <param name="formField">表单字段对象</param>
        /// <returns>OfdWriter实例，用于链式调用</returns>
        public OfdWriter AddFormField(object formField)
        {
            // 暂时只是记录表单字段，稍后实现完整的表单处理
            if (formField == null) return this;
            _logger?.LogDebug("[OFDDoc][Form] 添加表单字段: {Type}", formField.GetType().Name);
            return this;
        }

        #region 辅助 - CTM
        private static double[]? NormalizeCtm(double[]? ctm)
        {
            if (ctm == null) return null;
            if (ctm.Length != 6) return null;
            // 如果是默认矩阵 1 0 0 1 0 0 则忽略，减少输出
            if (Math.Abs(ctm[0] - 1) < 1e-9 && Math.Abs(ctm[1]) < 1e-9 && Math.Abs(ctm[2]) < 1e-9 && Math.Abs(ctm[3] - 1) < 1e-9 && Math.Abs(ctm[4]) < 1e-9 && Math.Abs(ctm[5]) < 1e-9)
                return null;
            return ctm.ToArray();
        }
        private static string FormatCtm(double[] ctm)
            => string.Join(" ", ctm.Select(v => v.ToString("0.########")));
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
                if (_streamQueue.Count > 0 || _rawImages.Count > 0)
                {
                    // Simplified: treat all raw additions as a single virtual page for now
                    var vp = new VirtualPage(_pageLayout);
                    foreach (var item in _streamQueue)
                    {
                        // This part needs a proper layout engine to place elements.
                        // For now, we just add them to a virtual page.
                    }
                    _virtualPageList.Add(vp);
                }

                if (_virtualPageList.Count == 0) _virtualPageList.Add(new VirtualPage(_pageLayout));

                await GenerateDocumentAsync();
            }
            finally
            {
                _disposed = true;
                _ofdContainer.Dispose();
            }
        }

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
                await CreateOfdStructureAsync(tempDir);
                if (File.Exists(outputPath)) File.Delete(outputPath);

                // 检查输出路径是目录还是文件
                if (Directory.Exists(outputPath) || Path.GetExtension(outputPath).ToLowerInvariant() != ".ofd")
                {
                    // 如果是目录或没有.ofd扩展名，则复制文件到目录
                    if (!Directory.Exists(outputPath))
                    {
                        Directory.CreateDirectory(outputPath);
                    }
                    // 复制所有文件
                    foreach (var file in Directory.GetFiles(tempDir, "*", SearchOption.AllDirectories))
                    {
                        var relativePath = Path.GetRelativePath(tempDir, file);
                        var destFile = Path.Combine(outputPath, relativePath);
                        var destDir = Path.GetDirectoryName(destFile);
                        if (!string.IsNullOrEmpty(destDir) && !Directory.Exists(destDir)) Directory.CreateDirectory(destDir);
                        File.Copy(file, destFile, true);
                    }
                    _logger?.LogInformation("[OFDDoc][GenerateOfdContentAsync] OFD文件已复制到目录: {OutputPath}", outputPath);
                }
                else
                {
                    // 如果是.ofd文件，则创建ZIP
                    ZipFile.CreateFromDirectory(tempDir, outputPath);
                    _logger?.LogInformation("[OFDDoc][GenerateOfdContentAsync] OFD文件已创建: {OutputPath}", outputPath);
                }
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

        private async Task CreateOfdStructureAsync(string baseDir)
        {
            ComputePageGroups();
            _logger?.LogDebug("[OFD][CreateStructure] Pages={PageCount}", _pageGroups?.Count);
            var ofdXml = "<?xml version=\"1.0\" encoding=\"UTF-8\"?>\n" +
                         "<ofd:OFD xmlns:ofd=\"http://www.ofdspec.org/2016\" DocType=\"OFD\" Version=\"1.1\">\n" +
                         "    <ofd:DocBody>\n" +
                         "        <ofd:DocInfo>\n" +
                         $"            <ofd:DocID>{Guid.NewGuid()}</ofd:DocID>\n" +
                         "        </ofd:DocInfo>\n" +
                         "        <ofd:DocRoot>Doc/Document.xml</ofd:DocRoot>\n" +
                         "    </ofd:DocBody>\n" +
                         "</ofd:OFD>";
            await WriteTextFileUtf8LfAsync(Path.Combine(baseDir, "OFD.xml"), ofdXml);
            var docDir = Path.Combine(baseDir, "Doc"); Directory.CreateDirectory(docDir);
            _logger?.LogInformation("[OFDDoc][CreateOfdStructureAsync] 准备调用GenerateResourcesAsync DocDir={DocDir}", docDir);
            await GenerateResourcesAsync(docDir);
            _logger?.LogInformation("[OFDDoc][CreateOfdStructureAsync] GenerateResourcesAsync调用完成");
            var pagesDir = Path.Combine(docDir, "Pages"); Directory.CreateDirectory(pagesDir);
            // 先生成页面文件（页面和内容会为对象分配ID），再生成 Document.xml，以便 MaxUnitID 能覆盖所有ID
            await GeneratePageFilesAsync(pagesDir);
            var documentXml = await GenerateDocumentXmlAsync();
            await WriteTextFileUtf8LfAsync(Path.Combine(docDir, "Document.xml"), documentXml);
            _logger?.LogInformation("[OFDDoc][CreateOfdStructureAsync] 准备调用PostGenerationFixAsync");
            await PostGenerationFixAsync(baseDir);
            _logger?.LogInformation("[OFDDoc][CreateOfdStructureAsync] PostGenerationFixAsync调用完成");
        }

        private static readonly XNamespace OfdNs = "http://www.ofdspec.org/2016"; // 新增：统一命名空间
        private async Task<string> GenerateDocumentXmlAsync()
        {
            if (_pageGroups == null) ComputePageGroups();
            if (_pageGroups == null) _pageGroups = new List<(int PageNumber, List<object> Items)>();
            // 先为每一页预分配ID，保证 MaxUnitID 能覆盖页面ID
            var orderedPages = _pageGroups.OrderBy(p => p.PageNumber).ToList();
            var pageIds = new List<int>();
            foreach (var pg in orderedPages)
            {
                pageIds.Add(GetNextID());
            }
            // 资源ID可能在 GenerateResourcesAsync 中分配，_maxUnitID 已随 GetNextID 增加
            var pagesXml = new StringBuilder();
            for (int i = 0; i < orderedPages.Count; i++)
            {
                var pg = orderedPages[i];
                var pid = pageIds[i];
                pagesXml.AppendLine($"        <ofd:Page ID=\"{pid}\" BaseLoc=\"Pages/Page_{pg.PageNumber}/Content.xml\"/>");
            }
            var sb = new StringBuilder();
            sb.AppendLine("<?xml version=\"1.0\" encoding=\"UTF-8\"?>");
            sb.AppendLine("<ofd:Document xmlns:ofd=\"http://www.ofdspec.org/2016\">");
            sb.AppendLine("    <ofd:CommonData>");
            sb.AppendLine($"        <ofd:MaxUnitID>{_maxUnitID}</ofd:MaxUnitID>");
            sb.AppendLine("        <ofd:PageArea>");
            sb.AppendLine($"            <ofd:PhysicalBox>0 0 {_pageLayout.Width:0.###} {_pageLayout.Height:0.###}</ofd:PhysicalBox>");
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
        {
            if (_fontMap.Count == 0) BuildFontMap();
            if (_pageGroups == null) ComputePageGroups();
            var pageGroupsLocal = _pageGroups ?? new List<(int PageNumber, List<object> Items)>();
            foreach (var pg in pageGroupsLocal)
            {
                await CreatePageFileAsync(pagesDir, pg.PageNumber, pg.Items);
            }
        }

        private async Task CreatePageFileAsync(string pagesDir, int pageNumber, List<object> items)
        {
            var contentXml = new StringBuilder();
            var imagesOnPageList = _rawImages.Where(i => i.Page == pageNumber).ToList();
            if (imagesOnPageList.Count > 0)
            { _logger?.LogDebug("[OFDDoc][PageWrite] Page={Page} ImageCount={Count} RIDs={RIDs}", pageNumber, imagesOnPageList.Count, string.Join(',', imagesOnPageList.Select(i => i.ResourceID))); }
            else
            { _logger?.LogDebug("[OFDDoc][PageWrite] Page={Page} ImageCount=0", pageNumber); }
            // T023: 修复CTM/Boundary语义 - 分离定位和变换
            foreach (var img in imagesOnPageList)
            {
                if (img.ResourceID == 0) _logger?.LogWarning("[OFDDoc][ImageDiag] Page={Page} Image Hash={Hash} ResourceID=0 (未分配)", pageNumber, img.Hash[..Math.Min(12, img.Hash.Length)]);
                string boundary = $"{img.X:0.###} {img.Y:0.###} {img.Width:0.###} {img.Height:0.###}";

                // T023: CTM应该只包含缩放/翻转，不包含平移。定位由Boundary控制
                string ctmAttr = string.Empty;
                if (img.CTM != null && img.CTM.Length >= 6)
                {
                    // 保留缩放和翻转（a, b, c, d），但将平移分量（e, f）设为0
                    var fixedCtm = new double[6] { img.CTM[0], img.CTM[1], img.CTM[2], img.CTM[3], 0, 0 };
                    ctmAttr = $" CTM=\"{FormatCtm(fixedCtm)}\"";
                }

                contentXml.AppendLine($"        <ofd:ImageObject ID=\"{GetNextID()}\" ResourceID=\"{img.ResourceID}\" Boundary=\"{boundary}\"{ctmAttr}/>");
            }
            var pathsOnPageList = _rawPaths.Where(p => p.Page == pageNumber).ToList();
            if (pathsOnPageList.Count > 0)
            { _logger?.LogDebug("[OFDDoc][PageWrite] Page={Page} PathCount={Count}", pageNumber, pathsOnPageList.Count); }

            // T024: 修复图层对象顺序 - 正确顺序：ImageObject → PathObject → TextObject
            // 分离不同类型的对象以确保正确顺序
            var pathItems = items.OfType<RawPath>().ToList();
            var textItems = items.OfType<RawGlyphRun>().ToList();

            // 第二步：PathObject (在ImageObject之后，TextObject之前)
            foreach (var pathItem in pathItems)
            {
                string boundary = $"{pathItem.X:0.###} {pathItem.Y:0.###} {pathItem.Width:0.###} {pathItem.Height:0.###}";
                var ctmAttr = pathItem.CTM != null ? $" CTM=\"{FormatCtm(pathItem.CTM)}\"" : string.Empty;
                contentXml.AppendLine($"        <ofd:PathObject ID=\"{GetNextID()}\" Boundary=\"{boundary}\"{ctmAttr}><ofd:StrokeColor><ofd:Value>0 0 0</ofd:Value></ofd:StrokeColor><ofd:AbbreviatedData>{pathItem.PathData}</ofd:AbbreviatedData></ofd:PathObject>");
            }

            // 第三步：TextObject (最后输出，避免被其他对象遮挡)
            foreach (var run in textItems)
            {
                if (string.IsNullOrEmpty(run.Text)) continue;
                if (!_fontMap.TryGetValue(run.FontName, out var font)) font = _fontMap.Values.First();
                var textContent = System.Security.SecurityElement.Escape(run.Text);
                int textLen = run.Text?.Length ?? 0;
                double width = (run.DeltaX?.Sum() ?? (textLen * run.FontSizeMm * 0.6));

                // T025: 修复TextCode Y坐标计算，考虑字体ascent防止裁切
                // TextCode Y是相对于Boundary的坐标，需要为ascent预留空间
                // 为文本提供合理的边距，防止ascent和descent被裁切
                var fontAscentRatio = 0.85; // 典型字体ascent约为字体大小的85%
                var lineHeightFactor = 1.2; // 行高为字体大小的120%，提供合理边距

                var ascentHeight = run.FontSizeMm * fontAscentRatio;
                var totalTextHeight = run.FontSizeMm * lineHeightFactor; // 使用120%行高
                var textCodeY = ascentHeight; // 基线位置：从顶部预留ascent空间

                string boundary = $"{run.X:0.###} {run.Y:0.###} {width:0.###} {totalTextHeight:0.###}";
                var textCodeAttrs = $"X=\"0\" Y=\"{textCodeY.ToString("0.######")}\"" + (run.DeltaX != null && run.DeltaX.Length > 0 ? $" DeltaX=\"{string.Join(" ", run.DeltaX.Select(d => d.ToString("0.###")))}\"" : string.Empty);
                var ctmAttr = run.CTM != null ? $" CTM=\"{FormatCtm(run.CTM)}\"" : string.Empty;
                var textObjectStr = $"<ofd:TextObject ID=\"{GetNextID()}\" Font=\"{font.ID}\" Size=\"{run.FontSizeMm.ToString("0.##")}\" Boundary=\"{boundary}\"{ctmAttr}><ofd:TextCode {textCodeAttrs}>{textContent}</ofd:TextCode></ofd:TextObject>";
                contentXml.AppendLine("        " + textObjectStr);
            }
            var pageDir = Path.Combine(pagesDir, $"Page_{pageNumber}");
            if (!Directory.Exists(pageDir)) Directory.CreateDirectory(pageDir);
            var pageXml = new StringBuilder();
            pageXml.AppendLine("<?xml version=\"1.0\" encoding=\"UTF-8\"?>");
            pageXml.AppendLine("<ofd:Page xmlns:ofd=\"http://www.ofdspec.org/2016\">");
            pageXml.AppendLine("    <ofd:Area>");
            pageXml.AppendLine($"        <ofd:PhysicalBox>0 0 {_pageLayout.Width:0.###} {_pageLayout.Height:0.###}</ofd:PhysicalBox>");
            pageXml.AppendLine("    </ofd:Area>");
            pageXml.AppendLine("    <ofd:Content>");
            pageXml.AppendLine("        <ofd:Layer ID=\"1\"> ");
            pageXml.Append(contentXml.ToString());
            pageXml.AppendLine("        </ofd:Layer>");
            pageXml.AppendLine("    </ofd:Content>");
            pageXml.AppendLine("</ofd:Page>");
            await WriteTextFileUtf8LfAsync(Path.Combine(pageDir, "Content.xml"), pageXml.ToString());
        }

        private void BuildFontMap()
        {
            _fontMap.Clear(); int id = 1;
            var fontNames = _streamQueue.OfType<RawGlyphRun>().Select(rg => rg.FontName)
                                      .Concat(_externalFontFiles.Keys)
                                      .Distinct(StringComparer.OrdinalIgnoreCase);

            foreach (var name in fontNames)
            {
                if (!_fontMap.ContainsKey(name))
                {
                    _fontMap[name] = new OfdFont(id++, name, name);
                }
            }
            if (_fontMap.Count == 0) _fontMap["SimSun"] = new OfdFont(1, "SimSun", "宋体");
        }

        private void ComputePageGroups()
        {
            if (_pageGroups != null) return;
            var runs = _streamQueue.OfType<RawGlyphRun>().ToList();
            int maxPage = 0;
            if (_virtualPageList?.Count > 0) maxPage = Math.Max(maxPage, _virtualPageList.Count);
            if (runs.Any()) maxPage = Math.Max(maxPage, runs.Max(r => r.Page));
            if (_rawImages != null && _rawImages.Any()) maxPage = Math.Max(maxPage, _rawImages.Max(i => i.Page));
            if (_rawPaths != null && _rawPaths.Any()) maxPage = Math.Max(maxPage, _rawPaths.Max(p => p.Page));
            if (maxPage == 0) maxPage = 1;
            var pages = new List<(int PageNumber, List<object> Items)>();
            for (int p = 1; p <= maxPage; p++)
            {
                var items = new List<object>();
                items.AddRange(runs.Where(r => r.Page == p));
                if (_rawPaths != null) items.AddRange(_rawPaths.Where(pth => pth.Page == p));
                if (p == 1)
                {
                    items.AddRange(_streamQueue.Where(s => !(s is RawGlyphRun)));
                }
                pages.Add((p, items));
            }
            _pageGroups = pages;
            _logger?.LogDebug("[OFDDoc][ComputePageGroups] Pages={Count} Runs={Runs} Images={Images} Paths={Paths} VirtualPages={VP}", _pageGroups!.Count, runs.Count, _rawImages?.Count ?? 0, _rawPaths?.Count ?? 0, _virtualPageList?.Count ?? 0);
        }

        private async Task GenerateResourcesAsync(string docDir)
        {
            _logger?.LogInformation("[OFDDoc][GenerateResourcesAsync] 开始生成资源文件 DocDir={DocDir}", docDir);
            BuildFontMap();
            _logger?.LogInformation("[OFDDoc][GenerateResourcesAsync] 字体映射构建完成 FontCount={FontCount}", _fontMap.Count);
            var resDir = Path.Combine(docDir, "Res");
            if (!Directory.Exists(resDir)) Directory.CreateDirectory(resDir);
            _publicResRelativePath = "PublicRes.xml";
            _documentResRelativePath = "DocumentRes.xml";
            var ofd = OfdNs; var nsDecl = new XAttribute(XNamespace.Xmlns + "ofd", ofd.NamespaceName);
            var publicRes = new XElement(ofd + "Res", nsDecl, new XAttribute("BaseLoc", "Res"));
            var fontsElement = new XElement(ofd + "Fonts");
            foreach (var font in _fontMap.Values)
            {
                XElement fontEl;
                if (_externalFontFiles.TryGetValue(font.FontName, out var fontFile))
                {
                    var destFontFile = Path.Combine(resDir, Path.GetFileName(fontFile));
                    File.Copy(fontFile, destFontFile, true);
                    fontEl = new XElement(ofd + "Font",
                        new XAttribute("ID", font.ID),
                        new XAttribute("FamilyName", font.FamilyName),
                        new XAttribute("FontName", font.FontName),
                        new XAttribute("Serif", "true")
                    );
                    fontEl.Add(new XElement(ofd + "FontFile", Path.GetFileName(fontFile)));
                }
                else
                {
                    fontEl = new XElement(ofd + "Font",
                        new XAttribute("ID", font.ID),
                        new XAttribute("FamilyName", font.FamilyName),
                        new XAttribute("FontName", font.FontName)
                    );
                }
                fontsElement.Add(fontEl);
            }
            publicRes.Add(fontsElement);
            await WriteTextFileUtf8LfAsync(Path.Combine(docDir, _publicResRelativePath), "<?xml version=\"1.0\" encoding=\"utf-8\"?>\n" + publicRes.ToString(SaveOptions.None));
            _logger?.LogInformation("[OFDDoc][GenerateResourcesAsync] PublicRes.xml 生成完成 FontCount={FontCount}", _fontMap.Count);
            var documentRes = new XElement(ofd + "Res", nsDecl, new XAttribute("BaseLoc", "Res"));
            var multiMedias = new XElement(ofd + "MultiMedias");
            if (_rawImages.Any())
            {
                int nextId = (_fontMap.Values.Any() ? _fontMap.Values.Max(f => f.ID) : 0) + 1;
                foreach (var first in _rawImages.Where(r => r.IsFirstResource)) if (first.ResourceID == 0) first.ResourceID = nextId++;
                var map = _rawImages.Where(r => r.IsFirstResource).ToDictionary(r => r.Hash, r => r.ResourceID);
                foreach (var dup in _rawImages.Where(r => !r.IsFirstResource)) if (map.TryGetValue(dup.Hash, out var rid)) dup.ResourceID = rid; else if (dup.ResourceID == 0) dup.ResourceID = nextId++;
                foreach (var img in _rawImages.Where(r => r.IsFirstResource).OrderBy(i => i.ResourceID))
                {
                    // 保持原始 format 大小写（用于文件扩展名与 Format 属性），但对 JPEG 规范化为 JPG
                    var fmt = img.Format?.TrimStart('.') ?? "PNG";
                    var fmtUpper = fmt.ToUpperInvariant();
                    if (fmtUpper == "JPEG") fmtUpper = "JPG";
                    // If TIFF, try to convert to PNG to avoid .tif files in output
                    if (fmtUpper == "TIFF" || fmtUpper == "TIF")
                    {
                        try
                        {
                            using var image = Image.Load(img.Data);
                            using var msOut = new MemoryStream();
                            image.Save(msOut, new PngEncoder());
                            img.Data = msOut.ToArray();
                            fmtUpper = "PNG";
                            _logger?.LogInformation("[OFDDoc][ImageConvert] ResourceID={Rid} TIFF -> PNG 转换成功", img.ResourceID);
                        }
                        catch (Exception ex)
                        {
                            _logger?.LogWarning(ex, "[OFDDoc][ImageConvert] ResourceID={Rid} TIFF -> PNG 转换失败，使用透明占位PNG", img.ResourceID);
                            img.Data = new byte[] {
                                0x89,0x50,0x4E,0x47,0x0D,0x0A,0x1A,0x0A,0x00,0x00,0x00,0x0D,0x49,0x48,0x44,0x52,
                                0x00,0x00,0x00,0x01,0x00,0x00,0x00,0x01,0x08,0x06,0x00,0x00,0x00,0x1F,0x15,0xC4,
                                0x89,0x00,0x00,0x00,0x0A,0x49,0x44,0x41,0x54,0x78,0x9C,0x63,0x00,0x01,0x00,0x00,
                                0x05,0x00,0x01,0x0D,0x0A,0x2D,0xB4,0x00,0x00,0x00,0x00,0x49,0x45,0x4E,0x44,0xAE,
                                0x42,0x60,0x82 };
                            fmtUpper = "PNG";
                        }
                    }
                    // 输出文件名使用小写扩展名以提高兼容性（部分解析器对大小写敏感）
                    var ext = "." + fmtUpper.ToLowerInvariant(); // e.g. .png 或 .jpg
                    string fileName = $"Image_{img.ResourceID}{ext}"; // Image_10.png
                    var dest = Path.Combine(resDir, fileName);
                    if (img.Data.Length > 0)
                    {
                        await File.WriteAllBytesAsync(dest, img.Data);
                        _logger?.LogDebug("[OFDDoc][ImageWrite] 写入资源文件 Path={Path} ResourceID={Rid} Format={Fmt}", dest, img.ResourceID, fmtUpper);
                    }
                    multiMedias.Add(new XElement(ofd + "MultiMedia",
                        new XAttribute("ID", img.ResourceID),
                        new XAttribute("Type", "Image"),
                        new XAttribute("Format", fmtUpper),
                        new XElement(ofd + "MediaFile", fileName)));
                }
            }
            documentRes.Add(multiMedias);
            await WriteTextFileUtf8LfAsync(Path.Combine(docDir, _documentResRelativePath), "<?xml version=\"1.0\" encoding=\"utf-8\"?>\n" + documentRes.ToString(SaveOptions.None));
            _logger?.LogInformation("[OFDDoc][GenerateResourcesAsync] DocumentRes.xml 生成完成 ImageResCount={Cnt}", multiMedias.Elements().Count());
            // 更新 MaxUnitID
            var maxFont = _fontMap.Values.Any() ? _fontMap.Values.Max(f => f.ID) : 0; var maxImg = _rawImages.Any() ? _rawImages.Max(i => i.ResourceID) : 0; _maxUnitID = Math.Max(_maxUnitID, Math.Max(maxFont, maxImg));
            _logger?.LogInformation("[OFDDoc][GenerateResourcesAsync] 资源文件生成完毕 MaxUnitID={Max}", _maxUnitID);
        }

        private async Task PostGenerationFixAsync(string baseDir)
        {
            // Simplified, as the new generation logic should be more correct.
            await Task.CompletedTask;
        }

        #endregion
        public void Dispose()
        {
            if (!_disposed)
            {
                _ofdContainer?.Dispose();
                _disposed = true;
            }
        }

        /// <summary>字体资源描述。</summary>
        public class OfdFont
        {
            /// <summary>字体资源ID。</summary>
            public int ID { get; }
            /// <summary>字体名称（FontName）。</summary>
            public string FontName { get; }
            /// <summary>字体族名（FamilyName）。</summary>
            public string FamilyName { get; }
            /// <summary>构造新的字体资源描述。</summary>
            public OfdFont(int id, string fontName, string familyName)
            {
                ID = id;
                FontName = fontName;
                FamilyName = familyName;
            }
        }

        /// <summary>虚拟页面容器（占位）。</summary>
        public class VirtualPage
        {
            /// <summary>页面布局。</summary>
            public PageLayout Layout { get; }
            /// <summary>构造虚拟页面。</summary>
            public VirtualPage(PageLayout layout) { Layout = layout; }
        }

        // 兼容类型别名（已移至命名空间级别）
    }

    #endregion

    [Obsolete("Use OfdWriter instead. This shim will be removed in future releases.")]
    public class OFDDoc : OfdWriter
    {
        public OFDDoc(string outPath, ILogger? logger = null) : base(outPath, logger) { }
        public OFDDoc(Stream outStream, ILogger? logger = null) : base(outStream, logger) { }
    }
}
