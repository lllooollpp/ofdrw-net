using System.Xml.Linq;
using OfdrwNet.Core.BasicType;

namespace OfdrwNet.Core.Graph.Tight.Method;

/// <summary>
/// 基于XML元素的直线命令
/// 
/// 对应Java版本中从XML元素构造的Line命令
/// </summary>
public class LineCommand : OfdElement, ICommand
{
    public LineCommand(XElement element) : base(element)
    {
    }

    public LineCommand() : base("Line")
    {
    }

    public LineCommand(double x, double y) : base("Line")
    {
        SetPoint1(new StPos(x, y));
    }

    /// <summary>
    /// 设置目标点
    /// </summary>
    /// <param name="point">目标点</param>
    /// <returns>this</returns>
    public LineCommand SetPoint1(StPos point)
    {
        SetAttribute("Point1", point.ToString());
        return this;
    }

    /// <summary>
    /// 获取目标点
    /// </summary>
    /// <returns>目标点</returns>
    public StPos GetPoint1()
    {
        var value = GetAttributeValue("Point1");
        return string.IsNullOrEmpty(value) ? throw new InvalidOperationException("Point1 is required") : StPos.Parse(value);
    }

    /// <summary>
    /// 转换为字符串表示
    /// </summary>
    /// <returns>字符串表示</returns>
    public override string ToString()
    {
        var point = GetPoint1();
        return $"L {StBase.Fmt(point.X)} {StBase.Fmt(point.Y)}";
    }
}