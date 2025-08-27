using System;
using System.Globalization;
using System.Text.RegularExpressions;

namespace OfdrwNet.Core.BasicType;

/// <summary>
/// 简单类型基类，用于提供便捷的方法实例化元素
/// <para>
/// 对应Java版本的 org.ofdrw.core.basicType.STBase
/// </para>
/// 
/// 作者: 权观宇
/// 起始时间: 2019-10-01 03:25:28
/// </summary>
public abstract class StBase
{
    /// <summary>
    /// 最大保留小数有效位数
    /// </summary>
    public static int MaxKeepDecimal { get; set; } = 3;

    /// <summary>
    /// 如果浮点数为整数，则省略小数
    /// <para>
    /// 浮点数含有小数，那么对保留3位有效小数
    /// </para>
    /// <para>
    /// 可以通过 MaxKeepDecimal 修改最大保留小数位数，非特殊情况不建议修改。
    /// 若数值小于10^-8，那么则认为为0
    /// </para>
    /// </summary>
    /// <param name="d">浮点数</param>
    /// <returns>数字字符串</returns>
    public static string Fmt(double d)
    {
        // 若数值小于10^-8，则认为为0
        if (Math.Abs(d) < 1e-8)
        {
            return "0";
        }

        // 如果是整数，则不显示小数部分
        if (Math.Abs(d - Math.Round(d)) < 1e-9)
        {
            return ((long)Math.Round(d)).ToString(CultureInfo.InvariantCulture);
        }

        // 保留指定位数的小数
        var formatted = d.ToString($"F{MaxKeepDecimal}", CultureInfo.InvariantCulture);
        
        // 移除末尾的0
        formatted = formatted.TrimEnd('0');
        if (formatted.EndsWith("."))
        {
            formatted = formatted.TrimEnd('.');
        }
        
        return formatted;
    }

    /// <summary>
    /// 将字符串转换为double
    /// <para>
    /// 支持处理包含特殊字符的字符串（如右至左标记 \u202c）
    /// </para>
    /// </summary>
    /// <param name="str">字符串</param>
    /// <returns>转换后的double值</returns>
    public static double ToDouble(string str)
    {
        if (string.IsNullOrWhiteSpace(str))
        {
            return 0.0;
        }

        // 移除可能存在的右至左标记和其他不可见字符
        str = Regex.Replace(str, @"[\u202c\u200e\u200f]", "").Trim();

        // 处理正号
        if (str.StartsWith("+"))
        {
            str = str.Substring(1);
        }

        if (double.TryParse(str, NumberStyles.Float, CultureInfo.InvariantCulture, out double result))
        {
            return result;
        }

        throw new FormatException($"无法将字符串 '{str}' 转换为double类型");
    }

    /// <summary>
    /// 将字符串转换为float
    /// </summary>
    /// <param name="str">字符串</param>
    /// <returns>转换后的float值</returns>
    public static float ToFloat(string str)
    {
        return (float)ToDouble(str);
    }

    /// <summary>
    /// 将字符串转换为int
    /// </summary>
    /// <param name="str">字符串</param>
    /// <returns>转换后的int值</returns>
    public static int ToInt(string str)
    {
        if (string.IsNullOrWhiteSpace(str))
        {
            return 0;
        }

        // 移除特殊字符
        str = Regex.Replace(str, @"[\u202c\u200e\u200f]", "").Trim();

        if (str.StartsWith("+"))
        {
            str = str.Substring(1);
        }

        if (int.TryParse(str, NumberStyles.Integer, CultureInfo.InvariantCulture, out int result))
        {
            return result;
        }

        throw new FormatException($"无法将字符串 '{str}' 转换为int类型");
    }

    /// <summary>
    /// 将字符串转换为long
    /// </summary>
    /// <param name="str">字符串</param>
    /// <returns>转换后的long值</returns>
    public static long ToLong(string str)
    {
        if (string.IsNullOrWhiteSpace(str))
        {
            return 0L;
        }

        // 移除特殊字符
        str = Regex.Replace(str, @"[\u202c\u200e\u200f]", "").Trim();

        if (str.StartsWith("+"))
        {
            str = str.Substring(1);
        }

        if (long.TryParse(str, NumberStyles.Integer, CultureInfo.InvariantCulture, out long result))
        {
            return result;
        }

        throw new FormatException($"无法将字符串 '{str}' 转换为long类型");
    }

    /// <summary>
    /// 检查字符串是否为数字
    /// </summary>
    /// <param name="str">字符串</param>
    /// <returns>是否为数字</returns>
    public static bool IsNumeric(string str)
    {
        if (string.IsNullOrWhiteSpace(str))
        {
            return false;
        }

        str = Regex.Replace(str, @"[\u202c\u200e\u200f]", "").Trim();
        
        return double.TryParse(str, NumberStyles.Float, CultureInfo.InvariantCulture, out _);
    }

    /// <summary>
    /// 安全的格式化数值
    /// </summary>
    /// <param name="value">数值</param>
    /// <returns>格式化后的字符串</returns>
    public static string SafeFmt(object? value)
    {
        if (value == null)
        {
            return "0";
        }

        return value switch
        {
            double d => Fmt(d),
            float f => Fmt(f),
            int i => i.ToString(CultureInfo.InvariantCulture),
            long l => l.ToString(CultureInfo.InvariantCulture),
            decimal dec => Fmt((double)dec),
            _ => value.ToString() ?? "0"
        };
    }
}