using System.Xml.Linq;
using OfdrwNet.Core.BasicType;

namespace OfdrwNet.Core.Graph.Tight.Method;

/// <summary>
/// 基于XML元素的二次贝塞尔曲线命令
/// </summary>
public class QuadraticBezierCommand : OfdElement, ICommand
{
    public QuadraticBezierCommand(XElement element) : base(element) { }
    public QuadraticBezierCommand() : base("QuadraticBezier") { }
}

/// <summary>
/// 基于XML元素的三次贝塞尔曲线命令
/// </summary>
public class CubicBezierCommand : OfdElement, ICommand
{
    public CubicBezierCommand(XElement element) : base(element) { }
    public CubicBezierCommand() : base("CubicBezier") { }
}

/// <summary>
/// 基于XML元素的椭圆弧命令
/// </summary>
public class ArcCommand : OfdElement, ICommand
{
    public ArcCommand(XElement element) : base(element) { }
    public ArcCommand() : base("Arc") { }
}

/// <summary>
/// 基于XML元素的闭合命令
/// </summary>
public class CloseCommand : OfdElement, ICommand
{
    public CloseCommand(XElement element) : base(element) { }
    public CloseCommand() : base("Close") { }
    
    public override string ToString() => "C";
}