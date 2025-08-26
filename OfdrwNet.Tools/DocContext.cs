using OfdrwNet.Reader;
using OfdrwNet.Packaging.Container;

namespace OfdrwNet.Tools;

/// <summary>
/// 文档上下文类
/// 对应 Java 版本的 org.ofdrw.tool.merge.DocContext
/// 用于管理文档读取器和相关资源
/// </summary>
public class DocContext : IDisposable
{
    /// <summary>
    /// OFD文档读取器
    /// </summary>
    public OfdReader Reader { get; private set; }

    /// <summary>
    /// 文档文件路径
    /// </summary>
    public string FilePath { get; private set; }

    /// <summary>
    /// 虚拟容器
    /// </summary>
    public VirtualContainer? Container { get; private set; }

    /// <summary>
    /// 是否已释放资源
    /// </summary>
    private bool _disposed = false;

    /// <summary>
    /// 构造函数
    /// </summary>
    /// <param name="filePath">OFD文档文件路径</param>
    public DocContext(string filePath)
    {
        FilePath = filePath ?? throw new ArgumentNullException(nameof(filePath));

        if (!File.Exists(filePath))
        {
            throw new FileNotFoundException($"文件不存在: {filePath}");
        }

        try
        {
            // 解压OFD文件到临时目录
            var tempDir = Path.Combine(Path.GetTempPath(), 
                $"OfdrwNet_{Path.GetFileNameWithoutExtension(filePath)}_{Guid.NewGuid():N}");
            
            if (Directory.Exists(tempDir))
            {
                Directory.Delete(tempDir, true);
            }
            Directory.CreateDirectory(tempDir);

            // 解压文件
            System.IO.Compression.ZipFile.ExtractToDirectory(filePath, tempDir);
            
            // 创建读取器
            Reader = new OfdReader(tempDir, true); // 关闭时删除临时目录
            Container = new VirtualContainer(tempDir);
        }
        catch (Exception ex)
        {
            throw new InvalidOperationException($"无法打开OFD文档: {filePath}", ex);
        }
    }

    /// <summary>
    /// 获取文档总页数
    /// </summary>
    /// <returns>总页数</returns>
    public int GetNumberOfPages()
    {
        return Reader.GetNumberOfPages();
    }

    /// <summary>
    /// 获取页面信息
    /// </summary>
    /// <param name="pageIndex">页面索引（从1开始）</param>
    /// <returns>页面信息</returns>
    public PageInfo GetPageInfo(int pageIndex)
    {
        return Reader.GetPageInfo(pageIndex);
    }

    /// <summary>
    /// 获取文件名
    /// </summary>
    /// <returns>文件名</returns>
    public string GetFileName()
    {
        return Path.GetFileName(FilePath);
    }

    /// <summary>
    /// 获取文件绝对路径
    /// </summary>
    /// <returns>文件绝对路径</returns>
    public string GetAbsolutePath()
    {
        return Path.GetFullPath(FilePath);
    }

    /// <summary>
    /// 验证页面索引是否有效
    /// </summary>
    /// <param name="pageIndex">页面索引（从1开始）</param>
    /// <returns>是否有效</returns>
    public bool IsValidPageIndex(int pageIndex)
    {
        return pageIndex >= 1 && pageIndex <= GetNumberOfPages();
    }

    /// <summary>
    /// 相等性比较
    /// </summary>
    /// <param name="obj">比较对象</param>
    /// <returns>是否相等</returns>
    public override bool Equals(object? obj)
    {
        if (obj is DocContext other)
        {
            return FilePath.Equals(other.FilePath, StringComparison.OrdinalIgnoreCase);
        }
        return false;
    }

    /// <summary>
    /// 获取哈希码
    /// </summary>
    /// <returns>哈希码</returns>
    public override int GetHashCode()
    {
        return FilePath.ToLowerInvariant().GetHashCode();
    }

    /// <summary>
    /// 转换为字符串
    /// </summary>
    /// <returns>描述字符串</returns>
    public override string ToString()
    {
        return $"DocContext: {GetFileName()} ({GetNumberOfPages()} pages)";
    }

    /// <summary>
    /// 释放资源
    /// </summary>
    public void Dispose()
    {
        if (!_disposed)
        {
            Reader?.Dispose();
            Container?.Dispose();
            _disposed = true;
        }
    }
}