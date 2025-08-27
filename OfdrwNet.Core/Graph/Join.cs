using OfdrwNet.Core.BasicType;

namespace OfdrwNet.Core.Graph;

/// <summary>
/// 连接方式
/// 
/// 定义线段之间的连接样式，用于描述路径中两条线段连接点的处理方式。
/// 
/// 对应Java版本：org.ofdrw.core.pageDescription.drawParam.Join
/// 表 28 Join
/// </summary>
public class Join : StLoc
{
    /// <summary>
    /// 斜接连接
    /// 两条线段延长相交形成尖角
    /// </summary>
    public static readonly Join Miter = new("Miter");

    /// <summary>
    /// 圆角连接
    /// 在连接点用圆弧连接两条线段
    /// </summary>
    public static readonly Join Round = new("Round");

    /// <summary>
    /// 斜切连接
    /// 在连接点用直线切掉尖角
    /// </summary>
    public static readonly Join Bevel = new("Bevel");

    /// <summary>
    /// 构造连接方式
    /// </summary>
    /// <param name="value">连接方式值</param>
    protected Join(string value) : base(value)
    {
    }

    /// <summary>
    /// 从字符串解析连接方式
    /// </summary>
    /// <param name="str">字符串表示</param>
    /// <returns>连接方式</returns>
    public static new Join Parse(string str)
    {
        if (string.IsNullOrWhiteSpace(str))
        {
            throw new ArgumentException("连接方式字符串不能为空", nameof(str));
        }

        return str.Trim().ToUpperInvariant() switch
        {
            "MITER" => Miter,
            "ROUND" => Round,
            "BEVEL" => Bevel,
            _ => throw new ArgumentException($"未知的连接方式: {str}", nameof(str))
        };
    }

    /// <summary>
    /// 判断是否为斜接连接
    /// </summary>
    /// <returns>是否为斜接连接</returns>
    public bool IsMiter()
    {
        return Equals(Miter);
    }

    /// <summary>
    /// 判断是否为圆角连接
    /// </summary>
    /// <returns>是否为圆角连接</returns>
    public bool IsRound()
    {
        return Equals(Round);
    }

    /// <summary>
    /// 判断是否为斜切连接
    /// </summary>
    /// <returns>是否为斜切连接</returns>
    public bool IsBevel()
    {
        return Equals(Bevel);
    }

    /// <summary>
    /// 获取连接方式的显示名称
    /// </summary>
    /// <returns>显示名称</returns>
    public string GetDisplayName()
    {
        return ToString() switch
        {
            "Miter" => "斜接",
            "Round" => "圆角",
            "Bevel" => "斜切",
            _ => ToString()
        };
    }

    /// <summary>
    /// 克隆连接方式
    /// </summary>
    /// <returns>克隆的连接方式</returns>
    public Join Clone()
    {
        return new Join(ToString());
    }

    /// <summary>
    /// 验证连接方式是否有效
    /// </summary>
    /// <returns>是否有效</returns>
    public bool IsValid()
    {
        var value = ToString();
        return value == "Miter" || value == "Round" || value == "Bevel";
    }

    /// <summary>
    /// 获取所有支持的连接方式
    /// </summary>
    /// <returns>连接方式数组</returns>
    public static Join[] GetAllJoins()
    {
        return new[] { Miter, Round, Bevel };
    }

    /// <summary>
    /// 隐式转换为字符串
    /// </summary>
    /// <param name="join">连接方式</param>
    /// <returns>字符串</returns>
    public static implicit operator string(Join join)
    {
        return join?.ToString() ?? "";
    }

    /// <summary>
    /// 从字符串隐式转换
    /// </summary>
    /// <param name="str">字符串</param>
    /// <returns>连接方式</returns>
    public static implicit operator Join(string str)
    {
        return Parse(str);
    }
}
