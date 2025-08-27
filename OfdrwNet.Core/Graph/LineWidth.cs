using OfdrwNet.Core.BasicType;

namespace OfdrwNet.Core.Graph;

/// <summary>
/// 线宽
/// 
/// 定义线段的宽度，单位为毫米。
/// 
/// 对应Java版本：org.ofdrw.core.pageDescription.drawParam.LineWidth
/// </summary>
public class LineWidth : StFloat
{
    /// <summary>
    /// 默认线宽值（毫米）
    /// </summary>
    public static readonly double DefaultValue = 0.353; // 1像素 = 0.353毫米

    /// <summary>
    /// 最小线宽值
    /// </summary>
    public static readonly double MinValue = 0.0;

    /// <summary>
    /// 构造线宽
    /// </summary>
    /// <param name="value">线宽值，单位为毫米，必须大于等于0</param>
    public LineWidth(double value) : base(value)
    {
        if (value < MinValue)
        {
            throw new ArgumentException($"线宽值必须大于等于{MinValue}，当前值为{value}", nameof(value));
        }
    }

    /// <summary>
    /// 从字符串解析线宽
    /// </summary>
    /// <param name="str">字符串表示</param>
    /// <returns>线宽</returns>
    public static new LineWidth Parse(string str)
    {
        if (string.IsNullOrWhiteSpace(str))
        {
            throw new ArgumentException("线宽字符串不能为空", nameof(str));
        }

        if (!double.TryParse(str.Trim(), out double value))
        {
            throw new ArgumentException($"无法解析线宽值: {str}", nameof(str));
        }

        return new LineWidth(value);
    }

    /// <summary>
    /// 创建默认线宽
    /// </summary>
    /// <returns>默认线宽</returns>
    public static LineWidth Default()
    {
        return new LineWidth(DefaultValue);
    }

    /// <summary>
    /// 创建细线
    /// </summary>
    /// <returns>细线线宽</returns>
    public static LineWidth Thin()
    {
        return new LineWidth(0.177); // 0.5像素
    }

    /// <summary>
    /// 创建粗线
    /// </summary>
    /// <returns>粗线线宽</returns>
    public static LineWidth Thick()
    {
        return new LineWidth(1.058); // 3像素
    }

    /// <summary>
    /// 从像素值创建线宽
    /// </summary>
    /// <param name="pixels">像素值</param>
    /// <param name="dpi">DPI，默认为72</param>
    /// <returns>线宽</returns>
    public static LineWidth FromPixels(double pixels, double dpi = 72.0)
    {
        // 1英寸 = 25.4毫米，像素转毫米：pixels * 25.4 / dpi
        var mm = pixels * 25.4 / dpi;
        return new LineWidth(mm);
    }

    /// <summary>
    /// 从点值创建线宽（1点 = 1/72英寸）
    /// </summary>
    /// <param name="points">点值</param>
    /// <returns>线宽</returns>
    public static LineWidth FromPoints(double points)
    {
        // 1点 = 1/72英寸 = 25.4/72毫米
        var mm = points * 25.4 / 72.0;
        return new LineWidth(mm);
    }

    /// <summary>
    /// 获取线宽值（毫米）
    /// </summary>
    /// <returns>线宽值</returns>
    public double GetValue()
    {
        return Value;
    }

    /// <summary>
    /// 获取线宽的像素值
    /// </summary>
    /// <param name="dpi">DPI，默认为72</param>
    /// <returns>像素值</returns>
    public double ToPixels(double dpi = 72.0)
    {
        // 毫米转像素：mm * dpi / 25.4
        return Value * dpi / 25.4;
    }

    /// <summary>
    /// 获取线宽的点值
    /// </summary>
    /// <returns>点值</returns>
    public double ToPoints()
    {
        // 毫米转点：mm * 72 / 25.4
        return Value * 72.0 / 25.4;
    }

    /// <summary>
    /// 设置线宽值
    /// </summary>
    /// <param name="value">线宽值</param>
    /// <returns>this</returns>
    public LineWidth SetValue(double value)
    {
        if (value < MinValue)
        {
            throw new ArgumentException($"线宽值必须大于等于{MinValue}，当前值为{value}", nameof(value));
        }
        Value = value;
        return this;
    }

    /// <summary>
    /// 判断是否为默认线宽
    /// </summary>
    /// <returns>是否为默认线宽</returns>
    public bool IsDefault()
    {
        return Math.Abs(Value - DefaultValue) < 0.001;
    }

    /// <summary>
    /// 判断是否为零线宽（不绘制）
    /// </summary>
    /// <returns>是否为零线宽</returns>
    public bool IsZero()
    {
        return Value < 0.001;
    }

    /// <summary>
    /// 判断是否为细线
    /// </summary>
    /// <returns>是否为细线</returns>
    public bool IsThin()
    {
        return Value < DefaultValue;
    }

    /// <summary>
    /// 判断是否为粗线
    /// </summary>
    /// <returns>是否为粗线</returns>
    public bool IsThick()
    {
        return Value > DefaultValue * 2;
    }

    /// <summary>
    /// 克隆线宽
    /// </summary>
    /// <returns>克隆的线宽</returns>
    public LineWidth Clone()
    {
        return new LineWidth(Value);
    }

    /// <summary>
    /// 验证线宽是否有效
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
    /// <param name="lineWidth">线宽</param>
    /// <returns>double值</returns>
    public static implicit operator double(LineWidth lineWidth)
    {
        return lineWidth?.Value ?? DefaultValue;
    }

    /// <summary>
    /// 从double隐式转换
    /// </summary>
    /// <param name="value">double值</param>
    /// <returns>线宽</returns>
    public static implicit operator LineWidth(double value)
    {
        return new LineWidth(value);
    }

    /// <summary>
    /// 比较两个线宽是否相等
    /// </summary>
    /// <param name="other">另一个线宽</param>
    /// <returns>是否相等</returns>
    public bool Equals(LineWidth? other)
    {
        if (other is null) return false;
        return Math.Abs(Value - other.Value) < 0.001;
    }

    /// <summary>
    /// 重写Equals方法
    /// </summary>
    /// <param name="obj">比较对象</param>
    /// <returns>是否相等</returns>
    public override bool Equals(object? obj)
    {
        return obj is LineWidth other && Equals(other);
    }

    /// <summary>
    /// 获取哈希码
    /// </summary>
    /// <returns>哈希码</returns>
    public override int GetHashCode()
    {
        return Value.GetHashCode();
    }
}
