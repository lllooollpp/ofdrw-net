using System.Collections.Generic;
using OfdrwNet.Layout.Element;
using OfdrwNet.Layout.Engine;

namespace OfdrwNet.Layout;

/// <summary>
/// 流式内容收集器
/// 对应 Java 版本的 org.ofdrw.layout.StreamCollect
/// </summary>
public class StreamCollect
{
    /// <summary>
    /// 流式内容
    /// </summary>
    private readonly List<Div> _content = new();

    /// <summary>
    /// 页码（可选）
    /// </summary>
    public int? PageNum { get; set; }

    /// <summary>
    /// 添加内容元素
    /// </summary>
    /// <param name="element">要添加的元素</param>
    public void Add(Div element)
    {
        if (element != null)
        {
            _content.Add(element);
        }
    }

    /// <summary>
    /// 分析流式内容，转换为虚拟页面
    /// </summary>
    /// <param name="pageLayout">页面布局信息</param>
    /// <returns>虚拟页面集合</returns>
    public List<VirtualPage> Analyze(PageLayout pageLayout)
    {
        var sgmEngine = new SegmentationEngine(pageLayout);
        var analyzer = new StreamingLayoutAnalyzer(pageLayout);
        
        // 流式布局队列经过分段引擎，获取分段队列
        var sgmQueue = sgmEngine.Process(_content);
        
        // 段队列进入布局分析器，构造基于固定布局的虚拟页面
        var virtualPageList = analyzer.Analyze(sgmQueue);
        
        // 如果指定了页码，设置虚拟页面的页码
        if (PageNum.HasValue)
        {
            int start = PageNum.Value;
            foreach (var vPage in virtualPageList)
            {
                vPage.PageNum = start;
                start++;
            }
        }
        
        return virtualPageList;
    }

    /// <summary>
    /// 检查是否为空
    /// </summary>
    /// <returns>是否为空</returns>
    public bool IsEmpty()
    {
        return _content.Count == 0;
    }

    /// <summary>
    /// 清空内容
    /// </summary>
    public void Clear()
    {
        _content.Clear();
    }
}
