namespace OfdrwNet.Models;

/// <summary>
/// 字体资源描述
/// </summary>
public class OfdFont
{
    public int ID
    {
        get;
    }
    public string FontName
    {
        get;
    }
    public string FamilyName
    {
        get;
    }
    public OfdFont(int id, string fontName, string familyName)
    {
        ID = id;
        FontName = fontName;
        FamilyName = familyName;
    }
}
