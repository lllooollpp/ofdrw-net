using System.Xml.Linq;
using OfdrwNet.Core.BasicType;

namespace OfdrwNet.Core.Graph.Tight.Method;

/// <summary>
/// 基于XML元素的移动命令
/// 
/// 对应Java版本中从XML元素构造的Move命令
/// </summary>
public class MoveCommand : OfdElement, ICommand
{
    public MoveCommand(XElement element) : base(element)
    {
    }

    public MoveCommand() : base("Move")
    {
    }

    public MoveCommand(double x, double y) : base("Move")
    {
        SetPoint1(new StPos(x, y));
    }

    /// <summary>
    /// 设置目标点
    /// </summary>
    /// <param name="point">目标点</param>
    /// <returns>this</returns>
    public MoveCommand SetPoint1(StPos point)
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
        return $"M {StBase.Fmt(point.X)} {StBase.Fmt(point.Y)}";
    }
}