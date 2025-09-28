using OfdrwNet.Layout;

namespace OfdrwNet.Models;

/// <summary>
/// 虚拟页面容器（占位）
/// </summary>
public class VirtualPage
{
    public PageLayout Layout { get; }
    public VirtualPage(PageLayout layout){ Layout = layout; }
}
