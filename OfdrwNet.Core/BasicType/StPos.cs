using System;
using System.Globalization;

namespace OfdrwNet.Core.BasicType;

/// <summary>
/// 点坐标，以空格分割，前者为 x值，后者为 y值，可以是整数或浮点数
/// 
/// 示例：0 0
/// 
/// ————《GB/T 33190-2016》 表 2 基本数据类型
/// 
/// 对应Java版本的 org.ofdrw.core.basicType.ST_Pos
/// </summary>
public class StPos
{
    /// <summary>
    /// X坐标（从左到右）
    /// </summary>
    public double X { get; set; }

    /// <summary>
    /// Y坐标（从上到下）
    /// </summary>
    public double Y { get; set; }

    /// <summary>
    /// 构造函数
    /// </summary>
    /// <param name="x">X坐标</param>
    /// <param name="y">Y坐标</param>
    public StPos(double x, double y)
    {
        X = x;
        Y = y;
    }

    /// <summary>
    /// 获取ST_Pos实例
    /// </summary>
    /// <param name="x">X坐标</param>
    /// <param name="y">Y坐标</param>
    /// <returns>实例</returns>
    public static StPos GetInstance(double x, double y)
    {
        return new StPos(x, y);
    }

    /// <summary>
    /// 获取ST_Pos实例，如果参数非法则返回null
    /// </summary>
    /// <param name="arrStr">数字字符串</param>
    /// <returns>实例或null</returns>
    public static StPos? Parse(string? arrStr)
    {
        if (string.IsNullOrWhiteSpace(arrStr))
        {
            return null;
        }

        var values = arrStr.Trim().Split(' ', StringSplitOptions.RemoveEmptyEntries);

        if (values.Length != 2)
        {
            return null;
        }

        try
        {
            var x = double.Parse(values[0], CultureInfo.InvariantCulture);
            var y = double.Parse(values[1], CultureInfo.InvariantCulture);
            return new StPos(x, y);
        }
        catch (FormatException)
        {
            return null;
        }
    }

    /// <summary>
    /// 设置X坐标
    /// </summary>
    /// <param name="x">X坐标</param>
    /// <returns>this</returns>
    public StPos SetX(double x)
    {
        X = x;
        return this;
    }

    /// <summary>
    /// 设置Y坐标
    /// </summary>
    /// <param name="y">Y坐标</param>
    /// <returns>this</returns>
    public StPos SetY(double y)
    {
        Y = y;
        return this;
    }

    /// <summary>
    /// 转换为字符串表示
    /// </summary>
    /// <returns>字符串表示</returns>
    public override string ToString()
    {
        return $"{StBase.Fmt(X)} {StBase.Fmt(Y)}";
    }

    /// <summary>
    /// 判断是否相等
    /// </summary>
    /// <param name="obj">比较对象</param>
    /// <returns>是否相等</returns>
    public override bool Equals(object? obj)
    {
        if (obj is StPos other)
        {
            return Math.Abs(X - other.X) < 1e-9 && Math.Abs(Y - other.Y) < 1e-9;
        }
        return false;
    }

    /// <summary>
    /// 获取哈希码
    /// </summary>
    /// <returns>哈希码</returns>
    public override int GetHashCode()
    {
        return HashCode.Combine(X, Y);
    }
}