using System.Collections.Generic;
using System.Linq;
using System.Xml.Linq;
using OfdrwNet.Core.BasicStructure.PageObj.Layer;
using OfdrwNet.Core.BasicType;

namespace OfdrwNet.Core.CompositeObj;

/// <summary>
/// 矢量图像
/// 
/// 复合对象引用的资源时 Res 中的矢量图像（CompositeGraphUnit）
/// 
/// 对应 Java 版本的 org.ofdrw.core.compositeObj.CT_VectorG
/// 13 图 72 表 50
/// </summary>
public class VectorG : OfdElement
{
    /// <summary>
    /// 从现有元素构造矢量图像
    /// </summary>
    /// <param name="element">XML元素</param>
    public VectorG(XElement element) : base(element)
    {
    }

    /// <summary>
    /// 构造新的矢量图像
    /// </summary>
    public VectorG() : base("VectorG")
    {
    }

    /// <summary>
    /// 构造带名称的矢量图像
    /// </summary>
    /// <param name="name">元素名称</param>
    protected VectorG(string name) : base(name)
    {
    }

    /// <summary>
    /// 【必选 属性】
    /// 设置矢量图像的标识
    /// </summary>
    /// <param name="id">矢量图像标识</param>
    /// <returns>this</returns>
    public VectorG SetId(StId id)
    {
        SetAttribute("ID", id.ToString());
        return this;
    }

    /// <summary>
    /// 【必选 属性】
    /// 获取矢量图像的标识
    /// </summary>
    /// <returns>矢量图像标识</returns>
    public StId? GetId()
    {
        var value = GetAttributeValue("ID");
        return string.IsNullOrEmpty(value) ? null : StId.Parse(value);
    }

    /// <summary>
    /// 【必选 属性】
    /// 设置矢量图像的宽度
    /// 超出部分做裁剪处理
    /// </summary>
    /// <param name="width">宽度，单位为毫米</param>
    /// <returns>this</returns>
    public VectorG SetWidth(double width)
    {
        SetAttribute("Width", width.ToString("F3"));
        return this;
    }

    /// <summary>
    /// 【必选 属性】
    /// 获取矢量图像的宽度
    /// </summary>
    /// <returns>宽度，单位为毫米</returns>
    public double? GetWidth()
    {
        var value = GetAttributeValue("Width");
        return double.TryParse(value, out var width) ? width : null;
    }

    /// <summary>
    /// 【必选 属性】
    /// 设置矢量图像的高度
    /// 超出部分做裁剪处理
    /// </summary>
    /// <param name="height">高度，单位为毫米</param>
    /// <returns>this</returns>
    public VectorG SetHeight(double height)
    {
        SetAttribute("Height", height.ToString("F3"));
        return this;
    }

    /// <summary>
    /// 【必选 属性】
    /// 获取矢量图像的高度
    /// </summary>
    /// <returns>高度，单位为毫米</returns>
    public double? GetHeight()
    {
        var value = GetAttributeValue("Height");
        return double.TryParse(value, out var height) ? height : null;
    }

    /// <summary>
    /// 【可选 属性】
    /// 设置矢量图像的替换颜色空间标识
    /// </summary>
    /// <param name="substitution">替换颜色空间标识</param>
    /// <returns>this</returns>
    public VectorG SetSubstitution(StRefId substitution)
    {
        SetAttribute("Substitution", substitution.ToString());
        return this;
    }

    /// <summary>
    /// 【可选 属性】
    /// 获取矢量图像的替换颜色空间标识
    /// </summary>
    /// <returns>替换颜色空间标识</returns>
    public StRefId? GetSubstitution()
    {
        var value = GetAttributeValue("Substitution");
        return string.IsNullOrEmpty(value) ? null : StRefId.Parse(value);
    }

    /// <summary>
    /// 【必选】
    /// 设置内容的矢量描述
    /// </summary>
    /// <param name="content">矢量内容</param>
    /// <returns>this</returns>
    public VectorG SetContent(VectorContent content)
    {
        if (content == null)
        {
            throw new ArgumentNullException(nameof(content), "矢量内容不能为空");
        }
        RemoveOfdElementsByNames("Content");
        Add(content);
        return this;
    }

    /// <summary>
    /// 【必选】
    /// 获取内容的矢量描述
    /// </summary>
    /// <returns>矢量内容</returns>
    public VectorContent? GetContent()
    {
        var contentElement = Element.Element(Const.OfdNamespace + "Content");
        return contentElement != null ? new VectorContent(contentElement) : null;
    }

    /// <summary>
    /// 【必选】
    /// 添加页面块到内容中
    /// </summary>
    /// <param name="blockType">页面块类型</param>
    /// <returns>this</returns>
    public VectorG AddContent(PageBlockType blockType)
    {
        var content = GetContent();
        if (content == null)
        {
            content = new VectorContent();
            SetContent(content);
        }
        
        var pageBlock = new PageBlock().SetType(blockType);
        content.AddPageBlock(pageBlock);
        return this;
    }

    /// <summary>
    /// 验证矢量图像是否有效
    /// </summary>
    /// <returns>是否有效</returns>
    public bool IsValid()
    {
        return GetId() != null && 
               GetWidth().HasValue && GetWidth() > 0 &&
               GetHeight().HasValue && GetHeight() > 0 &&
               GetContent() != null;
    }

    /// <summary>
    /// 获取矢量图像的边界框
    /// </summary>
    /// <returns>边界框</returns>
    public StBox? GetBoundingBox()
    {
        var width = GetWidth();
        var height = GetHeight();
        if (width.HasValue && height.HasValue)
        {
            return new StBox(0, 0, width.Value, height.Value);
        }
        return null;
    }

    /// <summary>
    /// 创建矢量图像
    /// </summary>
    /// <param name="id">矢量图像标识</param>
    /// <param name="width">宽度</param>
    /// <param name="height">高度</param>
    /// <returns>矢量图像</returns>
    public static VectorG Create(StId id, double width, double height)
    {
        var vectorG = new VectorG()
            .SetId(id)
            .SetWidth(width)
            .SetHeight(height);

        // 创建默认内容
        var content = new VectorContent();
        vectorG.SetContent(content);

        return vectorG;
    }

    /// <summary>
    /// 获取矢量图像描述信息
    /// </summary>
    /// <returns>描述字符串</returns>
    public override string ToString()
    {
        var id = GetId();
        var width = GetWidth();
        var height = GetHeight();
        return $"VectorG[ID={id}, Width={width}, Height={height}]";
    }
}

/// <summary>
/// 矢量内容
/// </summary>
public class VectorContent : OfdElement
{
    /// <summary>
    /// 从现有元素构造矢量内容
    /// </summary>
    /// <param name="element">XML元素</param>
    public VectorContent(XElement element) : base(element)
    {
    }

    /// <summary>
    /// 构造新的矢量内容
    /// </summary>
    public VectorContent() : base("Content")
    {
    }

    /// <summary>
    /// 添加页面块
    /// </summary>
    /// <param name="pageBlock">页面块</param>
    /// <returns>this</returns>
    public VectorContent AddPageBlock(PageBlock pageBlock)
    {
        if (pageBlock != null)
        {
            Add(pageBlock);
        }
        return this;
    }

    /// <summary>
    /// 获取所有页面块
    /// </summary>
    /// <returns>页面块列表</returns>
    public List<PageBlock> GetPageBlocks()
    {
        return Element.Elements(Const.OfdNamespace + "PageBlock")
            .Select(e => new PageBlock(e))
            .ToList();
    }

    /// <summary>
    /// 获取页面块数量
    /// </summary>
    /// <returns>页面块数量</returns>
    public int GetPageBlockCount()
    {
        return Element.Elements(Const.OfdNamespace + "PageBlock").Count();
    }

    /// <summary>
    /// 清除所有页面块
    /// </summary>
    /// <returns>this</returns>
    public VectorContent ClearPageBlocks()
    {
        RemoveOfdElementsByNames("PageBlock");
        return this;
    }
}
