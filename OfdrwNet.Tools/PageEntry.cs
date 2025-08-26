using OfdrwNet.Reader;

namespace OfdrwNet.Tools;

/// <summary>
/// 页面条目类
/// 对应 Java 版本的 org.ofdrw.tool.merge.PageEntry
/// 表示合并操作中的一个页面条目
/// </summary>
public class PageEntry
{
    /// <summary>
    /// 页面索引（从1开始）
    /// </summary>
    public int PageIndex { get; set; }

    /// <summary>
    /// 文档上下文
    /// </summary>
    public DocContext DocContext { get; set; }

    /// <summary>
    /// 构造函数
    /// </summary>
    /// <param name="pageIndex">页面索引（从1开始）</param>
    /// <param name="docContext">文档上下文</param>
    public PageEntry(int pageIndex, DocContext docContext)
    {
        if (pageIndex < 1)
        {
            throw new ArgumentOutOfRangeException(nameof(pageIndex), "页面索引必须从1开始");
        }

        PageIndex = pageIndex;
        DocContext = docContext ?? throw new ArgumentNullException(nameof(docContext));
    }

    /// <summary>
    /// 获取页面信息
    /// </summary>
    /// <returns>页面信息对象</returns>
    public PageInfo GetPageInfo()
    {
        return DocContext.Reader.GetPageInfo(PageIndex);
    }

    /// <summary>
    /// 获取文档文件名
    /// </summary>
    /// <returns>文件名</returns>
    public string GetDocumentFileName()
    {
        return DocContext.GetFileName();
    }

    /// <summary>
    /// 转换为字符串
    /// </summary>
    /// <returns>描述字符串</returns>
    public override string ToString()
    {
        return $"{GetDocumentFileName()}:Page{PageIndex}";
    }

    /// <summary>
    /// 相等性比较
    /// </summary>
    /// <param name="obj">比较对象</param>
    /// <returns>是否相等</returns>
    public override bool Equals(object? obj)
    {
        if (obj is PageEntry other)
        {
            return PageIndex == other.PageIndex && 
                   DocContext.Equals(other.DocContext);
        }
        return false;
    }

    /// <summary>
    /// 获取哈希码
    /// </summary>
    /// <returns>哈希码</returns>
    public override int GetHashCode()
    {
        return HashCode.Combine(PageIndex, DocContext);
    }
}