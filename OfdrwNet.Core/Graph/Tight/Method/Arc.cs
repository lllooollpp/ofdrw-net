using OfdrwNet.Core.BasicType;

namespace OfdrwNet.Core.Graph.Tight.Method;

/// <summary>
/// 椭圆弧命令
/// 
/// 从当前点连接到点(x, y)的圆弧，并将当前点移动到点(x, y)。
/// rx 表示椭圆的长轴长度，ry 表示椭圆的短轴长度。angle 表示
/// 椭圆在当前坐标系下旋转的角度，正值为顺时针，负值为逆时针，
/// large 为 1 时表示对应度数大于180°的弧，为 0 时表示对应
/// 度数小于 180°的弧。sweep 为 1 时表示由圆弧起始点到结束点
/// 是顺时针旋转，为 0 时表示由圆弧起始点到结束点是逆时针旋转。
/// 
/// 对应Java版本的 org.ofdrw.core.graph.tight.method.Arc
/// </summary>
public class Arc : ICommand
{
    /// <summary>
    /// 椭圆长轴长度
    /// </summary>
    public double Rx { get; set; }

    /// <summary>
    /// 椭圆短轴长度
    /// </summary>
    public double Ry { get; set; }

    /// <summary>
    /// 旋转角度，正值顺时针，负值逆时针
    /// </summary>
    public double Angle { get; set; }

    /// <summary>
    /// 1 时表示对应度数大于 180°的弧，0 时表示对应度数小于 180°的弧
    /// </summary>
    public double Large { get; set; }

    /// <summary>
    /// 1 时表示由圆弧起始点到结束点是顺时针旋转，0 时表示逆时针旋转
    /// </summary>
    public double Sweep { get; set; }

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
    /// <param name="rx">椭圆长轴长度</param>
    /// <param name="ry">椭圆短轴长度</param>
    /// <param name="angle">旋转角度</param>
    /// <param name="large">大弧标志</param>
    /// <param name="sweep">扫描方向</param>
    /// <param name="x">目标点X坐标</param>
    /// <param name="y">目标点Y坐标</param>
    public Arc(double rx, double ry, double angle, double large, double sweep, double x, double y)
    {
        if (large != 0 && large != 1)
        {
            throw new ArgumentException("large 只接受 0 或 1");
        }
        if (sweep != 0 && sweep != 1)
        {
            throw new ArgumentException("sweep 只接受 0 或 1");
        }

        Rx = rx;
        Ry = ry;
        Angle = angle;
        Large = large;
        Sweep = sweep;
        X = x;
        Y = y;
    }

    /// <summary>
    /// 转换为字符串表示
    /// </summary>
    /// <returns>字符串表示</returns>
    public override string ToString()
    {
        return $"A {StBase.Fmt(Rx)} {StBase.Fmt(Ry)} {StBase.Fmt(Angle)} {StBase.Fmt(Large)} {StBase.Fmt(Sweep)} {StBase.Fmt(X)} {StBase.Fmt(Y)}";
    }
}