using System.Xml.Linq;
using OfdrwNet.Core.BasicType;
using OfdrwNet.Core.PageDescription.Color;

namespace OfdrwNet.Core.Graph;

/// <summary>
/// 填充颜色
/// 
/// 用于定义图形对象的填充颜色。继承自颜色基类，提供颜色的完整定义。
/// 
/// 对应Java版本：org.ofdrw.core.graph.pathObj.FillColor
/// OFD标准 9.1 表 35
/// </summary>
public class FillColor : CtColor
{
    /// <summary>
    /// 从现有XML元素构造填充颜色
    /// </summary>
    /// <param name="element">XML元素</param>
    public FillColor(XElement element) : base(element)
    {
    }

    /// <summary>
    /// 构造新的填充颜色
    /// </summary>
    public FillColor() : base("FillColor")
    {
    }

    /// <summary>
    /// 使用RGB值构造填充颜色
    /// </summary>
    /// <param name="r">红色分量 (0-255)</param>
    /// <param name="g">绿色分量 (0-255)</param>
    /// <param name="b">蓝色分量 (0-255)</param>
    public FillColor(byte r, byte g, byte b) : base("FillColor")
    {
        SetValue(new StArray(r, g, b));
    }

    /// <summary>
    /// 使用十六进制颜色值构造填充颜色
    /// </summary>
    /// <param name="hexColor">十六进制颜色值，如 "#FF0000"</param>
    public FillColor(string hexColor) : base("FillColor")
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
        return $"FillColor[{colorValue}]";
    }
}
