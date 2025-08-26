using System.Xml.Linq;
using OfdrwNet.Core.BasicType;

namespace OfdrwNet.Core.BasicStructure.Doc;

/// <summary>
/// 文档公共数据结构
/// 
/// ————《GB/T 33190-2016》 图 6
/// 
/// 对应 Java 版本的 org.ofdrw.core.basicStructure.doc.CT_CommonData
/// </summary>
public class CtCommonData : OfdElement
{
    /// <summary>
    /// 从现有元素构造公共数据
    /// </summary>
    /// <param name="element">XML元素</param>
    public CtCommonData(XElement element) : base(element)
    {
    }

    /// <summary>
    /// 构造新的公共数据元素
    /// </summary>
    public CtCommonData() : base("CommonData")
    {
    }

    /// <summary>
    /// 【必选】
    /// 设置当前文档中所有对象使用标识的最大值。
    /// 初始值为 0。MaxUnitID主要用于文档编辑，
    /// 在向文档增加一个新对象时，需要分配一个
    /// 新的标识符，新标识符取值宜为 MaxUnitID + 1，
    /// 同时需要修改此 MaxUnitID值。
    /// </summary>
    /// <param name="maxUnitId">对象标识符最大值</param>
    /// <returns>this</returns>
    public CtCommonData SetMaxUnitId(StId maxUnitId)
    {
        SetOfdEntity("MaxUnitID", maxUnitId);
        return this;
    }

    /// <summary>
    /// 【必选】
    /// 设置当前文档中所有对象使用标识的最大值。
    /// 初始值为 0。MaxUnitID主要用于文档编辑，
    /// 在向文档增加一个新对象时，需要分配一个
    /// 新的标识符，新标识符取值宜为 MaxUnitID + 1，
    /// 同时需要修改此 MaxUnitID值。
    /// </summary>
    /// <param name="maxUnitId">对象标识符最大值</param>
    /// <returns>this</returns>
    public CtCommonData SetMaxUnitId(long maxUnitId)
    {
        return SetMaxUnitId(new StId(maxUnitId));
    }

    /// <summary>
    /// 【必选】
    /// 获取当前文档中所有对象使用标识的最大值
    /// </summary>
    /// <returns>当前文档中所有对象使用标识的最大值</returns>
    public StId? GetMaxUnitId()
    {
        var text = GetOfdElementText("MaxUnitID");
        return string.IsNullOrEmpty(text) ? null : StId.Parse(text);
    }

    /// <summary>
    /// 【必选】
    /// 设置该文档页面区域的默认大小和位置
    /// </summary>
    /// <param name="pageArea">文档页面区域的默认大小和位置</param>
    /// <returns>this</returns>
    public CtCommonData SetPageArea(CtPageArea pageArea)
    {
        Set(pageArea);
        return this;
    }

    /// <summary>
    /// 【必选】
    /// 获取该文档页面区域的默认大小和位置
    /// </summary>
    /// <returns>该文档页面区域的默认大小和位置</returns>
    public CtPageArea? GetPageArea()
    {
        var element = GetOfdElement("PageArea");
        return element == null ? null : new CtPageArea(element);
    }

    /// <summary>
    /// 【可选】
    /// 设置公共资源序列路径（如果已经存在PublicRes那么替换）
    /// 公共资源序列，每个节点指向OFD包内的一个资源描述文件，
    /// 源部分的描述见 7.9，字形和颜色空间等宜在公共资源文件中描述
    /// </summary>
    /// <param name="publicRes">公共资源序列</param>
    /// <returns>this</returns>
    public CtCommonData SetPublicRes(StLoc publicRes)
    {
        SetOfdEntity("PublicRes", publicRes);
        return this;
    }

    /// <summary>
    /// 【可选】
    /// 获取公共资源序列（列表中的第一个PublicRes）
    /// 公共资源序列，每个节点指向OFD包内的一个资源描述文件，
    /// 源部分的描述见 7.9，字形和颜色空间等宜在公共资源文件中描述
    /// </summary>
    /// <returns>公共资源序列路径</returns>
    public StLoc? GetPublicRes()
    {
        var text = GetOfdElementText("PublicRes");
        return string.IsNullOrEmpty(text) ? null : StLoc.Parse(text);
    }

    /// <summary>
    /// 【可选】
    /// 添加公共资源序列路径
    /// 公共资源序列，每个节点指向OFD包内的一个资源描述文件，
    /// 源部分的描述见 7.9，字形和颜色空间等宜在公共资源文件中描述
    /// </summary>
    /// <param name="publicRes">公共资源序列</param>
    /// <returns>this</returns>
    public CtCommonData AddPublicRes(StLoc publicRes)
    {
        if (publicRes?.ToString() != null)
        {
            AddOfdEntity("PublicRes", publicRes);
        }
        return this;
    }

    /// <summary>
    /// 【可选】
    /// 获取公共资源序列
    /// 公共资源序列，每个节点指向OFD包内的一个资源描述文件，
    /// 源部分的描述见 7.9，字形和颜色空间等宜在公共资源文件中描述
    /// PublicRes 数量为 0~n
    /// </summary>
    /// <returns>公共资源序列路径</returns>
    public List<StLoc> GetPublicResList()
    {
        return GetOfdElements("PublicRes")
            .Select(element => element.Value)
            .Where(text => !string.IsNullOrEmpty(text))
            .Select(text => StLoc.Parse(text))
            .ToList();
    }

    /// <summary>
    /// 【可选】
    /// 设置文件资源序列路径（DocumentRes已经存在那么替换）
    /// 公共资源序列，每个节点指向OFD包内的一个资源描述文件，
    /// 源部分的描述见 7.9，
    /// 绘制参数、多媒体和矢量图像等宜在文件资源文件中描述
    /// </summary>
    /// <param name="documentRes">公共资源序列</param>
    /// <returns>this</returns>
    public CtCommonData SetDocumentRes(StLoc documentRes)
    {
        SetOfdEntity("DocumentRes", documentRes);
        return this;
    }

    /// <summary>
    /// 【可选】
    /// 添加文件资源序列路径
    /// 公共资源序列，每个节点指向OFD包内的一个资源描述文件，
    /// 源部分的描述见 7.9，
    /// 绘制参数、多媒体和矢量图像等宜在文件资源文件中描述
    /// </summary>
    /// <param name="documentRes">公共资源序列</param>
    /// <returns>this</returns>
    public CtCommonData AddDocumentRes(StLoc documentRes)
    {
        if (documentRes?.ToString() != null)
        {
            AddOfdEntity("DocumentRes", documentRes);
        }
        return this;
    }

    /// <summary>
    /// 【可选】
    /// 获取文件资源序列路径
    /// 公共资源序列，每个节点指向OFD包内的一个资源描述文件，
    /// 源部分的描述见 7.9，
    /// 绘制参数、多媒体和矢量图像等宜在文件资源文件中描述
    /// </summary>
    /// <returns>文件资源序列路径</returns>
    public StLoc? GetDocumentRes()
    {
        var text = GetOfdElementText("DocumentRes");
        return string.IsNullOrEmpty(text) ? null : StLoc.Parse(text);
    }

    /// <summary>
    /// 【可选】
    /// 获取文件资源序列
    /// 公共资源序列，每个节点指向OFD包内的一个资源描述文件，
    /// 源部分的描述见 7.9，
    /// 绘制参数、多媒体和矢量图像等宜在文件资源文件中描述
    /// DocumentRes 数量为 0~n
    /// </summary>
    /// <returns>文件资源序列路径</returns>
    public List<StLoc> GetDocumentResList()
    {
        return GetOfdElements("DocumentRes")
            .Select(element => element.Value)
            .Where(text => !string.IsNullOrEmpty(text))
            .Select(text => StLoc.Parse(text))
            .ToList();
    }

    /// <summary>
    /// 【可选】
    /// 增加模板页序列
    /// 为一系列的模板页的集合，模板页内容机构和普通页相同，描述将7.7
    /// </summary>
    /// <param name="templatePage">模板页序列</param>
    /// <returns>this</returns>
    public CtCommonData AddTemplatePage(CtTemplatePage templatePage)
    {
        Add(templatePage);
        return this;
    }

    /// <summary>
    /// 【可选】
    /// 获取模板页序列
    /// 为一系列的模板页的集合，模板页内容机构和普通页相同，描述将7.7
    /// </summary>
    /// <returns>模板页序列（可能为空容器）</returns>
    public List<CtTemplatePage> GetTemplatePages()
    {
        return GetOfdElements("TemplatePage")
            .Select(element => new CtTemplatePage(element))
            .ToList();
    }

    /// <summary>
    /// 【可选】
    /// 设置引用在资源文件中定义的颜色标识符
    /// 有关颜色空间的描述见 8.3.1。如果不存在此项，采用RGB作为默认颜色空间
    /// </summary>
    /// <param name="defaultCs">颜色空间引用</param>
    /// <returns>this</returns>
    public CtCommonData SetDefaultCs(StRefId defaultCs)
    {
        SetOfdEntity("DefaultCS", defaultCs);
        return this;
    }

    /// <summary>
    /// 【可选】
    /// 获取引用在资源文件中定义的颜色标识符
    /// 有关颜色空间的描述见 8.3.1。如果不存在此项，采用RGB作为默认颜色空间
    /// </summary>
    /// <returns>颜色空间引用</returns>
    public StRefId? GetDefaultCs()
    {
        var text = GetOfdElementText("DefaultCS");
        return string.IsNullOrEmpty(text) ? null : StRefId.Parse(text);
    }
}
