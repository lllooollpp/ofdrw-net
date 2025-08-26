using OfdrwNet.Layout.Element;

namespace OfdrwNet.Layout.Engine;

/// <summary>
/// 流式布局分析器
/// 对应 Java 版本的 org.ofdrw.layout.engine.StreamingLayoutAnalyzer
/// 负责将流式布局元素转换为虚拟页面
/// </summary>
public class StreamingLayoutAnalyzer
{
    /// <summary>
    /// 页面布局配置
    /// </summary>
    private readonly PageLayout _pageLayout;

    /// <summary>
    /// 构造函数
    /// </summary>
    /// <param name="pageLayout">页面布局</param>
    public StreamingLayoutAnalyzer(PageLayout pageLayout)
    {
        _pageLayout = pageLayout ?? throw new ArgumentNullException(nameof(pageLayout));
    }

    /// <summary>
    /// 分析布局段落序列，转换为虚拟页面列表
    /// </summary>
    /// <param name="segments">布局段落序列</param>
    /// <returns>虚拟页面列表</returns>
    public List<VirtualPage> Analyze(List<Segment> segments)
    {
        var pages = new List<VirtualPage>();
        var currentPage = CreateNewPage();
        double currentY = _pageLayout.StartY;

        foreach (var segment in segments)
        {
            if (segment == null || segment.Elements.Count == 0)
            {
                continue;
            }

            // 检查是否需要分页
            double segmentHeight = segment.GetHeight();
            double availableHeight = _pageLayout.ContentHeight - (currentY - _pageLayout.StartY);

            if (segmentHeight > availableHeight && !currentPage.IsEmpty())
            {
                // 需要分页，保存当前页面并创建新页面
                pages.Add(currentPage);
                currentPage = CreateNewPage();
                currentY = _pageLayout.StartY;
            }

            // 将段落中的元素添加到当前页面
            foreach (var element in segment.Elements)
            {
                // 设置元素位置
                element.SetXY(_pageLayout.StartX, currentY);
                element.SetPosition(Position.Absolute);

                // 确保元素有宽度
                if (element.Width == null)
                {
                    element.Width = _pageLayout.ContentWidth;
                }

                currentPage.Add(element);
            }

            currentY += segmentHeight;
        }

        // 添加最后一个页面（如果不为空）
        if (!currentPage.IsEmpty())
        {
            pages.Add(currentPage);
        }

        // 如果没有页面，至少创建一个空页面
        if (pages.Count == 0)
        {
            pages.Add(CreateNewPage());
        }

        return pages;
    }

    /// <summary>
    /// 创建新的虚拟页面
    /// </summary>
    /// <returns>新的虚拟页面</returns>
    private VirtualPage CreateNewPage()
    {
        return new VirtualPage(_pageLayout);
    }
}

/// <summary>
/// 布局段落
/// 表示可以作为一个整体进行布局的元素组合
/// </summary>
public class Segment
{
    /// <summary>
    /// 段落中的元素列表
    /// </summary>
    public List<Div> Elements { get; private set; } = new List<Div>();

    /// <summary>
    /// 段落的最大宽度
    /// </summary>
    public double MaxWidth { get; private set; }

    /// <summary>
    /// 构造函数
    /// </summary>
    /// <param name="maxWidth">最大宽度</param>
    public Segment(double maxWidth)
    {
        MaxWidth = maxWidth;
    }

    /// <summary>
    /// 尝试添加元素到段落
    /// </summary>
    /// <param name="element">要添加的元素</param>
    /// <returns>是否成功添加</returns>
    public bool TryAdd(Div element)
    {
        if (element == null)
        {
            return false;
        }

        // 检查宽度是否超限
        double elementWidth = (element.Width ?? 0) + element.WidthPlus();
        if (elementWidth > MaxWidth)
        {
            return false;
        }

        Elements.Add(element);
        return true;
    }

    /// <summary>
    /// 强制添加元素到段落
    /// </summary>
    /// <param name="element">要添加的元素</param>
    public void Add(Div element)
    {
        if (element != null)
        {
            Elements.Add(element);
        }
    }

    /// <summary>
    /// 获取段落的总高度
    /// </summary>
    /// <returns>总高度</returns>
    public double GetHeight()
    {
        if (Elements.Count == 0)
        {
            return 0;
        }

        return Elements.Max(e => (e.Height ?? 0) + e.HeightPlus());
    }

    /// <summary>
    /// 获取段落的总宽度
    /// </summary>
    /// <returns>总宽度</returns>
    public double GetWidth()
    {
        if (Elements.Count == 0)
        {
            return 0;
        }

        return Elements.Sum(e => (e.Width ?? 0) + e.WidthPlus());
    }

    /// <summary>
    /// 检查段落是否为空
    /// </summary>
    /// <returns>是否为空</returns>
    public bool IsEmpty()
    {
        return Elements.Count == 0;
    }

    /// <summary>
    /// 清空段落
    /// </summary>
    public void Clear()
    {
        Elements.Clear();
    }

    /// <summary>
    /// 转换为字符串表示
    /// </summary>
    /// <returns>字符串表示</returns>
    public override string ToString()
    {
        return $"Segment(Elements={Elements.Count}, Width={GetWidth()}, Height={GetHeight()})";
    }
}