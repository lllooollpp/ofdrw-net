using System.Xml.Linq;
using OfdrwNet.Core.BasicType;
using OfdrwNet.Core.Text.Font;

namespace OfdrwNet.Core.BasicStructure.Res;

/// <summary>
/// 资源层的 FontInfo 包装，用于 Res 管理（Layout 期望的类型）
/// 该类可从 CtFont 构建并暴露简单的访问器：GetFontName/GetFamilyName/GetFontFile
/// </summary>
public class FontInfo : OfdElement
{
    public FontInfo(XElement element) : base(element) { }
    public FontInfo() : base("Font") { }

    /// <summary>
    /// 从 CtFont 构造资源 FontInfo（浅拷贝常用属性）
    /// </summary>
    public FontInfo(CtFont ctFont) : this()
    {
        if (ctFont == null) return;
        try
        {
            var name = ctFont.GetFontName();
            if (!string.IsNullOrEmpty(name)) SetFontName(name);
        }
        catch { }
        try
        {
            var fam = ctFont.GetFamilyName();
            if (!string.IsNullOrEmpty(fam)) SetFamilyName(fam);
        }
        catch { }
        try
        {
            var loc = ctFont.GetFontFile();
            if (loc != null) SetFontFile(loc);
        }
        catch { }

        // 复制子集（如果有）
        try
        {
            var subsets = ctFont.GetSubsets();
            if (subsets != null)
            {
                foreach (var s in subsets)
                {
                    AddOfdEntity("Subset", s);
                }
            }
        }
        catch { }
    }

    public FontInfo SetID(StId id)
    {
        SetAttribute("ID", id.ToString());
        return this;
    }

    public StId? GetID()
    {
        var v = GetAttributeValue("ID");
        return string.IsNullOrEmpty(v) ? null : StId.Parse(v);
    }

    public FontInfo SetFontName(string name)
    {
        SetAttribute("FontName", name);
        return this;
    }

    public string? GetFontName()
    {
        return GetAttributeValue("FontName");
    }

    public FontInfo SetFamilyName(string family)
    {
        SetAttribute("FamilyName", family);
        return this;
    }

    public string? GetFamilyName()
    {
        return GetAttributeValue("FamilyName");
    }

    public FontInfo SetFontFile(StLoc loc)
    {
        SetAttribute("FontFile", loc.ToString());
        return this;
    }

    public StLoc? GetFontFile()
    {
        var v = GetAttributeValue("FontFile");
        return string.IsNullOrEmpty(v) ? null : new StLoc(v);
    }
}
