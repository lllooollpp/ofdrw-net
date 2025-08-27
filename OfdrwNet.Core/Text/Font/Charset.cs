using System;

namespace OfdrwNet.Core.Text.Font;

/// <summary>
/// 字形适用的字符分类
/// 
/// 用于匹配替代字形
/// 
/// 11.1 表 44
/// 附录 A.5 CT_Font
/// 
/// 对应Java版本的 org.ofdrw.core.text.font.Charset
/// </summary>
public enum Charset
{
    /// <summary>
    /// 符号
    /// </summary>
    Symbol,
    
    /// <summary>
    /// 中文简体
    /// </summary>
    Prc,
    
    /// <summary>
    /// 中文繁体
    /// </summary>
    Big5,
    
    /// <summary>
    /// 日文
    /// </summary>
    ShiftJis,
    
    /// <summary>
    /// 韩文
    /// </summary>
    Wansung,
    
    /// <summary>
    /// 韩文
    /// </summary>
    Johab,
    
    /// <summary>
    /// 默认值 - Unicode
    /// </summary>
    Unicode
}

/// <summary>
/// Charset枚举扩展方法
/// </summary>
public static class CharsetExtensions
{
    /// <summary>
    /// 从字符串解析Charset枚举值
    /// </summary>
    /// <param name="name">字符串名称</param>
    /// <returns>Charset枚举值</returns>
    public static Charset Parse(string? name)
    {
        name = name?.Trim() ?? "";
        
        return name.ToLowerInvariant() switch
        {
            "symbol" => Charset.Symbol,
            "prc" => Charset.Prc,
            "big5" => Charset.Big5,
            "shift-jis" => Charset.ShiftJis,
            "wansung" => Charset.Wansung,
            "johab" => Charset.Johab,
            "" or "unicode" => Charset.Unicode,
            _ => throw new ArgumentException($"未知的字形适用的字符分类：{name}")
        };
    }

    /// <summary>
    /// 转换为字符串表示
    /// </summary>
    /// <param name="charset">字符集枚举</param>
    /// <returns>字符串表示</returns>
    public static string ToOfdString(this Charset charset)
    {
        return charset switch
        {
            Charset.Symbol => "symbol",
            Charset.Prc => "prc",
            Charset.Big5 => "big5",
            Charset.ShiftJis => "shift-jis",
            Charset.Wansung => "wansung",
            Charset.Johab => "johab",
            Charset.Unicode => "unicode",
            _ => "unicode"
        };
    }
}