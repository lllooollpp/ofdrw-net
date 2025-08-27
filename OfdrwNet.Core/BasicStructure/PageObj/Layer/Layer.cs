using System.Xml.Linq;
using OfdrwNet.Core.BasicType;

namespace OfdrwNet.Core.BasicStructure.PageObj.Layer;

/// <summary>
/// 图层
/// 
/// 定义页面中的图层结构，用于组织和管理页面内容。
/// 图层提供了内容的分层显示和管理功能。
/// 
/// 对应OFD标准中的Layer定义
/// 7.7 图层 图 17 表 30
/// </summary>
public class Layer : OfdElement
{
    /// <summary>
    /// 从现有元素构造图层
    /// </summary>
    /// <param name="element">XML元素</param>
    public Layer(XElement element) : base(element)
    {
    }

    /// <summary>
    /// 构造新的图层元素
    /// </summary>
    public Layer() : base("Layer")
    {
    }

    /// <summary>
    /// 【可选 属性】
    /// 设置图层标识符
    /// </summary>
    /// <param name="id">图层标识符</param>
    /// <returns>this</returns>
    public Layer SetID(StId id)
    {
        SetAttribute("ID", id.ToString());
        return this;
    }

    /// <summary>
    /// 【可选 属性】
    /// 获取图层标识符
    /// </summary>
    /// <returns>图层标识符</returns>
    public StId? GetID()
    {
        var value = GetAttributeValue("ID");
        return string.IsNullOrEmpty(value) ? null : StId.Parse(value);
    }

    /// <summary>
    /// 【可选 属性】
    /// 设置图层类型
    /// </summary>
    /// <param name="type">图层类型</param>
    /// <returns>this</returns>
    public Layer SetType(LayerType type)
    {
        SetAttribute("Type", type.ToString());
        return this;
    }

    /// <summary>
    /// 【可选 属性】
    /// 获取图层类型
    /// </summary>
    /// <returns>图层类型</returns>
    public new LayerType GetType()
    {
        var value = GetAttributeValue("Type");
        return Enum.TryParse<LayerType>(value, out var type) 
            ? type 
            : LayerType.Body; // 默认值
    }

    /// <summary>
    /// 【可选 属性】
    /// 设置图层名称
    /// </summary>
    /// <param name="name">图层名称</param>
    /// <returns>this</returns>
    public Layer SetName(string name)
    {
        SetAttribute("Name", name);
        return this;
    }

    /// <summary>
    /// 【可选 属性】
    /// 获取图层名称
    /// </summary>
    /// <returns>图层名称</returns>
    public string? GetName()
    {
        return GetAttributeValue("Name");
    }

    /// <summary>
    /// 【可选 属性】
    /// 设置图层是否可见
    /// </summary>
    /// <param name="visible">是否可见</param>
    /// <returns>this</returns>
    public Layer SetVisible(bool visible)
    {
        SetAttribute("Visible", visible ? "true" : "false");
        return this;
    }

    /// <summary>
    /// 【可选 属性】
    /// 获取图层是否可见
    /// </summary>
    /// <returns>是否可见，默认true</returns>
    public bool GetVisible()
    {
        var value = GetAttributeValue("Visible");
        return string.IsNullOrEmpty(value) || bool.Parse(value);
    }

    /// <summary>
    /// 【可选 属性】
    /// 设置图层透明度
    /// </summary>
    /// <param name="opacity">透明度（0.0-1.0）</param>
    /// <returns>this</returns>
    public Layer SetOpacity(double opacity)
    {
        if (opacity < 0.0 || opacity > 1.0)
            throw new ArgumentException("Opacity must be between 0.0 and 1.0", nameof(opacity));
        
        SetAttribute("Opacity", opacity.ToString());
        return this;
    }

    /// <summary>
    /// 【可选 属性】
    /// 获取图层透明度
    /// </summary>
    /// <returns>透明度，默认1.0（不透明）</returns>
    public double GetOpacity()
    {
        var value = GetAttributeValue("Opacity");
        return string.IsNullOrEmpty(value) ? 1.0 : double.Parse(value);
    }

    /// <summary>
    /// 【可选 属性】
    /// 设置绘制参数引用
    /// </summary>
    /// <param name="drawParamRef">绘制参数引用</param>
    /// <returns>this</returns>
    public Layer SetDrawParam(StRefId drawParamRef)
    {
        SetAttribute("DrawParam", drawParamRef.ToString());
        return this;
    }

    /// <summary>
    /// 【可选 属性】
    /// 获取绘制参数引用
    /// </summary>
    /// <returns>绘制参数引用</returns>
    public StRefId? GetDrawParam()
    {
        var value = GetAttributeValue("DrawParam");
        return string.IsNullOrEmpty(value) ? null : StRefId.Parse(value);
    }

    /// <summary>
    /// 添加页面对象
    /// </summary>
    /// <param name="pageObject">页面对象</param>
    /// <returns>this</returns>
    public Layer AddPageObject(OfdElement pageObject)
    {
        Add(pageObject);
        return this;
    }

    /// <summary>
    /// 获取所有页面对象
    /// </summary>
    /// <returns>页面对象列表</returns>
    public List<XElement> GetPageObjects()
    {
        return Element.Elements().ToList();
    }

    /// <summary>
    /// 获取页面对象数量
    /// </summary>
    /// <returns>页面对象数量</returns>
    public int GetPageObjectCount()
    {
        return Element.Elements().Count();
    }

    /// <summary>
    /// 清除所有页面对象
    /// </summary>
    /// <returns>this</returns>
    public Layer ClearPageObjects()
    {
        Element.Elements().Remove();
        return this;
    }
}

/// <summary>
/// 模板
/// 
/// 定义页面模板，用于重复使用的页面布局和内容。
/// 模板可以被多个页面引用。
/// 
/// 对应OFD标准中的Template定义
/// 7.9 模板页 图 19 表 32
/// </summary>
public class Template : OfdElement
{
    /// <summary>
    /// 从现有元素构造模板
    /// </summary>
    /// <param name="element">XML元素</param>
    public Template(XElement element) : base(element)
    {
    }

    /// <summary>
    /// 构造新的模板元素
    /// </summary>
    public Template() : base("Template")
    {
    }

    /// <summary>
    /// 【必选 属性】
    /// 设置模板标识符
    /// </summary>
    /// <param name="id">模板标识符</param>
    /// <returns>this</returns>
    public Template SetID(StId id)
    {
        SetAttribute("ID", id.ToString());
        return this;
    }

    /// <summary>
    /// 【必选 属性】
    /// 获取模板标识符
    /// </summary>
    /// <returns>模板标识符</returns>
    /// <exception cref="InvalidOperationException">ID未设置时抛出</exception>
    public StId GetID()
    {
        var value = GetAttributeValue("ID");
        return string.IsNullOrEmpty(value) 
            ? throw new InvalidOperationException("Template ID is required") 
            : StId.Parse(value);
    }

    /// <summary>
    /// 【可选 属性】
    /// 设置模板名称
    /// </summary>
    /// <param name="name">模板名称</param>
    /// <returns>this</returns>
    public Template SetName(string name)
    {
        SetAttribute("Name", name);
        return this;
    }

    /// <summary>
    /// 【可选 属性】
    /// 获取模板名称
    /// </summary>
    /// <returns>模板名称</returns>
    public string? GetName()
    {
        return GetAttributeValue("Name");
    }

    /// <summary>
    /// 【可选 属性】
    /// 设置模板使用区域
    /// </summary>
    /// <param name="zOrder">使用区域（前景、背景等）</param>
    /// <returns>this</returns>
    public Template SetZOrder(TemplateZOrder zOrder)
    {
        SetAttribute("ZOrder", zOrder.ToString());
        return this;
    }

    /// <summary>
    /// 【可选 属性】
    /// 获取模板使用区域
    /// </summary>
    /// <returns>使用区域</returns>
    public TemplateZOrder GetZOrder()
    {
        var value = GetAttributeValue("ZOrder");
        return Enum.TryParse<TemplateZOrder>(value, out var zOrder) 
            ? zOrder 
            : TemplateZOrder.Background; // 默认值
    }

    /// <summary>
    /// 【可选 属性】
    /// 设置模板边界框
    /// </summary>
    /// <param name="boundary">边界框</param>
    /// <returns>this</returns>
    public Template SetBoundary(StBox boundary)
    {
        SetAttribute("Boundary", boundary.ToString());
        return this;
    }

    /// <summary>
    /// 【可选 属性】
    /// 获取模板边界框
    /// </summary>
    /// <returns>边界框</returns>
    public StBox? GetBoundary()
    {
        var value = GetAttributeValue("Boundary");
        return string.IsNullOrEmpty(value) ? null : StBox.Parse(value);
    }

    /// <summary>
    /// 添加页面对象到模板
    /// </summary>
    /// <param name="pageObject">页面对象</param>
    /// <returns>this</returns>
    public Template AddPageObject(OfdElement pageObject)
    {
        Add(pageObject);
        return this;
    }

    /// <summary>
    /// 获取模板中的所有页面对象
    /// </summary>
    /// <returns>页面对象列表</returns>
    public List<XElement> GetPageObjects()
    {
        return Element.Elements().ToList();
    }

    /// <summary>
    /// 获取模板页面对象数量
    /// </summary>
    /// <returns>页面对象数量</returns>
    public int GetPageObjectCount()
    {
        return Element.Elements().Count();
    }

    /// <summary>
    /// 是否为背景模板
    /// </summary>
    /// <returns>是否为背景模板</returns>
    public bool IsBackground()
    {
        return GetZOrder() == TemplateZOrder.Background;
    }

    /// <summary>
    /// 是否为前景模板
    /// </summary>
    /// <returns>是否为前景模板</returns>
    public bool IsForeground()
    {
        return GetZOrder() == TemplateZOrder.Foreground;
    }
}

/// <summary>
/// 页面块
/// 
/// 定义页面中的内容块，用于组织页面内容的逻辑结构。
/// 页面块可以包含多种页面对象。
/// 
/// 对应OFD标准中的PageBlock定义
/// </summary>
public class PageBlock : OfdElement
{
    /// <summary>
    /// 从现有元素构造页面块
    /// </summary>
    /// <param name="element">XML元素</param>
    public PageBlock(XElement element) : base(element)
    {
    }

    /// <summary>
    /// 构造新的页面块元素
    /// </summary>
    public PageBlock() : base("PageBlock")
    {
    }

    /// <summary>
    /// 【可选 属性】
    /// 设置页面块标识符
    /// </summary>
    /// <param name="id">页面块标识符</param>
    /// <returns>this</returns>
    public PageBlock SetID(StId id)
    {
        SetAttribute("ID", id.ToString());
        return this;
    }

    /// <summary>
    /// 【可选 属性】
    /// 获取页面块标识符
    /// </summary>
    /// <returns>页面块标识符</returns>
    public StId? GetID()
    {
        var value = GetAttributeValue("ID");
        return string.IsNullOrEmpty(value) ? null : StId.Parse(value);
    }

    /// <summary>
    /// 【可选 属性】
    /// 设置页面块名称
    /// </summary>
    /// <param name="name">页面块名称</param>
    /// <returns>this</returns>
    public PageBlock SetName(string name)
    {
        SetAttribute("Name", name);
        return this;
    }

    /// <summary>
    /// 【可选 属性】
    /// 获取页面块名称
    /// </summary>
    /// <returns>页面块名称</returns>
    public string? GetName()
    {
        return GetAttributeValue("Name");
    }

    /// <summary>
    /// 【可选 属性】
    /// 设置页面块类型
    /// </summary>
    /// <param name="type">页面块类型</param>
    /// <returns>this</returns>
    public PageBlock SetType(PageBlockType type)
    {
        SetAttribute("Type", type.ToString());
        return this;
    }

    /// <summary>
    /// 【可选 属性】
    /// 获取页面块类型
    /// </summary>
    /// <returns>页面块类型</returns>
    public new PageBlockType GetType()
    {
        var value = GetAttributeValue("Type");
        return Enum.TryParse<PageBlockType>(value, out var type) 
            ? type 
            : PageBlockType.Normal; // 默认值
    }

    /// <summary>
    /// 【可选 属性】
    /// 设置页面块边界框
    /// </summary>
    /// <param name="boundary">边界框</param>
    /// <returns>this</returns>
    public PageBlock SetBoundary(StBox boundary)
    {
        SetAttribute("Boundary", boundary.ToString());
        return this;
    }

    /// <summary>
    /// 【可选 属性】
    /// 获取页面块边界框
    /// </summary>
    /// <returns>边界框</returns>
    public StBox? GetBoundary()
    {
        var value = GetAttributeValue("Boundary");
        return string.IsNullOrEmpty(value) ? null : StBox.Parse(value);
    }

    /// <summary>
    /// 添加页面对象到页面块
    /// </summary>
    /// <param name="pageObject">页面对象</param>
    /// <returns>this</returns>
    public PageBlock AddPageObject(OfdElement pageObject)
    {
        Add(pageObject);
        return this;
    }

    /// <summary>
    /// 获取页面块中的所有页面对象
    /// </summary>
    /// <returns>页面对象列表</returns>
    public List<XElement> GetPageObjects()
    {
        return Element.Elements().ToList();
    }

    /// <summary>
    /// 获取页面块页面对象数量
    /// </summary>
    /// <returns>页面对象数量</returns>
    public int GetPageObjectCount()
    {
        return Element.Elements().Count();
    }

    /// <summary>
    /// 清除页面块中的所有页面对象
    /// </summary>
    /// <returns>this</returns>
    public PageBlock ClearPageObjects()
    {
        Element.Elements().Remove();
        return this;
    }
}

/// <summary>
/// 模板Z顺序枚举
/// </summary>
public enum TemplateZOrder
{
    /// <summary>
    /// 背景
    /// </summary>
    Background,

    /// <summary>
    /// 前景
    /// </summary>
    Foreground
}

/// <summary>
/// 页面块类型枚举
/// </summary>
public enum PageBlockType
{
    /// <summary>
    /// 普通块
    /// </summary>
    Normal,

    /// <summary>
    /// 标题块
    /// </summary>
    Header,

    /// <summary>
    /// 页脚块
    /// </summary>
    Footer,

    /// <summary>
    /// 边栏块
    /// </summary>
    Sidebar,

    /// <summary>
    /// 内容块
    /// </summary>
    Content
}
