using OfdrwNet.Core;
using OfdrwNet.Core.BasicType;
using OfdrwNet.Reader;
using OfdrwNet.Packaging.Container;
using System.Xml.Linq;

namespace OfdrwNet.Tools;

/// <summary>
/// OFD文档合并工具
/// 对应 Java 版本的 org.ofdrw.tool.merge.OFDMerger
/// 提供文档合并、页面裁剪、页面重组、页面混合等功能
/// </summary>
public class OFDMerger : IDisposable
{
    /// <summary>
    /// 新页面列表
    /// 每一个元素代表新文档中的一页
    /// </summary>
    public List<PageEntry> PageArray { get; }

    /// <summary>
    /// 文档上下文映射
    /// Key: 文档文件名, Value: 文档上下文
    /// </summary>
    private readonly Dictionary<string, DocContext> _docContextMap;

    /// <summary>
    /// 合并后生成文档位置
    /// </summary>
    private readonly string _destinationPath;

    /// <summary>
    /// 合并的目标文档容器
    /// </summary>
    private VirtualContainer? _ofdContainer;

    /// <summary>
    /// 是否已释放资源
    /// </summary>
    private bool _disposed = false;

    /// <summary>
    /// 构造函数
    /// </summary>
    /// <param name="destinationPath">合并结果输出路径</param>
    public OFDMerger(string destinationPath)
    {
        if (string.IsNullOrEmpty(destinationPath))
        {
            throw new ArgumentException("合并结果路径不能为空", nameof(destinationPath));
        }

        _destinationPath = Path.GetFullPath(destinationPath);
        
        // 确保输出目录存在
        var outputDir = Path.GetDirectoryName(_destinationPath);
        if (!string.IsNullOrEmpty(outputDir) && !Directory.Exists(outputDir))
        {
            Directory.CreateDirectory(outputDir);
        }

        PageArray = new List<PageEntry>();
        _docContextMap = new Dictionary<string, DocContext>();
    }

    /// <summary>
    /// 向合并文件中添加页面
    /// </summary>
    /// <param name="filePath">待合并的OFD文件路径</param>
    /// <param name="pageIndexes">页面序列，如果为空表示所有页面（页码从1开始）</param>
    /// <returns>this</returns>
    public OFDMerger Add(string filePath, params int[] pageIndexes)
    {
        if (string.IsNullOrEmpty(filePath))
        {
            return this;
        }

        if (!File.Exists(filePath))
        {
            throw new FileNotFoundException($"文件不存在: {filePath}");
        }

        var key = Path.GetFileName(filePath);
        
        // 检查缓存中是否已有该文件映射
        if (!_docContextMap.TryGetValue(key, out var docContext))
        {
            // 加载文件上下文
            docContext = new DocContext(filePath);
            _docContextMap[key] = docContext;
        }

        // 没有传递页码时认为需要追加所有页面
        if (pageIndexes == null || pageIndexes.Length == 0)
        {
            int numberOfPages = docContext.GetNumberOfPages();
            pageIndexes = Enumerable.Range(1, numberOfPages).ToArray();
        }

        // 追加内容到页面列表中
        foreach (int pageIndex in pageIndexes)
        {
            if (docContext.IsValidPageIndex(pageIndex))
            {
                PageArray.Add(new PageEntry(pageIndex, docContext));
            }
            else
            {
                Console.WriteLine($"警告: 页面索引 {pageIndex} 超出文档 {key} 的页面范围 (1-{docContext.GetNumberOfPages()})");
            }
        }

        return this;
    }

    /// <summary>
    /// 添加并混合两个文档中的页面
    /// 注意：两个页面大小不一致时，以第一个文档的页面大小为准
    /// </summary>
    /// <param name="dstDocFilePath">原始文档路径（用于作为基础页面）</param>
    /// <param name="dstPageIndex">要混合的页面索引（页码从1开始）</param>
    /// <param name="tbMixDocFilePath">需要混合的文档路径</param>
    /// <param name="tbMixPageIndex">需要混合的文档页索引（页码从1开始）</param>
    /// <returns>this</returns>
    public OFDMerger AddMix(string dstDocFilePath, int dstPageIndex, string tbMixDocFilePath, int tbMixPageIndex)
    {
        var pageList = new List<DocPage>
        {
            new DocPage(dstDocFilePath, dstPageIndex),
            new DocPage(tbMixDocFilePath, tbMixPageIndex)
        };
        return AddMix(pageList);
    }

    /// <summary>
    /// 添加并混合多个文档页面到一个页面中
    /// 注意：页面大小不一致时，以第一个文档的页面大小为准
    /// </summary>
    /// <param name="pageList">需要混合的页面列表，按照顺序依次混合，第1个文档页面处于最底下</param>
    /// <returns>this</returns>
    public OFDMerger AddMix(List<DocPage> pageList)
    {
        if (pageList == null || pageList.Count == 0)
        {
            return this;
        }

        // 验证所有页面都有效
        foreach (var docPage in pageList)
        {
            if (!File.Exists(docPage.FilePath))
            {
                throw new FileNotFoundException($"混合文档不存在: {docPage.FilePath}");
            }

            if (!docPage.IsValidPageIndex())
            {
                throw new ArgumentException($"无效的页面索引: {docPage.PageIndex}");
            }
        }

        // 创建混合页面条目
        // 这里需要实现页面混合逻辑，当前简化为添加第一个页面
        // 实际实现需要将多个页面的内容合并到一个页面中
        var firstPage = pageList[0];
        var key = Path.GetFileName(firstPage.FilePath);

        if (!_docContextMap.TryGetValue(key, out var docContext))
        {
            docContext = new DocContext(firstPage.FilePath);
            _docContextMap[key] = docContext;
        }

        if (docContext.IsValidPageIndex(firstPage.PageIndex))
        {
            PageArray.Add(new PageEntry(firstPage.PageIndex, docContext));
            
            Console.WriteLine($"页面混合功能当前简化实现，仅添加第一个页面: {firstPage}");
            Console.WriteLine($"完整的页面混合功能需要更复杂的页面内容合并逻辑");
        }

        return this;
    }

    /// <summary>
    /// 执行文档合并操作
    /// </summary>
    /// <returns>合并任务</returns>
    public async Task MergeAsync()
    {
        if (PageArray.Count == 0)
        {
            throw new InvalidOperationException("没有页面可以合并");
        }

        try
        {
            Console.WriteLine($"开始合并 {PageArray.Count} 个页面到文档: {_destinationPath}");

            // 创建输出容器
            var tempWorkDir = Path.Combine(Path.GetTempPath(), $"OfdrwNet_Merge_{Guid.NewGuid():N}");
            Directory.CreateDirectory(tempWorkDir);
            _ofdContainer = new VirtualContainer(tempWorkDir);

            // 创建OFD文档结构
            await CreateOfdDocumentStructure();

            // 合并页面
            await MergePages();

            // 打包输出文件
            await PackageDocument();

            Console.WriteLine($"文档合并完成: {_destinationPath}");
        }
        catch (Exception ex)
        {
            throw new InvalidOperationException($"文档合并失败: {ex.Message}", ex);
        }
    }

    /// <summary>
    /// 获取合并的页面数量
    /// </summary>
    /// <returns>页面数量</returns>
    public int GetPageCount()
    {
        return PageArray.Count;
    }

    /// <summary>
    /// 获取合并的文档数量
    /// </summary>
    /// <returns>文档数量</returns>
    public int GetDocumentCount()
    {
        return _docContextMap.Count;
    }

    /// <summary>
    /// 清除所有页面
    /// </summary>
    public void Clear()
    {
        PageArray.Clear();
    }

    /// <summary>
    /// 创建OFD文档基础结构
    /// </summary>
    private async Task CreateOfdDocumentStructure()
    {
        if (_ofdContainer == null)
            throw new InvalidOperationException("OFD容器未初始化");

        // 创建OFD.xml根文件
        var ofdElement = OfdElement.GetInstance("OFD")
            .AddAttribute("Version", "1.0")
            .AddOfdEntity("DocBody", "")
                .AddAttribute("DocInfo", "/Doc_0/DocumentRes.xml")
                .AddAttribute("DocRoot", "/Doc_0/Document.xml");

        _ofdContainer.PutObj("OFD.xml", ofdElement.ToXml());

        // 创建文档容器
        var doc0Container = _ofdContainer.ObtainContainer("Doc_0", 
            () => new VirtualContainer(Path.Combine(_ofdContainer.GetSysAbsPath(), "Doc_0")));

        // 创建Document.xml
        var documentElement = await CreateDocumentXml();
        doc0Container.PutObj("Document.xml", documentElement);

        // 创建DocumentRes.xml
        var documentResElement = CreateDocumentResXml();
        doc0Container.PutObj("DocumentRes.xml", documentResElement);

        await Task.CompletedTask;
    }

    /// <summary>
    /// 创建Document.xml内容
    /// </summary>
    private async Task<OfdElement> CreateDocumentXml()
    {
        var documentElement = OfdElement.GetInstance("Document")
            .AddOfdEntity("CommonData", "")
                .AddOfdEntity("MaxUnitID", (PageArray.Count + 100).ToString())
                .AddOfdEntity("PageArea", "")
                    .AddAttribute("PhysicalBox", "0 0 210.0 297.0");
        
        documentElement.AddOfdEntity("PublicRes", "/Doc_0/PublicRes.xml")
                     .AddOfdEntity("Pages", "");

        // 添加页面引用
        for (int i = 0; i < PageArray.Count; i++)
        {
            documentElement.AddOfdEntity("Page", "")
                .AddAttribute("ID", (i + 1).ToString())
                .AddAttribute("BaseLoc", $"/Doc_0/Pages/Page_{i}/Content.xml");
        }

        await Task.CompletedTask;
        return documentElement;
    }

    /// <summary>
    /// 创建DocumentRes.xml内容
    /// </summary>
    private OfdElement CreateDocumentResXml()
    {
        var documentResElement = OfdElement.GetInstance("DocumentRes")
            .AddOfdEntity("Creator", "OfdrwNet")
            .AddOfdEntity("CreationDate", DateTime.Now.ToString("yyyy-MM-ddTHH:mm:ss"))
            .AddOfdEntity("ModDate", DateTime.Now.ToString("yyyy-MM-ddTHH:mm:ss"))
            .AddOfdEntity("Title", "合并文档")
            .AddOfdEntity("Subject", "通过OfdrwNet合并生成的文档");

        return documentResElement;
    }

    /// <summary>
    /// 合并页面内容
    /// </summary>
    private async Task MergePages()
    {
        if (_ofdContainer == null)
            throw new InvalidOperationException("OFD容器未初始化");

        var doc0Container = _ofdContainer.ObtainContainer("Doc_0", 
            () => new VirtualContainer(Path.Combine(_ofdContainer.GetSysAbsPath(), "Doc_0")));
        
        var pagesContainer = doc0Container.ObtainContainer("Pages",
            () => new VirtualContainer(Path.Combine(_ofdContainer.GetSysAbsPath(), "Doc_0", "Pages")));

        for (int i = 0; i < PageArray.Count; i++)
        {
            var pageEntry = PageArray[i];
            Console.WriteLine($"合并页面 {i + 1}/{PageArray.Count}: {pageEntry}");

            // 创建页面容器
            var pageContainer = pagesContainer.ObtainContainer($"Page_{i}",
                () => new VirtualContainer(Path.Combine(_ofdContainer.GetSysAbsPath(), "Doc_0", "Pages", $"Page_{i}")));

            // 获取源页面内容
            var sourcePageInfo = pageEntry.GetPageInfo();
            var sourcePageContent = sourcePageInfo.Obj;

            // 复制页面内容 - 直接保存为XML字符串
            var contentXml = sourcePageContent.ToString();
            await File.WriteAllTextAsync(Path.Combine(pageContainer.GetSysAbsPath(), "Content.xml"), contentXml);

            // TODO: 这里需要复制相关的资源文件（图像、字体等）
            // 当前简化实现，实际需要分析页面引用的资源并复制
        }

        await Task.CompletedTask;
    }

    /// <summary>
    /// 打包文档
    /// </summary>
    private async Task PackageDocument()
    {
        if (_ofdContainer == null)
            throw new InvalidOperationException("OFD容器未初始化");

        // 刷新容器到文件系统
        _ofdContainer.Flush();

        // 压缩为OFD文件
        var workDir = _ofdContainer.GetSysAbsPath();
        if (File.Exists(_destinationPath))
        {
            File.Delete(_destinationPath);
        }

        System.IO.Compression.ZipFile.CreateFromDirectory(workDir, _destinationPath);

        // 清理临时目录
        if (Directory.Exists(workDir))
        {
            Directory.Delete(workDir, true);
        }

        await Task.CompletedTask;
    }

    /// <summary>
    /// 释放资源
    /// </summary>
    public void Dispose()
    {
        if (!_disposed)
        {
            // 释放文档上下文
            foreach (var docContext in _docContextMap.Values)
            {
                docContext.Dispose();
            }
            _docContextMap.Clear();

            // 释放容器
            _ofdContainer?.Dispose();

            _disposed = true;
        }
    }
}