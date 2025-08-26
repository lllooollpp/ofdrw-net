using OfdrwNet.Core.BasicType;

namespace OfdrwNet.Tools;

/// <summary>
/// 文档页面引用类
/// 对应 Java 版本的 org.ofdrw.tool.merge.DocPage
/// 用于表示文档中特定页面的引用
/// </summary>
public class DocPage
{
    /// <summary>
    /// 文档文件路径
    /// </summary>
    public string FilePath { get; set; }

    /// <summary>
    /// 页面索引（从1开始）
    /// </summary>
    public int PageIndex { get; set; }

    /// <summary>
    /// 构造函数
    /// </summary>
    /// <param name="filePath">文档文件路径</param>
    /// <param name="pageIndex">页面索引（从1开始）</param>
    public DocPage(string filePath, int pageIndex)
    {
        FilePath = filePath ?? throw new ArgumentNullException(nameof(filePath));
        PageIndex = pageIndex;
    }

    /// <summary>
    /// 验证页面索引有效性
    /// </summary>
    /// <returns>页面索引是否有效</returns>
    public bool IsValidPageIndex()
    {
        return PageIndex >= 1;
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
    /// 转换为字符串
    /// </summary>
    /// <returns>描述字符串</returns>
    public override string ToString()
    {
        return $"{GetFileName()}:Page{PageIndex}";
    }

    /// <summary>
    /// 相等性比较
    /// </summary>
    /// <param name="obj">比较对象</param>
    /// <returns>是否相等</returns>
    public override bool Equals(object? obj)
    {
        if (obj is DocPage other)
        {
            return FilePath.Equals(other.FilePath, StringComparison.OrdinalIgnoreCase) && 
                   PageIndex == other.PageIndex;
        }
        return false;
    }

    /// <summary>
    /// 获取哈希码
    /// </summary>
    /// <returns>哈希码</returns>
    public override int GetHashCode()
    {
        return HashCode.Combine(FilePath.ToLowerInvariant(), PageIndex);
    }
}