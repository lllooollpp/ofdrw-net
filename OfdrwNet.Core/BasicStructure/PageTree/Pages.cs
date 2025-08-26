using System.Xml.Linq;

namespace OfdrwNet.Core.BasicStructure.PageTree;

/// <summary>
/// 页树
/// 
/// 图 12 页树结构
/// 
/// 对应 Java 版本的 org.ofdrw.core.basicStructure.pageTree.Pages
/// </summary>
public class Pages : OfdElement
{
    /// <summary>
    /// 从现有元素构造页树
    /// </summary>
    /// <param name="element">XML元素</param>
    public Pages(XElement element) : base(element)
    {
    }

    /// <summary>
    /// 构造新的页树元素
    /// </summary>
    public Pages() : base("Pages")
    {
    }

    /// <summary>
    /// 【必选】
    /// 增加叶节点
    /// 一个页树中可以包含一个或多个叶节点，页顺序是
    /// 根据页树进行前序遍历时叶节点的顺序。
    /// </summary>
    /// <param name="page">叶节点</param>
    /// <returns>this</returns>
    public Pages AddPage(Page page)
    {
        Add(page);
        return this;
    }

    /// <summary>
    /// 获取页面数量
    /// </summary>
    /// <returns>页面数量</returns>
    public int GetSize()
    {
        return GetOfdElements("Page").Count();
    }

    /// <summary>
    /// 【必选】
    /// 获取叶节点序列
    /// 一个页树中可以包含一个或多个叶节点，页顺序是
    /// 根据页树进行前序遍历时叶节点的顺序。
    /// </summary>
    /// <returns>叶节点序列（大于等于 1）</returns>
    public List<Page> GetPages()
    {
        return GetOfdElements("Page")
            .Select(element => new Page(element))
            .ToList();
    }

    /// <summary>
    /// 获取指定页面
    /// </summary>
    /// <param name="index">页面索引（页码 - 1）</param>
    /// <returns>页节点</returns>
    public Page GetPageByIndex(int index)
    {
        var pages = GetPages();
        if (index < 0 || index >= pages.Count)
        {
            throw new ArgumentOutOfRangeException(nameof(index), $"页面索引 {index} 超出范围，总页数: {pages.Count}");
        }
        return pages[index];
    }
}
