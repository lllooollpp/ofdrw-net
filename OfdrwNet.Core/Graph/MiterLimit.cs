using OfdrwNet.Core.BasicType;

namespace OfdrwNet.Core.Graph;

/// <summary>
/// 斜接限制
/// 
/// 当线段连接方式为斜接(Miter)时，如果两条线段的夹角很小，
/// 斜接连接可能会产生很长的尖角。斜接限制用于控制这种情况，
/// 当斜接长度超过限制时，自动转换为斜切连接。
/// 
/// 斜接限制 = 斜接长度 / 线宽
/// 
/// 对应Java版本：org.ofdrw.core.pageDescription.drawParam.MiterLimit
/// </summary>
public class MiterLimit : StFloat
{
    /// <summary>
    /// 默认斜接限制值
    /// </summary>
    public static readonly double DefaultValue = 10.0;

    /// <summary>
    /// 最小斜接限制值
    /// </summary>
    public static readonly double MinValue = 1.0;

    /// <summary>
    /// 构造斜接限制
    /// </summary>
    /// <param name="value">斜接限制值，必须大于等于1.0</param>
    public MiterLimit(double value) : base(value)
    {
        if (value < MinValue)
        {
            throw new ArgumentException($"斜接限制值必须大于等于{MinValue}，当前值为{value}", nameof(value));
        }
    }

    /// <summary>
    /// 从字符串解析斜接限制
    /// </summary>
    /// <param name="str">字符串表示</param>
    /// <returns>斜接限制</returns>
    public static new MiterLimit Parse(string str)
    {
        if (string.IsNullOrWhiteSpace(str))
        {
            throw new ArgumentException("斜接限制字符串不能为空", nameof(str));
        }

        if (!double.TryParse(str.Trim(), out double value))
        {
            throw new ArgumentException($"无法解析斜接限制值: {str}", nameof(str));
        }

        return new MiterLimit(value);
    }

    /// <summary>
    /// 创建默认斜接限制
    /// </summary>
    /// <returns>默认斜接限制</returns>
    public static MiterLimit Default()
    {
        return new MiterLimit(DefaultValue);
    }

    /// <summary>
    /// 获取斜接限制值
    /// </summary>
    /// <returns>斜接限制值</returns>
    public double GetValue()
    {
        return Value;
    }

    /// <summary>
    /// 设置斜接限制值
    /// </summary>
    /// <param name="value">斜接限制值</param>
    /// <returns>this</returns>
    public MiterLimit SetValue(double value)
    {
        if (value < MinValue)
        {
            throw new ArgumentException($"斜接限制值必须大于等于{MinValue}，当前值为{value}", nameof(value));
        }
        Value = value;
        return this;
    }

    /// <summary>
    /// 判断是否为默认值
    /// </summary>
    /// <returns>是否为默认值</returns>
    public bool IsDefault()
    {
        return Math.Abs(Value - DefaultValue) < 0.001;
    }

    /// <summary>
    /// 计算给定角度的斜接长度与线宽的比值
    /// </summary>
    /// <param name="angleRadians">两条线段的夹角（弧度）</param>
    /// <returns>斜接比值</returns>
    public static double CalculateMiterRatio(double angleRadians)
    {
        // 斜接比值 = 1 / sin(angle/2)
        var halfAngle = Math.Abs(angleRadians) / 2.0;
        if (halfAngle < 0.001) // 避免除零
        {
            return double.MaxValue;
        }
        return 1.0 / Math.Sin(halfAngle);
    }

    /// <summary>
    /// 判断给定角度是否会被限制
    /// </summary>
    /// <param name="angleRadians">两条线段的夹角（弧度）</param>
    /// <returns>是否会被限制</returns>
    public bool WillLimit(double angleRadians)
    {
        var ratio = CalculateMiterRatio(angleRadians);
        return ratio > Value;
    }

    /// <summary>
    /// 获取被限制的临界角度
    /// </summary>
    /// <returns>临界角度（弧度）</returns>
    public double GetLimitAngle()
    {
        // angle = 2 * arcsin(1 / miterLimit)
        return 2.0 * Math.Asin(1.0 / Value);
    }

    /// <summary>
    /// 获取被限制的临界角度（度数）
    /// </summary>
    /// <returns>临界角度（度数）</returns>
    public double GetLimitAngleDegrees()
    {
        return GetLimitAngle() * 180.0 / Math.PI;
    }

    /// <summary>
    /// 克隆斜接限制
    /// </summary>
    /// <returns>克隆的斜接限制</returns>
    public MiterLimit Clone()
    {
        return new MiterLimit(Value);
    }

    /// <summary>
    /// 验证斜接限制是否有效
    /// </summary>
    /// <returns>是否有效</returns>
    public bool IsValid()
    {
        return Value >= MinValue && !double.IsNaN(Value) && !double.IsInfinity(Value);
    }

    /// <summary>
    /// 转换为字符串表示
    /// </summary>
    /// <returns>字符串表示</returns>
    public override string ToString()
    {
        return Value.ToString("0.###");
    }

    /// <summary>
    /// 隐式转换为double
    /// </summary>
    /// <param name="miterLimit">斜接限制</param>
    /// <returns>double值</returns>
    public static implicit operator double(MiterLimit miterLimit)
    {
        return miterLimit?.Value ?? DefaultValue;
    }

    /// <summary>
    /// 从double隐式转换
    /// </summary>
    /// <param name="value">double值</param>
    /// <returns>斜接限制</returns>
    public static implicit operator MiterLimit(double value)
    {
        return new MiterLimit(value);
    }
}
