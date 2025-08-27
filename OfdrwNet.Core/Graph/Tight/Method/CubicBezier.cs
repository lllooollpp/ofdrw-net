using OfdrwNet.Core.BasicType;

namespace OfdrwNet.Core.Graph.Tight.Method;

/// <summary>
/// 三次贝塞尔曲线命令
/// 
/// 从当前点连接一条到点(x3, y3)的三次贝塞尔曲线，
/// 并将当前点移动到点(x3, y3)，此贝塞尔曲线使用点
/// (x1, y1)和点(x2, y2)作为控制点
/// 
/// 对应Java版本的 org.ofdrw.core.graph.tight.method.CubicBezier
/// </summary>
public class CubicBezier : ICommand
{
    /// <summary>
    /// 控制点1 X坐标
    /// </summary>
    public double X1 { get; set; }

    /// <summary>
    /// 控制点1 Y坐标
    /// </summary>
    public double Y1 { get; set; }

    /// <summary>
    /// 控制点2 X坐标
    /// </summary>
    public double X2 { get; set; }

    /// <summary>
    /// 控制点2 Y坐标
    /// </summary>
    public double Y2 { get; set; }

    /// <summary>
    /// 目标点X坐标
    /// </summary>
    public double X3 { get; set; }

    /// <summary>
    /// 目标点Y坐标
    /// </summary>
    public double Y3 { get; set; }

    /// <summary>
    /// 构造函数
    /// </summary>
    /// <param name="x1">控制点1 X坐标</param>
    /// <param name="y1">控制点1 Y坐标</param>
    /// <param name="x2">控制点2 X坐标</param>
    /// <param name="y2">控制点2 Y坐标</param>
    /// <param name="x3">目标点X坐标</param>
    /// <param name="y3">目标点Y坐标</param>
    public CubicBezier(double x1, double y1, double x2, double y2, double x3, double y3)
    {
        X1 = x1;
        Y1 = y1;
        X2 = x2;
        Y2 = y2;
        X3 = x3;
        Y3 = y3;
    }

    /// <summary>
    /// 转换为字符串表示
    /// </summary>
    /// <returns>字符串表示</returns>
    public override string ToString()
    {
        return $"B {StBase.Fmt(X1)} {StBase.Fmt(Y1)} {StBase.Fmt(X2)} {StBase.Fmt(Y2)} {StBase.Fmt(X3)} {StBase.Fmt(Y3)}";
    }
}