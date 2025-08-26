using System.Xml.Linq;

namespace OfdrwNet.Core.BasicStructure.Doc;

/// <summary>
/// 视图首选项
/// 对应 Java 版本的 org.ofdrw.core.basicStructure.doc.vpreferences.CT_VPreferences
/// </summary>
public class CtVPreferences : OfdElement
{
    /// <summary>
    /// 从现有元素构造视图首选项
    /// </summary>
    /// <param name="element">XML元素</param>
    public CtVPreferences(XElement element) : base(element)
    {
    }

    /// <summary>
    /// 构造新的视图首选项元素
    /// </summary>
    public CtVPreferences() : base("VPreferences")
    {
    }
}
