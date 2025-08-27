using System;

namespace OfdrwNet.Core.Graph.PathObj;

/// <summary>
/// 图形的填充规则
/// 
/// 9.1 表 35 图形对象描述
/// 
/// 对应Java版本的 org.ofdrw.core.graph.pathObj.Rule
/// </summary>
public enum Rule
{
    /// <summary>
    /// 非零规则（默认）
    /// </summary>
    NonZero,

    /// <summary>
    /// 奇偶规则
    /// </summary>
    EvenOdd
}

/// <summary>
/// Rule枚举扩展方法
/// </summary>
public static class RuleExtensions
{
    /// <summary>
    /// 从字符串解析Rule枚举值
    /// </summary>
    /// <param name="type">字符串类型</param>
    /// <returns>Rule枚举值</returns>
    public static Rule Parse(string? type)
    {
        type = type?.Trim() ?? "";
        
        return type switch
        {
            "" or "NonZero" => Rule.NonZero,
            "Even-Odd" => Rule.EvenOdd,
            _ => throw new ArgumentException($"未知的图形的填充规则类型：{type}")
        };
    }

    /// <summary>
    /// 转换为OFD字符串表示
    /// </summary>
    /// <param name="rule">填充规则</param>
    /// <returns>字符串表示</returns>
    public static string ToOfdString(this Rule rule)
    {
        return rule switch
        {
            Rule.NonZero => "NonZero",
            Rule.EvenOdd => "Even-Odd",
            _ => "NonZero"
        };
    }
}