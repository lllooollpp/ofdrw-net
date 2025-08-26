namespace OfdrwNet.Core;

/// <summary>
/// 图层类型枚举
/// 对应OFD文档中的图层顺序
/// </summary>
public enum LayerType
{
    /// <summary>
    /// 背景层
    /// </summary>
    Background = 0,

    /// <summary>
    /// 正文层
    /// </summary>
    Body = 1,

    /// <summary>
    /// 前景层
    /// </summary>
    Foreground = 2
}