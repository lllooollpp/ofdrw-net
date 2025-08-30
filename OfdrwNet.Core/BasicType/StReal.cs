using System;
using System.Globalization;

namespace OfdrwNet.Core.BasicType;

/// <summary>
/// 数值类型
/// 
/// 用于表示包含小数的数字值，精度不小于64位浮点数的精度
/// 在OFDRW中，浮点数总是使用精确到小数点后3位的字符串形式存储，例如"23.000"
/// 
/// 对应 Java 版本的 org.ofdrw.core.basicType.ST_Real
/// </summary>
public class StReal : StBase
{
    /// <summary>
    /// 默认小数位数
    /// </summary>
    private const int DefaultDecimalPlaces = 3;

    /// <summary>
    /// 实际数值
    /// </summary>
    private double _value;

    /// <summary>
    /// 小数位数
    /// </summary>
    private int _decimalPlaces;

    /// <summary>
    /// 构造函数
    /// </summary>
    /// <param name="value">数值</param>
    /// <param name="decimalPlaces">小数位数，默认为3位</param>
    public StReal(double value, int decimalPlaces = DefaultDecimalPlaces)
    {
        _value = value;
        _decimalPlaces = Math.Max(0, decimalPlaces);
    }

    /// <summary>
    /// 从字符串解析数值
    /// </summary>
    /// <param name="valueStr">数值字符串</param>
    /// <returns>StReal实例或null</returns>
    public static StReal? Parse(string? valueStr)
    {
        if (string.IsNullOrWhiteSpace(valueStr))
        {
            return null;
        }

        try
        {
            var trimmed = valueStr.Trim();
            var value = double.Parse(trimmed, NumberStyles.Float, CultureInfo.InvariantCulture);
            
            // 计算小数位数
            var decimalIndex = trimmed.IndexOf('.');
            var decimalPlaces = decimalIndex == -1 ? 0 : trimmed.Length - decimalIndex - 1;
            
            return new StReal(value, Math.Max(decimalPlaces, DefaultDecimalPlaces));
        }
        catch (FormatException)
        {
            return null;
        }
        catch (OverflowException)
        {
            return null;
        }
    }

    /// <summary>
    /// 从double值创建实例
    /// </summary>
    /// <param name="value">数值</param>
    /// <param name="decimalPlaces">小数位数</param>
    /// <returns>StReal实例</returns>
    public static StReal Of(double value, int decimalPlaces = DefaultDecimalPlaces)
    {
        return new StReal(value, decimalPlaces);
    }

    /// <summary>
    /// 获取或设置数值
    /// </summary>
    public double Value
    {
        get => _value;
        set => _value = value;
    }

    /// <summary>
    /// 获取或设置小数位数
    /// </summary>
    public int DecimalPlaces
    {
        get => _decimalPlaces;
        set => _decimalPlaces = Math.Max(0, value);
    }

    /// <summary>
    /// 设置数值
    /// </summary>
    /// <param name="value">数值</param>
    /// <returns>当前实例</returns>
    public StReal SetValue(double value)
    {
        _value = value;
        return this;
    }

    /// <summary>
    /// 设置小数位数
    /// </summary>
    /// <param name="decimalPlaces">小数位数</param>
    /// <returns>当前实例</returns>
    public StReal SetDecimalPlaces(int decimalPlaces)
    {
        _decimalPlaces = Math.Max(0, decimalPlaces);
        return this;
    }

    /// <summary>
    /// 转换为字符串
    /// </summary>
    /// <returns>格式化的数值字符串</returns>
    public override string ToString()
    {
        var format = _decimalPlaces > 0 ? $"F{_decimalPlaces}" : "F0";
        return _value.ToString(format, CultureInfo.InvariantCulture);
    }

    /// <summary>
    /// 判断是否相等
    /// </summary>
    /// <param name="obj">比较对象</param>
    /// <returns>是否相等</returns>
    public override bool Equals(object? obj)
    {
        if (ReferenceEquals(this, obj))
        {
            return true;
        }

        return obj is StReal other && 
               Math.Abs(_value - other._value) < 1e-10 && 
               _decimalPlaces == other._decimalPlaces;
    }

    /// <summary>
    /// 获取哈希码
    /// </summary>
    /// <returns>哈希码</returns>
    public override int GetHashCode()
    {
        return HashCode.Combine(_value, _decimalPlaces);
    }

    /// <summary>
    /// 隐式转换到 double
    /// </summary>
    /// <param name="real">StReal对象</param>
    /// <returns>double值</returns>
    public static implicit operator double(StReal real)
    {
        return real._value;
    }

    /// <summary>
    /// 隐式转换到字符串
    /// </summary>
    /// <param name="real">StReal对象</param>
    /// <returns>格式化的数值字符串</returns>
    public static implicit operator string(StReal real)
    {
        return real.ToString();
    }

    /// <summary>
    /// 隐式转换从 double
    /// </summary>
    /// <param name="value">double值</param>
    /// <returns>StReal对象</returns>
    public static implicit operator StReal(double value)
    {
        return new StReal(value);
    }

    /// <summary>
    /// 显式转换从 string
    /// </summary>
    /// <param name="valueStr">数值字符串</param>
    /// <returns>StReal对象</returns>
    /// <exception cref="ArgumentException">无法解析的字符串</exception>
    public static explicit operator StReal(string valueStr)
    {
        var result = Parse(valueStr);
        return result ?? throw new ArgumentException($"无法解析数值: {valueStr}", nameof(valueStr));
    }

    /// <summary>
    /// 相等运算符
    /// </summary>
    /// <param name="left">左操作数</param>
    /// <param name="right">右操作数</param>
    /// <returns>是否相等</returns>
    public static bool operator ==(StReal? left, StReal? right)
    {
        if (ReferenceEquals(left, right))
        {
            return true;
        }

        if (left is null || right is null)
        {
            return false;
        }

        return Math.Abs(left._value - right._value) < 1e-10 &&
               left._decimalPlaces == right._decimalPlaces;
    }

    /// <summary>
    /// 不等运算符
    /// </summary>
    /// <param name="left">左操作数</param>
    /// <param name="right">右操作数</param>
    /// <returns>是否不等</returns>
    public static bool operator !=(StReal? left, StReal? right)
    {
        return !(left == right);
    }

    /// <summary>
    /// 加法运算符
    /// </summary>
    /// <param name="left">左操作数</param>
    /// <param name="right">右操作数</param>
    /// <returns>运算结果</returns>
    public static StReal operator +(StReal left, StReal right)
    {
        var decimalPlaces = Math.Max(left._decimalPlaces, right._decimalPlaces);
        return new StReal(left._value + right._value, decimalPlaces);
    }

    /// <summary>
    /// 减法运算符
    /// </summary>
    /// <param name="left">左操作数</param>
    /// <param name="right">右操作数</param>
    /// <returns>运算结果</returns>
    public static StReal operator -(StReal left, StReal right)
    {
        var decimalPlaces = Math.Max(left._decimalPlaces, right._decimalPlaces);
        return new StReal(left._value - right._value, decimalPlaces);
    }

    /// <summary>
    /// 乘法运算符
    /// </summary>
    /// <param name="left">左操作数</param>
    /// <param name="right">右操作数</param>
    /// <returns>运算结果</returns>
    public static StReal operator *(StReal left, StReal right)
    {
        var decimalPlaces = Math.Max(left._decimalPlaces, right._decimalPlaces);
        return new StReal(left._value * right._value, decimalPlaces);
    }

    /// <summary>
    /// 除法运算符
    /// </summary>
    /// <param name="left">左操作数</param>
    /// <param name="right">右操作数</param>
    /// <returns>运算结果</returns>
    /// <exception cref="DivideByZeroException">除数为零</exception>
    public static StReal operator /(StReal left, StReal right)
    {
        if (Math.Abs(right._value) < 1e-10)
        {
            throw new DivideByZeroException("除数不能为零");
        }

        var decimalPlaces = Math.Max(left._decimalPlaces, right._decimalPlaces);
        return new StReal(left._value / right._value, decimalPlaces);
    }

    /// <summary>
    /// 大于运算符
    /// </summary>
    /// <param name="left">左操作数</param>
    /// <param name="right">右操作数</param>
    /// <returns>比较结果</returns>
    public static bool operator >(StReal left, StReal right)
    {
        return left._value > right._value;
    }

    /// <summary>
    /// 小于运算符
    /// </summary>
    /// <param name="left">左操作数</param>
    /// <param name="right">右操作数</param>
    /// <returns>比较结果</returns>
    public static bool operator <(StReal left, StReal right)
    {
        return left._value < right._value;
    }

    /// <summary>
    /// 大于等于运算符
    /// </summary>
    /// <param name="left">左操作数</param>
    /// <param name="right">右操作数</param>
    /// <returns>比较结果</returns>
    public static bool operator >=(StReal left, StReal right)
    {
        return left._value >= right._value;
    }

    /// <summary>
    /// 小于等于运算符
    /// </summary>
    /// <param name="left">左操作数</param>
    /// <param name="right">右操作数</param>
    /// <returns>比较结果</returns>
    public static bool operator <=(StReal left, StReal right)
    {
        return left._value <= right._value;
    }
}
