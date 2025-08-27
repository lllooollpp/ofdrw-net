using System.Collections.Generic;
using System.Linq;
using OfdrwNet.Core;
using OfdrwNet.Core.BasicStructure.Doc;
using OfdrwNet.Core.BasicStructure.PageTree;
using OfdrwNet.Core.BasicType;
using OfdrwNet.Layout.Element;

namespace OfdrwNet.Layout.Engine;

/// <summary>
/// 虚拟页面解析引擎
/// 
/// 解析虚拟页面转换为OFD页面，放入文档容器中
/// 
/// 对应 Java 版本的 org.ofdrw.layout.engine.VPageParseEngine
/// </summary>
public class VPageParseEngine
{
    /// <summary>
    /// 自动增长的文档ID引用
    /// </summary>
    private int _maxUnitId;

    /// <summary>
    /// 页面序列索引
    /// </summary>
    private Pages? _pages;

    /// <summary>
    /// 页面元素布局
    /// </summary>
    private PageLayout _pageLayout;

    /// <summary>
    /// 公共资源管理器
    /// </summary>
    private ResManager _resManager;

    /// <summary>
    /// 注册的OFDRW元素处理器
    /// </summary>
    private static readonly Dictionary<string, IProcessor> RegisteredProcessors = new();

    /// <summary>
    /// 页面解析前处理器
    /// </summary>
    private IVPageHandler? _beforePageParseHandler;

    static VPageParseEngine()
    {
        // 注册处理器
        Register("Img", new ImgProcessor());
        Register("Paragraph", new ParagraphProcessor());
        Register("Canvas", new CanvasProcessor());
        Register("AreaHolderBlock", new AreaHolderBlockProcessor());
    }

    /// <summary>
    /// 注册处理器
    /// </summary>
    /// <param name="elementType">处理的元素类型</param>
    /// <param name="processor">处理器</param>
    public static void Register(string elementType, IProcessor processor)
    {
        RegisteredProcessors[elementType] = processor;
    }

    /// <summary>
    /// 创建虚拟页面解析器
    /// </summary>
    /// <param name="pageLayout">页面布局样式</param>
    /// <param name="document">文档对象</param>
    /// <param name="resManager">公共资源管理器</param>
    /// <param name="maxUnitId">自增的ID获取器</param>
    public VPageParseEngine(PageLayout pageLayout, Document document, ResManager resManager, int maxUnitId = 1)
    {
        _pageLayout = pageLayout ?? throw new ArgumentNullException(nameof(pageLayout));
        _resManager = resManager ?? throw new ArgumentNullException(nameof(resManager));
        _maxUnitId = maxUnitId;

        // 获取或创建页面集合
        _pages = document.GetPages();
        _pages ??= new Pages();
        document.SetPages(_pages);
    }

    /// <summary>
    /// 设置页面解析前处理器
    /// </summary>
    /// <param name="handler">处理器</param>
    public void SetBeforePageParseHandler(IVPageHandler handler)
    {
        _beforePageParseHandler = handler;
    }

    /// <summary>
    /// 解析序列页面队列为OFD页面
    /// </summary>
    /// <param name="vPageList">虚拟页面队列</param>
    public void Process(List<VirtualPage> vPageList)
    {
        if (vPageList == null || vPageList.Count == 0)
        {
            return;
        }

        var queue = new Queue<VirtualPage>(vPageList);
        while (queue.Count > 0)
        {
            var virtualPage = queue.Dequeue();
            if (virtualPage == null)
            {
                continue;
            }

            // 调用页面解析前处理器
            _beforePageParseHandler?.Handle(virtualPage);

            // 创建新页面
            var pageDir = CreateNewPage();
            var pageLoc = $"Pages/Page_{pageDir.PageId}/Content.xml";

            // 设置虚拟页面页码
            if (virtualPage.PageNum == null)
            {
                virtualPage.PageNum = _pages!.GetSize() + 1;
            }

            // 转换页面内容
            ConvertPageContent(pageLoc, virtualPage, pageDir);
        }
    }

    /// <summary>
    /// 创建新页面目录
    /// </summary>
    /// <returns>页面目录对象</returns>
    private PageDir CreateNewPage()
    {
        var pageId = new StId(++_maxUnitId);
        var pageDir = new PageDir(pageId);
        
        // 添加到页面集合
        var page = new Page(pageId, $"Pages/Page_{pageId}/Content.xml");
        _pages!.AddPage(page);
        
        return pageDir;
    }

    /// <summary>
    /// 转化虚拟页面的内容为实际OFD元素
    /// </summary>
    /// <param name="pageLoc">页面xml绝对路径</param>
    /// <param name="vPage">虚拟页面</param>
    /// <param name="pageDir">虚拟页面目录</param>
    private void ConvertPageContent(string pageLoc, VirtualPage vPage, PageDir pageDir)
    {
        // 创建底层的OFD页面对象
        var page = new Page();
        
        // 设置页面样式
        var vPageStyle = vPage.Style;
        if (!_pageLayout.Equals(vPageStyle))
        {
            // 如果与默认页面样式不一致，需要单独设置页面样式
            page.SetArea(vPageStyle.GetPageArea());
        }

        // 处理页面模板
        var templates = vPage.GetTemplates();
        if (templates != null && templates.Count > 0)
        {
            foreach (var template in templates)
            {
                page.AddTemplate(template);
            }
        }

        // 设置页面内容
        pageDir.SetContent(page);
        
        if (vPage.Content.Count == 0)
        {
            return;
        }

        // 创建页面内容
        var content = new Core.BasicStructure.PageObj.Content();
        var layer = new Core.BasicStructure.PageObj.Layer.CtLayer()
            .SetType(Core.LayerType.Body);

        // 处理虚拟页面中的元素
        foreach (var element in vPage.Content)
        {
            ProcessElement(element, layer);
        }

        content.AddLayer(layer);
        page.SetContent(content);
    }

    /// <summary>
    /// 处理虚拟页面元素
    /// </summary>
    /// <param name="element">虚拟元素</param>
    /// <param name="layer">目标图层</param>
    private void ProcessElement(IElement element, Core.BasicStructure.PageObj.Layer.CtLayer layer)
    {
        if (element == null) return;

        var elementType = element.GetType().Name;
        if (RegisteredProcessors.TryGetValue(elementType, out var processor))
        {
            processor.Process(element, layer, _resManager);
        }
        else
        {
            // 默认处理器 - 处理基本Div元素
            var defaultProcessor = new DivProcessor();
            defaultProcessor.Process(element, layer, _resManager);
        }
    }

    /// <summary>
    /// 获取当前最大单元ID
    /// </summary>
    /// <returns>最大单元ID</returns>
    public int GetMaxUnitId()
    {
        return _maxUnitId;
    }
}

/// <summary>
/// 页面目录类（简化实现）
/// </summary>
public class PageDir
{
    public StId PageId { get; }
    public Core.BasicStructure.PageObj.Page? Content { get; private set; }

    public PageDir(StId pageId)
    {
        PageId = pageId;
    }

    public void SetContent(Core.BasicStructure.PageObj.Page content)
    {
        Content = content;
    }
}

/// <summary>
/// 元素处理器接口
/// </summary>
public interface IProcessor
{
    /// <summary>
    /// 处理元素
    /// </summary>
    /// <param name="element">要处理的元素</param>
    /// <param name="layer">目标图层</param>
    /// <param name="resManager">资源管理器</param>
    void Process(IElement element, Core.BasicStructure.PageObj.Layer.CtLayer layer, ResManager resManager);
}

/// <summary>
/// 虚拟页面处理器接口
/// </summary>
public interface IVPageHandler
{
    /// <summary>
    /// 处理虚拟页面
    /// </summary>
    /// <param name="vPage">虚拟页面</param>
    void Handle(VirtualPage vPage);
}

/// <summary>
/// 默认Div处理器
/// </summary>
public class DivProcessor : IProcessor
{
    public void Process(IElement element, Core.BasicStructure.PageObj.Layer.CtLayer layer, ResManager resManager)
    {
        // TODO: 实现Div元素处理逻辑
    }
}

/// <summary>
/// 图像处理器
/// </summary>
public class ImgProcessor : IProcessor
{
    public void Process(IElement element, Core.BasicStructure.PageObj.Layer.CtLayer layer, ResManager resManager)
    {
        // TODO: 实现图像处理逻辑
    }
}

/// <summary>
/// 段落处理器
/// </summary>
public class ParagraphProcessor : IProcessor
{
    public void Process(IElement element, Core.BasicStructure.PageObj.Layer.CtLayer layer, ResManager resManager)
    {
        // TODO: 实现段落处理逻辑
    }
}

/// <summary>
/// 画布处理器
/// </summary>
public class CanvasProcessor : IProcessor
{
    public void Process(IElement element, Core.BasicStructure.PageObj.Layer.CtLayer layer, ResManager resManager)
    {
        // TODO: 实现画布处理逻辑
    }
}

/// <summary>
/// 区域holder块处理器
/// </summary>
public class AreaHolderBlockProcessor : IProcessor
{
    public void Process(IElement element, Core.BasicStructure.PageObj.Layer.CtLayer layer, ResManager resManager)
    {
        // TODO: 实现区域holder块处理逻辑
    }
}