namespace OfdrwNet.Font;

/// <summary>
/// OFD 字体资源描述
/// </summary>
public class OfdFont
{
    /// <summary>
    /// 字体ID
    /// </summary>
    public int ID { get; }

    /// <summary>
    /// 字体名称
    /// </summary>
    public string FontName { get; }

    /// <summary>
    /// 字体族名称
    /// </summary>
    public string FamilyName { get; }

    /// <summary>
    /// 构造函数
    /// </summary>
    /// <param name="id">字体ID</param>
    /// <param name="fontName">字体名称</param>
    /// <param name="familyName">字体族名称</param>
    public OfdFont(int id, string fontName, string familyName)
    {
        ID = id;
        FontName = fontName;
        FamilyName = familyName;
    }

    /// <summary>
    /// 重写ToString方法
    /// </summary>
    /// <returns>字符串表示</returns>
    public override string ToString()
    {
        return $"OfdFont[ID={ID}, FontName='{FontName}', FamilyName='{FamilyName}']";
    }

    /// <summary>
    /// 重写Equals方法
    /// </summary>
    /// <param name="obj">比较对象</param>
    /// <returns>是否相等</returns>
    public override bool Equals(object? obj)
    {
        return obj is OfdFont other && ID == other.ID && FontName == other.FontName;
    }

    /// <summary>
    /// 重写GetHashCode方法
    /// </summary>
    /// <returns>哈希码</returns>
    public override int GetHashCode()
    {
        return HashCode.Combine(ID, FontName);
    }
}
