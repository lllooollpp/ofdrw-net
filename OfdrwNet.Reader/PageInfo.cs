using OfdrwNet.Core;
using OfdrwNet.Core.BasicType;
using OfdrwNet.Reader.Model;
using System.Xml.Linq;

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
}