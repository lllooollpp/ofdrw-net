using System.Xml.Linq;

namespace OfdrwNet.Core.BasicStructure.Res;

/// <summary>
/// 字体资源
/// 对应 Java 版本的 org.ofdrw.core.basicStructure.res.resources.Fonts
/// </summary>
public class Fonts : OfdResource
{
    /// <summary>
    /// 从现有元素构造字体资源
    /// </summary>
    /// <param name="element">XML元素</param>
    public Fonts(XElement element) : base(element)
    {
    }

    /// <summary>
    /// 构造新的字体资源元素
    /// </summary>
    public Fonts() : base("Fonts")
    {
    }
}

/// <summary>
/// 颜色空间资源
/// </summary>
public class ColorSpaces : OfdResource
{
    public ColorSpaces(XElement element) : base(element) { }
    public ColorSpaces() : base("ColorSpaces") { }
}

/// <summary>
/// 绘制参数资源
/// </summary>
public class DrawParams : OfdResource
{
    public DrawParams(XElement element) : base(element) { }
    public DrawParams() : base("DrawParams") { }
}

/// <summary>
/// 多媒体资源
/// </summary>
public class MultiMedias : OfdResource
{
    public MultiMedias(XElement element) : base(element) { }
    public MultiMedias() : base("MultiMedias") { }
}
