using System;
using System.Xml.Linq;

namespace OfdrwNet.Core
{
    /// <summary>
    /// OFD 通用限定名称
    /// 
    /// 对应 Java 的 org.ofdrw.core.OFDCommonQName
    /// 只要名称相同并且命名空间前缀保持一致就认为是同一种限定名称
    /// </summary>
    /// <author>翻译自 权观宇 的 Java 实现</author>
    /// <since>2020-09-15 21:14:27</since>
    public class OfdCommonQName : IEquatable<OfdCommonQName>, IEquatable<XName>
    {
        /// <summary>
        /// 通用的 OFD 命名空间前缀
        /// </summary>
        public const string CommonOfdNamespacePrefix = "http://www.ofdspec.org";

        /// <summary>
        /// 底层的 XName 对象
        /// </summary>
        public XName XName { get; }

        /// <summary>
        /// 获取本地名称
        /// </summary>
        public string LocalName => XName.LocalName;

        /// <summary>
        /// 获取命名空间名称
        /// </summary>
        public string NamespaceName => XName.NamespaceName;

        /// <summary>
        /// 使用 OFD 元素名称创建限定名称
        /// </summary>
        /// <param name="name">OFD 元素名称</param>
        public OfdCommonQName(string name)
        {
            if (string.IsNullOrEmpty(name))
                throw new ArgumentException("元素名称不能为空", nameof(name));

            XName = Const.OfdNamespace + name;
        }

        /// <summary>
        /// 使用指定命名空间和本地名称创建限定名称
        /// </summary>
        /// <param name="namespaceName">命名空间名称</param>
        /// <param name="localName">本地名称</param>
        public OfdCommonQName(string namespaceName, string localName)
        {
            if (string.IsNullOrEmpty(localName))
                throw new ArgumentException("本地名称不能为空", nameof(localName));

            XNamespace ns = namespaceName ?? string.Empty;
            XName = ns + localName;
        }

        /// <summary>
        /// 从 XName 创建 OFD 限定名称
        /// </summary>
        /// <param name="xname">XName 对象</param>
        public OfdCommonQName(XName xname)
        {
            XName = xname ?? throw new ArgumentNullException(nameof(xname));
        }

        /// <summary>
        /// 隐式转换到 XName
        /// </summary>
        /// <param name="qname">OFD 限定名称</param>
        /// <returns>XName 对象</returns>
        public static implicit operator XName(OfdCommonQName qname)
        {
            return qname?.XName ?? throw new ArgumentNullException(nameof(qname));
        }

        /// <summary>
        /// 隐式从 XName 转换
        /// </summary>
        /// <param name="xname">XName 对象</param>
        /// <returns>OFD 限定名称</returns>
        public static implicit operator OfdCommonQName(XName xname)
        {
            return new OfdCommonQName(xname);
        }

        /// <summary>
        /// 隐式从字符串转换
        /// </summary>
        /// <param name="name">元素名称</param>
        /// <returns>OFD 限定名称</returns>
        public static implicit operator OfdCommonQName(string name)
        {
            return new OfdCommonQName(name);
        }

        /// <summary>
        /// 检查两个限定名称是否相等
        /// Name 相同并且，只要符合命名空间前缀相同那么认定为是相等的限定名称
        /// </summary>
        /// <param name="other">比较对象</param>
        /// <returns>true 相同；false 不同</returns>
        public bool Equals(OfdCommonQName? other)
        {
            if (ReferenceEquals(other, null)) return false;
            if (ReferenceEquals(this, other)) return true;

            return LocalName.Equals(other.LocalName, StringComparison.Ordinal) &&
                   other.NamespaceName.StartsWith(CommonOfdNamespacePrefix, StringComparison.Ordinal);
        }

        /// <summary>
        /// 检查与 XName 是否相等
        /// </summary>
        /// <param name="other">XName 对象</param>
        /// <returns>true 相同；false 不同</returns>
        public bool Equals(XName? other)
        {
            if (other == null) return false;

            return LocalName.Equals(other.LocalName, StringComparison.Ordinal) &&
                   other.NamespaceName.StartsWith(CommonOfdNamespacePrefix, StringComparison.Ordinal);
        }

        /// <summary>
        /// 检查两个对象是否相等
        /// </summary>
        /// <param name="obj">比较对象</param>
        /// <returns>true 相同；false 不同</returns>
        public override bool Equals(object? obj)
        {
            return obj switch
            {
                OfdCommonQName qname => Equals(qname),
                XName xname => Equals(xname),
                _ => false
            };
        }

        /// <summary>
        /// 获取哈希码
        /// </summary>
        /// <returns>哈希码</returns>
        public override int GetHashCode()
        {
            return HashCode.Combine(LocalName, CommonOfdNamespacePrefix);
        }

        /// <summary>
        /// 转换为字符串表示
        /// </summary>
        /// <returns>限定名称字符串</returns>
        public override string ToString()
        {
            return XName.ToString();
        }

        /// <summary>
        /// 相等比较运算符
        /// </summary>
        /// <param name="left">左操作数</param>
        /// <param name="right">右操作数</param>
        /// <returns>是否相等</returns>
        public static bool operator ==(OfdCommonQName? left, OfdCommonQName? right)
        {
            if (ReferenceEquals(left, right)) return true;
            if (left is null || right is null) return false;
            return left.Equals(right);
        }

        /// <summary>
        /// 不等比较运算符
        /// </summary>
        /// <param name="left">左操作数</param>
        /// <param name="right">右操作数</param>
        /// <returns>是否不等</returns>
        public static bool operator !=(OfdCommonQName? left, OfdCommonQName? right)
        {
            return !(left == right);
        }

        /// <summary>
        /// 与 XName 相等比较运算符
        /// </summary>
        /// <param name="left">OFD 限定名称</param>
        /// <param name="right">XName</param>
        /// <returns>是否相等</returns>
        public static bool operator ==(OfdCommonQName? left, XName right)
        {
            return left?.Equals(right) ?? false;
        }

        /// <summary>
        /// 与 XName 不等比较运算符
        /// </summary>
        /// <param name="left">OFD 限定名称</param>
        /// <param name="right">XName</param>
        /// <returns>是否不等</returns>
        public static bool operator !=(OfdCommonQName? left, XName right)
        {
            return !(left == right);
        }

        /// <summary>
        /// XName 与 OFD 限定名称相等比较运算符
        /// </summary>
        /// <param name="left">XName</param>
        /// <param name="right">OFD 限定名称</param>
        /// <returns>是否相等</returns>
        public static bool operator ==(XName left, OfdCommonQName? right)
        {
            return right?.Equals(left) ?? false;
        }

        /// <summary>
        /// XName 与 OFD 限定名称不等比较运算符
        /// </summary>
        /// <param name="left">XName</param>
        /// <param name="right">OFD 限定名称</param>
        /// <returns>是否不等</returns>
        public static bool operator !=(XName left, OfdCommonQName? right)
        {
            return !(left == right);
        }
    }
}
