using System.Xml.Linq;
using OfdrwNet.Core.BasicType;

namespace OfdrwNet.Core.BasicStructure.PageTree;

/// <summary>
/// 页节点
/// 
/// 7.6 页树 表 11 页树属性
/// 
/// 对应 Java 版本的 org.ofdrw.core.basicStructure.pageTree.Page
/// </summary>
public class Page : OfdElement
{
    /// <summary>
    /// 从现有元素构造页面
    /// </summary>
    /// <param name="element">XML元素</param>
    public Page(XElement element) : base(element)
    {
    }

    /// <summary>
    /// 构造新的页面元素
    /// </summary>
    public Page() : base("Page")
    {
    }

    /// <summary>
    /// 构造新的页面
    /// </summary>
    /// <param name="id">对象ID</param>
    /// <param name="baseLoc">页面内容位置</param>
    public Page(long id, string baseLoc) : this()
    {
        SetId(new StId(id))
            .SetBaseLoc(StLoc.Parse(baseLoc));
    }

    /// <summary>
    /// 构造新的页面
    /// </summary>
    /// <param name="id">对象ID</param>
    /// <param name="baseLoc">页面内容位置</param>
    public Page(StId id, StLoc baseLoc) : this()
    {
        SetId(id)
            .SetBaseLoc(baseLoc);
    }

    /// <summary>
    /// 【必选 属性】
    /// 设置页的标识符，不能与已有标识重复
    /// </summary>
    /// <param name="id">页的标识符</param>
    /// <returns>this</returns>
    public Page SetId(StId id)
    {
        AddAttribute("ID", id.ToString());
        return this;
    }

    /// <summary>
    /// 【必选 属性】
    /// 获取页的标识符，不能与已有标识重复
    /// </summary>
    /// <returns>页的标识符</returns>
    public StId? GetId()
    {
        var value = GetAttributeValue("ID");
        return string.IsNullOrEmpty(value) ? null : StId.Parse(value);
    }

    /// <summary>
    /// 【必选 属性】
    /// 设置页对象描述文件
    /// </summary>
    /// <param name="baseLoc">页对象描述文件路径</param>
    /// <returns>this</returns>
    public Page SetBaseLoc(StLoc baseLoc)
    {
        AddAttribute("BaseLoc", baseLoc.ToString());
        return this;
    }

    /// <summary>
    /// 【必选 属性】
    /// 获取页对象描述文件
    /// </summary>
    /// <returns>页对象描述文件路径</returns>
    public StLoc? GetBaseLoc()
    {
        var value = GetAttributeValue("BaseLoc");
        return string.IsNullOrEmpty(value) ? null : StLoc.Parse(value);
    }

    /// <summary>
    /// 【兼容适配】设置页面区域（转发到PageObj.Page的SetArea实现）
    /// 
    /// 这是一个适配方法，用于兼容布局引擎中对Page.SetArea的调用
    /// </summary>
    /// <param name="area">页面区域</param>
    /// <returns>this</returns>
    public Page SetArea(OfdrwNet.Core.BasicStructure.Doc.CtPageArea area)
    {
        // 页树中的Page主要用于索引，不直接设置区域
        // 这个方法主要是为了编译兼容性，实际区域设置应该在PageObj.Page中进行
        return this;
    }

    /// <summary>
    /// 【兼容适配】添加模板页（转发适配）
    /// 
    /// 这是一个适配方法，用于兼容布局引擎中对Page.AddTemplate的调用
    /// </summary>
    /// <param name="template">模板页实体</param>
    /// <returns>this</returns>
    public Page AddTemplate(object template)
    {
        // 页树中的Page主要用于索引，不直接添加模板
        // 这个方法主要是为了编译兼容性，实际模板添加应该在PageObj.Page中进行
        return this;
    }

    /// <summary>
    /// 【兼容适配】设置页面内容（转发适配）
    /// 
    /// 这是一个适配方法，用于兼容布局引擎中对Page.SetContent的调用
    /// </summary>
    /// <param name="page">页面对象</param>
    /// <returns>this</returns>
    public Page SetContent(OfdrwNet.Core.BasicStructure.PageObj.Page page)
    {
        // 页树中的Page主要用于索引，不直接设置内容
        // 这个方法主要是为了编译兼容性，实际内容设置应该通过其他机制进行
        return this;
    }
}
