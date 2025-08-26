using OfdrwNet.Core.BasicType;
using OfdrwNet.Core;
using System.Xml.Linq;

namespace OfdrwNet.Reader.Model;

/// <summary>
/// 页面模板对象实体
/// 对应 Java 版本的 org.ofdrw.reader.model.TemplatePageEntity
/// 用于表示页面中使用的模板页面信息
/// </summary>
public class TemplatePageEntity
{
    /// <summary>
    /// 模板内容页面对象
    /// </summary>
    public XElement Page { get; set; }

    /// <summary>
    /// 模板信息对象
    /// </summary>
    public XElement? TemplateInfo { get; set; }

    /// <summary>
    /// 页面Z顺序类型
    /// </summary>
    public LayerType Order { get; set; }

    /// <summary>
    /// 模板ID
    /// </summary>
    public string? Id { get; set; }

    /// <summary>
    /// 模板页名称
    /// </summary>
    public string? TemplatePageName { get; set; }

    /// <summary>
    /// 构造函数
    /// </summary>
    /// <param name="templateInfo">模板信息</param>
    /// <param name="page">模板页面内容</param>
    public TemplatePageEntity(XElement templateInfo, XElement page)
    {
        TemplateInfo = templateInfo ?? throw new ArgumentNullException(nameof(templateInfo));
        Page = page ?? throw new ArgumentNullException(nameof(page));
        
        // 解析模板ID
        Id = templateInfo.Attribute("ID")?.Value;
        
        // 解析模板页名称
        TemplatePageName = templateInfo.Attribute("Name")?.Value;
        
        // 解析Z顺序
        var zOrderAttr = templateInfo.Attribute("ZOrder")?.Value;
        Order = ParseLayerType(zOrderAttr);
    }

    /// <summary>
    /// 用于构造排序的构造函数
    /// </summary>
    /// <param name="order">Z顺序类型</param>
    /// <param name="page">页面内容</param>
    public TemplatePageEntity(LayerType order, XElement page)
    {
        Order = order;
        Page = page ?? throw new ArgumentNullException(nameof(page));
    }

    /// <summary>
    /// 设置页面顺序
    /// </summary>
    /// <param name="order">新的顺序</param>
    public void SetOrder(LayerType order)
    {
        Order = order;
    }

    /// <summary>
    /// 获取Z顺序值
    /// </summary>
    /// <returns>Z顺序的数值</returns>
    public int GetZOrder()
    {
        return (int)Order;
    }

    /// <summary>
    /// 解析图层类型
    /// </summary>
    /// <param name="value">字符串值</param>
    /// <returns>图层类型</returns>
    private static LayerType ParseLayerType(string? value)
    {
        if (string.IsNullOrEmpty(value))
            return LayerType.Body;

        return value.ToLowerInvariant() switch
        {
            "background" => LayerType.Background,
            "body" => LayerType.Body,
            "foreground" => LayerType.Foreground,
            _ => LayerType.Body
        };
    }
}