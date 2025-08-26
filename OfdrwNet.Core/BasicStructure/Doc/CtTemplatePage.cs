using System.Xml.Linq;

namespace OfdrwNet.Core.BasicStructure.Doc;

/// <summary>
/// 模板页
/// 对应 Java 版本的 org.ofdrw.core.basicStructure.pageObj.CT_TemplatePage
/// </summary>
public class CtTemplatePage : OfdElement
{
    /// <summary>
    /// 从现有元素构造模板页
    /// </summary>
    /// <param name="element">XML元素</param>
    public CtTemplatePage(XElement element) : base(element)
    {
    }

    /// <summary>
    /// 构造新的模板页元素
    /// </summary>
    public CtTemplatePage() : base("TemplatePage")
    {
    }
}
