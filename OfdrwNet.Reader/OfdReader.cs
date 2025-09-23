using OfdrwNet.Core;
using OfdrwNet.Core.BasicStructure.PageObj.Layer;
using OfdrwNet.Core.BasicType;
using OfdrwNet.Packaging.Container;
using OfdrwNet.Reader.Model;
using System.IO.Compression;
using System.Xml.Linq;
using System.Threading.Tasks;
using System.Threading;

namespace OfdrwNet.Reader;

/// <summary>
/// OFD文档读取器
/// 对应 Java 版本的 org.ofdrw.reader.OFDReader
/// 用于解析和读取OFD文档
/// </summary>
public class OfdReader : IDisposable
{
    private string _workDir;
    private VirtualContainer _ofdContainer;
    private ResourceLocator _resourceLocator;
    private bool _closed = false;
    private bool _deleteOnClose = true;

    /// <summary>
    /// 从文件构造 OFD 读取器
    /// </summary>
    public OfdReader(string ofdFile) : this(new FileInfo(ofdFile))
    {
    }

    /// <summary>
    /// 从文件信息构造 OFD 读取器
    /// </summary>
    public OfdReader(FileInfo ofdFile)
    {
        if (ofdFile == null || !ofdFile.Exists)
        {
            throw new ArgumentException("文件位置不正确或文件不存在");
        }

        _workDir = Path.Combine(Path.GetTempPath(), $"ofd-tmp-{Guid.NewGuid()}");
        Directory.CreateDirectory(_workDir);

        ExtractOfdFile(ofdFile.FullName, _workDir);

        _ofdContainer = new VirtualContainer(_workDir);
        _resourceLocator = new ResourceLocator(_ofdContainer);
    }

    /// <summary>
    /// 从输入流构造 OFD 读取器
    /// </summary>
    public OfdReader(Stream inputStream)
    {
        if (inputStream == null)
        {
            throw new ArgumentNullException(nameof(inputStream), "文件输入流不能为空");
        }

        _workDir = Path.Combine(Path.GetTempPath(), $"ofd-tmp-{Guid.NewGuid()}");
        Directory.CreateDirectory(_workDir);

        string tempFile = Path.Combine(_workDir, "temp.ofd");
        using (var fileStream = File.Create(tempFile))
        {
            inputStream.CopyTo(fileStream);
        }

        ExtractOfdFile(tempFile, _workDir);
        File.Delete(tempFile);

        _ofdContainer = new VirtualContainer(_workDir);
        _resourceLocator = new ResourceLocator(_ofdContainer);
    }

    /// <summary>
    /// 从已解压的目录构造 OFD 读取器
    /// </summary>
    public OfdReader(string unzippedPathRoot, bool deleteOnClose)
    {
        if (string.IsNullOrEmpty(unzippedPathRoot) || !Directory.Exists(unzippedPathRoot))
        {
            throw new ArgumentException("目录路径不正确或目录不存在");
        }

        _workDir = Path.GetFullPath(unzippedPathRoot);
        _deleteOnClose = deleteOnClose;

        _ofdContainer = new VirtualContainer(_workDir);
        _resourceLocator = new ResourceLocator(_ofdContainer);
    }

    /// <summary>
    /// 获取OFD含有的总页面数量
    /// </summary>
    public int GetNumberOfPages()
    {
        try
        {
            _resourceLocator.Save();
            var document = NavigateToDefaultDoc();
            var pagesElement = document.Element("Pages");
            if (pagesElement != null)
            {
                var pageElements = pagesElement.Elements("Page");
                return pageElements.Count();
            }
            return 0;
        }
        finally
        {
            _resourceLocator.Restore();
        }
    }

    /// <summary>
    /// 获取页面信息
    /// </summary>
    public PageInfo GetPageInfo(int pageNum)
    {
        if (pageNum <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(pageNum), "页码不能小于1");
        }

        try
        {
            _resourceLocator.Save();
            int index = pageNum - 1;

            var document = NavigateToDefaultDoc();
            var pagesElement = document.Element("Pages");
            if (pagesElement == null)
            {
                throw new InvalidOperationException("文档中没有Pages元素");
            }

            var pageElements = pagesElement.Elements("Page").ToList();
            if (index >= pageElements.Count)
            {
                throw new ArgumentOutOfRangeException(nameof(pageNum), $"页码{pageNum}超过最大页码{pageElements.Count}");
            }

            var pageElement = pageElements[index];
            var baseLocAttr = pageElement.Attribute("BaseLoc");
            if (baseLocAttr == null)
            {
                throw new InvalidOperationException($"页面{pageNum}缺少BaseLoc属性");
            }

            var pageLoc = new StLoc(baseLocAttr.Value);
            var pageObj = _resourceLocator.Get(pageLoc.ToString(), element => element);
            var pageAbsLoc = _resourceLocator.GetAbsTo(pageLoc);
            var pageSize = GetPageSize(pageObj);
            var pageId = StId.Parse(pageElement.Attribute("ID")?.Value ?? "0");
            var templatePages = LoadTemplatePages(pageObj);

            return new PageInfo()
                .SetIndex(pageNum)
                .SetId(pageId)
                .SetObj(pageObj)
                .SetSize(pageSize)
                .SetPageAbsLoc(pageAbsLoc)
                .SetTemplates(templatePages);
        }
        finally
        {
            _resourceLocator.Restore();
        }
    }

    /// <summary>
    /// 获取页面信息集合
    /// </summary>
    public List<PageInfo> GetPageList()
    {
        int numberOfPages = GetNumberOfPages();
        var result = new List<PageInfo>(numberOfPages);

        for (int i = 1; i <= numberOfPages; i++)
        {
            var pageInfo = GetPageInfo(i);
            result.Add(pageInfo);
        }

        return result;
    }

    /// <summary>
    /// 获取页面对象
    /// </summary>
    public XElement GetPage(int pageNum)
    {
        var pageInfo = GetPageInfo(pageNum);
        return pageInfo.Obj;
    }

    /// <summary>
    /// 获取工作目录
    /// </summary>
    public string GetWorkDir() => _workDir;

    /// <summary>
    /// 获取文档虚拟容器
    /// </summary>
    public VirtualContainer GetOfdContainer() => _ofdContainer;

    /// <summary>
    /// 获取 OFDDir 包装器（包装 VirtualContainer）
    /// </summary>
    public OfdrwNet.Packaging.Container.OFDDir GetOFDDir() => new OfdrwNet.Packaging.Container.OFDDir(_ofdContainer);

    /// <summary>
    /// 获取资源定位器
    /// </summary>
    public ResourceLocator GetResourceLocator() => _resourceLocator;

    /// <summary>
    /// 解压OFD文件
    /// </summary>
    private void ExtractOfdFile(string ofdFilePath, string extractPath)
    {
        try
        {
            using var archive = ZipFile.OpenRead(ofdFilePath);
            foreach (var entry in archive.Entries)
            {
                string entryPath = entry.FullName.Replace('\\', '/');
                if (entryPath.Contains(".."))
                    continue;

                string destinationPath = Path.GetFullPath(Path.Combine(extractPath, entryPath));

                if (!destinationPath.StartsWith(extractPath))
                    continue;

                if (entry.FullName.EndsWith("/"))
                {
                    Directory.CreateDirectory(destinationPath);
                }
                else
                {
                    Directory.CreateDirectory(Path.GetDirectoryName(destinationPath)!);
                    entry.ExtractToFile(destinationPath, true);
                }
            }
        }
        catch (Exception e)
        {
            throw new IOException($"解压OFD文件失败: {e.Message}", e);
        }
    }

    /// <summary>
    /// 导航到默认文档
    /// </summary>
    private XElement NavigateToDefaultDoc()
    {
        _resourceLocator.Cd("/");
        var ofdElement = _resourceLocator.Get("OFD.xml", element => element);

        // OFD文档使用命名空间，需要正确处理
        var ofdNamespace = ofdElement.Name.Namespace;
        var docBodyElement = ofdElement.Element(ofdNamespace + "DocBody");

        if (docBodyElement == null)
        {
            throw new InvalidOperationException("OFD文档中没有DocBody元素\n\n请检查文件是否为有效的OFD格式。");
        }

        var docRootElement = docBodyElement.Element(ofdNamespace + "DocRoot");
        if (docRootElement == null)
        {
            throw new InvalidOperationException("DocBody中没有DocRoot元素");
        }

        var docRoot = docRootElement.Value;
        if (string.IsNullOrWhiteSpace(docRoot))
        {
            throw new InvalidOperationException("DocRoot元素值为空");
        }

        return _resourceLocator.Get(docRoot, element => element);
    }

    /// <summary>
    /// 获取页面大小
    /// </summary>
    private StBox GetPageSize(XElement pageObj)
    {
        var areaElement = pageObj.Element("Area");
        if (areaElement != null)
        {
            var physicalBoxAttr = areaElement.Attribute("PhysicalBox");
            if (physicalBoxAttr != null)
            {
                return StBox.Parse(physicalBoxAttr.Value);
            }
        }

        return new StBox(0, 0, 210, 297); // A4默认大小
    }

    /// <summary>
    /// 加载模板页面
    /// </summary>
    private List<TemplatePageEntity> LoadTemplatePages(XElement pageObj)
    {
        var templates = new List<TemplatePageEntity>();

        var templateElements = pageObj.Elements("Template");
        foreach (var templateElement in templateElements)
        {
            var templateIdAttr = templateElement.Attribute("TemplateID");
            var zOrderAttr = templateElement.Attribute("ZOrder");

            if (templateIdAttr != null)
            {
                var templatePage = new XElement("Page");
                var layerType = ParseLayerType(zOrderAttr?.Value);

                var template = new TemplatePageEntity(layerType, templatePage)
                {
                    Id = templateIdAttr.Value
                };

                templates.Add(template);
            }
        }

        return templates;
    }

    /// <summary>
    /// 解析图层类型
    /// </summary>
    private static LayerType ParseLayerType(string? value)
    {
        if (string.IsNullOrEmpty(value))
            return LayerType.Body;

        return value.ToLowerInvariant() switch
        {
            "background" => LayerType.Background,
            "body" => LayerType.Body,
            "foreground" => LayerType.Foreground,
            _ => LayerType.Body
        };
    }

    // ===== T026: 新增文档加载方法和配置支持 =====

    /// <summary>
    /// 异步加载OFD文档
    /// </summary>
    /// <param name="options">加载选项</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>完整的OFD文档对象</returns>
    public async Task<OfdDocument> LoadDocumentAsync(LoadOptions? options = null, CancellationToken cancellationToken = default)
    {
        return await Task.Run(() =>
        {
            cancellationToken.ThrowIfCancellationRequested();

            var document = new OfdDocument(_workDir)
            {
                State = DocumentState.Loading,
                LoadedAt = DateTime.UtcNow
            };

            try
            {
                // 加载文档结构
                document.Structure = LoadDocumentStructure();

                // 加载元数据
                document.Metadata = LoadDocumentMetadata();

                // 加载页面信息
                var pages = GetPageList();
                foreach (var page in pages)
                {
                    document.AddPage(page);
                    cancellationToken.ThrowIfCancellationRequested();
                }

                // 设置资源管理器
                document.Resources = CreateResourceManager();

                // 应用加载选项
                if (options != null)
                {
                    ApplyLoadOptions(document, options);
                }

                document.State = DocumentState.Loaded;
                return document;
            }
            catch (Exception ex)
            {
                document.State = DocumentState.Error;
                throw new DocumentLoadException($"文档加载失败: {ex.Message}");
            }
        }, cancellationToken);
    }

    /// <summary>
    /// 异步验证OFD文档
    /// </summary>
    /// <returns>验证结果</returns>
    public async Task<ValidationResult> ValidateDocumentAsync()
    {
        return await Task.Run(() =>
        {
            var result = new ValidationResult { IsValid = true };

            try
            {
                // 验证文档结构
                var structure = LoadDocumentStructure();
                if (!structure.ValidateStructure())
                {
                    result.IsValid = false;
                    result.Errors.AddRange(structure.GetValidationErrors());
                }

                // 验证页面完整性
                var pages = GetPageList();
                if (pages.Count == 0)
                {
                    result.IsValid = false;
                    result.Errors.Add(new ValidationError
                    {
                        Code = "VALID001",
                        Message = "文档必须包含至少一个页面",
                        Severity = ValidationSeverity.Error
                    });
                }

                result.Version = DetermineOfdVersion();
            }
            catch (Exception ex)
            {
                result.IsValid = false;
                result.Errors.Add(new ValidationError
                {
                    Code = "VALID002",
                    Message = $"验证过程中发生错误: {ex.Message}",
                    Severity = ValidationSeverity.Error
                });
            }

            return result;
        });
    }

    /// <summary>
    /// 异步获取文档基本信息
    /// </summary>
    /// <returns>文档元数据</returns>
    public async Task<DocumentMetadata> GetDocumentInfoAsync()
    {
        return await Task.Run(() => LoadDocumentMetadata());
    }

    /// <summary>
    /// 应用文档查看器配置
    /// </summary>
    /// <param name="configuration">配置对象</param>
    public void ApplyConfiguration(IDocumentViewerConfiguration configuration)
    {
        if (configuration == null) return;

        // 在这里应用配置到读取器
        // 具体实现会在后续任务中完成
        ConfigurationApplied?.Invoke(this, new ConfigurationAppliedEventArgs
        {
            Configuration = configuration,
            AppliedAt = DateTime.UtcNow
        });
    }

    /// <summary>
    /// 获取资源管理器
    /// </summary>
    /// <returns>资源管理器实例</returns>
    public IResourceManager GetResourceManager()
    {
        return CreateResourceManager();
    }

    /// <summary>
    /// 配置应用事件
    /// </summary>
    public event EventHandler<ConfigurationAppliedEventArgs>? ConfigurationApplied;

    /// <summary>
    /// 文档加载进度事件
    /// </summary>
    public event EventHandler<DocumentLoadProgressEventArgs>? LoadProgress;

    /// <summary>
    /// 错误发生事件
    /// </summary>
    public event EventHandler<ErrorEventArgs>? ErrorOccurred;

    // ===== 私有辅助方法 =====

    /// <summary>
    /// 加载文档结构
    /// </summary>
    private DocumentStructure LoadDocumentStructure()
    {
        var structure = new DocumentStructure();

        try
        {
            // 加载OFD.xml
            var ofdXmlPath = Path.Combine(_workDir, "OFD.xml");
            if (File.Exists(ofdXmlPath))
            {
                structure.OfdXml = XDocument.Load(ofdXmlPath);
            }

            // 加载Document.xml
            var docXmlPath = Path.Combine(_workDir, "Doc_0", "Document.xml");
            if (File.Exists(docXmlPath))
            {
                structure.DocumentXml = XDocument.Load(docXmlPath);
            }

            // 加载页面XML
            var pagesDir = Path.Combine(_workDir, "Doc_0", "Pages");
            if (Directory.Exists(pagesDir))
            {
                var pageDirectories = Directory.GetDirectories(pagesDir, "Page_*");
                foreach (var pageDir in pageDirectories)
                {
                    var pageXmlPath = Path.Combine(pageDir, "Content.xml");
                    if (File.Exists(pageXmlPath))
                    {
                        var pageNumber = ExtractPageNumber(pageDir);
                        structure.AddPageXml(pageNumber, XDocument.Load(pageXmlPath));
                    }
                }
            }

            structure.Version = DetermineOfdVersion();
        }
        catch (Exception ex)
        {
            structure.ValidationErrors.Add(new ValidationError
            {
                Code = "STRUCT005",
                Message = $"文档结构加载失败: {ex.Message}",
                Severity = ValidationSeverity.Error
            });
        }

        return structure;
    }

    /// <summary>
    /// 加载文档元数据
    /// </summary>
    private DocumentMetadata LoadDocumentMetadata()
    {
        var metadata = new DocumentMetadata();

        try
        {
            var docXmlPath = Path.Combine(_workDir, "Doc_0", "Document.xml");
            if (File.Exists(docXmlPath))
            {
                var docXml = XDocument.Load(docXmlPath);
                var commonData = docXml.Root?.Element("CommonData");

                if (commonData != null)
                {
                    metadata.Title = commonData.Element("Title")?.Value ?? "";
                    metadata.Author = commonData.Element("Author")?.Value ?? "";
                    metadata.Subject = commonData.Element("Subject")?.Value ?? "";
                    metadata.Creator = commonData.Element("Creator")?.Value ?? "";

                    if (DateTime.TryParse(commonData.Element("CreationDate")?.Value, out var creationDate))
                        metadata.CreationDate = creationDate;

                    if (DateTime.TryParse(commonData.Element("ModDate")?.Value, out var modDate))
                        metadata.ModificationDate = modDate;
                }
            }

            metadata.Version = DetermineOfdVersion();
            metadata.PageCount = GetNumberOfPages();

            var directoryInfo = new DirectoryInfo(_workDir);
            metadata.FileSize = GetDirectorySize(directoryInfo);
        }
        catch (Exception)
        {
            // 元数据加载失败时使用默认值
        }

        return metadata;
    }

    /// <summary>
    /// 创建资源管理器
    /// </summary>
    private IResourceManager CreateResourceManager()
    {
        // TODO: 在T038中实现完整的资源管理器
        // 这里返回一个基于现有ResourceLocator的简单实现
        return new BasicResourceManager(_resourceLocator);
    }

    /// <summary>
    /// 应用加载选项
    /// </summary>
    private void ApplyLoadOptions(OfdDocument document, LoadOptions options)
    {
        if (options.ValidateOnLoad)
        {
            var validationResult = document.Validate();
            if (!validationResult.IsValid)
            {
                ErrorOccurred?.Invoke(this, new ErrorEventArgs
                {
                    ErrorType = ErrorType.DocumentLoad,
                    Message = "文档验证失败",
                    Context = "LoadOptions.ValidateOnLoad"
                });
            }
        }

        // 其他选项的应用将在后续任务中实现
    }

    /// <summary>
    /// 确定OFD版本
    /// </summary>
    private OfdVersion DetermineOfdVersion()
    {
        try
        {
            var ofdXmlPath = Path.Combine(_workDir, "OFD.xml");
            if (File.Exists(ofdXmlPath))
            {
                var ofdXml = XDocument.Load(ofdXmlPath);
                var versionAttr = ofdXml.Root?.Attribute("Version");

                if (versionAttr != null)
                {
                    return versionAttr.Value switch
                    {
                        "1.0" => OfdVersion.V1_0,
                        "1.1" => OfdVersion.V1_1,
                        "2.0" => OfdVersion.V2_0,
                        _ => OfdVersion.Unknown
                    };
                }
            }
        }
        catch
        {
            // 忽略版本检测错误
        }

        return OfdVersion.V1_0; // 默认版本
    }

    /// <summary>
    /// 提取页面编号
    /// </summary>
    private int ExtractPageNumber(string pageDir)
    {
        var dirName = Path.GetFileName(pageDir);
        if (dirName.StartsWith("Page_") && int.TryParse(dirName.Substring(5), out var pageNumber))
        {
            return pageNumber;
        }
        return 0;
    }

    /// <summary>
    /// 获取目录大小
    /// </summary>
    private long GetDirectorySize(DirectoryInfo dirInfo)
    {
        long size = 0;
        try
        {
            foreach (var file in dirInfo.GetFiles("*", SearchOption.AllDirectories))
            {
                size += file.Length;
            }
        }
        catch
        {
            // 忽略访问错误
        }
        return size;
    }

    /// <summary>
    /// 释放资源
    /// </summary>
    public void Dispose()
    {
        Dispose(true);
        GC.SuppressFinalize(this);
    }

    /// <summary>
    /// 释放资源
    /// </summary>
    /// <param name="disposing">是否正在进行托管释放</param>
    protected virtual void Dispose(bool disposing)
    {
        if (!_closed && disposing)
        {
            _resourceLocator?.Dispose();
            _ofdContainer?.Dispose();

            if (_deleteOnClose && Directory.Exists(_workDir))
            {
                try
                {
                    Directory.Delete(_workDir, true);
                }
                catch
                {
                    // 忽略删除失败的情况
                }
            }

            _closed = true;
        }
    }

    /// <summary>
    /// 兼容 Java 版本的 Close() 方法，转发到 Dispose()
    /// </summary>
    public void Close()
    {
        Dispose();
    }
}
