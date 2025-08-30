using OfdrwNet.Layout.Element;

namespace OfdrwNet.Layout.Engine;

/// <summary>
/// 页面剩余区域填充器
/// 
/// 用于填充页面剩余的空白区域，通常用于布局计算
/// </summary>
public class PageAreaFiller : Div<PageAreaFiller>
{
    /// <summary>
    /// 构造页面区域填充器
    /// </summary>
    public PageAreaFiller()
    {
        // 填充器默认为可拆分的
        // 在页面布局中用于占据剩余空间
    }
    
    /// <summary>
    /// 创建新的页面区域填充器实例
    /// </summary>
    /// <returns>页面区域填充器</returns>
    public static PageAreaFiller Instance()
    {
        return new PageAreaFiller();
    }
}
