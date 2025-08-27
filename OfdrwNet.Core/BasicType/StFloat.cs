namespace OfdrwNet.Core.BasicType;

/// <summary>
/// ST_Float 浮点数类型
/// 对应 Java 版本的浮点数基础类型
/// 用于表示OFD标准中的浮点数值
/// </summary>
public class StFloat
{
    /// <summary>
    /// 浮点数值
    /// </summary>
    public double Value { get; set; }

    /// <summary>
    /// 初始化浮点数
    /// </summary>
    /// <param name="value">浮点数值</param>
    public StFloat(double value)
    {
        Value = value;
    }

    /// <summary>
    /// 从字符串解析浮点数
    /// </summary>
    /// <param name="str">字符串表示</param>
    /// <returns>浮点数实例</returns>
    public static StFloat Parse(string str)
    {
        if (double.TryParse(str, out double value))
        {
            return new StFloat(value);
        }
        throw new ArgumentException($"无法解析浮点数: {str}");
    }

    /// <summary>
    /// 转换为字符串
    /// </summary>
    /// <returns>浮点数的字符串表示</returns>
    public override string ToString()
    {
        return Value.ToString("0.###");
    }

    /// <summary>
    /// 隐式转换为 double
    /// </summary>
    /// <param name="stFloat">浮点数实例</param>
    /// <returns>double值</returns>
    public static implicit operator double(StFloat stFloat)
    {
        return stFloat.Value;
    }

    /// <summary>
    /// 隐式转换从 double
    /// </summary>
    /// <param name="value">double值</param>
    /// <returns>浮点数实例</returns>
    public static implicit operator StFloat(double value)
    {
        return new StFloat(value);
    }

    /// <summary>
    /// 相等比较
    /// </summary>
    /// <param name="obj">比较对象</param>
    /// <returns>是否相等</returns>
    public override bool Equals(object? obj)
    {
        if (obj is StFloat other)
            return Math.Abs(Value - other.Value) < 1e-10;
        if (obj is double doubleValue)
            return Math.Abs(Value - doubleValue) < 1e-10;
        return false;
    }

    /// <summary>
    /// 相等性操作符
    /// </summary>
    public static bool operator ==(StFloat left, StFloat right)
    {
        if (ReferenceEquals(left, right)) return true;
        if (left is null || right is null) return false;
        return Math.Abs(left.Value - right.Value) < 1e-10;
    }

    /// <summary>
    /// 不相等操作符
    /// </summary>
    public static bool operator !=(StFloat left, StFloat right)
    {
        return !(left == right);
    }

    /// <summary>
    /// 获取哈希码
    /// </summary>
    /// <returns>哈希码</returns>
    public override int GetHashCode()
    {
        return Value.GetHashCode();
    }

    /// <summary>
    /// 加法操作符
    /// </summary>
    public static StFloat operator +(StFloat left, StFloat right)
    {
        return new StFloat(left.Value + right.Value);
    }

    /// <summary>
    /// 减法操作符
    /// </summary>
    public static StFloat operator -(StFloat left, StFloat right)
    {
        return new StFloat(left.Value - right.Value);
    }

    /// <summary>
    /// 乘法操作符
    /// </summary>
    public static StFloat operator *(StFloat left, StFloat right)
    {
        return new StFloat(left.Value * right.Value);
    }

    /// <summary>
    /// 除法操作符
    /// </summary>
    public static StFloat operator /(StFloat left, StFloat right)
    {
        return new StFloat(left.Value / right.Value);
    }
}
