using System.Xml.Linq;

namespace OfdrwNet.Core.BasicStructure.Doc;

/// <summary>
/// 书签集
/// 对应 Java 版本的 org.ofdrw.core.basicStructure.doc.bookmark.Bookmarks
/// </summary>
public class Bookmarks : OfdElement
{
    /// <summary>
    /// 从现有元素构造书签集
    /// </summary>
    /// <param name="element">XML元素</param>
    public Bookmarks(XElement element) : base(element)
    {
    }

    /// <summary>
    /// 构造新的书签集元素
    /// </summary>
    public Bookmarks() : base("Bookmarks")
    {
    }
}
