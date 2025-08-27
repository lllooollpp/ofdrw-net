using System.Xml.Linq;
using OfdrwNet.Core.BasicType;

namespace OfdrwNet.Core.BasicStructure.PageObj.Layer;

/// <summary>
/// 图层类型枚举
/// 对应Java版本的LayerType
/// </summary>
public enum LayerType
{
    /// <summary>
    /// 正文图层
    /// </summary>
    Body,
    /// <summary>
    /// 前景图层
    /// </summary>
    Foreground,
    /// <summary>
    /// 背景图层
    /// </summary>
    Background
}

/// <summary>
/// 图层
/// 
/// 图层是页面内容的逻辑分组，用于组织页面对象的显示顺序。
/// 页面对象按照从后往前的顺序依次放在不同图层中。
/// 
/// 对应Java版本：org.ofdrw.core.basicStructure.pageObj.layer.CT_Layer
/// 7.9 图层 图 46
/// </summary>
public class CtLayer : OfdElement, IPageBlockType
{
    /// <summary>
    /// 从现有元素构造图层
    /// </summary>
    /// <param name="element">XML元素</param>
    public CtLayer(XElement element) : base(element)
    {
    }

    /// <summary>
    /// 构造新的图层元素
    /// </summary>
    public CtLayer() : base("Layer")
    {
    }

    /// <summary>
    /// 【可选 属性】
    /// 设置图层类型描述
    /// 默认值为Body
    /// </summary>
    /// <param name="type">图层类型</param>
    /// <returns>this</returns>
    public CtLayer SetType(LayerType type)
    {
        SetAttribute("Type", type.ToString());
        return this;
    }

    /// <summary>
    /// 【可选 属性】
    /// 获取图层类型描述
    /// 默认值为Body
    /// </summary>
    /// <returns>图层类型</returns>
    public LayerType GetLayerType()
    {
        var value = GetAttributeValue("Type");
        if (string.IsNullOrEmpty(value))
            return LayerType.Body;
            
        if (Enum.TryParse<LayerType>(value, true, out var type))
            return type;
            
        return LayerType.Body;
    }

    /// <summary>
    /// 【可选 属性】
    /// 设置图层的绘制参数
    /// 引用资源文件中定义的绘制参数标识
    /// </summary>
    /// <param name="drawParam">绘制参数标识</param>
    /// <returns>this</returns>
    public CtLayer SetDrawParam(StRefId drawParam)
    {
        SetAttribute("DrawParam", drawParam.ToString());
        return this;
    }

    /// <summary>
    /// 【可选 属性】
    /// 获取图层的绘制参数
    /// </summary>
    /// <returns>绘制参数标识，可能为null</returns>
    public StRefId? GetDrawParam()
    {
        var value = GetAttributeValue("DrawParam");
        return string.IsNullOrEmpty(value) ? null : StRefId.Parse(value);
    }

    /// <summary>
    /// 添加页面对象到图层
    /// </summary>
    /// <param name="pageObject">页面对象</param>
    /// <returns>this</returns>
    public CtLayer AddPageObject(OfdElement pageObject)
    {
        Add(pageObject);
        return this;
    }

    /// <summary>
    /// 获取图层中的所有页面对象数量
    /// </summary>
    /// <returns>页面对象数量</returns>
    public int GetPageObjectCount()
    {
        return Element.Elements().Count();
    }

    /// <summary>
    /// 清空图层中的所有页面对象
    /// </summary>
    /// <returns>this</returns>
    public CtLayer ClearPageObjects()
    {
        Element.RemoveAll();
        return this;
    }
}