using OfdrwNet.Core.BasicType;
using OfdrwNet.Core.Container;

namespace OfdrwNet.Core.Annotation;

/// <summary>
/// OFD注释管理器
/// 负责管理文档中的所有注释并生成注释文件XML结构
/// </summary>
public class AnnotationManager : IDisposable
{
    /// <summary>
    /// 注释缓存
    /// Key: 注释ID，Value: 注释对象
    /// </summary>
    private readonly Dictionary<string, AnnotationBase> _annotations;

    /// <summary>
    /// 页面注释映射
    /// Key: 页面ID，Value: 该页面的注释ID列表
    /// </summary>
    private readonly Dictionary<string, List<string>> _pageAnnotations;

    /// <summary>
    /// 注释ID生成器
    /// </summary>
    private int _nextAnnotationId = 1;

    /// <summary>
    /// 虚拟容器引用
    /// </summary>
    private readonly IContainer _container;

    /// <summary>
    /// 注释目录路径
    /// </summary>
    public string AnnotationDirectory { get; set; } = "Annotations";

    /// <summary>
    /// 是否已释放资源
    /// </summary>
    private bool _disposed = false;

    /// <summary>
    /// 构造函数
    /// </summary>
    /// <param name="container">虚拟容器</param>
    public AnnotationManager(IContainer container)
    {
        _container = container ?? throw new ArgumentNullException(nameof(container));
        _annotations = new Dictionary<string, AnnotationBase>();
        _pageAnnotations = new Dictionary<string, List<string>>();
    }

    #region 注释添加方法

    /// <summary>
    /// 添加高亮注释
    /// </summary>
    /// <param name="pageId">页面ID</param>
    /// <param name="boundary">边界框</param>
    /// <param name="highlightColor">高亮颜色</param>
    /// <param name="highlightAreas">高亮区域列表</param>
    /// <returns>高亮注释对象</returns>
    public HighlightAnnotation AddHighlightAnnotation(StId pageId, StBox boundary, 
        OfdrwNet.Core.Resource.Color highlightColor, List<StBox>? highlightAreas = null)
    {
        var annotationId = GenerateAnnotationId();
        var annotation = new HighlightAnnotation(annotationId, pageId, boundary, highlightColor);

        if (highlightAreas != null && highlightAreas.Count > 0)
        {
            annotation.AddHighlightAreas(highlightAreas);
        }

        AddAnnotation(annotation);
        return annotation;
    }

    /// <summary>
    /// 添加URL链接注释
    /// </summary>
    /// <param name="pageId">页面ID</param>
    /// <param name="boundary">边界框</param>
    /// <param name="url">URL地址</param>
    /// <returns>链接注释对象</returns>
    public LinkAnnotation AddUrlLinkAnnotation(StId pageId, StBox boundary, string url)
    {
        var annotationId = GenerateAnnotationId();
        var annotation = LinkAnnotation.CreateUrlLink(annotationId, pageId, boundary, url);
        
        AddAnnotation(annotation);
        return annotation;
    }

    /// <summary>
    /// 添加页面跳转链接注释
    /// </summary>
    /// <param name="pageId">当前页面ID</param>
    /// <param name="boundary">边界框</param>
    /// <param name="targetPageId">目标页面ID</param>
    /// <param name="targetLocation">目标位置</param>
    /// <returns>链接注释对象</returns>
    public LinkAnnotation AddPageLinkAnnotation(StId pageId, StBox boundary, StId targetPageId, StBox? targetLocation = null)
    {
        var annotationId = GenerateAnnotationId();
        var annotation = LinkAnnotation.CreatePageLink(annotationId, pageId, boundary, targetPageId, targetLocation);
        
        AddAnnotation(annotation);
        return annotation;
    }

    /// <summary>
    /// 添加邮件链接注释
    /// </summary>
    /// <param name="pageId">页面ID</param>
    /// <param name="boundary">边界框</param>
    /// <param name="email">邮箱地址</param>
    /// <param name="subject">邮件主题</param>
    /// <returns>链接注释对象</returns>
    public LinkAnnotation AddEmailLinkAnnotation(StId pageId, StBox boundary, string email, string? subject = null)
    {
        var annotationId = GenerateAnnotationId();
        var annotation = LinkAnnotation.CreateEmailLink(annotationId, pageId, boundary, email, subject);
        
        AddAnnotation(annotation);
        return annotation;
    }

    /// <summary>
    /// 添加注释到管理器
    /// </summary>
    /// <param name="annotation">注释对象</param>
    private void AddAnnotation(AnnotationBase annotation)
    {
        var annotationIdStr = annotation.Id.ToString();
        var pageIdStr = annotation.PageId.ToString();

        _annotations[annotationIdStr] = annotation;

        if (!_pageAnnotations.ContainsKey(pageIdStr))
        {
            _pageAnnotations[pageIdStr] = new List<string>();
        }
        _pageAnnotations[pageIdStr].Add(annotationIdStr);
    }

    #endregion

    #region 注释查询方法

    /// <summary>
    /// 获取注释
    /// </summary>
    /// <param name="annotationId">注释ID</param>
    /// <returns>注释对象，如果不存在则返回null</returns>
    public AnnotationBase? GetAnnotation(StId annotationId)
    {
        return _annotations.TryGetValue(annotationId.ToString(), out var annotation) ? annotation : null;
    }

    /// <summary>
    /// 获取指定页面的所有注释
    /// </summary>
    /// <param name="pageId">页面ID</param>
    /// <returns>注释列表</returns>
    public List<AnnotationBase> GetPageAnnotations(StId pageId)
    {
        var pageIdStr = pageId.ToString();
        if (!_pageAnnotations.ContainsKey(pageIdStr))
        {
            return new List<AnnotationBase>();
        }

        var annotations = new List<AnnotationBase>();
        foreach (var annotationId in _pageAnnotations[pageIdStr])
        {
            if (_annotations.TryGetValue(annotationId, out var annotation))
            {
                annotations.Add(annotation);
            }
        }

        return annotations;
    }

    /// <summary>
    /// 获取所有注释
    /// </summary>
    /// <returns>注释列表</returns>
    public List<AnnotationBase> GetAllAnnotations()
    {
        return _annotations.Values.ToList();
    }

    /// <summary>
    /// 获取指定类型的注释
    /// </summary>
    /// <param name="type">注释类型</param>
    /// <returns>注释列表</returns>
    public List<AnnotationBase> GetAnnotationsByType(AnnotationType type)
    {
        return _annotations.Values.Where(a => a.Type == type).ToList();
    }

    /// <summary>
    /// 获取注释数量
    /// </summary>
    /// <returns>注释数量</returns>
    public int GetAnnotationCount()
    {
        return _annotations.Count;
    }

    /// <summary>
    /// 获取指定页面的注释数量
    /// </summary>
    /// <param name="pageId">页面ID</param>
    /// <returns>注释数量</returns>
    public int GetPageAnnotationCount(StId pageId)
    {
        var pageIdStr = pageId.ToString();
        return _pageAnnotations.TryGetValue(pageIdStr, out var annotations) ? annotations.Count : 0;
    }

    #endregion

    #region 注释删除方法

    /// <summary>
    /// 删除注释
    /// </summary>
    /// <param name="annotationId">注释ID</param>
    /// <returns>是否成功删除</returns>
    public bool RemoveAnnotation(StId annotationId)
    {
        var annotationIdStr = annotationId.ToString();
        if (_annotations.TryGetValue(annotationIdStr, out var annotation))
        {
            var pageIdStr = annotation.PageId.ToString();
            
            // 从页面注释映射中移除
            if (_pageAnnotations.TryGetValue(pageIdStr, out var pageAnnotations))
            {
                pageAnnotations.Remove(annotationIdStr);
                if (pageAnnotations.Count == 0)
                {
                    _pageAnnotations.Remove(pageIdStr);
                }
            }

            // 从注释缓存中移除
            _annotations.Remove(annotationIdStr);
            return true;
        }

        return false;
    }

    /// <summary>
    /// 删除指定页面的所有注释
    /// </summary>
    /// <param name="pageId">页面ID</param>
    /// <returns>删除的注释数量</returns>
    public int RemovePageAnnotations(StId pageId)
    {
        var pageIdStr = pageId.ToString();
        if (!_pageAnnotations.TryGetValue(pageIdStr, out var annotationIds))
        {
            return 0;
        }

        var removedCount = 0;
        foreach (var annotationId in annotationIds.ToList())
        {
            if (_annotations.Remove(annotationId))
            {
                removedCount++;
            }
        }

        _pageAnnotations.Remove(pageIdStr);
        return removedCount;
    }

    #endregion

    #region XML生成方法

    /// <summary>
    /// 生成注释列表文件XML
    /// </summary>
    /// <returns>注释列表XML内容</returns>
    public string GenerateAnnotationListXml()
    {
        var xml = "<?xml version='1.0' encoding='UTF-8'?>\n";
        xml += "<ofd:Annotations xmlns:ofd='http://www.ofdspec.org/2016'>\n";

        // 按页面分组生成注释
        foreach (var pageGroup in _pageAnnotations.OrderBy(kvp => int.Parse(kvp.Key)))
        {
            var pageId = pageGroup.Key;
            var annotationIds = pageGroup.Value;

            if (annotationIds.Count > 0)
            {
                xml += $"  <ofd:Page PageID='{pageId}'>\n";

                foreach (var annotationId in annotationIds)
                {
                    if (_annotations.TryGetValue(annotationId, out var annotation))
                    {
                        xml += $"    <ofd:AnnotRef ID='{annotation.Id}' Type='{annotation.Type}' />\n";
                    }
                }

                xml += "  </ofd:Page>\n";
            }
        }

        xml += "</ofd:Annotations>";
        return xml;
    }

    /// <summary>
    /// 生成指定页面的注释XML
    /// </summary>
    /// <param name="pageId">页面ID</param>
    /// <returns>页面注释XML内容</returns>
    public string GeneratePageAnnotationXml(StId pageId)
    {
        var annotations = GetPageAnnotations(pageId);
        if (annotations.Count == 0)
        {
            return "";
        }

        var xml = "<?xml version='1.0' encoding='UTF-8'?>\n";
        xml += "<ofd:PageAnnotations xmlns:ofd='http://www.ofdspec.org/2016'>\n";

        foreach (var annotation in annotations.OrderBy(a => a.Id.Value))
        {
            var annotationXml = annotation.ToXml();
            // 添加缩进
            var indentedXml = string.Join("\n", annotationXml.Split('\n').Select(line => "  " + line));
            xml += indentedXml + "\n";
        }

        xml += "</ofd:PageAnnotations>";
        return xml;
    }

    /// <summary>
    /// 将所有注释写入容器
    /// </summary>
    /// <param name="containerFactory">容器工厂函数</param>
    /// <returns>异步任务</returns>
    public async Task FlushToContainerAsync(Func<IContainer> containerFactory)
    {
        if (_annotations.Count == 0)
        {
            return;
        }

        // 确保注释目录存在
        var annotationContainer = _container.ObtainContainer(AnnotationDirectory,
            containerFactory);

        // 生成注释列表文件
        var annotationListXml = GenerateAnnotationListXml();
        annotationContainer.PutObj("Annotations.xml", OfdElement.FromXml(annotationListXml));

        // 为每个有注释的页面生成注释文件
        foreach (var pageGroup in _pageAnnotations)
        {
            var pageId = new StId(int.Parse(pageGroup.Key));
            var pageAnnotationXml = GeneratePageAnnotationXml(pageId);
            
            if (!string.IsNullOrEmpty(pageAnnotationXml))
            {
                var fileName = $"Page_{pageGroup.Key}_Annotations.xml";
                annotationContainer.PutObj(fileName, OfdElement.FromXml(pageAnnotationXml));
            }
        }

        await annotationContainer.FlushAsync();
    }

    #endregion

    #region 实用方法

    /// <summary>
    /// 在指定区域查找注释
    /// </summary>
    /// <param name="pageId">页面ID</param>
    /// <param name="searchArea">搜索区域</param>
    /// <returns>找到的注释列表</returns>
    public List<AnnotationBase> FindAnnotationsInArea(StId pageId, StBox searchArea)
    {
        var pageAnnotations = GetPageAnnotations(pageId);
        return pageAnnotations.Where(annotation => annotation.Boundary.IntersectsWith(searchArea)).ToList();
    }

    /// <summary>
    /// 在指定点查找注释
    /// </summary>
    /// <param name="pageId">页面ID</param>
    /// <param name="x">X坐标</param>
    /// <param name="y">Y坐标</param>
    /// <returns>找到的注释列表</returns>
    public List<AnnotationBase> FindAnnotationsAtPoint(StId pageId, double x, double y)
    {
        var pageAnnotations = GetPageAnnotations(pageId);
        return pageAnnotations.Where(annotation => annotation.Boundary.Contains(x, y)).ToList();
    }

    /// <summary>
    /// 验证所有注释
    /// </summary>
    /// <returns>验证结果字典，Key为注释ID，Value为验证是否通过</returns>
    public Dictionary<string, bool> ValidateAllAnnotations()
    {
        var results = new Dictionary<string, bool>();

        foreach (var annotation in _annotations.Values)
        {
            bool isValid = true;

            // 验证基本属性
            if (annotation.Boundary.Width <= 0 || annotation.Boundary.Height <= 0)
            {
                isValid = false;
            }

            // 验证特定类型的注释
            if (annotation is LinkAnnotation linkAnnotation)
            {
                isValid = isValid && linkAnnotation.ValidateTarget();
            }

            results[annotation.Id.ToString()] = isValid;
        }

        return results;
    }

    /// <summary>
    /// 获取注释统计信息
    /// </summary>
    /// <returns>统计信息</returns>
    public AnnotationStatistics GetStatistics()
    {
        var stats = new AnnotationStatistics
        {
            TotalCount = _annotations.Count,
            PageCount = _pageAnnotations.Count,
            HighlightCount = _annotations.Values.Count(a => a.Type == AnnotationType.Highlight),
            LinkCount = _annotations.Values.Count(a => a.Type == AnnotationType.Link),
            TextCount = _annotations.Values.Count(a => a.Type == AnnotationType.Text),
            StampCount = _annotations.Values.Count(a => a.Type == AnnotationType.Stamp),
            InkCount = _annotations.Values.Count(a => a.Type == AnnotationType.Ink),
            ShapeCount = _annotations.Values.Count(a => a.Type == AnnotationType.Shape)
        };

        return stats;
    }

    /// <summary>
    /// 生成下一个注释ID
    /// </summary>
    /// <returns>注释ID</returns>
    private StId GenerateAnnotationId()
    {
        return new StId(_nextAnnotationId++);
    }

    /// <summary>
    /// 清空所有注释
    /// </summary>
    public void Clear()
    {
        _annotations.Clear();
        _pageAnnotations.Clear();
        _nextAnnotationId = 1;
    }

    /// <summary>
    /// 释放资源
    /// </summary>
    public void Dispose()
    {
        if (!_disposed)
        {
            Clear();
            _disposed = true;
        }
    }

    #endregion

    /// <summary>
    /// 获取管理器摘要信息
    /// </summary>
    /// <returns>摘要字符串</returns>
    public override string ToString()
    {
        return $"AnnotationManager[Annotations={_annotations.Count}, Pages={_pageAnnotations.Count}]";
    }
}

/// <summary>
/// 注释统计信息
/// </summary>
public class AnnotationStatistics
{
    /// <summary>
    /// 总注释数量
    /// </summary>
    public int TotalCount { get; set; }

    /// <summary>
    /// 有注释的页面数量
    /// </summary>
    public int PageCount { get; set; }

    /// <summary>
    /// 高亮注释数量
    /// </summary>
    public int HighlightCount { get; set; }

    /// <summary>
    /// 链接注释数量
    /// </summary>
    public int LinkCount { get; set; }

    /// <summary>
    /// 文本注释数量
    /// </summary>
    public int TextCount { get; set; }

    /// <summary>
    /// 图章注释数量
    /// </summary>
    public int StampCount { get; set; }

    /// <summary>
    /// 墨迹注释数量
    /// </summary>
    public int InkCount { get; set; }

    /// <summary>
    /// 形状注释数量
    /// </summary>
    public int ShapeCount { get; set; }

    /// <summary>
    /// 平均每页注释数量
    /// </summary>
    public double AverageAnnotationsPerPage => PageCount > 0 ? (double)TotalCount / PageCount : 0;

    /// <summary>
    /// 获取统计摘要
    /// </summary>
    /// <returns>统计摘要字符串</returns>
    public override string ToString()
    {
        return $"AnnotationStats[Total={TotalCount}, Pages={PageCount}, Highlight={HighlightCount}, Link={LinkCount}, Text={TextCount}, Stamp={StampCount}, Ink={InkCount}, Shape={ShapeCount}]";
    }
}