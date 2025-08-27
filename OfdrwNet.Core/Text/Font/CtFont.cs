using System.Xml.Linq;
using OfdrwNet.Core.BasicType;

namespace OfdrwNet.Core.Text.Font;

/// <summary>
/// 字体资源
/// 
/// 字体是用来定义文字外观的资源，它包括字体名称、字体文件位置、
/// 字符集合、字符编码等信息。
/// 
/// 对应Java版本：org.ofdrw.core.text.font.CT_Font
/// 11.1 字形 图 58 表 44
/// </summary>
public class CtFont : OfdElement
{
    /// <summary>
    /// 从现有元素构造字体资源
    /// </summary>
    /// <param name="element">XML元素</param>
    public CtFont(XElement element) : base(element)
    {
    }

    /// <summary>
    /// 构造新的字体资源元素
    /// </summary>
    public CtFont() : base("Font")
    {
    }

    /// <summary>
    /// 【必选 属性】
    /// 设置字体的标识
    /// </summary>
    /// <param name="id">字体标识</param>
    /// <returns>this</returns>
    public CtFont SetID(StId id)
    {
        SetAttribute("ID", id.ToString());
        return this;
    }

    /// <summary>
    /// 【必选 属性】
    /// 获取字体的标识
    /// </summary>
    /// <returns>字体标识</returns>
    public StId GetID()
    {
        var value = GetAttributeValue("ID");
        return string.IsNullOrEmpty(value) ? throw new InvalidOperationException("Font ID is required") : StId.Parse(value);
    }

    /// <summary>
    /// 【必选 属性】
    /// 设置字体名称
    /// </summary>
    /// <param name="fontName">字体名称</param>
    /// <returns>this</returns>
    public CtFont SetFontName(string fontName)
    {
        SetAttribute("FontName", fontName);
        return this;
    }

    /// <summary>
    /// 【必选 属性】
    /// 获取字体名称
    /// </summary>
    /// <returns>字体名称</returns>
    public string GetFontName()
    {
        return GetAttributeValue("FontName") ?? throw new InvalidOperationException("FontName is required");
    }

    /// <summary>
    /// 【可选 属性】
    /// 设置字体族名称
    /// </summary>
    /// <param name="familyName">字体族名称</param>
    /// <returns>this</returns>
    public CtFont SetFamilyName(string familyName)
    {
        SetAttribute("FamilyName", familyName);
        return this;
    }

    /// <summary>
    /// 【可选 属性】
    /// 获取字体族名称
    /// </summary>
    /// <returns>字体族名称，可能为null</returns>
    public string? GetFamilyName()
    {
        return GetAttributeValue("FamilyName");
    }

    /// <summary>
    /// 【可选 属性】
    /// 设置字符集
    /// </summary>
    /// <param name="charset">字符集</param>
    /// <returns>this</returns>
    public CtFont SetCharset(string charset)
    {
        SetAttribute("Charset", charset);
        return this;
    }

    /// <summary>
    /// 【可选 属性】
    /// 设置字符集（使用枚举）
    /// </summary>
    /// <param name="charset">字符集枚举</param>
    /// <returns>this</returns>
    public CtFont SetCharset(Charset charset)
    {
        SetAttribute("Charset", charset.ToOfdString());
        return this;
    }

    /// <summary>
    /// 【可选 属性】
    /// 获取字符集
    /// </summary>
    /// <returns>字符集，可能为null</returns>
    public string? GetCharset()
    {
        return GetAttributeValue("Charset");
    }

    /// <summary>
    /// 【可选 属性】
    /// 获取字符集枚举值
    /// </summary>
    /// <returns>字符集枚举，默认Unicode</returns>
    public Charset GetCharsetEnum()
    {
        var charsetStr = GetCharset();
        return string.IsNullOrEmpty(charsetStr) ? Charset.Unicode : CharsetExtensions.Parse(charsetStr);
    }

    /// <summary>
    /// 【可选 属性】
    /// 设置是否粗体
    /// </summary>
    /// <param name="bold">是否粗体</param>
    /// <returns>this</returns>
    public CtFont SetBold(bool bold)
    {
        SetAttribute("Bold", bold.ToString().ToLower());
        return this;
    }

    /// <summary>
    /// 【可选 属性】
    /// 获取是否粗体
    /// </summary>
    /// <returns>是否粗体，默认false</returns>
    public bool IsBold()
    {
        var value = GetAttributeValue("Bold");
        return !string.IsNullOrEmpty(value) && bool.Parse(value);
    }

    /// <summary>
    /// 【可选 属性】
    /// 设置是否斜体
    /// </summary>
    /// <param name="italic">是否斜体</param>
    /// <returns>this</returns>
    public CtFont SetItalic(bool italic)
    {
        SetAttribute("Italic", italic.ToString().ToLower());
        return this;
    }

    /// <summary>
    /// 【可选 属性】
    /// 获取是否斜体
    /// </summary>
    /// <returns>是否斜体，默认false</returns>
    public bool IsItalic()
    {
        var value = GetAttributeValue("Italic");
        return !string.IsNullOrEmpty(value) && bool.Parse(value);
    }

    /// <summary>
    /// 【可选 属性】
    /// 设置字体文件引用
    /// </summary>
    /// <param name="fontFile">字体文件资源标识</param>
    /// <returns>this</returns>
    public CtFont SetFontFile(StLoc fontFile)
    {
        SetAttribute("FontFile", fontFile.ToString());
        return this;
    }

    /// <summary>
    /// 【可选 属性】
    /// 获取字体文件引用
    /// </summary>
    /// <returns>字体文件资源标识，可能为null</returns>
    public StLoc? GetFontFile()
    {
        var value = GetAttributeValue("FontFile");
        return string.IsNullOrEmpty(value) ? null : StLoc.Parse(value);
    }

    /// <summary>
    /// 添加字符子集定义
    /// </summary>
    /// <param name="subset">字符子集</param>
    /// <returns>this</returns>
    public CtFont AddSubset(string subset)
    {
        var subsetElement = new XElement(Element.Name.Namespace + "Subset", subset);
        Element.Add(subsetElement);
        return this;
    }

    /// <summary>
    /// 获取所有字符子集
    /// </summary>
    /// <returns>字符子集列表</returns>
    public List<string> GetSubsets()
    {
        return Element.Elements(Element.Name.Namespace + "Subset")
                     .Select(e => e.Value)
                     .ToList();
    }
}
