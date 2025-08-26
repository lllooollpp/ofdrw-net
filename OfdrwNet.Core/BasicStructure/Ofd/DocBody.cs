using System.Xml.Linq;
using OfdrwNet.Core;
using OfdrwNet.Core.BasicType;

namespace OfdrwNet.Core.BasicStructure.Ofd;

/// <summary>
/// 文件对象入口，可以存在多个，以便在一个文档中包含多个版式文档
/// 对应 Java 版本的 org.ofdrw.core.basicStructure.ofd.DocBody
/// </summary>
public class DocBody : OfdElement
{
    /// <summary>
    /// 文档根节点文档名称
    /// </summary>
    public const string DocRoot = "DocRoot";

    /// <summary>
    /// 基于现有Element构造
    /// </summary>
    /// <param name="element">现有元素</param>
    public DocBody(XElement element) : base(element)
    {
    }

    /// <summary>
    /// 创建新的DocBody
    /// </summary>
    public DocBody() : base("DocBody")
    {
    }

    /// <summary>
    /// 【必选】设置文档元数据信息描述
    /// </summary>
    /// <param name="docInfo">文档元数据信息描述</param>
    /// <returns>this</returns>
    public DocBody SetDocInfo(DocInfo docInfo)
    {
        Set(docInfo);
        return this;
    }

    /// <summary>
    /// 【必选】获取文档元数据信息描述
    /// </summary>
    /// <returns>文档元数据信息描述 或null</returns>
    public DocInfo? GetDocInfo()
    {
        var element = GetOfdElement("DocInfo");
        return element == null ? null : new DocInfo(element);
    }

    /// <summary>
    /// 【可选】设置指向文档根节点文档
    /// </summary>
    /// <param name="docRoot">指向根节点文档路径</param>
    /// <returns>this</returns>
    public DocBody SetDocRoot(StLoc docRoot)
    {
        var element = new XElement(Const.OfdNamespace + DocRoot, docRoot.ToString());
        Set(new OfdElement(element));
        return this;
    }

    /// <summary>
    /// 【可选】获取指向文档根节点文档路径
    /// </summary>
    /// <returns>指向文档根节点文档路径</returns>
    public StLoc? GetDocRoot()
    {
        var locStr = GetOfdElementText("DocRoot");
        if (string.IsNullOrWhiteSpace(locStr))
        {
            return null;
        }
        return new StLoc(locStr);
    }

    /// <summary>
    /// 【可选】设置包含多个版本描述节点，用于定义文件因注释和其他改动产生的版本信息
    /// </summary>
    /// <param name="versions">版本序列</param>
    /// <returns>this</returns>
    public DocBody SetVersions(Versions versions)
    {
        Set(versions);
        return this;
    }

    /// <summary>
    /// 【可选】获取包含多个版本描述序列
    /// </summary>
    /// <returns>包含多个版本描述序列</returns>
    public Versions? GetVersions()
    {
        var element = GetOfdElement("Versions");
        return element == null ? null : new Versions(element);
    }

    /// <summary>
    /// 【可选】设置指向该文档中签名和签章结构的路径
    /// </summary>
    /// <param name="signatures">指向该文档中签名和签章结构的路径</param>
    /// <returns>this</returns>
    public DocBody SetSignatures(StLoc signatures)
    {
        SetOfdEntity("Signatures", signatures.ToString());
        return this;
    }

    /// <summary>
    /// 【可选】获取指向该文档中签名和签章结构的路径
    /// </summary>
    /// <returns>指向该文档中签名和签章结构的路径</returns>
    public StLoc? GetSignatures()
    {
        var locStr = GetOfdElementText("Signatures");
        if (string.IsNullOrWhiteSpace(locStr))
        {
            return null;
        }
        return new StLoc(locStr);
    }

    /// <summary>
    /// 获取限定名称
    /// </summary>
    public override string QualifiedName => "ofd:DocBody";
}

/// <summary>
/// 占位符类 - 版本管理
/// 待后续实现
/// </summary>
public class Versions : OfdElement
{
    public Versions(XElement element) : base(element) { }
    public Versions() : base("Versions") { }
    public override string QualifiedName => "ofd:Versions";
}