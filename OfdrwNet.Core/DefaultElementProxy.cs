using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Xml;
using System.Xml.Linq;

namespace OfdrwNet.Core
{
    /// <summary>
    /// 元素代理对象
    /// 
    /// 对应 Java 的 org.ofdrw.core.DefaultElementProxy
    /// 用于为失去类型信息的 XML 元素提供代理访问
    /// </summary>
    /// <author>翻译自 权观宇 的 Java 实现</author>
    /// <since>2019-10-01 01:48:13</since>
    public abstract class DefaultElementProxy
    {
        /// <summary>
        /// 代理对象
        /// 当从容器中获取到 XElement 会失去类型，对于失去类型的对象统一采用代理的方式获取属性或者对象内容
        /// </summary>
        protected XElement proxy;

        /// <summary>
        /// 私有构造函数，防止无参数实例化
        /// </summary>
        private DefaultElementProxy()
        {
        }

        /// <summary>
        /// 使用元素名称创建代理
        /// </summary>
        /// <param name="name">元素名称</param>
        public DefaultElementProxy(string name)
        {
            if (string.IsNullOrEmpty(name))
                throw new ArgumentException("元素名称不能为空", nameof(name));
            
            this.proxy = new XElement(name);
        }

        /// <summary>
        /// 使用 XName 创建代理
        /// </summary>
        /// <param name="name">XName 对象</param>
        public DefaultElementProxy(XName name)
        {
            if (name == null)
                throw new ArgumentNullException(nameof(name));
            
            this.proxy = new XElement(name);
        }

        /// <summary>
        /// 使用命名空间创建代理
        /// </summary>
        /// <param name="namespaceName">命名空间名称</param>
        /// <param name="localName">本地名称</param>
        public DefaultElementProxy(string namespaceName, string localName)
        {
            if (string.IsNullOrEmpty(localName))
                throw new ArgumentException("本地名称不能为空", nameof(localName));
            
            XNamespace ns = namespaceName ?? string.Empty;
            this.proxy = new XElement(ns + localName);
        }

        /// <summary>
        /// 使用现有元素创建代理
        /// </summary>
        /// <param name="proxy">被代理的元素</param>
        public DefaultElementProxy(XElement proxy)
        {
            this.proxy = proxy ?? throw new ArgumentNullException(nameof(proxy), "被代理对象(proxy)不能为空");
        }

        /// <summary>
        /// 获取被代理对象本身
        /// </summary>
        /// <returns>被代理的 XElement 对象</returns>
        public XElement GetProxy()
        {
            return proxy;
        }

        /// <summary>
        /// 设置代理对象
        /// </summary>
        /// <param name="proxy">代理对象</param>
        public void SetProxy(XElement proxy)
        {
            this.proxy = proxy ?? throw new ArgumentNullException(nameof(proxy));
        }

        /// <summary>
        /// 获取元素的完全限定名称
        /// 需要继承的子类实现该方法，用于在代理对象中做类型检查
        /// </summary>
        /// <returns>元素全名（含有前缀）</returns>
        public abstract string GetQualifiedName();

        #region 基本属性

        /// <summary>
        /// 获取或设置元素名称
        /// </summary>
        public XName Name
        {
            get => proxy.Name;
            set => proxy.Name = value;
        }

        /// <summary>
        /// 获取或设置元素的本地名称
        /// </summary>
        public string LocalName
        {
            get => proxy.Name.LocalName;
        }

        /// <summary>
        /// 获取元素的命名空间
        /// </summary>
        public XNamespace Namespace => proxy.Name.Namespace;

        /// <summary>
        /// 获取命名空间前缀
        /// </summary>
        public string NamespacePrefix => proxy.GetPrefixOfNamespace(proxy.Name.Namespace) ?? string.Empty;

        /// <summary>
        /// 获取命名空间 URI
        /// </summary>
        public string NamespaceUri => proxy.Name.NamespaceName;

        /// <summary>
        /// 获取或设置元素的文本内容
        /// </summary>
        public string Text
        {
            get => proxy.Value;
            set => proxy.Value = value ?? string.Empty;
        }

        /// <summary>
        /// 获取修剪空格后的文本内容
        /// </summary>
        public string TextTrim => proxy.Value.Trim();

        /// <summary>
        /// 检查元素是否有内容
        /// </summary>
        public bool HasContent => proxy.HasElements || !string.IsNullOrEmpty(proxy.Value);

        /// <summary>
        /// 获取父元素
        /// </summary>
        public XElement? Parent => proxy.Parent;

        /// <summary>
        /// 获取所属文档
        /// </summary>
        public XDocument? Document => proxy.Document;

        /// <summary>
        /// 检查是否为根元素
        /// </summary>
        public bool IsRootElement => proxy.Parent == null || proxy.Document?.Root == proxy;

        #endregion

        #region 属性操作

        /// <summary>
        /// 添加属性
        /// </summary>
        /// <param name="name">属性名</param>
        /// <param name="value">属性值</param>
        /// <returns>当前元素</returns>
        public DefaultElementProxy AddAttribute(string name, string value)
        {
            proxy.SetAttributeValue(name, value);
            return this;
        }

        /// <summary>
        /// 添加属性
        /// </summary>
        /// <param name="name">属性名</param>
        /// <param name="value">属性值</param>
        /// <returns>当前元素</returns>
        public DefaultElementProxy AddAttribute(XName name, string value)
        {
            proxy.SetAttributeValue(name, value);
            return this;
        }

        /// <summary>
        /// 获取属性值
        /// </summary>
        /// <param name="name">属性名</param>
        /// <returns>属性值，如果不存在返回 null</returns>
        public string? GetAttributeValue(string name)
        {
            return proxy.Attribute(name)?.Value;
        }

        /// <summary>
        /// 获取属性值
        /// </summary>
        /// <param name="name">属性名</param>
        /// <param name="defaultValue">默认值</param>
        /// <returns>属性值，如果不存在返回默认值</returns>
        public string GetAttributeValue(string name, string defaultValue)
        {
            return proxy.Attribute(name)?.Value ?? defaultValue;
        }

        /// <summary>
        /// 获取属性值
        /// </summary>
        /// <param name="name">属性名</param>
        /// <returns>属性值，如果不存在返回 null</returns>
        public string? GetAttributeValue(XName name)
        {
            return proxy.Attribute(name)?.Value;
        }

        /// <summary>
        /// 获取属性值
        /// </summary>
        /// <param name="name">属性名</param>
        /// <param name="defaultValue">默认值</param>
        /// <returns>属性值，如果不存在返回默认值</returns>
        public string GetAttributeValue(XName name, string defaultValue)
        {
            return proxy.Attribute(name)?.Value ?? defaultValue;
        }

        /// <summary>
        /// 设置属性值
        /// </summary>
        /// <param name="name">属性名</param>
        /// <param name="value">属性值</param>
        [Obsolete("请使用 AddAttribute 方法")]
        public void SetAttributeValue(string name, string value)
        {
            proxy.SetAttributeValue(name, value);
        }

        /// <summary>
        /// 设置属性值
        /// </summary>
        /// <param name="name">属性名</param>
        /// <param name="value">属性值</param>
        [Obsolete("请使用 AddAttribute 方法")]
        public void SetAttributeValue(XName name, string value)
        {
            proxy.SetAttributeValue(name, value);
        }

        /// <summary>
        /// 获取属性
        /// </summary>
        /// <param name="name">属性名</param>
        /// <returns>属性对象</returns>
        public XAttribute? GetAttribute(string name)
        {
            return proxy.Attribute(name);
        }

        /// <summary>
        /// 获取属性
        /// </summary>
        /// <param name="name">属性名</param>
        /// <returns>属性对象</returns>
        public XAttribute? GetAttribute(XName name)
        {
            return proxy.Attribute(name);
        }

        /// <summary>
        /// 获取所有属性
        /// </summary>
        /// <returns>属性集合</returns>
        public IEnumerable<XAttribute> GetAttributes()
        {
            return proxy.Attributes();
        }

        /// <summary>
        /// 获取属性数量
        /// </summary>
        public int AttributeCount => proxy.Attributes().Count();

        /// <summary>
        /// 移除属性
        /// </summary>
        /// <param name="name">属性名</param>
        /// <returns>是否成功移除</returns>
        public bool RemoveAttribute(string name)
        {
            var attr = proxy.Attribute(name);
            if (attr != null)
            {
                attr.Remove();
                return true;
            }
            return false;
        }

        /// <summary>
        /// 移除属性
        /// </summary>
        /// <param name="name">属性名</param>
        /// <returns>是否成功移除</returns>
        public bool RemoveAttribute(XName name)
        {
            var attr = proxy.Attribute(name);
            if (attr != null)
            {
                attr.Remove();
                return true;
            }
            return false;
        }

        #endregion

        #region 子元素操作

        /// <summary>
        /// 获取第一个指定名称的子元素
        /// </summary>
        /// <param name="name">元素名</param>
        /// <returns>子元素</returns>
        public XElement? GetElement(string name)
        {
            return proxy.Element(name);
        }

        /// <summary>
        /// 获取第一个指定名称的子元素
        /// </summary>
        /// <param name="name">元素名</param>
        /// <returns>子元素</returns>
        public XElement? GetElement(XName name)
        {
            return proxy.Element(name);
        }

        /// <summary>
        /// 获取所有子元素
        /// </summary>
        /// <returns>子元素集合</returns>
        public IEnumerable<XElement> GetElements()
        {
            return proxy.Elements();
        }

        /// <summary>
        /// 获取指定名称的所有子元素
        /// </summary>
        /// <param name="name">元素名</param>
        /// <returns>子元素集合</returns>
        public IEnumerable<XElement> GetElements(string name)
        {
            return proxy.Elements(name);
        }

        /// <summary>
        /// 获取指定名称的所有子元素
        /// </summary>
        /// <param name="name">元素名</param>
        /// <returns>子元素集合</returns>
        public IEnumerable<XElement> GetElements(XName name)
        {
            return proxy.Elements(name);
        }

        /// <summary>
        /// 添加子元素
        /// </summary>
        /// <param name="name">元素名</param>
        /// <returns>添加的子元素</returns>
        public XElement AddElement(string name)
        {
            var element = new XElement(name);
            proxy.Add(element);
            return element;
        }

        /// <summary>
        /// 添加子元素
        /// </summary>
        /// <param name="name">元素名</param>
        /// <returns>添加的子元素</returns>
        public XElement AddElement(XName name)
        {
            var element = new XElement(name);
            proxy.Add(element);
            return element;
        }

        /// <summary>
        /// 添加子元素
        /// </summary>
        /// <param name="namespaceName">命名空间名称</param>
        /// <param name="localName">本地名称</param>
        /// <returns>添加的子元素</returns>
        public XElement AddElement(string namespaceName, string localName)
        {
            XNamespace ns = namespaceName ?? string.Empty;
            var element = new XElement(ns + localName);
            proxy.Add(element);
            return element;
        }

        /// <summary>
        /// 添加子元素
        /// </summary>
        /// <param name="element">要添加的元素</param>
        public void AddElement(XElement element)
        {
            if (element == null)
                throw new ArgumentNullException(nameof(element));
            proxy.Add(element);
        }

        /// <summary>
        /// 移除子元素
        /// </summary>
        /// <param name="element">要移除的元素</param>
        /// <returns>是否成功移除</returns>
        public bool RemoveElement(XElement element)
        {
            if (element?.Parent == proxy)
            {
                element.Remove();
                return true;
            }
            return false;
        }

        /// <summary>
        /// 获取指定名称的子元素文本
        /// </summary>
        /// <param name="name">元素名</param>
        /// <returns>元素文本内容</returns>
        public string? GetElementText(string name)
        {
            return proxy.Element(name)?.Value;
        }

        /// <summary>
        /// 获取指定名称的子元素文本
        /// </summary>
        /// <param name="name">元素名</param>
        /// <returns>元素文本内容</returns>
        public string? GetElementText(XName name)
        {
            return proxy.Element(name)?.Value;
        }

        /// <summary>
        /// 获取指定名称的子元素文本（修剪空格）
        /// </summary>
        /// <param name="name">元素名</param>
        /// <returns>修剪后的元素文本内容</returns>
        public string? GetElementTextTrim(string name)
        {
            return proxy.Element(name)?.Value?.Trim();
        }

        /// <summary>
        /// 获取指定名称的子元素文本（修剪空格）
        /// </summary>
        /// <param name="name">元素名</param>
        /// <returns>修剪后的元素文本内容</returns>
        public string? GetElementTextTrim(XName name)
        {
            return proxy.Element(name)?.Value?.Trim();
        }

        #endregion

        #region 内容操作

        /// <summary>
        /// 添加文本内容
        /// </summary>
        /// <param name="text">文本内容</param>
        /// <returns>当前元素</returns>
        public DefaultElementProxy AddText(string text)
        {
            if (text != null)
            {
                proxy.Add(new XText(text));
            }
            return this;
        }

        /// <summary>
        /// 添加 CDATA 内容
        /// </summary>
        /// <param name="cdata">CDATA 内容</param>
        /// <returns>当前元素</returns>
        public DefaultElementProxy AddCData(string cdata)
        {
            if (cdata != null)
            {
                proxy.Add(new XCData(cdata));
            }
            return this;
        }

        /// <summary>
        /// 添加注释
        /// </summary>
        /// <param name="comment">注释内容</param>
        /// <returns>当前元素</returns>
        public DefaultElementProxy AddComment(string comment)
        {
            if (comment != null)
            {
                proxy.Add(new XComment(comment));
            }
            return this;
        }

        /// <summary>
        /// 清除所有内容
        /// </summary>
        public void ClearContent()
        {
            proxy.RemoveNodes();
        }

        /// <summary>
        /// 检查是否包含混合内容
        /// </summary>
        public bool HasMixedContent => proxy.HasElements && proxy.Nodes().OfType<XText>().Any(t => !string.IsNullOrWhiteSpace(t.Value));

        /// <summary>
        /// 检查是否仅包含文本
        /// </summary>
        public bool IsTextOnly => !proxy.HasElements && proxy.Nodes().All(n => n is XText);

        #endregion

        #region XML 操作

        /// <summary>
        /// 将元素输出为 XML 字符串
        /// </summary>
        /// <returns>XML 字符串</returns>
        public string AsXml()
        {
            return proxy.ToString();
        }

        /// <summary>
        /// 将元素写入到指定的写入器
        /// </summary>
        /// <param name="writer">文本写入器</param>
        public void Write(TextWriter writer)
        {
            if (writer == null)
                throw new ArgumentNullException(nameof(writer));
            proxy.Save(writer);
        }

        /// <summary>
        /// 创建元素的副本
        /// </summary>
        /// <returns>元素副本</returns>
        public XElement CreateCopy()
        {
            return new XElement(proxy);
        }

        /// <summary>
        /// 创建指定名称的元素副本
        /// </summary>
        /// <param name="name">新元素名称</param>
        /// <returns>元素副本</returns>
        public XElement CreateCopy(string name)
        {
            var copy = new XElement(proxy);
            copy.Name = name;
            return copy;
        }

        /// <summary>
        /// 创建指定名称的元素副本
        /// </summary>
        /// <param name="name">新元素名称</param>
        /// <returns>元素副本</returns>
        public XElement CreateCopy(XName name)
        {
            var copy = new XElement(proxy);
            copy.Name = name;
            return copy;
        }

        /// <summary>
        /// 分离元素
        /// </summary>
        /// <returns>分离后的元素</returns>
        public XElement Detach()
        {
            var detached = new XElement(proxy);
            proxy.Remove();
            return detached;
        }

        #endregion

        #region XPath 支持

        /// <summary>
        /// 根据 XPath 表达式选择节点
        /// </summary>
        /// <param name="xpath">XPath 表达式</param>
        /// <returns>匹配的节点集合</returns>
        public IEnumerable<XObject> SelectNodes(string xpath)
        {
            if (string.IsNullOrEmpty(xpath))
                throw new ArgumentException("XPath 表达式不能为空", nameof(xpath));
            
            // 简化的 XPath 支持，实际项目中可能需要使用第三方库
            // 这里只实现基本的元素选择
            if (xpath.StartsWith("//"))
            {
                var elementName = xpath.Substring(2);
                return proxy.Descendants(elementName).Cast<XObject>();
            }
            else if (xpath.Contains("/"))
            {
                var parts = xpath.Split('/');
                IEnumerable<XElement> current = new[] { proxy };
                
                foreach (var part in parts.Where(p => !string.IsNullOrEmpty(p)))
                {
                    current = current.SelectMany(e => e.Elements(part));
                }
                
                return current.Cast<XObject>();
            }
            else
            {
                return proxy.Elements(xpath).Cast<XObject>();
            }
        }

        /// <summary>
        /// 根据 XPath 表达式选择单个节点
        /// </summary>
        /// <param name="xpath">XPath 表达式</param>
        /// <returns>匹配的第一个节点</returns>
        public XObject? SelectSingleNode(string xpath)
        {
            return SelectNodes(xpath).FirstOrDefault();
        }

        /// <summary>
        /// 根据 XPath 表达式获取值
        /// </summary>
        /// <param name="xpath">XPath 表达式</param>
        /// <returns>值</returns>
        public string? ValueOf(string xpath)
        {
            var node = SelectSingleNode(xpath);
            if (node is XElement element)
                return element.Value;
            if (node is XAttribute attribute)
                return attribute.Value;
            return node?.ToString();
        }

        /// <summary>
        /// 根据 XPath 表达式获取数值
        /// </summary>
        /// <param name="xpath">XPath 表达式</param>
        /// <returns>数值</returns>
        public double? NumberValueOf(string xpath)
        {
            var value = ValueOf(xpath);
            if (double.TryParse(value, NumberStyles.Float, CultureInfo.InvariantCulture, out var result))
                return result;
            return null;
        }

        /// <summary>
        /// 检查是否匹配 XPath 表达式
        /// </summary>
        /// <param name="xpath">XPath 表达式</param>
        /// <returns>是否匹配</returns>
        public bool Matches(string xpath)
        {
            return SelectNodes(xpath).Any();
        }

        #endregion

        #region 重写方法

        /// <summary>
        /// 判断两个对象是否相等
        /// </summary>
        /// <param name="obj">比较对象</param>
        /// <returns>是否相等</returns>
        public override bool Equals(object? obj)
        {
            if (this == obj) return true;
            
            if (obj is DefaultElementProxy other)
            {
                return XNode.DeepEquals(proxy, other.proxy);
            }
            
            if (obj is XElement element)
            {
                return XNode.DeepEquals(proxy, element);
            }
            
            return false;
        }

        /// <summary>
        /// 获取哈希码
        /// </summary>
        /// <returns>哈希码</returns>
        public override int GetHashCode()
        {
            return proxy?.GetHashCode() ?? 0;
        }

        /// <summary>
        /// 转换为字符串
        /// </summary>
        /// <returns>XML 字符串表示</returns>
        public override string ToString()
        {
            return proxy?.ToString() ?? string.Empty;
        }

        /// <summary>
        /// 克隆对象
        /// </summary>
        /// <returns>克隆的对象</returns>
        public virtual object Clone()
        {
            // 由于这是抽象类，实际的克隆需要由子类实现
            throw new NotSupportedException("克隆操作需要由具体的子类实现");
        }

        #endregion

        #region 数据存储

        private object? _data;

        /// <summary>
        /// 获取或设置附加数据
        /// </summary>
        public object? Data
        {
            get => _data;
            set => _data = value;
        }

        #endregion
    }
}
