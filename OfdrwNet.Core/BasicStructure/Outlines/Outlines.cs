using System.Xml.Linq;

namespace OfdrwNet.Core.BasicStructure.Outlines;

/// <summary>
/// 大纲
/// 对应 Java 版本的 org.ofdrw.core.basicStructure.outlines.Outlines
/// </summary>
public class Outlines : OfdElement
{
    /// <summary>
    /// 从现有元素构造大纲
    /// </summary>
    /// <param name="element">XML元素</param>
    public Outlines(XElement element) : base(element)
    {
    }

    /// <summary>
    /// 构造新的大纲元素
    /// </summary>
    public Outlines() : base("Outlines")
    {
    }
}
