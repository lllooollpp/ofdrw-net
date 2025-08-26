namespace OfdrwNet.Core.PageDescription.DrawParam;

/// <summary>
/// 线条端点样式枚举
/// 对应 Java 版本的 org.ofdrw.core.pageDescription.drawParam.LineCapType
/// </summary>
public enum LineCapType
{
    /// <summary>
    /// 平端点
    /// </summary>
    Butt,
    
    /// <summary>
    /// 圆端点
    /// </summary>
    Round,
    
    /// <summary>
    /// 方端点
    /// </summary>
    Square
}

/// <summary>
/// 线条端点样式扩展方法
/// </summary>
public static class LineCapTypeExtensions
{
    /// <summary>
    /// 从字符串解析线条端点样式
    /// </summary>
    /// <param name="str">字符串表示</param>
    /// <returns>线条端点样式</returns>
    public static LineCapType Parse(string? str)
    {
        return str?.ToLower() switch
        {
            "butt" => LineCapType.Butt,
            "round" => LineCapType.Round,
            "square" => LineCapType.Square,
            _ => LineCapType.Butt // 默认值
        };
    }

    /// <summary>
    /// 转换为字符串
    /// </summary>
    /// <param name="capType">线条端点样式</param>
    /// <returns>字符串表示</returns>
    public static string ToString(this LineCapType capType)
    {
        return capType switch
        {
            LineCapType.Butt => "Butt",
            LineCapType.Round => "Round",
            LineCapType.Square => "Square",
            _ => "Butt"
        };
    }
}
