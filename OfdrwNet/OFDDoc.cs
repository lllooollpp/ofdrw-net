using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using System.IO.Compression;
using System.Text;

namespace OfdrwNet;

/// <summary>
/// OFD文档主要API类
/// 对应Java版本的org.ofdrw.layout.OFDDoc
/// 提供OFD文档的创建、编辑和操作功能
/// </summary>
public class OFDDoc : IDisposable
{
    #region 私有字段

    /// <summary>
    /// 打包后OFD文档存放路径
    /// </summary>
    private readonly string? _outPath;

    /// <summary>
    /// 打包后OFD文档输出流
    /// </summary>
    private readonly Stream? _outStream;

    /// <summary>
    /// 当前文档中所有对象使用标识的最大值
    /// </summary>
    private int _maxUnitID;

    /// <summary>
    /// 页面布局设置 (默认A4)
    /// </summary>
    private PageLayout _pageLayout = PageLayout.A4();

    /// <summary>
    /// 文档是否已关闭
    /// </summary>
    private bool _disposed = false;

    /// <summary>
    /// 流式布局元素队列
    /// </summary>
    private readonly List<OfdrwNet.Layout.Element.Div> _streamQueue = new();

    /// <summary>
    /// 虚拟页面队列
    /// </summary>
    private readonly List<VirtualPage> _virtualPageList = new();

    private Dictionary<string, int> _fontMap = new();
    private string? _publicResRelativePath; // 例如 Res/PublicRes.xml

    #endregion

    #region 构造函数

    /// <summary>
    /// 在指定路径创建新的OFD文档
    /// </summary>
    /// <param name="outPath">OFD输出路径</param>
    /// <exception cref="ArgumentException">路径无效时抛出</exception>
    public OFDDoc(string outPath)
    {
        if (string.IsNullOrEmpty(outPath))
            throw new ArgumentException("OFD文件存储路径不能为空", nameof(outPath));

        var directory = Path.GetDirectoryName(Path.GetFullPath(outPath));
        if (!string.IsNullOrEmpty(directory) && !Directory.Exists(directory))
            throw new ArgumentException($"OFD文件存储路径的上级目录不存在: {directory}");

        _outPath = outPath;
        InitializeContainer();
    }

    /// <summary>
    /// 向指定流创建新的OFD文档
    /// </summary>
    /// <param name="outStream">OFD输出流</param>
    /// <exception cref="ArgumentNullException">输出流为null时抛出</exception>
    public OFDDoc(Stream outStream)
    {
        _outStream = outStream ?? throw new ArgumentNullException(nameof(outStream));
        InitializeContainer();
    }

    #endregion

    #region 私有初始化方法

    /// <summary>
    /// 初始化新文档容器
    /// </summary>
    private void InitializeContainer()
    {
        _maxUnitID = 0;
        // 创建文档基础结构
        // 这里应该创建基本的OFD文档结构，包括文档信息、页面等
        // 具体实现需要根据OfdrwNet.Core和OfdrwNet.Packaging模块来完成
    }

    #endregion

    #region 公共属性

    /// <summary>
    /// 获取页面布局配置 (只读)
    /// </summary>
    public PageLayout PageLayout => _pageLayout.Clone();

    /// <summary>
    /// 获取当前最大对象ID
    /// </summary>
    public int MaxUnitID => _maxUnitID;

    #endregion

    #region 页面布局管理

    /// <summary>
    /// 设置默认页面布局
    /// </summary>
    /// <param name="pageLayout">页面布局</param>
    /// <returns>当前实例</returns>
    public OFDDoc SetDefaultPageLayout(PageLayout pageLayout)
    {
        if (pageLayout != null)
        {
            _pageLayout = pageLayout;
        }
        return this;
    }

    #endregion

    #region 内容添加

    /// <summary>
    /// 向文档中添加流式布局元素
    /// </summary>
    /// <param name="item">布局元素</param>
    /// <returns>当前实例</returns>
    /// <exception cref="ArgumentException">元素重复时抛出</exception>
    public OFDDoc Add(OfdrwNet.Layout.Element.Div item)
    {
        if (_streamQueue.Contains(item))
            throw new ArgumentException("元素已经存在，请勿重复添加");

        _streamQueue.Add(item);
        return this;
    }

    /// <summary>
    /// 向文档中添加虚拟页面 (固定布局)
    /// </summary>
    /// <param name="virtualPage">虚拟页面</param>
    /// <returns>当前实例</returns>
    public OFDDoc AddVirtualPage(VirtualPage virtualPage)
    {
        _virtualPageList.Add(virtualPage);
        return this;
    }

    #endregion

    #region ID管理

    /// <summary>
    /// 生成下一个对象ID
    /// </summary>
    /// <returns>新的对象ID</returns>
    public int GetNextID()
    {
        return Interlocked.Increment(ref _maxUnitID);
    }

    #endregion

    #region 文档关闭和处理

    /// <summary>
    /// 关闭文档并生成OFD文件
    /// </summary>
    /// <returns>异步任务</returns>
    public async Task CloseAsync()
    {
        if (_disposed) return;

        try
        {
            // 处理流式布局转换为固定布局
            if (_streamQueue.Count > 0)
            {
                await ProcessStreamLayoutAsync();
            }

            // 处理虚拟页面
            if (_virtualPageList.Count > 0)
            {
                await ProcessVirtualPagesAsync();
            }

            // 验证文档是否有内容，如果没有内容则创建空文档
            if (_streamQueue.Count == 0 && _virtualPageList.Count == 0)
            {
                // 为空文档创建一个默认页面
                _virtualPageList.Add(new VirtualPage());
            }

            // 生成文档
            await GenerateDocumentAsync();
        }
        finally
        {
            _disposed = true;
        }
    }

    /// <summary>
    /// 处理流式布局
    /// </summary>
    private async Task ProcessStreamLayoutAsync()
    {
        // 实现流式布局处理逻辑
        await Task.CompletedTask; // 占位符
    }

    /// <summary>
    /// 处理虚拟页面
    /// </summary>
    private async Task ProcessVirtualPagesAsync()
    {
        // 实现虚拟页面处理逻辑
        await Task.CompletedTask; // 占位符
    }

    /// <summary>
    /// 生成最终文档
    /// </summary>
    private async Task GenerateDocumentAsync()
    {
        Console.WriteLine($"[DEBUG] GenerateDocumentAsync 开始，_streamQueue.Count = {_streamQueue.Count}");
        
        if (!string.IsNullOrEmpty(_outPath))
        {
            Console.WriteLine($"[DEBUG] 开始生成OFD文件到路径: {_outPath}");
            // 生成包含实际内容的OFD文件
            await GenerateOfdContentAsync(_outPath);
            Console.WriteLine($"[DEBUG] OFD文件生成完成");
        }
        else if (_outStream != null)
        {
            Console.WriteLine($"[DEBUG] 开始生成OFD内容到流");
            // 向流写入实际的OFD内容
            await GenerateOfdContentAsync(_outStream);
            Console.WriteLine($"[DEBUG] OFD流写入完成");
        }
        else
        {
            throw new InvalidOperationException("未设置文档输出路径或输出流");
        }
    }

    /// <summary>
    /// 生成OFD文档内容到文件
    /// </summary>
    private async Task GenerateOfdContentAsync(string outputPath)
    {
        Console.WriteLine($"[DEBUG] GenerateOfdContentAsync 开始，输出路径: {outputPath}");
        
        // 创建简化的OFD文档结构
        var tempDir = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());
        Console.WriteLine($"[DEBUG] 创建临时目录: {tempDir}");
        Directory.CreateDirectory(tempDir);
        
        try
        {
            Console.WriteLine($"[DEBUG] 开始创建OFD结构");
            // 使用新的基础结构类创建OFD文档
            await CreateOfdStructureWithNewClassesAsync(tempDir);
            Console.WriteLine($"[DEBUG] OFD结构创建完成");
            
            Console.WriteLine($"[DEBUG] 开始打包ZIP文件");
            // 打包为ZIP格式的OFD文件
            ZipFile.CreateFromDirectory(tempDir, outputPath);
            Console.WriteLine($"[DEBUG] ZIP文件打包完成");
            
            // 检查输出文件
            if (File.Exists(outputPath))
            {
                var fileInfo = new FileInfo(outputPath);
                Console.WriteLine($"[DEBUG] 输出文件已创建，大小: {fileInfo.Length} 字节");
            }
            else
            {
                Console.WriteLine($"[DEBUG] 警告：输出文件未创建");
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[DEBUG] 生成OFD文件时出错: {ex.Message}");
            Console.WriteLine($"[DEBUG] 堆栈跟踪: {ex.StackTrace}");
            throw;
        }
        finally
        {
            // 清理临时目录
            if (Directory.Exists(tempDir))
            {
                Console.WriteLine($"[DEBUG] 清理临时目录: {tempDir}");
                Directory.Delete(tempDir, true);
            }
        }
    }

    /// <summary>
    /// 生成OFD文档内容到流
    /// </summary>
    private async Task GenerateOfdContentAsync(Stream outputStream)
    {
        var tempDir = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());
        Directory.CreateDirectory(tempDir);
        
        try
        {
            await CreateOfdStructureAsync(tempDir);
            
            using var archive = new System.IO.Compression.ZipArchive(outputStream, System.IO.Compression.ZipArchiveMode.Create, true);
            foreach (var file in Directory.GetFiles(tempDir, "*", SearchOption.AllDirectories))
            {
                var entryName = Path.GetRelativePath(tempDir, file).Replace('\\', '/');
                var entry = archive.CreateEntry(entryName);
                using var entryStream = entry.Open();
                using var fileStream = File.OpenRead(file);
                await fileStream.CopyToAsync(entryStream);
            }
        }
        finally
        {
            if (Directory.Exists(tempDir))
            {
                Directory.Delete(tempDir, true);
            }
        }
    }

    /// <summary>
    /// 创建OFD文档文件结构
    /// </summary>
    private async Task CreateOfdStructureAsync(string baseDir)
    {
        Console.WriteLine($"[DEBUG] CreateOfdStructureAsync 开始，基本目录: {baseDir}");
        
        // 创建OFD.xml根文件
        var ofdXml = $@"<?xml version=""1.0"" encoding=""UTF-8""?>
<ofd:OFD xmlns:ofd=""http://www.ofdspec.org/2016"" DocType=""OFD"" Version=""1.0"">
    <ofd:DocBody>
        <ofd:DocInfo DocID=""1"">
            <ofd:DocRoot>Doc/Document.xml</ofd:DocRoot>
        </ofd:DocInfo>
    </ofd:DocBody>
</ofd:OFD>";
        var ofdXmlPath = Path.Combine(baseDir, "OFD.xml");
        await File.WriteAllTextAsync(ofdXmlPath, ofdXml, Encoding.UTF8);
        Console.WriteLine($"[DEBUG] 创建 OFD.xml: {ofdXmlPath} ({new FileInfo(ofdXmlPath).Length} 字节)");
        
        // 创建Doc目录
        var docDir = Path.Combine(baseDir, "Doc");
        Directory.CreateDirectory(docDir);
        Console.WriteLine($"[DEBUG] 创建Doc目录: {docDir}");
        
        // 新增：生成公共资源(字体)文件
        Console.WriteLine("[DEBUG] 生成公共资源 PublicRes.xml ...");
        await GenerateResourcesAsync(docDir);
        Console.WriteLine("[DEBUG] PublicRes.xml 生成完成");

        // 创建Document.xml
        var documentXml = await GenerateDocumentXmlAsync();
        var documentXmlPath = Path.Combine(docDir, "Document.xml");
        await File.WriteAllTextAsync(documentXmlPath, documentXml, Encoding.UTF8);
        Console.WriteLine($"[DEBUG] 创建 Document.xml: {documentXmlPath} ({new FileInfo(documentXmlPath).Length} 字节)");
        
        // 创建Pages目录和页面文件
        var pagesDir = Path.Combine(docDir, "Pages");
        Directory.CreateDirectory(pagesDir);
        Console.WriteLine($"[DEBUG] 创建Pages目录: {pagesDir}");
        
        await GeneratePageFilesAsync(pagesDir);
        Console.WriteLine($"[DEBUG] CreateOfdStructureAsync 完成");
    }

    /// <summary>
    /// 生成Document.xml内容
    /// </summary>
    private async Task<string> GenerateDocumentXmlAsync()
    {
        var pageCount = Math.Max(1, (_streamQueue.Count + 19) / 20); // 假设每页20个段落
        var pagesXml = new StringBuilder();
        for (int i = 1; i <= pageCount; i++)
        {
            pagesXml.AppendLine($"        <ofd:Page ID=\"{i}\" BaseLoc=\"Pages/Page_{i}.xml\"/>");
        }
        var commonDataExtra = new StringBuilder();
        if (!string.IsNullOrEmpty(_publicResRelativePath))
        {
            commonDataExtra.AppendLine($"        <ofd:PublicRes>{_publicResRelativePath}</ofd:PublicRes>");
        }
        var documentXml = $@"<?xml version=""1.0"" encoding=""UTF-8""?>
<ofd:Document xmlns:ofd=""http://www.ofdspec.org/2016"">
    <ofd:CommonData>
        <ofd:PageArea>
            <ofd:PhysicalBox>0 0 {_pageLayout.Width} {_pageLayout.Height}</ofd:PhysicalBox>
        </ofd:PageArea>
{commonDataExtra.ToString().TrimEnd()}
    </ofd:CommonData>
    <ofd:Pages>
{pagesXml.ToString().TrimEnd()}
    </ofd:Pages>
</ofd:Document>";
        
        await Task.CompletedTask;
        return documentXml;
    }

    /// <summary>
    /// 生成页面文件
    /// </summary>
    private async Task GeneratePageFilesAsync(string pagesDir)
    {
        // 生成字体映射（如果尚未生成，通过段落字体统计）
        if (_fontMap.Count == 0)
        {
            BuildFontMap();
        }
        
        if (_streamQueue.Count == 0)
        {
            // 创建空页面
            await CreatePageFileAsync(pagesDir, 1, new List<OfdrwNet.Layout.Element.Div>());
            return;
        }
        
        var pageNumber = 1;
        var currentPageItems = new List<OfdrwNet.Layout.Element.Div>();
        
        foreach (var item in _streamQueue)
        {
            currentPageItems.Add(item);
            
            // 每20个段落创建一页
            if (currentPageItems.Count >= 20)
            {
                await CreatePageFileAsync(pagesDir, pageNumber, currentPageItems);
                pageNumber++;
                currentPageItems.Clear();
            }
        }
        
        // 处理剩余的内容
        if (currentPageItems.Count > 0)
        {
            await CreatePageFileAsync(pagesDir, pageNumber, currentPageItems);
        }
    }

    /// <summary>
    /// 创建单个页面文件
    /// </summary>
    private async Task CreatePageFileAsync(string pagesDir, int pageNumber, List<OfdrwNet.Layout.Element.Div> items)
    {
        var contentXml = new StringBuilder();
        double currentY = _pageLayout.Margins.Top;
        
        foreach (var item in items)
        {
            if (item is OfdrwNet.Layout.Element.Paragraph paragraph)
            {
                var textContent = System.Security.SecurityElement.Escape(paragraph.Text ?? "");
                var fontName = paragraph.FontName ?? "SimSun";
                if (!_fontMap.TryGetValue(fontName, out var fontId))
                {
                    fontId = 1; // fallback
                }
                double fontSize = paragraph.FontSize > 0 ? paragraph.FontSize : 12.0;
                double lineHeight = paragraph.LineHeight > 0 ? paragraph.LineHeight : 1.2;
                double estimatedHeight = fontSize * lineHeight;
                // 估算宽度（粗略）：字符数 * 字号 * 0.6
                double estimatedWidth = textContent.Length * fontSize * 0.6;
                // Boundary: x y w h 采用 mm 单位
                double x = _pageLayout.Margins.Left;
                double y = currentY; // 简单递增
                string boundary = $"{x:0.###} {y:0.###} {estimatedWidth:0.###} {estimatedHeight:0.###}";
                // TextObject：加入 Font, Size, Boundary
                contentXml.AppendLine(
                    $"        <ofd:TextObject ID=\"{GetNextID()}\" Font=\"{fontId}\" Size=\"{fontSize:0.##}\" Boundary=\"{boundary}\" CTM=\"1 0 0 1 0 0\">" +
                    $"<ofd:TextCode X=\"0\" Y=\"{fontSize:0.##}\">{textContent}</ofd:TextCode></ofd:TextObject>");
                currentY += estimatedHeight + 2;
            }
        }
        
        var pageXmlBuilder = new StringBuilder();
        pageXmlBuilder.AppendLine("<?xml version=\"1.0\" encoding=\"UTF-8\"?>");
        pageXmlBuilder.AppendLine("<ofd:Page xmlns:ofd=\"http://www.ofdspec.org/2016\">");
        pageXmlBuilder.AppendLine("    <ofd:Area>");
        pageXmlBuilder.AppendLine($"        <ofd:PhysicalBox>0 0 {_pageLayout.Width} {_pageLayout.Height}</ofd:PhysicalBox>");
        pageXmlBuilder.AppendLine("    </ofd:Area>");
        pageXmlBuilder.AppendLine("    <ofd:Content>");
        pageXmlBuilder.AppendLine("        <ofd:Layer ID=\"Layer1\">");
        pageXmlBuilder.Append(contentXml.ToString());
        pageXmlBuilder.AppendLine("        </ofd:Layer>");
        pageXmlBuilder.AppendLine("    </ofd:Content>");
        pageXmlBuilder.AppendLine("</ofd:Page>");
        
        await File.WriteAllTextAsync(Path.Combine(pagesDir, $"Page_{pageNumber}.xml"), pageXmlBuilder.ToString(), System.Text.Encoding.UTF8);
    }

    /// <summary>
    /// 使用新的基础结构类创建OFD文档结构
    /// </summary>
    private async Task CreateOfdStructureWithNewClassesAsync(string baseDir)
    {
        Console.WriteLine($"[DEBUG] CreateOfdStructureWithNewClassesAsync 开始，基本目录: {baseDir}");
        
        // 创建DocInfo
        var docInfo = new OfdrwNet.Core.BasicStructure.Ofd.DocInfo();
        docInfo.RandomDocId()
               .SetTitle("由OfdrwNet生成的OFD文档")
               .SetCreator("OfdrwNet")
               .SetCreatorVersion("1.0")
               .SetCreationDate(DateTime.Now)
               .SetModDate(DateTime.Now)
               .SetDocUsage(OfdrwNet.Core.BasicStructure.Ofd.DocUsage.Normal);
        
        // 创建DocBody
        var docBody = new OfdrwNet.Core.BasicStructure.Ofd.DocBody();
        docBody.SetDocInfo(docInfo)
               .SetDocRoot(new OfdrwNet.Core.BasicType.StLoc("Doc/Document.xml"));
        
        // 创建OFD根节点
        var ofd = new OfdrwNet.Core.BasicStructure.Ofd.OFD();
        ofd.AddDocBody(docBody);
        
        // 写入OFD.xml
        var ofdXmlPath = Path.Combine(baseDir, "OFD.xml");
        await File.WriteAllTextAsync(ofdXmlPath, ofd.ToXml(), System.Text.Encoding.UTF8);
        Console.WriteLine($"[DEBUG] 创建 OFD.xml: {ofdXmlPath} ({new FileInfo(ofdXmlPath).Length} 字节)");
        
        // 创建Doc目录
        var docDir = Path.Combine(baseDir, "Doc");
        Directory.CreateDirectory(docDir);
        Console.WriteLine($"[DEBUG] 创建Doc目录: {docDir}");
        
        // 新增：生成公共资源文件 (字体)
        Console.WriteLine("[DEBUG] 生成公共资源 PublicRes.xml ...");
        await GenerateResourcesAsync(docDir);
        Console.WriteLine("[DEBUG] PublicRes.xml 生成完成");

        // 创建Document.xml（使用旧的方法，待后续转换Document类）
        var documentXml = await GenerateDocumentXmlAsync();
        var documentXmlPath = Path.Combine(docDir, "Document.xml");
        await File.WriteAllTextAsync(documentXmlPath, documentXml, System.Text.Encoding.UTF8);
        Console.WriteLine($"[DEBUG] 创建 Document.xml: {documentXmlPath} ({new FileInfo(documentXmlPath).Length} 字节)");
        
        // 创建Pages目录和页面文件
        var pagesDir = Path.Combine(docDir, "Pages");
        Directory.CreateDirectory(pagesDir);
        Console.WriteLine($"[DEBUG] 创建Pages目录: {pagesDir}");
        
        await GeneratePageFilesAsync(pagesDir);
        Console.WriteLine($"[DEBUG] CreateOfdStructureWithNewClassesAsync 完成");
    }

    #endregion

    #region IDisposable实现

    /// <summary>
    /// 释放资源
    /// </summary>
    public void Dispose()
    {
        Dispose(true);
        GC.SuppressFinalize(this);
    }

    /// <summary>
    /// 受保护的释放方法
    /// </summary>
    /// <param name="disposing">是否正在释放</param>
    protected virtual void Dispose(bool disposing)
    {
        if (!_disposed && disposing)
        {
            // 如果还没有关闭，尝试异步关闭，但不抛出异常
            if (!_disposed)
            {
                try
                {
                    Task.Run(async () => await CloseAsync()).Wait();
                }
                catch
                {
                    // 忽略释放时的异常，避免在测试中出现问题
                }
            }
        }
    }

    #endregion

    private void BuildFontMap()
    {
        _fontMap.Clear();
        int id = 1;
        foreach (var div in _streamQueue)
        {
            if (div is OfdrwNet.Layout.Element.Paragraph p)
            {
                var fn = string.IsNullOrWhiteSpace(p.FontName) ? "SimSun" : p.FontName.Trim();
                if (!_fontMap.ContainsKey(fn))
                {
                    _fontMap[fn] = id++;
                }
            }
        }
        if (_fontMap.Count == 0)
        {
            _fontMap["SimSun"] = 1;
        }
    }

    private async Task GenerateResourcesAsync(string docDir)
    {
        BuildFontMap();
        var resDir = Path.Combine(docDir, "Res");
        Directory.CreateDirectory(resDir);
        _publicResRelativePath = "Res/PublicRes.xml"; // 相对 Document.xml 的路径
        var sb = new StringBuilder();
        sb.AppendLine("<?xml version=\"1.0\" encoding=\"UTF-8\"?>");
        sb.AppendLine("<ofd:Res xmlns:ofd=\"http://www.ofdspec.org/2016\">\n    <ofd:Fonts>");
        foreach (var kv in _fontMap)
        {
            var fontNameEsc = System.Security.SecurityElement.Escape(kv.Key);
            sb.AppendLine($"        <ofd:Font ID=\"{kv.Value}\" FontName=\"{fontNameEsc}\" FamilyName=\"{fontNameEsc}\" Charset=\"unicode\"/>");
        }
        sb.AppendLine("    </ofd:Fonts>\n</ofd:Res>");
        await File.WriteAllTextAsync(Path.Combine(resDir, "PublicRes.xml"), sb.ToString(), Encoding.UTF8);
    }
}

/// <summary>
/// 页面布局配置类
/// </summary>
public class PageLayout
{
    public double Width { get; set; }
    public double Height { get; set; }
    public Margins Margins { get; set; } = new();

    /// <summary>
    /// 创建A4页面布局
    /// </summary>
    /// <returns>A4页面布局</returns>
    public static PageLayout A4() => new()
    {
        Width = 210.0, // mm
        Height = 297.0, // mm
        Margins = new Margins { Top = 25.4, Bottom = 25.4, Left = 31.7, Right = 31.7 } // mm
    };

    /// <summary>
    /// 克隆页面布局
    /// </summary>
    /// <returns>克隆的页面布局</returns>
    public PageLayout Clone() => new()
    {
        Width = Width,
        Height = Height,
        Margins = Margins.Clone()
    };
}

/// <summary>
/// 页边距配置
/// </summary>
public class Margins
{
    public double Top { get; set; }
    public double Bottom { get; set; }
    public double Left { get; set; }
    public double Right { get; set; }

    /// <summary>
    /// 克隆页边距
    /// </summary>
    /// <returns>克隆的页边距</returns>
    public Margins Clone() => new()
    {
        Top = Top,
        Bottom = Bottom,
        Left = Left,
        Right = Right
    };
}

/// <summary>
/// 布局元素基类 (占位符)
/// </summary>
    /// <summary>
    /// 布局元素的基本属性和方法
    /// </summary>
    public abstract class Div
    {
        // 这个类已经被 OfdrwNet.Layout.Element.Div 替代
        // 保留仅为兼容性
    }

/// <summary>
/// 虚拟页面类 (占位符)
/// </summary>
public class VirtualPage
{
    // 虚拟页面的属性和方法
}

/// <summary>
/// 文本段落类 (占位符)
/// </summary>
public class TextParagraph : Div
{
    public string Text { get; set; }
    public double FontSize { get; set; }
    public string FontFamily { get; set; } = "SimSun";
    public Position Position { get; set; } = new();

    public TextParagraph(string text)
    {
        Text = text;
    }
}

/// <summary>
/// 位置类 (占位符)
/// </summary>
public class Position
{
    public double X { get; set; }
    public double Y { get; set; }

    public Position() { }
    public Position(double x, double y)
    {
        X = x;
        Y = y;
    }
}