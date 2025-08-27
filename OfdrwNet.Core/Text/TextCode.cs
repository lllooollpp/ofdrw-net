using System.Xml.Linq;
using OfdrwNet.Core.BasicType;
using OfdrwNet.Core.PageDescription.Clips;

namespace OfdrwNet.Core.Text;

/// <summary>
/// 文字定位
/// 
/// 文字对象使用严格的文字定位信息进行定位
/// 
/// 11.3 文字定位 图 61 表 46
/// 
/// 对应 Java 版本的 org.ofdrw.core.text.TextCode
/// </summary>
public class TextCode : OfdElement, IClipAble
{
    /// <summary>
    /// 从现有XML元素构造文字定位
    /// </summary>
    /// <param name="element">XML元素</param>
    public TextCode(XElement element) : base(element)
    {
    }

    /// <summary>
    /// 构造新的文字定位
    /// </summary>
    public TextCode() : base("TextCode")
    {
    }

    /// <summary>
    /// 设置文字内容
    /// </summary>
    /// <param name="content">内容</param>
    /// <returns>this</returns>
    public TextCode SetContent(string content)
    {
        Element.Value = content;
        return this;
    }

    /// <summary>
    /// 获取文字内容
    /// </summary>
    /// <returns>文字内容</returns>
    public string GetContent()
    {
        return Element.Value;
    }

    /// <summary>
    /// 设置坐标
    /// </summary>
    /// <param name="x">横坐标</param>
    /// <param name="y">纵坐标</param>
    /// <returns>this</returns>
    public TextCode SetCoordinate(double x, double y)
    {
        return SetX(x).SetY(y);
    }

    /// <summary>
    /// 【可选 属性】设置第一个文字的字形在对象坐标系下的 X 坐标
    /// 当 X 不出现，则采用上一个 TextCode 的 X 值，文字对象中的一个 TextCode 的属性必选
    /// </summary>
    /// <param name="x">第一个文字的字形在对象坐标系下的 X 坐标</param>
    /// <returns>this</returns>
    public TextCode SetX(double? x)
    {
        if (x.HasValue)
            AddAttribute("X", x.Value.ToString("F2"));
        else
            RemoveAttribute("X");
        return this;
    }

    /// <summary>
    /// 【可选 属性】获取第一个文字的字形在对象坐标系下的 X 坐标
    /// 当 X 不出现，则采用上一个 TextCode 的 X 值
    /// </summary>
    /// <returns>第一个文字的字形在对象坐标系下的 X 坐标；null表示采用上一个 TextCode 的 X 值</returns>
    public double? GetX()
    {
        var value = GetAttributeValue("X");
        return string.IsNullOrWhiteSpace(value) ? null : double.Parse(value);
    }

    /// <summary>
    /// 【可选 属性】设置第一个文字的字形原点在对象坐标系下的 Y 坐标
    /// 当 Y 不出现，则采用上一个 TextCode 的 Y 值，文字对象中的一个 TextCode 的属性必选
    /// </summary>
    /// <param name="y">第一个文字的字形原点在对象坐标系下的 Y 坐标</param>
    /// <returns>this</returns>
    public TextCode SetY(double? y)
    {
        if (y.HasValue)
            AddAttribute("Y", y.Value.ToString("F2"));
        else
            RemoveAttribute("Y");
        return this;
    }

    /// <summary>
    /// 【可选 属性】获取第一个文字的字形在对象坐标系下的 Y 坐标
    /// 当 Y 不出现，则采用上一个 TextCode 的 Y 值
    /// </summary>
    /// <returns>第一个文字的字形在对象坐标系下的 Y 坐标；null表示采用上一个 TextCode 的 Y 值</returns>
    public double? GetY()
    {
        var value = GetAttributeValue("Y");
        return string.IsNullOrWhiteSpace(value) ? null : double.Parse(value);
    }

    /// <summary>
    /// 【可选 属性】设置文字之间在 X 方向上的偏移值
    /// double 型数值队列，列表中的每个值代表一个文字与前一个文字之间在 X 方向的偏移值
    /// DeltaX 不出现时，表示文字的绘制点在 X 方向不做偏移。
    /// </summary>
    /// <param name="deltaX">文字之间在 X 方向上的偏移值</param>
    /// <returns>this</returns>
    public TextCode SetDeltaX(StArray? deltaX)
    {
        if (deltaX != null)
            AddAttribute("DeltaX", deltaX.ToString());
        else
            RemoveAttribute("DeltaX");
        return this;
    }

    /// <summary>
    /// 【可选 属性】设置文字之间在 X 方向上的偏移值
    /// </summary>
    /// <param name="arr">文字之间在 X 方向上的偏移值数值</param>
    /// <returns>this</returns>
    public TextCode SetDeltaX(params double[] arr)
    {
        return SetDeltaX(new StArray(arr));
    }

    /// <summary>
    /// 【可选 属性】获取文字之间在 X 方向上的偏移值
    /// DeltaX 不出现时，表示文字的绘制点在 X 方向不做偏移。
    /// </summary>
    /// <returns>文字之间在 X 方向上的偏移值；null表示不偏移</returns>
    public StArray? GetDeltaX()
    {
        var value = GetAttributeValue("DeltaX");
        return string.IsNullOrWhiteSpace(value) ? null : StArray.Parse(value);
    }

    /// <summary>
    /// 【可选 属性】设置文字之间在 Y 方向上的偏移值
    /// double 型数值队列，列表中的每个值代表一个文字与前一个文字之间在 Y 方向的偏移值
    /// DeltaY 不出现时，表示文字的绘制点在 Y 方向不做偏移。
    /// </summary>
    /// <param name="deltaY">文字之间在 Y 方向上的偏移值</param>
    /// <returns>this</returns>
    public TextCode SetDeltaY(StArray? deltaY)
    {
        if (deltaY != null)
            AddAttribute("DeltaY", deltaY.ToString());
        else
            RemoveAttribute("DeltaY");
        return this;
    }

    /// <summary>
    /// 【可选 属性】设置文字之间在 Y 方向上的偏移值
    /// </summary>
    /// <param name="arr">文字之间在 Y 方向上的偏移值数值</param>
    /// <returns>this</returns>
    public TextCode SetDeltaY(params double[] arr)
    {
        return SetDeltaY(new StArray(arr));
    }

    /// <summary>
    /// 【可选 属性】获取文字之间在 Y 方向上的偏移值
    /// DeltaY 不出现时，表示文字的绘制点在 Y 方向不做偏移。
    /// </summary>
    /// <returns>文字之间在 Y 方向上的偏移值；null表示不偏移</returns>
    public StArray? GetDeltaY()
    {
        var value = GetAttributeValue("DeltaY");
        return string.IsNullOrWhiteSpace(value) ? null : StArray.Parse(value);
    }

    /// <summary>
    /// 获取限定名称
    /// </summary>
    public override string QualifiedName => "ofd:TextCode";
}