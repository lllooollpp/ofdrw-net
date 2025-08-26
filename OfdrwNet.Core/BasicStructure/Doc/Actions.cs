using System.Xml.Linq;

namespace OfdrwNet.Core.BasicStructure.Doc;

/// <summary>
/// 动作序列
/// 对应 Java 版本的 org.ofdrw.core.action.Actions
/// </summary>
public class Actions : OfdElement
{
    /// <summary>
    /// 从现有元素构造动作序列
    /// </summary>
    /// <param name="element">XML元素</param>
    public Actions(XElement element) : base(element)
    {
    }

    /// <summary>
    /// 构造新的动作序列元素
    /// </summary>
    public Actions() : base("Actions")
    {
    }
}
