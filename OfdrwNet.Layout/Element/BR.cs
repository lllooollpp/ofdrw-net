namespace OfdrwNet.Layout.Element;

/// <summary>
/// 换行元素 (Break)
/// 
/// 表示强制换行的元素，类似于HTML中的&lt;br&gt;标签
/// </summary>
public class BR : Div<BR>
{
    /// <summary>
    /// 构造换行元素
    /// </summary>
    public BR()
    {
        // 换行元素默认没有内容，高度为0
        Height = 0;
    }
    
    /// <summary>
    /// 创建新的换行元素实例
    /// </summary>
    /// <returns>换行元素</returns>
    public static BR Instance()
    {
        return new BR();
    }
}
