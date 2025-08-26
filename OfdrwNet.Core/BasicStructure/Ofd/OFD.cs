using System.Collections.Generic;
using System.Linq;
using System.Xml.Linq;
using OfdrwNet.Core;

namespace OfdrwNet.Core.BasicStructure.Ofd;

/// <summary>
/// OFD文档主入口类
/// 对应 OFD.xml 根节点
/// 参考：《GB/T 33190-2016》 图 3
/// </summary>
public class OFD : OfdElement
{
    /// <summary>
    /// 【必选】文件格式的版本号
    /// 固定值：1.2
    /// 参照表 3
    /// </summary>
    public const string VERSION = "1.2";

    /// <summary>
    /// 【必选】文件格式子集类型，取值为"OFD"，表明此文件符合本标准
    /// </summary>
    public const string DOC_TYPE = "OFD";

    /// <summary>
    /// 基于现有Element构造
    /// </summary>
    /// <param name="element">现有元素</param>
    public OFD(XElement element) : base(element)
    {
    }

    /// <summary>
    /// 创建新的OFD根节点
    /// </summary>
    public OFD() : base("OFD")
    {
        // 添加命名空间声明
        Element.SetAttributeValue(XNamespace.Xmlns + Const.OfdValue, Const.OfdNamespaceUri);
        SetAttribute("Version", VERSION);
        SetAttribute("DocType", DOC_TYPE);
    }

    /// <summary>
    /// 通过文件对象入口列表创建文档对象
    /// </summary>
    /// <param name="docBodies">文件对象入口序列</param>
    public OFD(IEnumerable<DocBody> docBodies) : this()
    {
        foreach (var docBody in docBodies.Where(db => db != null))
        {
            Add(docBody);
        }
    }

    /// <summary>
    /// 通过文件对象入口创建文档对象
    /// </summary>
    /// <param name="docBody">文件对象入口</param>
    public OFD(DocBody docBody) : this()
    {
        Add(docBody);
    }

    /// <summary>
    /// 【必选 属性】获取文件格式版本号
    /// </summary>
    public string Version => GetAttributeValue("Version") ?? VERSION;

    /// <summary>
    /// 【必选 属性】设置文件版本号
    /// </summary>
    /// <param name="version">版本号</param>
    /// <returns>this</returns>
    public OFD SetVersion(string version)
    {
        SetAttribute("Version", version);
        return this;
    }

    /// <summary>
    /// 【必选 属性】文件格式子集类型，取值为"OFD"，表明此文件符合本标准
    /// </summary>
    public string DocType => DOC_TYPE;

    /// <summary>
    /// 【必选】增加文件对象入口
    /// 文件对象入口，可以存在多个，以便在一个文档中包含多个版式文档
    /// </summary>
    /// <param name="docBody">文件对象入口</param>
    /// <returns>this</returns>
    public OFD AddDocBody(DocBody docBody)
    {
        Add(docBody);
        return this;
    }

    /// <summary>
    /// 【必选】获取第一个文档入口
    /// </summary>
    /// <returns>文件对象入口（如果有多个则获取第一个）</returns>
    public DocBody? GetDocBody()
    {
        var element = GetOfdElement("DocBody");
        return element == null ? null : new DocBody(element);
    }

    /// <summary>
    /// 获取指定序号的文档
    /// </summary>
    /// <param name="index">文档序号，从0起</param>
    /// <returns>文件对象入口</returns>
    public DocBody GetDocBody(int index)
    {
        return GetDocBodies()[index];
    }

    /// <summary>
    /// 获取所有文档入口
    /// </summary>
    /// <returns>所有文档入口</returns>
    public List<DocBody> GetDocBodies()
    {
        return GetOfdElements("DocBody").Select(e => new DocBody(e)).ToList();
    }

    /// <summary>
    /// 获取限定名称
    /// </summary>
    public override string QualifiedName => "ofd:OFD";
    
    /// <summary>
    /// 转换为XML字符串
    /// </summary>
    /// <returns>XML字符串</returns>
    public new string ToXml()
    {
        // 确保根元素有正确的命名空间声明
        var elementCopy = new XElement(Element);
        if (elementCopy.Name.Namespace == Const.OfdNamespace)
        {
            // 添加命名空间声明到根元素
            elementCopy.SetAttributeValue(XNamespace.Xmlns + Const.OfdValue, Const.OfdNamespaceUri);
        }
        return elementCopy.ToString();
    }
}