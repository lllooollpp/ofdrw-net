using System.Xml.Linq;
using OfdrwNet.Core.BasicType;

namespace OfdrwNet.Core.PageDescription.Color;

/// <summary>
/// 基本颜色
/// 
/// 本标准中定义的颜色是一个广义的概念，包括基本颜色、底纹和渐变
/// 
/// 基本颜色支持两种指定方式：一种是通过设定颜色个通道值指定颜色空间的某个颜色，
/// 另一种是通过索引值取得颜色空间中的一个预定义颜色。
/// 
/// 由于不同颜色空间下，颜色通道的含义、数目各不相同，因此对颜色空间的类型、颜色值的
/// 描述格式等作出了详细的说明，见表 27。BitsPerComponent（简称 BPC）有效时，
/// 颜色通道值的取值下限是 0，上限由 BitsPerComponent 决定，取值区间 [0, 2^BPC - 1]
/// 内的整数，采用 10 进制或 16 进制的形式表示，采用 16 进制表示时，应以"#"加以标识。
/// 当颜色通道的值超出了相应区间，则按照默认颜色来处理。
/// 
/// 8.3.2 基本颜色 图 25 表 26
/// 
/// 对应 Java 版本的 org.ofdrw.core.pageDescription.color.color.CT_Color
/// </summary>
public class CtColor : OfdElement
{
    /// <summary>
    /// 从现有元素构造颜色
    /// </summary>
    /// <param name="element">XML元素</param>
    public CtColor(XElement element) : base(element)
    {
    }

    /// <summary>
    /// 构造新的颜色元素
    /// </summary>
    public CtColor() : base("Color")
    {
    }

    /// <summary>
    /// 构造颜色元素
    /// </summary>
    /// <param name="color">颜色族中的颜色</param>
    public CtColor(ColorClusterType color) : this()
    {
        SetColor(color);
    }

    /// <summary>
    /// 构造带名称的颜色元素
    /// </summary>
    /// <param name="name">元素名称</param>
    protected CtColor(string name) : base(name)
    {
    }

    /// <summary>
    /// RGB颜色值
    /// 其中颜色空间（CT_ColorSpace）的通道使用位数（BitsPerComponent）为 8
    /// 采用10进制表示方式
    /// </summary>
    /// <param name="r">红色 0~255</param>
    /// <param name="g">绿色 0~255</param>
    /// <param name="b">蓝色 0~255</param>
    /// <returns>RGB 颜色</returns>
    public static CtColor Rgb(int r, int g, int b)
    {
        return new CtColor()
            .SetValue(new StArray(r, g, b));
    }

    /// <summary>
    /// RGB颜色值
    /// </summary>
    /// <param name="rgb">RGB数组</param>
    /// <returns>RGB 颜色</returns>
    public static CtColor Rgb(int[] rgb)
    {
        return Rgb(rgb[0], rgb[1], rgb[2]);
    }

    /// <summary>
    /// 【可选 属性】
    /// 设置颜色值
    /// 指定了当前颜色空间下各通道的取值。Value 的取值应
    /// 符合"通道 1 通道 2 通道 3 ..."格式。此属性不出现时，
    /// 应采用 Index 属性从颜色空间的调色板中的取值。二者都不
    /// 出现时，该颜色各通道的值全部为 0
    /// </summary>
    /// <param name="value">颜色值</param>
    /// <returns>this</returns>
    public CtColor SetValue(StArray value)
    {
        AddAttribute("Value", value.ToString());
        return this;
    }

    /// <summary>
    /// 【可选 属性】
    /// 获取颜色值
    /// 指定了当前颜色空间下各通道的取值。Value 的取值应
    /// 符合"通道 1 通道 2 通道 3 ..."格式。此属性不出现时，
    /// 应采用 Index 属性从颜色空间的调色板中的取值。二者都不
    /// 出现时，该颜色各通道的值全部为 0
    /// </summary>
    /// <returns>颜色值</returns>
    public StArray? GetValue()
    {
        var value = GetAttributeValue("Value");
        return string.IsNullOrEmpty(value) ? null : StArray.Parse(value);
    }

    /// <summary>
    /// 【可选 属性】
    /// 设置颜色在调色板中的索引值
    /// 指定该颜色在调色板中的索引值，调色板在颜色空间中定义。
    /// 此属性不出现时，应采用 Value 属性指定各颜色通道的值。
    /// 二者都不出现时，该颜色各通道的值全部为 0
    /// </summary>
    /// <param name="index">颜色在调色板中的索引值</param>
    /// <returns>this</returns>
    public CtColor SetIndex(int index)
    {
        AddAttribute("Index", index.ToString());
        return this;
    }

    /// <summary>
    /// 【可选 属性】
    /// 获取颜色在调色板中的索引值
    /// 指定该颜色在调色板中的索引值，调色板在颜色空间中定义。
    /// 此属性不出现时，应采用 Value 属性指定各颜色通道的值。
    /// 二者都不出现时，该颜色各通道的值全部为 0
    /// </summary>
    /// <returns>颜色在调色板中的索引值</returns>
    public int? GetIndex()
    {
        var value = GetAttributeValue("Index");
        return int.TryParse(value, out int result) ? result : null;
    }

    /// <summary>
    /// 【可选 属性】
    /// 设置颜色空间引用
    /// 指明该颜色所在颜色空间，如果此属性不出现，则该颜色使用当前
    /// 颜色空间或当页默认颜色空间或文档默认颜色空间，查找先后顺序
    /// 依次为当前颜色空间、当前页面默认颜色空间、文档公共数据默认颜色空间
    /// </summary>
    /// <param name="colorSpace">颜色空间引用</param>
    /// <returns>this</returns>
    public CtColor SetColorSpace(StRefId colorSpace)
    {
        AddAttribute("ColorSpace", colorSpace.ToString());
        return this;
    }

    /// <summary>
    /// 【可选 属性】
    /// 获取颜色空间引用
    /// 指明该颜色所在颜色空间，如果此属性不出现，则该颜色使用当前
    /// 颜色空间或当页默认颜色空间或文档默认颜色空间，查找先后顺序
    /// 依次为当前颜色空间、当前页面默认颜色空间、文档公共数据默认颜色空间
    /// </summary>
    /// <returns>颜色空间引用</returns>
    public StRefId? GetColorSpace()
    {
        var value = GetAttributeValue("ColorSpace");
        return string.IsNullOrEmpty(value) ? null : StRefId.Parse(value);
    }

    /// <summary>
    /// 【可选 属性】
    /// 设置透明度
    /// 颜色的不透明度，取值区间 [0, 255]，默认值为 255，表示完全不透明
    /// </summary>
    /// <param name="alpha">透明度</param>
    /// <returns>this</returns>
    public CtColor SetAlpha(int alpha)
    {
        if (alpha < 0 || alpha > 255)
        {
            throw new ArgumentOutOfRangeException(nameof(alpha), "透明度值应在0-255范围内");
        }
        AddAttribute("Alpha", alpha.ToString());
        return this;
    }

    /// <summary>
    /// 【可选 属性】
    /// 获取透明度
    /// 颜色的不透明度，取值区间 [0, 255]，默认值为 255，表示完全不透明
    /// </summary>
    /// <returns>透明度</returns>
    public int GetAlpha()
    {
        var value = GetAttributeValue("Alpha");
        return int.TryParse(value, out int result) ? result : 255; // 默认值为255
    }

    /// <summary>
    /// 设置颜色
    /// </summary>
    /// <param name="color">颜色族中的颜色</param>
    /// <returns>this</returns>
    public CtColor SetColor(ColorClusterType color)
    {
        return SetValue(new StArray(color.Value));
    }
}

/// <summary>
/// 颜色族类型
/// </summary>
public class ColorClusterType
{
    /// <summary>
    /// 颜色值数组
    /// </summary>
    public double[] Value { get; set; }

    /// <summary>
    /// 构造颜色族类型
    /// </summary>
    /// <param name="values">颜色值数组</param>
    public ColorClusterType(params double[] values)
    {
        Value = values;
    }
}
