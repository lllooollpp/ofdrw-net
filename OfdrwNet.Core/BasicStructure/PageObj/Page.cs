using System.Collections.Generic;
using System.Linq;
using System.Xml.Linq;
using OfdrwNet.Core.BasicStructure.Doc;
using OfdrwNet.Core.BasicType;

namespace OfdrwNet.Core.BasicStructure.PageObj;

/// <summary>
/// 页对象
/// 
/// 页对象支持模板页描述，每一页经常要重复显示的内容可统一在模板页中描述，
/// 文档可以包含多个模板页。通过使用模板页可以使重复显示的内容不必出现在
/// 描述每一页的页面描述内容中，而只需通过 Template 节点进行应用。
/// 
/// 7.7 图 13 页对象结构；表 12 页对象属性
/// 
/// 对应 Java 版本的 org.ofdrw.core.basicStructure.pageObj.Page
/// </summary>
public class Page : OfdElement
{
    /// <summary>
    /// 从现有XML元素构造页面对象
    /// </summary>
    /// <param name="element">XML元素</param>
    public Page(XElement element) : base(element)
    {
    }

    /// <summary>
    /// 构造新的页面对象
    /// </summary>
    public Page() : base("Page")
    {
    }

    /// <summary>
    /// 【可选】设置页面区域的大小和位置，仅对该页面有效
    /// 
    /// 该节点不出现时则使用模板页中的定义，如果模板页不存在或模板页中
    /// 没有定义页面区域，则使用文件 CommonData 中的定义。
    /// </summary>
    /// <param name="area">页面区域的大小和位置</param>
    /// <returns>this</returns>
    public Page SetArea(CtPageArea area)
    {
        Set(area);
        return this;
    }

    /// <summary>
    /// 【可选】获取页面区域的大小和位置，仅对该页面有效
    /// 
    /// 该节点不出现时则使用模板页中的定义，如果模板页不存在或模板页中
    /// 没有定义页面区域，则使用文件 CommonData 中的定义。
    /// </summary>
    /// <returns>页面区域的大小和位置</returns>
    public CtPageArea? GetArea()
    {
        var element = GetOfdElement("Area");
        return element == null ? null : new CtPageArea(element);
    }

    /// <summary>
    /// 【可选】添加页面使用的模板页
    /// 
    /// 模板页的内容和结构与普通页相同，定义在 CommonData
    /// 指定的 XML 文件中。一个页可以使用多个模板页。该节点
    /// 使用是通过 TemplateID 来引用具体模板，并通过 ZOrder
    /// 属性来控制模板在页面中的显示顺序。
    /// </summary>
    /// <param name="template">页面使用的模板页</param>
    /// <returns>this</returns>
    public Page AddTemplate(Template template)
    {
        if (template != null)
        {
            Add(template);
        }
        return this;
    }

    /// <summary>
    /// 【可选】获取页面使用的模板页列表
    /// 
    /// 模板页的内容和结构与普通页相同，定义在 CommonData
    /// 指定的 XML 文件中。一个页可以使用多个模板页。
    /// </summary>
    /// <returns>页面使用的模板页列表</returns>
    public List<Template> GetTemplates()
    {
        return GetOfdElements("Template").Select(e => new Template(e)).ToList();
    }

    /// <summary>
    /// 【可选】添加页资源
    /// 
    /// 指向该页使用的资源文件
    /// </summary>
    /// <param name="pageRes">页资源路径</param>
    /// <returns>this</returns>
    public Page AddPageRes(StLoc pageRes)
    {
        AddOfdEntity("PageRes", pageRes.ToString());
        return this;
    }

    /// <summary>
    /// 【可选】获取页资源路径列表
    /// 
    /// 指向该页使用的资源文件
    /// </summary>
    /// <returns>页资源路径列表</returns>
    public List<StLoc> GetPageResList()
    {
        return GetOfdElements("PageRes").Select(e => StLoc.Parse(e.Value)).ToList();
    }

    /// <summary>
    /// 【可选】设置页面内容描述，该节点不存在时，标识空白页
    /// </summary>
    /// <param name="content">页面内容</param>
    /// <returns>this</returns>
    public Page SetContent(Content content)
    {
        Set(content);
        return this;
    }

    /// <summary>
    /// 【可选】获取页面内容描述，该节点不存在时，标识空白页
    /// </summary>
    /// <returns>页面内容</returns>
    public Content? GetContent()
    {
        var element = GetOfdElement("Content");
        return element == null ? null : new Content(element);
    }

    /// <summary>
    /// 【可选】设置与页面关联的动作序列
    /// 
    /// 当存在多个 Action对象时，所有动作依次执行。
    /// 动作列表的动作与页面关联，事件类型为 PO（页面打开）
    /// </summary>
    /// <param name="actions">动作序列</param>
    /// <returns>this</returns>
    public Page SetActions(Actions actions)
    {
        Set(actions);
        return this;
    }

    /// <summary>
    /// 【可选】获取与页面关联的动作序列
    /// 
    /// 当存在多个 Action对象时，所有动作依次执行。
    /// 动作列表的动作与页面关联，事件类型为 PO（页面打开）
    /// </summary>
    /// <returns>动作序列</returns>
    public Actions? GetActions()
    {
        var element = GetOfdElement("Actions");
        return element == null ? null : new Actions(element);
    }

    /// <summary>
    /// 获取限定名称
    /// </summary>
    public override string QualifiedName => "ofd:Page";
}

/// <summary>
/// 模板页（占位符实现）
/// 待后续完善
/// </summary>
public class Template : OfdElement
{
    public Template(XElement element) : base(element) { }
    public Template() : base("Template") { }
    public override string QualifiedName => "ofd:Template";
}

/// <summary>
/// 页面内容（占位符实现）
/// 待后续完善
/// </summary>
public class Content : OfdElement
{
    public Content(XElement element) : base(element) { }
    public Content() : base("Content") { }
    public override string QualifiedName => "ofd:Content";

    /// <summary>
    /// 添加图层
    /// </summary>
    /// <param name="layer">图层</param>
    /// <returns>this</returns>
    public Content AddLayer(Layer.CtLayer layer)
    {
        Add(layer);
        return this;
    }
}

/// <summary>
/// 动作序列（占位符实现）
/// 待后续完善
/// </summary>
public class Actions : OfdElement
{
    public Actions(XElement element) : base(element) { }
    public Actions() : base("Actions") { }
    public override string QualifiedName => "ofd:Actions";
}