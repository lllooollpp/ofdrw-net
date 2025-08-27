namespace OfdrwNet.Core.Text;

/// <summary>
/// 文字方向枚举
/// 
/// 指定了文字排列或字符放置的方向
/// 
/// 对应Java版本的 org.ofdrw.core.text.Direction
/// </summary>
public enum Direction
{
    /// <summary>
    /// 0度（从左到右）
    /// </summary>
    Left = 0,

    /// <summary>
    /// 90度（从下到上）
    /// </summary>
    Up = 90,

    /// <summary>
    /// 180度（从右到左）
    /// </summary>
    Right = 180,

    /// <summary>
    /// 270度（从上到下）
    /// </summary>
    Down = 270
}

/// <summary>
/// 文字粗细值枚举
/// 
/// 11.3 表 45
/// 
/// 对应Java版本的 org.ofdrw.core.text.text.Weight
/// </summary>
public enum Weight
{
    /// <summary>
    /// 100 - 极细
    /// </summary>
    Thin = 100,

    /// <summary>
    /// 200 - 特细
    /// </summary>
    ExtraLight = 200,

    /// <summary>
    /// 300 - 细
    /// </summary>
    Light = 300,

    /// <summary>
    /// 400 - 正常（默认值）
    /// </summary>
    Normal = 400,

    /// <summary>
    /// 500 - 中等
    /// </summary>
    Medium = 500,

    /// <summary>
    /// 600 - 半粗
    /// </summary>
    SemiBold = 600,

    /// <summary>
    /// 700 - 粗
    /// </summary>
    Bold = 700,

    /// <summary>
    /// 800 - 特粗
    /// </summary>
    ExtraBold = 800,

    /// <summary>
    /// 900 - 极粗
    /// </summary>
    Black = 900
}
