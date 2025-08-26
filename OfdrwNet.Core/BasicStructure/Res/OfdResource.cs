using System.Xml.Linq;

namespace OfdrwNet.Core.BasicStructure.Res;

/// <summary>
/// OFD资源基类
/// 对应 Java 版本的 org.ofdrw.core.basicStructure.res.OFDResource
/// </summary>
public abstract class OfdResource : OfdElement
{
    /// <summary>
    /// 从现有元素构造资源
    /// </summary>
    /// <param name="element">XML元素</param>
    protected OfdResource(XElement element) : base(element)
    {
    }

    /// <summary>
    /// 构造新的资源元素
    /// </summary>
    /// <param name="name">元素名称</param>
    protected OfdResource(string name) : base(name)
    {
    }

    /// <summary>
    /// 获取资源实例
    /// </summary>
    /// <param name="element">XML元素</param>
    /// <returns>资源实例</returns>
    public static OfdResource GetInstance(XElement element)
    {
        // 根据元素名称创建对应的资源类型
        return element.Name.LocalName switch
        {
            "Fonts" => new Fonts(element),
            "ColorSpaces" => new ColorSpaces(element),
            "DrawParams" => new DrawParams(element),
            "MultiMedias" => new MultiMedias(element),
            _ => new GenericResource(element)
        };
    }
}

/// <summary>
/// 通用资源类
/// </summary>
internal class GenericResource : OfdResource
{
    public GenericResource(XElement element) : base(element)
    {
    }
}
