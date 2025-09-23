using OfdrwNet.Core;
using OfdrwNet.Core.BasicStructure.PageObj.Layer;
using OfdrwNet.Core.BasicType;
using OfdrwNet.Reader.Model;
using System.Xml.Linq;
using System;
using System.Drawing;

namespace OfdrwNet.Reader;

/// <summary>
/// 页面信息类
/// 对应 Java 版本的 org.ofdrw.reader.PageInfo
/// 包含页面大小、对象、ID、模板等信息
/// </summary>
public class PageInfo
{
    /// <summary>
    /// 页面的物理大小
    /// </summary>
    public StBox Size { get; set; }

    /// <summary>
    /// 页面底层对象
    /// </summary>
    public XElement Obj { get; set; }

    /// <summary>
    /// 页面在OFD中的对象ID
    /// </summary>
    public StId Id { get; set; }

    /// <summary>
    /// 页码，从1起
    /// </summary>
    public int Index { get; set; }

    /// <summary>
    /// 该页面引用的模板页面列表
    /// </summary>
    public List<TemplatePageEntity> Templates { get; set; }

    /// <summary>
    /// 页面的绝对路径
    /// </summary>
    public StLoc PageAbsLoc { get; set; }

    /// <summary>
    /// 页码目录文件的序号 (Page_N 中的 N)
    /// </summary>
    public int PageN { get; set; }

    // ===== T027: 新增渲染状态和缓存属性 =====

    /// <summary>
    /// 渲染上下文
    /// </summary>
    public RenderContext? RenderContext { get; set; }

    /// <summary>
    /// 页面缓存
    /// </summary>
    public PageCacheData? Cache { get; set; }

    /// <summary>
    /// 导航状态
    /// </summary>
    public NavigationState? NavigationState { get; set; }

    /// <summary>
    /// 页面内容对象列表
    /// </summary>
    public List<ContentObject> ContentObjects { get; set; } = new List<ContentObject>();

    /// <summary>
    /// 页面状态
    /// </summary>
    public PageState State { get; set; } = PageState.NotLoaded;

    /// <summary>
    /// 最后渲染时间
    /// </summary>
    public DateTime LastRendered { get; set; }

    /// <summary>
    /// 渲染耗时
    /// </summary>
    public TimeSpan RenderDuration { get; set; }

    /// <summary>
    /// 构造函数
    /// </summary>
    public PageInfo()
    {
        Size = new StBox();
        Obj = new XElement("Page");
        Id = new StId(0);
        Index = 1;
        Templates = new List<TemplatePageEntity>();
        PageAbsLoc = new StLoc("/");
        PageN = 0;

        // T027: 初始化新增属性
        ContentObjects = new List<ContentObject>();
        State = PageState.NotLoaded;
        LastRendered = DateTime.MinValue;
        RenderDuration = TimeSpan.Zero;
    }

    /// <summary>
    /// 设置页面大小
    /// </summary>
    /// <param name="size">页面大小</param>
    /// <returns>this</returns>
    public PageInfo SetSize(StBox size)
    {
        Size = size;
        return this;
    }

    /// <summary>
    /// 设置页面对象
    /// </summary>
    /// <param name="obj">页面对象</param>
    /// <returns>this</returns>
    public PageInfo SetObj(XElement obj)
    {
        Obj = obj;
        return this;
    }

    /// <summary>
    /// 设置页面ID
    /// </summary>
    /// <param name="id">页面ID</param>
    /// <returns>this</returns>
    public PageInfo SetId(StId id)
    {
        Id = id;
        return this;
    }

    /// <summary>
    /// 设置页码
    /// </summary>
    /// <param name="index">页码</param>
    /// <returns>this</returns>
    public PageInfo SetIndex(int index)
    {
        Index = index;
        return this;
    }

    /// <summary>
    /// 设置页面的绝对路径
    /// 同时设置页面的索引号 Page_N
    /// </summary>
    /// <param name="pageAbsLoc">绝对路径</param>
    /// <returns>this</returns>
    public PageInfo SetPageAbsLoc(StLoc pageAbsLoc)
    {
        PageAbsLoc = pageAbsLoc;

        // 解析 Page_N 中的 N
        var pathParts = pageAbsLoc.ToString().Split('/', StringSplitOptions.RemoveEmptyEntries);
        if (pathParts.Length > 1)
        {
            var parentPart = pathParts[pathParts.Length - 2];

            if (parentPart.StartsWith("Page_", StringComparison.OrdinalIgnoreCase))
            {
                if (int.TryParse(parentPart.Substring(5), out int n))
                {
                    PageN = n;
                }
            }
        }

        return this;
    }

    /// <summary>
    /// 设置模板页面列表
    /// </summary>
    /// <param name="templates">模板页面列表</param>
    /// <returns>this</returns>
    public PageInfo SetTemplates(List<TemplatePageEntity> templates)
    {
        Templates = templates ?? new List<TemplatePageEntity>();
        return this;
    }

    /// <summary>
    /// 设置页码目录序号
    /// </summary>
    /// <param name="pageN">序号</param>
    /// <returns>this</returns>
    public PageInfo SetPageN(int pageN)
    {
        PageN = pageN;
        return this;
    }

    /// <summary>
    /// 获取按照order和出现顺序的页面和模板内容
    /// </summary>
    /// <returns>页面和模板内容列表</returns>
    public List<XElement> GetOrderRelatedPageList()
    {
        var result = new List<TemplatePageEntity>(Templates)
        {
            // 添加页面本身作为Body层
            new TemplatePageEntity(LayerType.Body, Obj)
        };

        // 按照order对数组进行排序
        result.Sort((p1, p2) => p1.GetZOrder().CompareTo(p2.GetZOrder()));

        return result.Select(t => t.Page).ToList();
    }

    /// <summary>
    /// 获取整个页面的图层列表（包含模板）
    /// </summary>
    /// <returns>页面所有图层</returns>
    public List<XElement> GetAllLayers()
    {
        var layerList = new List<XElement>();

        // 获取排好序的页面列表（包含页面模板和页面本身）
        foreach (var page in GetOrderRelatedPageList())
        {
            // 查找Content元素
            var contentElement = page.Element("Content");
            if (contentElement != null)
            {
                // 获取所有Layer元素
                var layers = contentElement.Elements("Layer");
                layerList.AddRange(layers);
            }
        }

        return layerList;
    }

    /// <summary>
    /// 获取页面摘要信息
    /// </summary>
    /// <returns>页面摘要字符串</returns>
    public override string ToString()
    {
        return $"Page {Index}: ID={Id}, Size={Size}, Templates={Templates.Count}, Path={PageAbsLoc}";
    }

    // ===== T027: 新增渲染和缓存管理方法 =====

    /// <summary>
    /// 设置渲染上下文
    /// </summary>
    /// <param name="renderContext">渲染上下文</param>
    /// <returns>this</returns>
    public PageInfo SetRenderContext(RenderContext renderContext)
    {
        RenderContext = renderContext;
        return this;
    }

    /// <summary>
    /// 设置页面缓存
    /// </summary>
    /// <param name="cache">页面缓存</param>
    /// <returns>this</returns>
    public PageInfo SetCache(PageCacheData cache)
    {
        Cache = cache;
        return this;
    }

    /// <summary>
    /// 添加内容对象
    /// </summary>
    /// <param name="contentObject">内容对象</param>
    /// <returns>this</returns>
    public PageInfo AddContentObject(ContentObject contentObject)
    {
        if (contentObject != null)
        {
            ContentObjects.Add(contentObject);
        }
        return this;
    }

    /// <summary>
    /// 移除内容对象
    /// </summary>
    /// <param name="objectId">对象ID</param>
    /// <returns>是否成功移除</returns>
    public bool RemoveContentObject(StId objectId)
    {
        var objectToRemove = ContentObjects.FirstOrDefault(obj => obj.Id.Equals(objectId));
        if (objectToRemove != null)
        {
            ContentObjects.Remove(objectToRemove);
            objectToRemove.Dispose();
            return true;
        }
        return false;
    }

    /// <summary>
    /// 获取指定ID的内容对象
    /// </summary>
    /// <param name="objectId">对象ID</param>
    /// <returns>内容对象</returns>
    public ContentObject? GetContentObject(StId objectId)
    {
        return ContentObjects.FirstOrDefault(obj => obj.Id.Equals(objectId));
    }

    /// <summary>
    /// 按Z-Order排序获取内容对象
    /// </summary>
    /// <returns>排序后的内容对象列表</returns>
    public List<ContentObject> GetContentObjectsByZOrder()
    {
        return ContentObjects.OrderBy(obj => obj.ZOrder).ToList();
    }

    /// <summary>
    /// 更新页面状态
    /// </summary>
    /// <param name="newState">新状态</param>
    /// <param name="updateTimestamp">是否更新时间戳</param>
    public void UpdateState(PageState newState, bool updateTimestamp = true)
    {
        var oldState = State;
        State = newState;

        if (updateTimestamp && newState == PageState.Rendered)
        {
            LastRendered = DateTime.UtcNow;
        }

        // 触发状态变化事件
        StateChanged?.Invoke(this, new PageStateChangedEventArgs
        {
            PageInfo = this,
            OldState = oldState,
            NewState = newState,
            Timestamp = DateTime.UtcNow
        });
    }

    /// <summary>
    /// 设置渲染耗时
    /// </summary>
    /// <param name="duration">渲染耗时</param>
    public void SetRenderDuration(TimeSpan duration)
    {
        RenderDuration = duration;
        if (Cache != null)
        {
            Cache.LastUpdate = DateTime.UtcNow;
        }
    }

    /// <summary>
    /// 检查缓存是否有效
    /// </summary>
    /// <returns>缓存是否有效</returns>
    public bool IsCacheValid()
    {
        return Cache?.RenderedBitmap != null &&
               State == PageState.Rendered &&
               DateTime.UtcNow - LastRendered < TimeSpan.FromMinutes(30); // 30分钟内的缓存有效
    }

    /// <summary>
    /// 清理页面缓存
    /// </summary>
    public void ClearCache()
    {
        if (Cache != null)
        {
            Cache.RenderedBitmap?.Dispose();
            Cache.ThumbnailBitmap?.Dispose();
            Cache.ObjectCache.Clear();
            Cache.LastUpdate = DateTime.UtcNow;
        }
    }

    /// <summary>
    /// 初始化页面缓存
    /// </summary>
    /// <param name="maxCacheSize">最大缓存大小(字节)</param>
    public void InitializeCache(long maxCacheSize = 50 * 1024 * 1024) // 默认50MB
    {
        Cache = new PageCacheData
        {
            MaxMemoryUsage = maxCacheSize,
            LastUpdate = DateTime.UtcNow,
            ObjectCache = new Dictionary<string, object>()
        };
    }

    /// <summary>
    /// 获取页面内存使用量
    /// </summary>
    /// <returns>内存使用量(字节)</returns>
    public long GetMemoryUsage()
    {
        long totalMemory = 0;

        if (Cache != null)
        {
            totalMemory += Cache.MemoryUsage;
        }

        // 估算内容对象内存使用
        foreach (var contentObject in ContentObjects)
        {
            totalMemory += EstimateObjectMemoryUsage(contentObject);
        }

        return totalMemory;
    }

    /// <summary>
    /// 检查页面是否需要重新渲染
    /// </summary>
    /// <returns>是否需要重新渲染</returns>
    public bool NeedsRerender()
    {
        return State != PageState.Rendered ||
               !IsCacheValid() ||
               ContentObjects.Any(obj => !obj.IsCacheValid);
    }

    /// <summary>
    /// 页面状态变化事件
    /// </summary>
    public event EventHandler<PageStateChangedEventArgs>? StateChanged;

    /// <summary>
    /// 估算对象内存使用量
    /// </summary>
    private long EstimateObjectMemoryUsage(ContentObject obj)
    {
        return obj switch
        {
            TextObject => 1024,      // 文本对象约1KB
            ImageObject => 10240,    // 图像对象约10KB
            VectorObject => 2048,    // 矢量对象约2KB
            _ => 512                 // 其他对象约512B
        };
    }

    /// <summary>
    /// 获取详细的页面信息
    /// </summary>
    /// <returns>详细页面信息字符串</returns>
    public string GetDetailedInfo()
    {
        return $"页面 {Index}: " +
               $"ID={Id}, " +
               $"尺寸={Size}, " +
               $"状态={State}, " +
               $"对象数={ContentObjects.Count}, " +
               $"模板数={Templates.Count}, " +
               $"内存={GetMemoryUsage() / 1024.0:F1}KB, " +
               $"最后渲染={LastRendered:yyyy-MM-dd HH:mm:ss}, " +
               $"渲染耗时={RenderDuration.TotalMilliseconds:F1}ms";
    }
}
