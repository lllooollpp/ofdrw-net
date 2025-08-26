using OfdrwNet.Core;
using OfdrwNet.Core.BasicType;
using OfdrwNet.Reader;
using OfdrwNet.Packaging.Container;
using System.Xml.Linq;

namespace OfdrwNet.Tools;

/// <summary>
/// OFD文档页面删除器
/// 对应 Java 版本的 org.ofdrw.tool.merge.OFDPageDeleter
/// 该删除器删除文档树中的页面节点，但不会删除相关资源和页面对象 Content.xml
/// </summary>
public class OFDPageDeleter : IDisposable
{
    /// <summary>
    /// OFD文档虚拟容器
    /// </summary>
    private readonly VirtualContainer _ofdContainer;

    /// <summary>
    /// OFD文档读取器
    /// </summary>
    private readonly OfdReader _reader;

    /// <summary>
    /// 输出路径
    /// </summary>
    private readonly string _outputPath;

    /// <summary>
    /// OFD.xml 根元素
    /// </summary>
    private XElement? _ofdElement;

    /// <summary>
    /// Document.xml 文档根元素
    /// </summary>
    private XElement? _documentElement;

    /// <summary>
    /// 是否已释放资源
    /// </summary>
    private bool _disposed = false;

    /// <summary>
    /// 创建OFD文档页面删除器
    /// </summary>
    /// <param name="sourcePath">OFD文件路径</param>
    /// <param name="outputPath">删除后文件路径</param>
    public OFDPageDeleter(string sourcePath, string outputPath)
    {
        if (string.IsNullOrEmpty(sourcePath) || !File.Exists(sourcePath))
        {
            throw new ArgumentException("OFD文件不存在", nameof(sourcePath));
        }

        if (string.IsNullOrEmpty(outputPath))
        {
            throw new ArgumentException("输出路径不能为空", nameof(outputPath));
        }

        _outputPath = outputPath;

        try
        {
            // 解压OFD文件到临时目录
            var tempDir = Path.Combine(Path.GetTempPath(), 
                $"OfdrwNet_PageDeleter_{Guid.NewGuid():N}");
            
            if (Directory.Exists(tempDir))
            {
                Directory.Delete(tempDir, true);
            }
            Directory.CreateDirectory(tempDir);

            // 解压文件
            System.IO.Compression.ZipFile.ExtractToDirectory(sourcePath, tempDir);

            // 创建读取器和容器
            _reader = new OfdReader(tempDir, false); // 不自动删除临时目录
            _ofdContainer = new VirtualContainer(tempDir);

            // 加载文档结构
            LoadDocumentStructure();
        }
        catch (Exception ex)
        {
            throw new InvalidOperationException($"无法打开OFD文档: {sourcePath}", ex);
        }
    }

    /// <summary>
    /// 加载文档结构
    /// </summary>
    private void LoadDocumentStructure()
    {
        // 加载OFD.xml
        var ofdObj = _ofdContainer.GetObj("OFD.xml");
        _ofdElement = ofdObj;

        // 获取Document.xml路径并加载
        var docBodyElement = _ofdElement?.Element("DocBody");
        var docRootAttr = docBodyElement?.Attribute("DocRoot");
        
        if (docRootAttr != null)
        {
            var docRootPath = docRootAttr.Value;
            if (docRootPath.StartsWith("/"))
            {
                docRootPath = docRootPath.Substring(1);
            }
            
            var documentObj = _ofdContainer.GetObj(docRootPath);
            _documentElement = documentObj;
        }
        else
        {
            throw new InvalidOperationException("无法找到Document.xml路径");
        }
    }

    /// <summary>
    /// 删除指定索引的页面
    /// </summary>
    /// <param name="indexes">页面索引列表（从0开始）</param>
    /// <returns>this</returns>
    public OFDPageDeleter Delete(params int[] indexes)
    {
        if (indexes == null || indexes.Length == 0)
        {
            return this;
        }

        if (_documentElement == null)
        {
            throw new InvalidOperationException("文档结构未加载");
        }

        var pagesElement = _documentElement.Element("Pages");
        if (pagesElement == null)
        {
            throw new InvalidOperationException("文档中没有Pages元素");
        }

        var pageElements = pagesElement.Elements("Page").ToList();
        var toBeDeleted = new List<XElement>();

        // 收集要删除的页面元素
        foreach (int index in indexes)
        {
            if (index >= 0 && index < pageElements.Count)
            {
                toBeDeleted.Add(pageElements[index]);
            }
            else
            {
                Console.WriteLine($"警告: 页面索引 {index} 超出范围 (0-{pageElements.Count - 1})");
            }
        }

        // 删除页面元素
        foreach (var pageElement in toBeDeleted)
        {
            pageElement.Remove();
        }

        Console.WriteLine($"已删除 {toBeDeleted.Count} 个页面");
        return this;
    }

    /// <summary>
    /// 删除指定页码的页面（页码从1开始）
    /// </summary>
    /// <param name="pageNumbers">页码列表（从1开始）</param>
    /// <returns>this</returns>
    public OFDPageDeleter DeleteByPageNumbers(params int[] pageNumbers)
    {
        if (pageNumbers == null || pageNumbers.Length == 0)
        {
            return this;
        }

        // 将页码转换为索引（页码从1开始，索引从0开始）
        var indexes = pageNumbers.Select(pageNum => pageNum - 1).ToArray();
        return Delete(indexes);
    }

    /// <summary>
    /// 获取当前文档的页面数量
    /// </summary>
    /// <returns>页面数量</returns>
    public int GetPageCount()
    {
        if (_documentElement == null)
        {
            return 0;
        }

        var pagesElement = _documentElement.Element("Pages");
        return pagesElement?.Elements("Page").Count() ?? 0;
    }

    /// <summary>
    /// 保存删除后的文档，并关闭删除器
    /// 注意：请在所有操作完成后调用该方法，否则无法删除页面
    /// </summary>
    public async Task SaveAsync()
    {
        try
        {
            // 更新文档修改时间
            await UpdateDocumentModificationDate();

            // 保存修改后的文档结构
            SaveDocumentStructure();

            // 刷新容器到文件系统
            _ofdContainer.Flush();

            // 打包输出文件
            var workDir = _ofdContainer.GetSysAbsPath();
            if (File.Exists(_outputPath))
            {
                File.Delete(_outputPath);
            }

            System.IO.Compression.ZipFile.CreateFromDirectory(workDir, _outputPath);

            Console.WriteLine($"页面删除完成，输出文件: {_outputPath}");

            await Task.CompletedTask;
        }
        catch (Exception ex)
        {
            throw new InvalidOperationException($"保存文档失败: {ex.Message}", ex);
        }
    }

    /// <summary>
    /// 更新文档修改时间
    /// </summary>
    private async Task UpdateDocumentModificationDate()
    {
        if (_ofdElement?.Element("DocBody") is XElement docBodyElement)
        {
            var docInfoAttr = docBodyElement.Attribute("DocInfo");
            if (docInfoAttr != null)
            {
                var docInfoPath = docInfoAttr.Value;
                if (docInfoPath.StartsWith("/"))
                {
                    docInfoPath = docInfoPath.Substring(1);
                }

                try
                {
                    var docInfoObj = _ofdContainer.GetObj(docInfoPath);
                    
                    // 更新或添加ModDate元素
                    var modDateElement = docInfoObj.Element("ModDate");
                    var currentDate = DateTime.Now.ToString("yyyy-MM-ddTHH:mm:ss");
                    
                    if (modDateElement != null)
                    {
                        modDateElement.Value = currentDate;
                    }
                    else
                    {
                        docInfoObj.Add(new XElement("ModDate", currentDate));
                    }

                    // 保存修改后的文档信息 - 直接写入XML文件
                    var docInfoXml = docInfoObj.ToString();
                    await File.WriteAllTextAsync(Path.Combine(_ofdContainer.GetSysAbsPath(), docInfoPath), docInfoXml);
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"更新文档修改时间失败: {ex.Message}");
                }
            }
        }
    }

    /// <summary>
    /// 保存文档结构
    /// </summary>
    private void SaveDocumentStructure()
    {
        if (_ofdElement != null)
        {
            // 直接写入XML文件
            var ofdXml = _ofdElement.ToString();
            File.WriteAllText(Path.Combine(_ofdContainer.GetSysAbsPath(), "OFD.xml"), ofdXml);
        }

        if (_documentElement != null && _ofdElement != null)
        {
            var docBodyElement = _ofdElement.Element("DocBody");
            var docRootAttr = docBodyElement?.Attribute("DocRoot");
            
            if (docRootAttr != null)
            {
                var docRootPath = docRootAttr.Value;
                if (docRootPath.StartsWith("/"))
                {
                    docRootPath = docRootPath.Substring(1);
                }
                
                var documentXml = _documentElement.ToString();
                File.WriteAllText(Path.Combine(_ofdContainer.GetSysAbsPath(), docRootPath), documentXml);
            }
        }
    }

    /// <summary>
    /// 释放资源
    /// </summary>
    public void Dispose()
    {
        if (!_disposed)
        {
            _reader?.Dispose();
            _ofdContainer?.Dispose();
            
            _disposed = true;
        }
    }
}