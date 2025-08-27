using System.Xml.Linq;
using OfdrwNet.Core.BasicType;

namespace OfdrwNet.Core.Text.Font;

/// <summary>
/// 字体信息
/// 
/// 描述字体的详细信息，包括字体名称、字体文件路径、字符集等。
/// 用于字体资源的管理和引用。
/// 
/// 对应OFD标准中的FontInfo定义
/// 8.2.4.1 字体 图 20 表 33
/// </summary>
public class FontInfo : OfdElement
{
    /// <summary>
    /// 从现有元素构造字体信息
    /// </summary>
    /// <param name="element">XML元素</param>
    public FontInfo(XElement element) : base(element)
    {
    }

    /// <summary>
    /// 构造新的字体信息元素
    /// </summary>
    public FontInfo() : base("FontInfo")
    {
    }

    /// <summary>
    /// 【必选 属性】
    /// 设置字体标识符
    /// </summary>
    /// <param name="id">字体标识符</param>
    /// <returns>this</returns>
    public FontInfo SetID(StId id)
    {
        SetAttribute("ID", id.ToString());
        return this;
    }

    /// <summary>
    /// 【必选 属性】
    /// 获取字体标识符
    /// </summary>
    /// <returns>字体标识符</returns>
    /// <exception cref="InvalidOperationException">ID未设置时抛出</exception>
    public StId GetID()
    {
        var value = GetAttributeValue("ID");
        return string.IsNullOrEmpty(value) 
            ? throw new InvalidOperationException("FontInfo ID is required") 
            : StId.Parse(value);
    }

    /// <summary>
    /// 【必选 属性】
    /// 设置字体名称
    /// </summary>
    /// <param name="fontName">字体名称</param>
    /// <returns>this</returns>
    public FontInfo SetFontName(string fontName)
    {
        SetAttribute("FontName", fontName);
        return this;
    }

    /// <summary>
    /// 【必选 属性】
    /// 获取字体名称
    /// </summary>
    /// <returns>字体名称</returns>
    /// <exception cref="InvalidOperationException">FontName未设置时抛出</exception>
    public string GetFontName()
    {
        var value = GetAttributeValue("FontName");
        return string.IsNullOrEmpty(value) 
            ? throw new InvalidOperationException("FontInfo FontName is required") 
            : value;
    }

    /// <summary>
    /// 【可选 属性】
    /// 设置字体族名称
    /// </summary>
    /// <param name="familyName">字体族名称</param>
    /// <returns>this</returns>
    public FontInfo SetFamilyName(string familyName)
    {
        SetAttribute("FamilyName", familyName);
        return this;
    }

    /// <summary>
    /// 【可选 属性】
    /// 获取字体族名称
    /// </summary>
    /// <returns>字体族名称</returns>
    public string? GetFamilyName()
    {
        return GetAttributeValue("FamilyName");
    }

    /// <summary>
    /// 【可选 属性】
    /// 设置字符集
    /// </summary>
    /// <param name="charset">字符集名称</param>
    /// <returns>this</returns>
    public FontInfo SetCharset(string charset)
    {
        SetAttribute("Charset", charset);
        return this;
    }

    /// <summary>
    /// 【可选 属性】
    /// 获取字符集
    /// </summary>
    /// <returns>字符集名称</returns>
    public string? GetCharset()
    {
        return GetAttributeValue("Charset");
    }

    /// <summary>
    /// 【可选 属性】
    /// 设置字体是否为粗体
    /// </summary>
    /// <param name="bold">是否为粗体</param>
    /// <returns>this</returns>
    public FontInfo SetBold(bool bold)
    {
        SetAttribute("Bold", bold ? "true" : "false");
        return this;
    }

    /// <summary>
    /// 【可选 属性】
    /// 获取字体是否为粗体
    /// </summary>
    /// <returns>是否为粗体</returns>
    public bool GetBold()
    {
        var value = GetAttributeValue("Bold");
        return bool.TryParse(value, out var result) && result;
    }

    /// <summary>
    /// 【可选 属性】
    /// 设置字体是否为斜体
    /// </summary>
    /// <param name="italic">是否为斜体</param>
    /// <returns>this</returns>
    public FontInfo SetItalic(bool italic)
    {
        SetAttribute("Italic", italic ? "true" : "false");
        return this;
    }

    /// <summary>
    /// 【可选 属性】
    /// 获取字体是否为斜体
    /// </summary>
    /// <returns>是否为斜体</returns>
    public bool GetItalic()
    {
        var value = GetAttributeValue("Italic");
        return bool.TryParse(value, out var result) && result;
    }

    /// <summary>
    /// 【可选 属性】
    /// 设置字体文件路径
    /// </summary>
    /// <param name="fontFile">字体文件路径</param>
    /// <returns>this</returns>
    public FontInfo SetFontFile(StLoc fontFile)
    {
        SetAttribute("FontFile", fontFile.ToString());
        return this;
    }

    /// <summary>
    /// 【可选 属性】
    /// 获取字体文件路径
    /// </summary>
    /// <returns>字体文件路径</returns>
    public StLoc? GetFontFile()
    {
        var value = GetAttributeValue("FontFile");
        return string.IsNullOrEmpty(value) ? null : new StLoc(value);
    }

    /// <summary>
    /// 添加字体子集
    /// </summary>
    /// <param name="fontSubset">字体子集</param>
    /// <returns>this</returns>
    public FontInfo AddFontSubset(FontSubset fontSubset)
    {
        Add(fontSubset);
        return this;
    }

    /// <summary>
    /// 获取所有字体子集
    /// </summary>
    /// <returns>字体子集列表</returns>
    public List<FontSubset> GetFontSubsets()
    {
        return Element.Elements(Const.OfdNamespace + "FontSubset")
            .Select(e => new FontSubset(e))
            .ToList();
    }

    /// <summary>
    /// 获取字体子集数量
    /// </summary>
    /// <returns>字体子集数量</returns>
    public int GetFontSubsetCount()
    {
        return Element.Elements(Const.OfdNamespace + "FontSubset").Count();
    }
}

/// <summary>
/// 字体子集
/// 
/// 描述字体的子集信息，包含特定的字符范围或字符集。
/// 用于优化字体文件大小，仅包含文档中实际使用的字符。
/// 
/// 对应OFD标准中的FontSubset定义
/// </summary>
public class FontSubset : OfdElement
{
    /// <summary>
    /// 从现有元素构造字体子集
    /// </summary>
    /// <param name="element">XML元素</param>
    public FontSubset(XElement element) : base(element)
    {
    }

    /// <summary>
    /// 构造新的字体子集元素
    /// </summary>
    public FontSubset() : base("FontSubset")
    {
    }

    /// <summary>
    /// 【可选 属性】
    /// 设置子集标识符
    /// </summary>
    /// <param name="id">子集标识符</param>
    /// <returns>this</returns>
    public FontSubset SetID(StId id)
    {
        SetAttribute("ID", id.ToString());
        return this;
    }

    /// <summary>
    /// 【可选 属性】
    /// 获取子集标识符
    /// </summary>
    /// <returns>子集标识符</returns>
    public StId? GetID()
    {
        var value = GetAttributeValue("ID");
        return string.IsNullOrEmpty(value) ? null : StId.Parse(value);
    }

    /// <summary>
    /// 【可选 属性】
    /// 设置子集名称
    /// </summary>
    /// <param name="subsetName">子集名称</param>
    /// <returns>this</returns>
    public FontSubset SetSubsetName(string subsetName)
    {
        SetAttribute("SubsetName", subsetName);
        return this;
    }

    /// <summary>
    /// 【可选 属性】
    /// 获取子集名称
    /// </summary>
    /// <returns>子集名称</returns>
    public string? GetSubsetName()
    {
        return GetAttributeValue("SubsetName");
    }

    /// <summary>
    /// 【可选 属性】
    /// 设置子集文件路径
    /// </summary>
    /// <param name="subsetFile">子集文件路径</param>
    /// <returns>this</returns>
    public FontSubset SetSubsetFile(StLoc subsetFile)
    {
        SetAttribute("SubsetFile", subsetFile.ToString());
        return this;
    }

    /// <summary>
    /// 【可选 属性】
    /// 获取子集文件路径
    /// </summary>
    /// <returns>子集文件路径</returns>
    public StLoc? GetSubsetFile()
    {
        var value = GetAttributeValue("SubsetFile");
        return string.IsNullOrEmpty(value) ? null : new StLoc(value);
    }

    /// <summary>
    /// 【可选 属性】
    /// 设置字符范围
    /// </summary>
    /// <param name="charRange">字符范围（如"U+0020-U+007E"）</param>
    /// <returns>this</returns>
    public FontSubset SetCharRange(string charRange)
    {
        SetAttribute("CharRange", charRange);
        return this;
    }

    /// <summary>
    /// 【可选 属性】
    /// 获取字符范围
    /// </summary>
    /// <returns>字符范围</returns>
    public string? GetCharRange()
    {
        return GetAttributeValue("CharRange");
    }

    /// <summary>
    /// 设置字符集合
    /// </summary>
    /// <param name="chars">字符集合</param>
    /// <returns>this</returns>
    public FontSubset SetChars(string chars)
    {
        Element.Value = chars;
        return this;
    }

    /// <summary>
    /// 获取字符集合
    /// </summary>
    /// <returns>字符集合</returns>
    public string? GetChars()
    {
        return Element.Value;
    }

    /// <summary>
    /// 添加字符到子集
    /// </summary>
    /// <param name="ch">要添加的字符</param>
    /// <returns>this</returns>
    public FontSubset AddChar(char ch)
    {
        var chars = GetChars() ?? "";
        if (!chars.Contains(ch))
        {
            Element.Value = chars + ch;
        }
        return this;
    }

    /// <summary>
    /// 添加字符串到子集
    /// </summary>
    /// <param name="text">要添加的字符串</param>
    /// <returns>this</returns>
    public FontSubset AddChars(string text)
    {
        var chars = GetChars() ?? "";
        var newChars = text.Where(ch => !chars.Contains(ch));
        Element.Value = chars + string.Concat(newChars);
        return this;
    }
}

/// <summary>
/// 字体集合
/// 
/// 包含文档中使用的所有字体信息。
/// 用于字体资源的管理和引用。
/// </summary>
public class Fonts : OfdElement
{
    /// <summary>
    /// 从现有元素构造字体集合
    /// </summary>
    /// <param name="element">XML元素</param>
    public Fonts(XElement element) : base(element)
    {
    }

    /// <summary>
    /// 构造新的字体集合
    /// </summary>
    public Fonts() : base("Fonts")
    {
    }

    /// <summary>
    /// 添加字体信息
    /// </summary>
    /// <param name="fontInfo">字体信息</param>
    /// <returns>this</returns>
    public Fonts AddFont(FontInfo fontInfo)
    {
        Add(fontInfo);
        return this;
    }

    /// <summary>
    /// 获取所有字体信息
    /// </summary>
    /// <returns>字体信息列表</returns>
    public List<FontInfo> GetFonts()
    {
        return Element.Elements(Const.OfdNamespace + "FontInfo")
            .Select(e => new FontInfo(e))
            .ToList();
    }

    /// <summary>
    /// 根据ID查找字体信息
    /// </summary>
    /// <param name="id">字体ID</param>
    /// <returns>字体信息或null</returns>
    public FontInfo? FindFont(StId id)
    {
        return GetFonts().FirstOrDefault(f => f.GetID().Equals(id));
    }

    /// <summary>
    /// 根据名称查找字体信息
    /// </summary>
    /// <param name="fontName">字体名称</param>
    /// <returns>字体信息或null</returns>
    public FontInfo? FindFont(string fontName)
    {
        return GetFonts().FirstOrDefault(f => f.GetFontName() == fontName);
    }

    /// <summary>
    /// 获取字体数量
    /// </summary>
    /// <returns>字体数量</returns>
    public int GetFontCount()
    {
        return Element.Elements(Const.OfdNamespace + "FontInfo").Count();
    }

    /// <summary>
    /// 清空所有字体
    /// </summary>
    /// <returns>this</returns>
    public Fonts ClearFonts()
    {
        Element.Elements(Const.OfdNamespace + "FontInfo").Remove();
        return this;
    }

    /// <summary>
    /// 获取限定名称
    /// </summary>
    public override string QualifiedName => "ofd:Fonts";
}
