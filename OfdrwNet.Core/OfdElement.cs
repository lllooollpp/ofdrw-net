using System.Xml.Linq;
using OfdrwNet.Core.BasicType;

namespace OfdrwNet.Core;

/// <summary>
/// OFD 元素基类
/// 对应 Java 版本的 org.ofdrw.core.OFDElement
/// 使用 System.Xml.Linq.XElement 替代 Java 的 DOM4J
/// </summary>
public class OfdElement
{
    /// <summary>
    /// 命名空间严格模式
    /// true - 严格使用OFD空间获取OFD元素
    /// false - 只要元素名称相同则认为是OFD元素（默认值）
    /// </summary>
    public static bool NSStrictMode = false;
    
    /// <summary>
    /// 底层 XML 元素
    /// </summary>
    public XElement Element { get; set; }
    
    /// <summary>
    /// 从现有元素构造
    /// </summary>
    /// <param name="element">XML 元素</param>
    public OfdElement(XElement element)
    {
        Element = element ?? throw new ArgumentNullException(nameof(element));
    }
    
    /// <summary>
    /// 构造新元素
    /// </summary>
    /// <param name="name">元素名称</param>
    protected OfdElement(string name)
    {
        // 使用带前缀的元素名
        Element = new XElement(Const.OfdNamespace + name);
    }
    
    /// <summary>
    /// 创建OFD类型元素实例
    /// </summary>
    /// <param name="name">元素名称</param>
    /// <returns>OFD元素实例</returns>
    public static OfdElement GetInstance(string name)
    {
        return new OfdElement(name);
    }
    
    /// <summary>
    /// 从XML字符串创建OFD元素实例
    /// </summary>
    /// <param name="xml">XML字符串</param>
    /// <returns>OFD元素实例</returns>
    public static OfdElement FromXml(string xml)
    {
        var xElement = XElement.Parse(xml);
        return new OfdElement(xElement);
    }
    
    /// <summary>
    /// 向元素中增加OFD子元素
    /// </summary>
    /// <param name="name">元素名称</param>
    /// <param name="value">元素文本</param>
    /// <returns>this</returns>
    public OfdElement AddOfdEntity(string name, object value)
    {
        var childElement = new XElement(Const.OfdNamespace + name, value?.ToString());
        Element.Add(childElement);
        return this;
    }
    
    /// <summary>
    /// 设置OFD参数
    /// 如果参数已经存在则修改参数
    /// </summary>
    /// <param name="name">元素名称</param>
    /// <param name="value">元素文本</param>
    /// <returns>this</returns>
    public OfdElement SetOfdEntity(string name, object value)
    {
        var existingElement = GetOfdElement(name);
        if (existingElement == null)
        {
            return AddOfdEntity(name, value);
        }
        else
        {
            existingElement.Value = value?.ToString() ?? "";
            return this;
        }
    }
    
    /// <summary>
    /// 设置元素名称
    /// </summary>
    /// <param name="name">元素名称</param>
    /// <returns>this</returns>
    public OfdElement SetOfdName(string name)
    {
        Element.Name = Const.OfdNamespace + name;
        return this;
    }
    
    /// <summary>
    /// 获取OFD的子元素
    /// </summary>
    /// <param name="name">OFD元素名称</param>
    /// <returns>OFD元素或null</returns>
    public XElement? GetOfdElement(string name)
    {
        if (NSStrictMode)
        {
            return Element.Element(Const.OfdNamespace + name);
        }
        else
        {
            // 兼容模式：先尝试带命名空间的，再尝试不带命名空间的
            return Element.Element(Const.OfdNamespace + name) ?? 
                   Element.Element(name);
        }
    }
    
    /// <summary>
    /// 获取OFD元素中的文本
    /// </summary>
    /// <param name="name">元素名称</param>
    /// <returns>文本内容</returns>
    public string? GetOfdElementText(string name)
    {
        var element = GetOfdElement(name);
        return element?.Value;
    }
    
    /// <summary>
    /// 获取指定名称OFD元素集合
    /// </summary>
    /// <param name="name">OFD元素名称</param>
    /// <returns>指定名称OFD元素集合</returns>
    public IEnumerable<XElement> GetOfdElements(string name)
    {
        if (NSStrictMode)
        {
            return Element.Elements(Const.OfdNamespace + name);
        }
        else
        {
            // 兼容模式：合并带命名空间和不带命名空间的元素
            var withNamespace = Element.Elements(Const.OfdNamespace + name);
            var withoutNamespace = Element.Elements(name).Where(e => e.Name.Namespace == XNamespace.None);
            return withNamespace.Concat(withoutNamespace);
        }
    }
    
    /// <summary>
    /// 添加属性
    /// </summary>
    /// <param name="name">属性名称</param>
    /// <param name="value">属性值</param>
    /// <returns>this</returns>
    public OfdElement AddAttribute(string name, object value)
    {
        Element.SetAttributeValue(name, value?.ToString());
        return this;
    }
    
    /// <summary>
    /// 设置属性值（如果存在则替换）
    /// </summary>
    /// <param name="name">属性名称</param>
    /// <param name="value">属性值</param>
    /// <returns>this</returns>
    public OfdElement SetAttribute(string name, object value)
    {
        Element.SetAttributeValue(name, value?.ToString());
        return this;
    }
    
    /// <summary>
    /// 获取属性值
    /// </summary>
    /// <param name="name">属性名称</param>
    /// <returns>属性值</returns>
    public string? GetAttributeValue(string name)
    {
        return Element.Attribute(name)?.Value;
    }
    
    /// <summary>
    /// 删除属性
    /// </summary>
    /// <param name="name">属性名称</param>
    /// <returns>是否删除成功</returns>
    public bool RemoveAttribute(string name)
    {
        var attr = Element.Attribute(name);
        if (attr != null)
        {
            attr.Remove();
            return true;
        }
        return false;
    }
    
    /// <summary>
    /// 删除指定名称的OFD元素
    /// </summary>
    /// <param name="names">元素名称列表</param>
    /// <returns>this</returns>
    public OfdElement RemoveOfdElementsByNames(params string[] names)
    {
        foreach (var name in names)
        {
            var elements = GetOfdElements(name).ToList();
            foreach (var element in elements)
            {
                element.Remove();
            }
        }
        return this;
    }
    
    /// <summary>
    /// 添加子元素
    /// </summary>
    /// <param name="child">子元素</param>
    /// <returns>this</returns>
    public OfdElement Add(XElement child)
    {
        Element.Add(child);
        return this;
    }
    
    /// <summary>
    /// 添加子元素
    /// </summary>
    /// <param name="child">子OFD元素</param>
    /// <returns>this</returns>
    public OfdElement Add(OfdElement child)
    {
        Element.Add(child.Element);
        return this;
    }
    
    /// <summary>
    /// 设置子元素（如果存在则替换）
    /// </summary>
    /// <param name="child">子OFD元素</param>
    /// <returns>this</returns>
    public OfdElement Set(OfdElement child)
    {
        var elementName = child.Element.Name.LocalName;
        RemoveOfdElementsByNames(elementName);
        return Add(child);
    }
    
    /// <summary>
    /// 克隆元素
    /// </summary>
    /// <returns>克隆的元素</returns>
    public virtual OfdElement Clone()
    {
        return new OfdElement(new XElement(Element));
    }
    
    /// <summary>
    /// 转换为XElement
    /// </summary>
    /// <returns>XElement实例</returns>
    public XElement ToXElement()
    {
        return new XElement(Element);
    }
    
    /// <summary>
    /// 转换为XML字符串
    /// </summary>
    /// <returns>XML字符串</returns>
    public string ToXml()
    {
        // 确保根元素有正确的命名空间声明
        var elementCopy = new XElement(Element);
        if (elementCopy.Name.Namespace == Const.OfdNamespace)
        {
            // 添加命名空间声明到根元素
            elementCopy.SetAttributeValue(XNamespace.Xmlns + Const.OfdValue, Const.OfdNamespaceUri);
        }
        return elementCopy.ToString();
    }
    
    /// <summary>
    /// 获取限定名称
    /// </summary>
    public virtual string QualifiedName => $"{Const.OfdPrefix}{Element.Name.LocalName}";

    /// <summary>
    /// 设置对象标识
    /// </summary>
    /// <param name="objId">对象标识</param>
    /// <returns>this</returns>
    public OfdElement SetObjId(StId objId)
    {
        if (objId is null)
        {
            RemoveAttribute("ID");
        }
        else
        {
            SetAttribute("ID", objId.ToString());
        }
        return this;
    }

    /// <summary>
    /// 设置对象标识
    /// </summary>
    /// <param name="objId">对象标识字符串</param>
    /// <returns>this</returns>
    public OfdElement SetObjId(string objId)
    {
        if (string.IsNullOrEmpty(objId))
        {
            RemoveAttribute("ID");
        }
        else
        {
            SetAttribute("ID", objId);
        }
        return this;
    }

    /// <summary>
    /// 获取对象标识
    /// </summary>
    /// <returns>对象标识</returns>
    public StId? GetObjId()
    {
        var idValue = GetAttributeValue("ID");
        return string.IsNullOrEmpty(idValue) ? null : StId.Parse(idValue);
    }

    /// <summary>
    /// 设置元素的文本内容
    /// </summary>
    /// <param name="text">文本内容</param>
    /// <returns>this</returns>
    public OfdElement SetText(string? text)
    {
        Element.Value = text ?? "";
        return this;
    }

    /// <summary>
    /// 获取元素的文本内容
    /// </summary>
    /// <returns>文本内容</returns>
    public string? GetText()
    {
        return Element.Value;
    }
}