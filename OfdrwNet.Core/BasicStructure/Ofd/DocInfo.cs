using System;
using System.Globalization;
using System.Xml.Linq;
using OfdrwNet.Core;
using OfdrwNet.Core.BasicType;

namespace OfdrwNet.Core.BasicStructure.Ofd;

/// <summary>
/// 文档元数据信息描述
/// 对应 Java 版本的 org.ofdrw.core.basicStructure.ofd.docInfo.CT_DocInfo
/// </summary>
public class DocInfo : OfdElement
{
    /// <summary>
    /// 基于现有Element构造
    /// </summary>
    /// <param name="element">现有元素</param>
    public DocInfo(XElement element) : base(element)
    {
    }

    /// <summary>
    /// 创建新的DocInfo
    /// </summary>
    public DocInfo() : base("DocInfo")
    {
    }

    /// <summary>
    /// 【必选】设置文件标识符，标识符应该是一个UUID
    /// </summary>
    /// <param name="docId">UUID文件标识</param>
    /// <returns>this</returns>
    public DocInfo SetDocId(Guid docId)
    {
        SetOfdEntity("DocID", docId.ToString("N")); // "N" 格式去掉连字符
        return this;
    }

    /// <summary>
    /// 随机产生一个UUID作为文件标识符
    /// </summary>
    /// <returns>this</returns>
    public DocInfo RandomDocId()
    {
        return SetDocId(Guid.NewGuid());
    }

    /// <summary>
    /// 【必选】采用UUID算法生成的由32个字符组成的文件标识
    /// 每个DocID在文件创建或生成的时候进行分配
    /// </summary>
    /// <returns>文件标识符</returns>
    public string? GetDocId()
    {
        return GetOfdElementText("DocID");
    }

    /// <summary>
    /// 【可选】设置文档标题。标题可以与文件名不同
    /// </summary>
    /// <param name="title">标题</param>
    /// <returns>this</returns>
    public DocInfo SetTitle(string title)
    {
        SetOfdEntity("Title", title);
        return this;
    }

    /// <summary>
    /// 【可选】获取文档标题。标题可以与文件名不同
    /// </summary>
    /// <returns>文档标题</returns>
    public string? GetTitle()
    {
        return GetOfdElementText("Title");
    }

    /// <summary>
    /// 【可选】设置文档作者
    /// </summary>
    /// <param name="author">文档作者</param>
    /// <returns>this</returns>
    public DocInfo SetAuthor(string author)
    {
        SetOfdEntity("Author", author);
        return this;
    }

    /// <summary>
    /// 【可选】获取文档作者
    /// </summary>
    /// <returns>文档作者</returns>
    public string? GetAuthor()
    {
        return GetOfdElementText("Author");
    }

    /// <summary>
    /// 【可选】设置文档主题
    /// </summary>
    /// <param name="subject">文档主题</param>
    /// <returns>this</returns>
    public DocInfo SetSubject(string subject)
    {
        SetOfdEntity("Subject", subject);
        return this;
    }

    /// <summary>
    /// 【可选】获取文档主题
    /// </summary>
    /// <returns>文档主题</returns>
    public string? GetSubject()
    {
        return GetOfdElementText("Subject");
    }

    /// <summary>
    /// 【可选】设置文档摘要与注释
    /// </summary>
    /// <param name="abstractText">文档摘要与注释</param>
    /// <returns>this</returns>
    public DocInfo SetAbstract(string abstractText)
    {
        SetOfdEntity("Abstract", abstractText);
        return this;
    }

    /// <summary>
    /// 【可选】获取文档摘要与注释
    /// </summary>
    /// <returns>文档摘要与注释</returns>
    public string? GetAbstract()
    {
        return GetOfdElementText("Abstract");
    }

    /// <summary>
    /// 【可选】设置文件创建日期
    /// </summary>
    /// <param name="creationDate">文件创建日期</param>
    /// <returns>this</returns>
    public DocInfo SetCreationDate(DateTime creationDate)
    {
        SetOfdEntity("CreationDate", creationDate.ToString(Const.DateFormat));
        return this;
    }

    /// <summary>
    /// 【可选】获取文件创建日期
    /// </summary>
    /// <returns>创建日期</returns>
    public DateTime? GetCreationDate()
    {
        var dateStr = GetOfdElementText("CreationDate");
        if (string.IsNullOrWhiteSpace(dateStr))
            return null;
        
        if (DateTime.TryParseExact(dateStr, Const.DateFormat, CultureInfo.InvariantCulture, DateTimeStyles.None, out var date))
            return date;
        
        return null;
    }

    /// <summary>
    /// 【可选】设置文档最近修改日期
    /// </summary>
    /// <param name="modDate">文档最近修改日期</param>
    /// <returns>this</returns>
    public DocInfo SetModDate(DateTime modDate)
    {
        SetOfdEntity("ModDate", modDate.ToString(Const.DateFormat));
        return this;
    }

    /// <summary>
    /// 【可选】获取文档最近修改日期
    /// </summary>
    /// <returns>文档最近修改日期</returns>
    public DateTime? GetModDate()
    {
        var dateStr = GetOfdElementText("ModDate");
        if (string.IsNullOrWhiteSpace(dateStr))
            return null;
        
        if (DateTime.TryParseExact(dateStr, Const.DateFormat, CultureInfo.InvariantCulture, DateTimeStyles.None, out var date))
            return date;
        
        return null;
    }

    /// <summary>
    /// 【可选】设置文档分类，可取值如下：
    /// Normal——普通文档
    /// EBook——电子书  
    /// ENewsPaper——电子报纸
    /// EMagzine——电子期刊
    /// 默认值为 Normal
    /// </summary>
    /// <param name="docUsage">文档分类</param>
    /// <returns>this</returns>
    public DocInfo SetDocUsage(DocUsage docUsage)
    {
        SetOfdEntity("DocUsage", docUsage.ToString());
        return this;
    }

    /// <summary>
    /// 【可选】获取文档分类
    /// 默认值为 Normal
    /// </summary>
    /// <returns>文档分类</returns>
    public DocUsage GetDocUsage()
    {
        var usageStr = GetOfdElementText("DocUsage");
        return DocUsageExtensions.FromString(usageStr);
    }

    /// <summary>
    /// 【可选】设置文档封面，此路径指向一个图片文件
    /// </summary>
    /// <param name="cover">文档封面路径</param>
    /// <returns>this</returns>
    public DocInfo SetCover(StLoc cover)
    {
        var element = new XElement(Const.OfdNamespace + "Cover", cover.ToString());
        Set(new OfdElement(element));
        return this;
    }

    /// <summary>
    /// 【可选】获取文档封面，此路径指向一个图片文件
    /// </summary>
    /// <returns>文档封面路径</returns>
    public StLoc? GetCover()
    {
        var locStr = GetOfdElementText("Cover");
        if (string.IsNullOrWhiteSpace(locStr))
        {
            return null;
        }
        return new StLoc(locStr);
    }

    /// <summary>
    /// 【可选】设置关键词集合
    /// 每一个关键词用一个"Keyword"子节点来表达
    /// </summary>
    /// <param name="keywords">关键词集合</param>
    /// <returns>this</returns>
    public DocInfo SetKeywords(Keywords keywords)
    {
        Set(keywords);
        return this;
    }

    /// <summary>
    /// 添加关键词
    /// </summary>
    /// <param name="keyword">关键词</param>
    /// <returns>this</returns>
    public DocInfo AddKeyword(string keyword)
    {
        var keywords = GetKeywords();
        if (keywords == null)
        {
            keywords = new Keywords();
            Add(keywords);
        }
        keywords.AddKeyword(keyword);
        return this;
    }

    /// <summary>
    /// 【可选】获取关键词集合
    /// </summary>
    /// <returns>关键词集合或null</returns>
    public Keywords? GetKeywords()
    {
        var element = GetOfdElement("Keywords");
        return element == null ? null : new Keywords(element);
    }

    /// <summary>
    /// 【可选】设置创建文档的应用程序
    /// </summary>
    /// <param name="creator">创建文档的应用程序</param>
    /// <returns>this</returns>
    public DocInfo SetCreator(string creator)
    {
        SetOfdEntity("Creator", creator);
        return this;
    }

    /// <summary>
    /// 【可选】获取创建文档的应用程序
    /// </summary>
    /// <returns>创建文档的应用程序或null</returns>
    public string? GetCreator()
    {
        return GetOfdElementText("Creator");
    }

    /// <summary>
    /// 【可选】设置创建文档的应用程序版本信息
    /// </summary>
    /// <param name="creatorVersion">创建文档的应用程序版本信息</param>
    /// <returns>this</returns>
    public DocInfo SetCreatorVersion(string creatorVersion)
    {
        SetOfdEntity("CreatorVersion", creatorVersion);
        return this;
    }

    /// <summary>
    /// 【可选】获取创建文档的应用程序版本信息
    /// </summary>
    /// <returns>创建文档的应用程序版本信息或null</returns>
    public string? GetCreatorVersion()
    {
        return GetOfdElementText("CreatorVersion");
    }

    /// <summary>
    /// 【可选】设置用户自定义元数据集合。其子节点为 CustomData
    /// </summary>
    /// <param name="customDatas">用户自定义元数据集合</param>
    /// <returns>this</returns>
    public DocInfo SetCustomDatas(CustomDatas customDatas)
    {
        Set(customDatas);
        return this;
    }

    /// <summary>
    /// 【可选】获取用户自定义元数据集合。其子节点为 CustomData
    /// </summary>
    /// <returns>用户自定义元数据集合</returns>
    public CustomDatas? GetCustomDatas()
    {
        var element = GetOfdElement("CustomDatas");
        return element == null ? null : new CustomDatas(element);
    }

    /// <summary>
    /// 获取限定名称
    /// </summary>
    public override string QualifiedName => "ofd:DocInfo";
}

/// <summary>
/// 文档分类枚举
/// </summary>
public enum DocUsage
{
    /// <summary>
    /// 普通文档
    /// </summary>
    Normal,
    
    /// <summary>
    /// 电子书
    /// </summary>
    EBook,
    
    /// <summary>
    /// 电子报纸
    /// </summary>
    ENewsPaper,
    
    /// <summary>
    /// 电子期刊
    /// </summary>
    EMagzine
}

/// <summary>
/// DocUsage扩展方法
/// </summary>
public static class DocUsageExtensions
{
    /// <summary>
    /// 从字符串转换为DocUsage
    /// </summary>
    /// <param name="value">字符串值</param>
    /// <returns>DocUsage枚举值</returns>
    public static DocUsage FromString(string? value)
    {
        return value switch
        {
            "EBook" => DocUsage.EBook,
            "ENewsPaper" => DocUsage.ENewsPaper,
            "EMagzine" => DocUsage.EMagzine,
            _ => DocUsage.Normal // 默认值
        };
    }
}

/// <summary>
/// 占位符类 - 关键词集合
/// 待后续实现
/// </summary>
public class Keywords : OfdElement
{
    public Keywords(XElement element) : base(element) { }
    public Keywords() : base("Keywords") { }
    
    public Keywords AddKeyword(string keyword)
    {
        AddOfdEntity("Keyword", keyword);
        return this;
    }
    
    public override string QualifiedName => "ofd:Keywords";
}

/// <summary>
/// 占位符类 - 自定义数据集合
/// 待后续实现
/// </summary>
public class CustomDatas : OfdElement
{
    public CustomDatas(XElement element) : base(element) { }
    public CustomDatas() : base("CustomDatas") { }
    public override string QualifiedName => "ofd:CustomDatas";
}