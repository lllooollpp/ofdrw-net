using OfdrwNet.Core.BasicType;

namespace OfdrwNet.Core.Graph.Tight.Method;

/// <summary>
/// 移动命令
/// 
/// 当前点移动到指定点(x, y)
/// 
/// 对应Java版本的 org.ofdrw.core.graph.tight.method.Move
/// </summary>
public class Move : ICommand
{
    /// <summary>
    /// 目标点X坐标
    /// </summary>
    public double X { get; set; }

    /// <summary>
    /// 目标点Y坐标
    /// </summary>
    public double Y { get; set; }

    /// <summary>
    /// 构造函数
    /// </summary>
    /// <param name="x">目标点X坐标</param>
    /// <param name="y">目标点Y坐标</param>
    public Move(double x, double y)
    {
        X = x;
        Y = y;
    }

    /// <summary>
    /// 转换为字符串表示
    /// </summary>
    /// <returns>字符串表示</returns>
    public override string ToString()
    {
        return $"M {StBase.Fmt(X)} {StBase.Fmt(Y)}";
    }
}