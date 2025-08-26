using System.Xml.Linq;
using OfdrwNet.Core.BasicType;
using OfdrwNet.Core.BasicStructure.PageTree;
using OutlinesType = OfdrwNet.Core.BasicStructure.Outlines.Outlines;

namespace OfdrwNet.Core.BasicStructure.Doc;

/// <summary>
/// 文档根节点
/// Document.xml
/// 
/// ————《GB/T 33190-2016》 图 5
/// 
/// 对应 Java 版本的 org.ofdrw.core.basicStructure.doc.Document
/// </summary>
public class Document : OfdElement
{
    /// <summary>
    /// 从现有元素构造文档
    /// </summary>
    /// <param name="element">XML元素</param>
    public Document(XElement element) : base(element)
    {
    }

    /// <summary>
    /// 构造新的文档元素
    /// </summary>
    public Document() : base("Document")
    {
    }

    /// <summary>
    /// 【必选】
    /// 设置文档公共数据
    /// 定义了页面区域、公共资源
    /// </summary>
    /// <param name="commonData">文档公共数据</param>
    /// <returns>this</returns>
    public Document SetCommonData(CtCommonData commonData)
    {
        Set(commonData);
        return this;
    }

    /// <summary>
    /// 【必选】
    /// 获取文档公共数据
    /// 定义了页面区域、公共资源
    /// </summary>
    /// <returns>文档公共数据</returns>
    public CtCommonData? GetCommonData()
    {
        var element = GetOfdElement("CommonData");
        return element == null ? null : new CtCommonData(element);
    }

    /// <summary>
    /// 【必选】
    /// 设置页树
    /// 有关页树的描述见 7.6
    /// </summary>
    /// <param name="pages">页树</param>
    /// <returns>this</returns>
    public Document SetPages(Pages pages)
    {
        Set(pages);
        return this;
    }

    /// <summary>
    /// 【必选】
    /// 获取页树
    /// 有关页树的描述见 7.6
    /// </summary>
    /// <returns>页树</returns>
    public Pages? GetPages()
    {
        var element = GetOfdElement("Pages");
        return element == null ? null : new Pages(element);
    }

    /// <summary>
    /// 【可选】
    /// 设置大纲
    /// </summary>
    /// <param name="outlines">大纲</param>
    /// <returns>this</returns>
    public Document SetOutlines(OutlinesType outlines)
    {
        Set(outlines);
        return this;
    }

    /// <summary>
    /// 【可选】
    /// 获取大纲
    /// </summary>
    /// <returns>大纲</returns>
    public OutlinesType? GetOutlines()
    {
        var element = GetOfdElement("Outlines");
        return element == null ? null : new OutlinesType(element);
    }

    /// <summary>
    /// 【可选】
    /// 设置文档的权限声明
    /// </summary>
    /// <param name="permission">文档的权限声明</param>
    /// <returns>this</returns>
    public Document SetPermissions(CtPermission permission)
    {
        Set(permission);
        return this;
    }

    /// <summary>
    /// 【可选】
    /// 获取文档的权限声明
    /// </summary>
    /// <returns>文档的权限声明</returns>
    public CtPermission? GetPermission()
    {
        var element = GetOfdElement("Permissions");
        return element == null ? null : new CtPermission(element);
    }

    /// <summary>
    /// 【可选】
    /// 设置文档关联的动作序列
    /// 当存在多个 Action 对象时，所有动作依次执行
    /// </summary>
    /// <param name="actions">文档关联的动作序列</param>
    /// <returns>this</returns>
    public Document SetActions(Actions actions)
    {
        Set(actions);
        return this;
    }

    /// <summary>
    /// 【可选】
    /// 获取文档关联的动作序列
    /// 当存在多个 Action 对象时，所有动作依次执行
    /// </summary>
    /// <returns>文档关联的动作序列</returns>
    public Actions? GetActions()
    {
        var element = GetOfdElement("Actions");
        return element == null ? null : new Actions(element);
    }

    /// <summary>
    /// 【可选】
    /// 设置文档的视图首选项
    /// </summary>
    /// <param name="vPreferences">文档的视图首选项</param>
    /// <returns>this</returns>
    public Document SetVPreferences(CtVPreferences vPreferences)
    {
        Set(vPreferences);
        return this;
    }

    /// <summary>
    /// 【可选】
    /// 获取文档的视图首选项
    /// </summary>
    /// <returns>文档的视图首选项</returns>
    public CtVPreferences? GetVPreferences()
    {
        var element = GetOfdElement("VPreferences");
        return element == null ? null : new CtVPreferences(element);
    }

    /// <summary>
    /// 【可选】
    /// 设置文档的书签集，包含一组书签
    /// 7.5 文档根节点 表 5 文档根节点属性
    /// </summary>
    /// <param name="bookmarks">文档的书签集</param>
    /// <returns>this</returns>
    public Document SetBookmarks(Bookmarks bookmarks)
    {
        Set(bookmarks);
        return this;
    }

    /// <summary>
    /// 【可选】
    /// 获取文档的书签集，包含一组书签
    /// 7.5 文档根节点 表 5 文档根节点属性
    /// </summary>
    /// <returns>文档的书签集</returns>
    public Bookmarks? GetBookmarks()
    {
        var element = GetOfdElement("Bookmarks");
        return element == null ? null : new Bookmarks(element);
    }

    /// <summary>
    /// 【可选】
    /// 设置指向注释列表的文件
    /// 有关注释描述见第 15 章
    /// </summary>
    /// <param name="annotations">指向注释列表的文件路径</param>
    /// <returns>this</returns>
    public Document SetAnnotations(StLoc annotations)
    {
        SetOfdEntity("Annotations", annotations.ToString());
        return this;
    }

    /// <summary>
    /// 【可选】
    /// 获取指向注释列表的文件
    /// 有关注释描述见第 15 章
    /// </summary>
    /// <returns>指向注释列表的文件路径</returns>
    public StLoc? GetAnnotations()
    {
        var text = GetOfdElementText("Annotations");
        return string.IsNullOrEmpty(text) ? null : StLoc.Parse(text);
    }

    /// <summary>
    /// 【可选】
    /// 设置指向自定义标引列表文件
    /// 有关自定义标引描述见第 16 章
    /// </summary>
    /// <param name="customTags">指向自定义标引列表文件路径</param>
    /// <returns>this</returns>
    public Document SetCustomTags(StLoc customTags)
    {
        SetOfdEntity("CustomTags", customTags.ToString());
        return this;
    }

    /// <summary>
    /// 【可选】
    /// 获取指向自定义标引列表文件
    /// 有关自定义标引描述见第 16 章
    /// </summary>
    /// <returns>指向自定义标引列表文件路径</returns>
    public StLoc? GetCustomTags()
    {
        var text = GetOfdElementText("CustomTags");
        return string.IsNullOrEmpty(text) ? null : StLoc.Parse(text);
    }

    /// <summary>
    /// 【可选】
    /// 设置指向附件列表文件
    /// 有关附件描述见第 20 章
    /// </summary>
    /// <param name="attachments">指向附件列表文件路径</param>
    /// <returns>this</returns>
    public Document SetAttachments(StLoc attachments)
    {
        SetOfdEntity("Attachments", attachments.ToString());
        return this;
    }

    /// <summary>
    /// 【可选】
    /// 获取指向附件列表文件
    /// 有关附件描述见第 20 章
    /// </summary>
    /// <returns>指向附件列表文件路径</returns>
    public StLoc? GetAttachments()
    {
        var text = GetOfdElementText("Attachments");
        return string.IsNullOrEmpty(text) ? null : StLoc.Parse(text);
    }

    /// <summary>
    /// 【可选】
    /// 设置指向扩展列表文件
    /// 有关扩展列表文件见第 17 章
    /// </summary>
    /// <param name="extensions">指向扩展列表文件路径</param>
    /// <returns>this</returns>
    public Document SetExtensions(StLoc extensions)
    {
        SetOfdEntity("Extensions", extensions.ToString());
        return this;
    }

    /// <summary>
    /// 【可选】
    /// 获取指向扩展列表文件
    /// 有关扩展列表文件见第 17 章
    /// </summary>
    /// <returns>指向扩展列表文件路径</returns>
    public StLoc? GetExtensions()
    {
        var text = GetOfdElementText("Extensions");
        return string.IsNullOrEmpty(text) ? null : StLoc.Parse(text);
    }
}
