using System.Xml.Linq;
using OfdrwNet.Core.BasicType;
using OfdrwNet.Core.PageDescription.Color;

namespace OfdrwNet.Core.Graph;

/// <summary>
/// 勾边颜色（描边颜色）
/// 
/// 用于定义图形对象的描边颜色。继承自颜色基类，提供颜色的完整定义。
/// 
/// 对应Java版本：org.ofdrw.core.graph.pathObj.StrokeColor
/// OFD标准 9.1 表 35
/// </summary>
public class StrokeColor : CtColor
{
    /// <summary>
    /// 从现有XML元素构造描边颜色
    /// </summary>
    /// <param name="element">XML元素</param>
    public StrokeColor(XElement element) : base(element)
    {
    }

    /// <summary>
    /// 构造新的描边颜色
    /// </summary>
    public StrokeColor() : base("StrokeColor")
    {
    }

    /// <summary>
    /// 使用RGB值构造描边颜色
    /// </summary>
    /// <param name="r">红色分量 (0-255)</param>
    /// <param name="g">绿色分量 (0-255)</param>
    /// <param name="b">蓝色分量 (0-255)</param>
    public StrokeColor(byte r, byte g, byte b) : base("StrokeColor")
    {
        SetValue(new StArray(r, g, b));
    }

    /// <summary>
    /// 使用十六进制颜色值构造描边颜色
    /// </summary>
    /// <param name="hexColor">十六进制颜色值，如 "#FF0000"</param>
    public StrokeColor(string hexColor) : base("StrokeColor")
    {
        if (hexColor.StartsWith("#"))
        {
            hexColor = hexColor.Substring(1);
        }

        if (hexColor.Length == 6)
        {
            var r = Convert.ToByte(hexColor.Substring(0, 2), 16);
            var g = Convert.ToByte(hexColor.Substring(2, 2), 16);
            var b = Convert.ToByte(hexColor.Substring(4, 2), 16);
            SetValue(new StArray(r, g, b));
        }
    }

    /// <summary>
    /// 获取颜色描述信息
    /// </summary>
    /// <returns>颜色描述</returns>
    public override string ToString()
    {
        var colorValue = GetValue();
        return $"StrokeColor[{colorValue}]";
    }
}
