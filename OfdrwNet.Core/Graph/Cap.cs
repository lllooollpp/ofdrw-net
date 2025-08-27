using OfdrwNet.Core.BasicType;

namespace OfdrwNet.Core.Graph;

/// <summary>
/// 端点样式
/// 
/// 定义线段端点的样式，用于描述开放路径端点的处理方式。
/// 
/// 对应Java版本：org.ofdrw.core.pageDescription.drawParam.Cap
/// 表 29 Cap
/// </summary>
public class Cap : StLoc
{
    /// <summary>
    /// 平端点
    /// 线段在端点处直接截断，不延伸
    /// </summary>
    public static readonly Cap Butt = new("Butt");

    /// <summary>
    /// 圆形端点
    /// 在端点处添加半圆形帽子
    /// </summary>
    public static readonly Cap Round = new("Round");

    /// <summary>
    /// 方形端点
    /// 在端点处添加矩形帽子，长度等于线宽的一半
    /// </summary>
    public static readonly Cap Square = new("Square");

    /// <summary>
    /// 构造端点样式
    /// </summary>
    /// <param name="value">端点样式值</param>
    protected Cap(string value) : base(value)
    {
    }

    /// <summary>
    /// 从字符串解析端点样式
    /// </summary>
    /// <param name="str">字符串表示</param>
    /// <returns>端点样式</returns>
    public static new Cap Parse(string str)
    {
        if (string.IsNullOrWhiteSpace(str))
        {
            throw new ArgumentException("端点样式字符串不能为空", nameof(str));
        }

        return str.Trim().ToUpperInvariant() switch
        {
            "BUTT" => Butt,
            "ROUND" => Round,
            "SQUARE" => Square,
            _ => throw new ArgumentException($"未知的端点样式: {str}", nameof(str))
        };
    }

    /// <summary>
    /// 判断是否为平端点
    /// </summary>
    /// <returns>是否为平端点</returns>
    public bool IsButt()
    {
        return Equals(Butt);
    }

    /// <summary>
    /// 判断是否为圆形端点
    /// </summary>
    /// <returns>是否为圆形端点</returns>
    public bool IsRound()
    {
        return Equals(Round);
    }

    /// <summary>
    /// 判断是否为方形端点
    /// </summary>
    /// <returns>是否为方形端点</returns>
    public bool IsSquare()
    {
        return Equals(Square);
    }

    /// <summary>
    /// 获取端点样式的显示名称
    /// </summary>
    /// <returns>显示名称</returns>
    public string GetDisplayName()
    {
        return ToString() switch
        {
            "Butt" => "平端点",
            "Round" => "圆形端点",
            "Square" => "方形端点",
            _ => ToString()
        };
    }

    /// <summary>
    /// 克隆端点样式
    /// </summary>
    /// <returns>克隆的端点样式</returns>
    public Cap Clone()
    {
        return new Cap(ToString());
    }

    /// <summary>
    /// 验证端点样式是否有效
    /// </summary>
    /// <returns>是否有效</returns>
    public bool IsValid()
    {
        var value = ToString();
        return value == "Butt" || value == "Round" || value == "Square";
    }

    /// <summary>
    /// 获取所有支持的端点样式
    /// </summary>
    /// <returns>端点样式数组</returns>
    public static Cap[] GetAllCaps()
    {
        return new[] { Butt, Round, Square };
    }

    /// <summary>
    /// 隐式转换为字符串
    /// </summary>
    /// <param name="cap">端点样式</param>
    /// <returns>字符串</returns>
    public static implicit operator string(Cap cap)
    {
        return cap?.ToString() ?? "";
    }

    /// <summary>
    /// 从字符串隐式转换
    /// </summary>
    /// <param name="str">字符串</param>
    /// <returns>端点样式</returns>
    public static implicit operator Cap(string str)
    {
        return Parse(str);
    }
}
