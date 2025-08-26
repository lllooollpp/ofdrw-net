using OfdrwNet.Core.BasicType;
using OfdrwNet.Packaging.Container;

namespace OfdrwNet.Graphics;

/// <summary>
/// OFD 图形文档
/// 对应 Java 版本的 org.ofdrw.graphics2d.OFDGraphicsDocument
/// 通过图形绘制生成OFD文档
/// </summary>
public class OFDGraphicsDocument : IDisposable
{
    private readonly string _outputPath;
    private readonly VirtualContainer _container;
    private readonly List<OFDPageGraphics> _pages;
    private bool _disposed = false;

    /// <summary>
    /// 最大单元ID计数器
    /// </summary>
    public int MaxUnitId { get; private set; } = 0;

    /// <summary>
    /// 默认页面宽度（毫米）
    /// </summary>
    public double DefaultPageWidth { get; set; } = 210.0; // A4宽度

    /// <summary>
    /// 默认页面高度（毫米）
    /// </summary>
    public double DefaultPageHeight { get; set; } = 297.0; // A4高度

    /// <summary>
    /// 构造函数
    /// </summary>
    /// <param name="outputPath">输出文件路径</param>
    public OFDGraphicsDocument(string outputPath)
    {
        _outputPath = outputPath ?? throw new ArgumentNullException(nameof(outputPath));
        
        // 创建临时工作目录
        var workDir = Path.Combine(Path.GetTempPath(), "ofd-graphics-" + Guid.NewGuid().ToString("N"));
        _container = new VirtualContainer(workDir);
        _pages = new List<OFDPageGraphics>();

        // 初始化OFD文档结构
        InitializeOfdStructure();
    }

    /// <summary>
    /// 创建新页面
    /// </summary>
    /// <returns>页面图形绘制对象</returns>
    public OFDPageGraphics NewPage()
    {
        return NewPage(DefaultPageWidth, DefaultPageHeight);
    }

    /// <summary>
    /// 创建指定尺寸的新页面
    /// </summary>
    /// <param name="width">页面宽度（毫米）</param>
    /// <param name="height">页面高度（毫米）</param>
    /// <returns>页面图形绘制对象</returns>
    public OFDPageGraphics NewPage(double width, double height)
    {
        var pageId = _pages.Count + 1;
        var page = new OFDPageGraphics(pageId, width, height);
        _pages.Add(page);
        return page;
    }

    /// <summary>
    /// 生成下一个ID
    /// </summary>
    /// <returns>新的ID</returns>
    public int NewId()
    {
        return ++MaxUnitId;
    }

    /// <summary>
    /// 保存并关闭文档
    /// </summary>
    public async Task SaveAsync()
    {
        if (_disposed) return;

        try
        {
            // 生成所有页面的内容
            await GeneratePages();

            // 更新文档结构
            UpdateDocumentStructure();

            // 刷新容器到文件系统
            _container.Flush();

            // 打包成OFD文件
            await PackageOfdFile();
        }
        catch (Exception ex)
        {
            throw new InvalidOperationException("保存OFD文档时发生错误", ex);
        }
    }

    /// <summary>
    /// 初始化OFD文档结构
    /// </summary>
    private void InitializeOfdStructure()
    {
        // 创建OFD.xml根文件
        var ofdContent = $@"<?xml version='1.0' encoding='UTF-8'?>
<ofd:OFD Version='1.0' DocType='OFD' xmlns:ofd='http://www.ofdspec.org/2016'>
    <ofd:DocBody>
        <ofd:DocInfo DocID='{Guid.NewGuid()}' Creator='OfdrwNet.Graphics' CreationDate='{DateTime.Now:yyyy-MM-dd}' />
        <ofd:DocRoot>Doc_0/Document.xml</ofd:DocRoot>
    </ofd:DocBody>
</ofd:OFD>";

        _container.PutObj("OFD.xml", ofdContent);

        // 创建基本的Document.xml结构
        var documentContent = $@"<?xml version='1.0' encoding='UTF-8'?>
<ofd:Document xmlns:ofd='http://www.ofdspec.org/2016'>
    <ofd:CommonData MaxUnitID='{MaxUnitId}'>
        <ofd:PageArea>
            <ofd:PhysicalBox>0 0 {DefaultPageWidth} {DefaultPageHeight}</ofd:PhysicalBox>
        </ofd:PageArea>
    </ofd:CommonData>
    <ofd:Pages>
        <!-- 页面列表将在这里生成 -->
    </ofd:Pages>
</ofd:Document>";

        var docContainer = _container.ObtainContainer("Doc_0", 
            () => new VirtualContainer(Path.Combine(_container.GetSysAbsPath(), "Doc_0")));
        docContainer.PutObj("Document.xml", documentContent);
    }

    /// <summary>
    /// 生成所有页面内容
    /// </summary>
    private async Task GeneratePages()
    {
        var docContainer = _container.ObtainContainer("Doc_0",
            () => new VirtualContainer(Path.Combine(_container.GetSysAbsPath(), "Doc_0")));
        
        var pagesContainer = docContainer.ObtainContainer("Pages",
            () => new VirtualContainer(Path.Combine(_container.GetSysAbsPath(), "Doc_0", "Pages")));

        for (int i = 0; i < _pages.Count; i++)
        {
            var page = _pages[i];
            var pageContainer = pagesContainer.ObtainContainer($"Page_{i}",
                () => new VirtualContainer(Path.Combine(pagesContainer.GetSysAbsPath(), $"Page_{i}")));

            // 生成页面内容
            await page.GenerateContentAsync(pageContainer, NewId);

            // 创建页面XML
            var pageContent = $@"<?xml version='1.0' encoding='UTF-8'?>
<ofd:Page ID='{page.PageId}' xmlns:ofd='http://www.ofdspec.org/2016'>
    <ofd:Area>
        <ofd:PhysicalBox>0 0 {page.Width} {page.Height}</ofd:PhysicalBox>
    </ofd:Area>
    <ofd:Content>Content.xml</ofd:Content>
</ofd:Page>";

            pageContainer.PutObj("Page.xml", pageContent);
        }
    }

    /// <summary>
    /// 更新文档结构
    /// </summary>
    private void UpdateDocumentStructure()
    {
        // 更新MaxUnitID
        MaxUnitId = Math.Max(MaxUnitId, _pages.Count * 10); // 预留一些ID空间

        // 重新生成Document.xml，包含页面列表
        var pagesXml = string.Join("\n        ", _pages.Select((page, index) => 
            $"<ofd:Page ID='{page.PageId}' BaseLoc='Pages/Page_{index}/Page.xml' />"));

        var documentContent = $@"<?xml version='1.0' encoding='UTF-8'?>
<ofd:Document xmlns:ofd='http://www.ofdspec.org/2016'>
    <ofd:CommonData MaxUnitID='{MaxUnitId}'>
        <ofd:PageArea>
            <ofd:PhysicalBox>0 0 {DefaultPageWidth} {DefaultPageHeight}</ofd:PhysicalBox>
        </ofd:PageArea>
    </ofd:CommonData>
    <ofd:Pages>
        {pagesXml}
    </ofd:Pages>
</ofd:Document>";

        var docContainer = _container.ObtainContainer("Doc_0",
            () => new VirtualContainer(Path.Combine(_container.GetSysAbsPath(), "Doc_0")));
        docContainer.PutObj("Document.xml", documentContent);
    }

    /// <summary>
    /// 打包成OFD文件
    /// </summary>
    private async Task PackageOfdFile()
    {
        await Task.Run(() =>
        {
            // 确保输出目录存在
            var outputDir = Path.GetDirectoryName(_outputPath);
            if (!string.IsNullOrEmpty(outputDir) && !Directory.Exists(outputDir))
            {
                Directory.CreateDirectory(outputDir);
            }

            // 使用ZIP压缩创建OFD文件
            if (File.Exists(_outputPath))
            {
                File.Delete(_outputPath);
            }

            System.IO.Compression.ZipFile.CreateFromDirectory(_container.GetSysAbsPath(), _outputPath);
        });
    }

    /// <summary>
    /// 释放资源
    /// </summary>
    public void Dispose()
    {
        if (!_disposed)
        {
            try
            {
                // 释放页面资源
                foreach (var page in _pages)
                {
                    page.Dispose();
                }
                _pages.Clear();

                // 释放容器资源
                _container?.Dispose();
            }
            catch
            {
                // 忽略释放异常
            }

            _disposed = true;
        }
    }
}