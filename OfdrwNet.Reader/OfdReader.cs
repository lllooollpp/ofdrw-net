using OfdrwNet.Core;
using OfdrwNet.Core.BasicType;
using OfdrwNet.Packaging.Container;
using OfdrwNet.Reader.Model;
using System.IO.Compression;
using System.Xml.Linq;

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

    /// <summary>
    /// 释放资源
    /// </summary>
    public void Dispose()
    {
        Dispose(true);
        GC.SuppressFinalize(this);
    }

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
}