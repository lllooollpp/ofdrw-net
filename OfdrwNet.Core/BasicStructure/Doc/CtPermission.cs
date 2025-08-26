using System.Xml.Linq;

namespace OfdrwNet.Core.BasicStructure.Doc;

/// <summary>
/// 权限声明
/// 对应 Java 版本的 org.ofdrw.core.basicStructure.doc.permission.CT_Permission
/// </summary>
public class CtPermission : OfdElement
{
    /// <summary>
    /// 从现有元素构造权限声明
    /// </summary>
    /// <param name="element">XML元素</param>
    public CtPermission(XElement element) : base(element)
    {
    }

    /// <summary>
    /// 构造新的权限声明元素
    /// </summary>
    public CtPermission() : base("Permissions")
    {
    }
}
