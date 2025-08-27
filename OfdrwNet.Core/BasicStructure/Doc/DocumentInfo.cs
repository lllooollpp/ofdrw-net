using System.Xml.Linq;
using OfdrwNet.Core.BasicType;

namespace OfdrwNet.Core.BasicStructure.Doc;

/// <summary>
/// 文档信息元数据描述
/// 
/// 这是一个增强的文档信息类，提供更丰富的文档元数据管理功能。
/// 与DocInfo类相比，这个类提供了更多的高级功能和便捷方法。
/// 
/// 对应OFD规范中的文档信息管理需求
/// </summary>
public class DocumentInfo : OfdElement
{
    /// <summary>
    /// 从现有元素构造文档信息
    /// </summary>
    /// <param name="element">XML元素</param>
    public DocumentInfo(XElement element) : base(element)
    {
    }

    /// <summary>
    /// 构造新的文档信息元素
    /// </summary>
    public DocumentInfo() : base("DocumentInfo")
    {
    }

    /// <summary>
    /// 【可选 属性】
    /// 设置文档标识符
    /// </summary>
    /// <param name="id">文档标识符</param>
    /// <returns>this</returns>
    public DocumentInfo SetDocumentID(StId id)
    {
        SetAttribute("DocumentID", id.ToString());
        return this;
    }

    /// <summary>
    /// 【可选 属性】
    /// 获取文档标识符
    /// </summary>
    /// <returns>文档标识符</returns>
    public StId? GetDocumentID()
    {
        var value = GetAttributeValue("DocumentID");
        return string.IsNullOrEmpty(value) ? null : StId.Parse(value);
    }

    /// <summary>
    /// 设置文档标题
    /// </summary>
    /// <param name="title">文档标题</param>
    /// <returns>this</returns>
    public DocumentInfo SetTitle(string title)
    {
        SetOfdEntity("Title", title);
        return this;
    }

    /// <summary>
    /// 获取文档标题
    /// </summary>
    /// <returns>文档标题</returns>
    public string? GetTitle()
    {
        return GetOfdElementText("Title");
    }

    /// <summary>
    /// 设置文档作者
    /// </summary>
    /// <param name="author">文档作者</param>
    /// <returns>this</returns>
    public DocumentInfo SetAuthor(string author)
    {
        SetOfdEntity("Author", author);
        return this;
    }

    /// <summary>
    /// 获取文档作者
    /// </summary>
    /// <returns>文档作者</returns>
    public string? GetAuthor()
    {
        return GetOfdElementText("Author");
    }

    /// <summary>
    /// 设置文档主题
    /// </summary>
    /// <param name="subject">文档主题</param>
    /// <returns>this</returns>
    public DocumentInfo SetSubject(string subject)
    {
        SetOfdEntity("Subject", subject);
        return this;
    }

    /// <summary>
    /// 获取文档主题
    /// </summary>
    /// <returns>文档主题</returns>
    public string? GetSubject()
    {
        return GetOfdElementText("Subject");
    }

    /// <summary>
    /// 设置文档关键词
    /// </summary>
    /// <param name="keywords">文档关键词</param>
    /// <returns>this</returns>
    public DocumentInfo SetKeywords(string keywords)
    {
        SetOfdEntity("Keywords", keywords);
        return this;
    }

    /// <summary>
    /// 获取文档关键词
    /// </summary>
    /// <returns>文档关键词</returns>
    public string? GetKeywords()
    {
        return GetOfdElementText("Keywords");
    }

    /// <summary>
    /// 设置文档创建者
    /// </summary>
    /// <param name="creator">文档创建者</param>
    /// <returns>this</returns>
    public DocumentInfo SetCreator(string creator)
    {
        SetOfdEntity("Creator", creator);
        return this;
    }

    /// <summary>
    /// 获取文档创建者
    /// </summary>
    /// <returns>文档创建者</returns>
    public string? GetCreator()
    {
        return GetOfdElementText("Creator");
    }

    /// <summary>
    /// 设置文档创建应用程序
    /// </summary>
    /// <param name="producer">创建应用程序</param>
    /// <returns>this</returns>
    public DocumentInfo SetProducer(string producer)
    {
        SetOfdEntity("Producer", producer);
        return this;
    }

    /// <summary>
    /// 获取文档创建应用程序
    /// </summary>
    /// <returns>创建应用程序</returns>
    public string? GetProducer()
    {
        return GetOfdElementText("Producer");
    }

    /// <summary>
    /// 设置创建日期
    /// </summary>
    /// <param name="creationDate">创建日期</param>
    /// <returns>this</returns>
    public DocumentInfo SetCreationDate(DateTime creationDate)
    {
        SetOfdEntity("CreationDate", creationDate.ToString(Const.DateTimeFormat));
        return this;
    }

    /// <summary>
    /// 获取创建日期
    /// </summary>
    /// <returns>创建日期</returns>
    public DateTime? GetCreationDate()
    {
        var dateStr = GetOfdElementText("CreationDate");
        if (string.IsNullOrEmpty(dateStr))
            return null;
            
        return DateTime.TryParse(dateStr, out var date) ? date : null;
    }

    /// <summary>
    /// 设置修改日期
    /// </summary>
    /// <param name="modificationDate">修改日期</param>
    /// <returns>this</returns>
    public DocumentInfo SetModificationDate(DateTime modificationDate)
    {
        SetOfdEntity("ModificationDate", modificationDate.ToString(Const.DateTimeFormat));
        return this;
    }

    /// <summary>
    /// 获取修改日期
    /// </summary>
    /// <returns>修改日期</returns>
    public DateTime? GetModificationDate()
    {
        var dateStr = GetOfdElementText("ModificationDate");
        if (string.IsNullOrEmpty(dateStr))
            return null;
            
        return DateTime.TryParse(dateStr, out var date) ? date : null;
    }

    /// <summary>
    /// 设置文档版本
    /// </summary>
    /// <param name="version">文档版本</param>
    /// <returns>this</returns>
    public DocumentInfo SetVersion(string version)
    {
        SetOfdEntity("Version", version);
        return this;
    }

    /// <summary>
    /// 获取文档版本
    /// </summary>
    /// <returns>文档版本</returns>
    public string? GetVersion()
    {
        return GetOfdElementText("Version");
    }

    /// <summary>
    /// 设置页面数量
    /// </summary>
    /// <param name="pageCount">页面数量</param>
    /// <returns>this</returns>
    public DocumentInfo SetPageCount(int pageCount)
    {
        SetOfdEntity("PageCount", pageCount.ToString());
        return this;
    }

    /// <summary>
    /// 获取页面数量
    /// </summary>
    /// <returns>页面数量</returns>
    public int GetPageCount()
    {
        var countStr = GetOfdElementText("PageCount");
        return int.TryParse(countStr, out var count) ? count : 0;
    }

    /// <summary>
    /// 设置文档安全级别
    /// </summary>
    /// <param name="securityLevel">安全级别</param>
    /// <returns>this</returns>
    public DocumentInfo SetSecurityLevel(SecurityLevel securityLevel)
    {
        SetOfdEntity("SecurityLevel", securityLevel.ToString());
        return this;
    }

    /// <summary>
    /// 获取文档安全级别
    /// </summary>
    /// <returns>安全级别</returns>
    public SecurityLevel GetSecurityLevel()
    {
        var levelStr = GetOfdElementText("SecurityLevel");
        return Enum.TryParse<SecurityLevel>(levelStr, out var level) ? level : SecurityLevel.Normal;
    }

    /// <summary>
    /// 设置自定义属性
    /// </summary>
    /// <param name="name">属性名称</param>
    /// <param name="value">属性值</param>
    /// <returns>this</returns>
    public DocumentInfo SetCustomProperty(string name, string value)
    {
        var customProps = GetCustomProperties();
        if (customProps == null)
        {
            customProps = new CustomProperties();
            Add(customProps);
        }
        customProps.SetProperty(name, value);
        return this;
    }

    /// <summary>
    /// 获取自定义属性值
    /// </summary>
    /// <param name="name">属性名称</param>
    /// <returns>属性值</returns>
    public string? GetCustomProperty(string name)
    {
        var customProps = GetCustomProperties();
        return customProps?.GetProperty(name);
    }

    /// <summary>
    /// 获取所有自定义属性
    /// </summary>
    /// <returns>自定义属性集合</returns>
    public CustomProperties? GetCustomProperties()
    {
        var element = GetOfdElement("CustomProperties");
        return element == null ? null : new CustomProperties(element);
    }
}

/// <summary>
/// 文档根路径引用
/// 
/// 用于指向文档的根结构文件，通常指向Document.xml。
/// 这是OFD文档结构中的重要组成部分。
/// 
/// 对应OFD标准中的DocRoot定义
/// </summary>
public class DocRoot : OfdElement
{
    /// <summary>
    /// 从现有元素构造文档根路径
    /// </summary>
    /// <param name="element">XML元素</param>
    public DocRoot(XElement element) : base(element)
    {
    }

    /// <summary>
    /// 构造新的文档根路径元素
    /// </summary>
    public DocRoot() : base("DocRoot")
    {
    }

    /// <summary>
    /// 使用路径构造文档根路径
    /// </summary>
    /// <param name="path">根文档路径</param>
    public DocRoot(string path) : base("DocRoot")
    {
        SetPath(path);
    }

    /// <summary>
    /// 使用StLoc构造文档根路径
    /// </summary>
    /// <param name="location">根文档位置</param>
    public DocRoot(StLoc location) : base("DocRoot")
    {
        SetLocation(location);
    }

    /// <summary>
    /// 设置根文档路径
    /// </summary>
    /// <param name="path">根文档路径</param>
    /// <returns>this</returns>
    public DocRoot SetPath(string path)
    {
        Element.Value = path;
        return this;
    }

    /// <summary>
    /// 获取根文档路径
    /// </summary>
    /// <returns>根文档路径</returns>
    public string GetPath()
    {
        return Element.Value ?? "Doc/Document.xml"; // 默认路径
    }

    /// <summary>
    /// 设置根文档位置
    /// </summary>
    /// <param name="location">根文档位置</param>
    /// <returns>this</returns>
    public DocRoot SetLocation(StLoc location)
    {
        Element.Value = location.ToString();
        return this;
    }

    /// <summary>
    /// 获取根文档位置
    /// </summary>
    /// <returns>根文档位置</returns>
    public StLoc GetLocation()
    {
        var path = Element.Value;
        return string.IsNullOrEmpty(path) 
            ? new StLoc("Doc/Document.xml") 
            : new StLoc(path);
    }

    /// <summary>
    /// 检查路径是否有效
    /// </summary>
    /// <returns>路径是否有效</returns>
    public bool IsValidPath()
    {
        var path = GetPath();
        return !string.IsNullOrWhiteSpace(path) && 
               (path.EndsWith(".xml", StringComparison.OrdinalIgnoreCase) ||
                path.EndsWith(".ofd", StringComparison.OrdinalIgnoreCase));
    }

    /// <summary>
    /// 获取文件名
    /// </summary>
    /// <returns>文件名</returns>
    public string GetFileName()
    {
        var path = GetPath();
        return Path.GetFileName(path) ?? "Document.xml";
    }

    /// <summary>
    /// 获取目录路径
    /// </summary>
    /// <returns>目录路径</returns>
    public string GetDirectory()
    {
        var path = GetPath();
        return Path.GetDirectoryName(path) ?? "Doc";
    }
}

/// <summary>
/// 文档安全级别枚举
/// </summary>
public enum SecurityLevel
{
    /// <summary>
    /// 普通级别
    /// </summary>
    Normal,

    /// <summary>
    /// 受限级别
    /// </summary>
    Restricted,

    /// <summary>
    /// 机密级别
    /// </summary>
    Confidential,

    /// <summary>
    /// 秘密级别
    /// </summary>
    Secret
}

/// <summary>
/// 自定义属性集合
/// </summary>
public class CustomProperties : OfdElement
{
    /// <summary>
    /// 从现有元素构造自定义属性集合
    /// </summary>
    /// <param name="element">XML元素</param>
    public CustomProperties(XElement element) : base(element)
    {
    }

    /// <summary>
    /// 构造新的自定义属性集合
    /// </summary>
    public CustomProperties() : base("CustomProperties")
    {
    }

    /// <summary>
    /// 设置属性值
    /// </summary>
    /// <param name="name">属性名称</param>
    /// <param name="value">属性值</param>
    /// <returns>this</returns>
    public CustomProperties SetProperty(string name, string value)
    {
        var property = GetOfdElement(name);
        if (property == null)
        {
            AddOfdEntity(name, value);
        }
        else
        {
            property.Value = value;
        }
        return this;
    }

    /// <summary>
    /// 获取属性值
    /// </summary>
    /// <param name="name">属性名称</param>
    /// <returns>属性值</returns>
    public string? GetProperty(string name)
    {
        return GetOfdElementText(name);
    }

    /// <summary>
    /// 删除属性
    /// </summary>
    /// <param name="name">属性名称</param>
    /// <returns>this</returns>
    public CustomProperties RemoveProperty(string name)
    {
        var property = GetOfdElement(name);
        property?.Remove();
        return this;
    }

    /// <summary>
    /// 获取所有属性名称
    /// </summary>
    /// <returns>属性名称列表</returns>
    public List<string> GetPropertyNames()
    {
        return Element.Elements().Select(e => e.Name.LocalName).ToList();
    }

    /// <summary>
    /// 获取属性数量
    /// </summary>
    /// <returns>属性数量</returns>
    public int GetPropertyCount()
    {
        return Element.Elements().Count();
    }
}
