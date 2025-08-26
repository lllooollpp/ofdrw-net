using System.Xml.Linq;

namespace OfdrwNet.Core;

/// <summary>
/// OFD 简单类型元素
/// 对应 Java 版本的 org.ofdrw.core.OFDSimpleTypeElement
/// 用于包装具有简单文本内容的OFD元素
/// </summary>
public class OfdSimpleTypeElement : OfdElement
{
    /// <summary>
    /// 构造简单类型元素
    /// </summary>
    /// <param name="name">元素名称</param>
    /// <param name="value">元素值</param>
    public OfdSimpleTypeElement(string name, object? value) : base(name)
    {
        if (value != null)
        {
            Element.Value = value.ToString() ?? "";
        }
    }
    
    /// <summary>
    /// 从现有元素构造
    /// </summary>
    /// <param name="element">XML 元素</param>
    public OfdSimpleTypeElement(XElement element) : base(element)
    {
    }
    
    /// <summary>
    /// 获取元素值
    /// </summary>
    /// <returns>元素的文本值</returns>
    public string GetValue()
    {
        return Element.Value;
    }
    
    /// <summary>
    /// 设置元素值
    /// </summary>
    /// <param name="value">新的元素值</param>
    /// <returns>this</returns>
    public OfdSimpleTypeElement SetValue(object? value)
    {
        Element.Value = value?.ToString() ?? "";
        return this;
    }
    
    /// <summary>
    /// 隐式转换为字符串
    /// </summary>
    /// <param name="element">简单类型元素</param>
    /// <returns>元素的文本值</returns>
    public static implicit operator string(OfdSimpleTypeElement element)
    {
        return element.GetValue();
    }
    
    /// <summary>
    /// 克隆元素
    /// </summary>
    /// <returns>克隆的简单类型元素</returns>
    public override OfdElement Clone()
    {
        return new OfdSimpleTypeElement(new XElement(Element));
    }
}