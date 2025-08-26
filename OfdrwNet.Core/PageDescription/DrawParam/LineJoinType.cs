namespace OfdrwNet.Core.PageDescription.DrawParam;

/// <summary>
/// 线条连接样式枚举
/// 对应 Java 版本的 org.ofdrw.core.pageDescription.drawParam.LineJoinType
/// </summary>
public enum LineJoinType
{
    /// <summary>
    /// 斜接连接
    /// </summary>
    Miter,
    
    /// <summary>
    /// 圆角连接
    /// </summary>
    Round,
    
    /// <summary>
    /// 斜角连接
    /// </summary>
    Bevel
}

/// <summary>
/// 线条连接样式扩展方法
/// </summary>
public static class LineJoinTypeExtensions
{
    /// <summary>
    /// 从字符串解析线条连接样式
    /// </summary>
    /// <param name="str">字符串表示</param>
    /// <returns>线条连接样式</returns>
    public static LineJoinType Parse(string? str)
    {
        return str?.ToLower() switch
        {
            "miter" => LineJoinType.Miter,
            "round" => LineJoinType.Round,
            "bevel" => LineJoinType.Bevel,
            _ => LineJoinType.Miter // 默认值
        };
    }

    /// <summary>
    /// 转换为字符串
    /// </summary>
    /// <param name="joinType">线条连接样式</param>
    /// <returns>字符串表示</returns>
    public static string ToString(this LineJoinType joinType)
    {
        return joinType switch
        {
            LineJoinType.Miter => "Miter",
            LineJoinType.Round => "Round",
            LineJoinType.Bevel => "Bevel",
            _ => "Miter"
        };
    }
}
