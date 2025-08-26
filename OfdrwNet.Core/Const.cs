using System.Xml.Linq;

namespace OfdrwNet.Core;

/// <summary>
/// 静态常量定义
/// 对应 Java 版本的 org.ofdrw.core.Const
/// </summary>
public static class Const
{
    /// <summary>
    /// 命名空间 URI，《GB/T_33190-2016》 7.1 命名空间
    /// </summary>
    public const string OfdNamespaceUri = "http://www.ofdspec.org/2016";
    
    /// <summary>
    /// 元素节点应使用命名空间标识符
    /// ——《GB/T 33190-2016》 7.1 命名空间
    /// </summary>
    public const string OfdValue = "ofd";
    
    /// <summary>
    /// OFD命名空间前缀
    /// </summary>
    public const string OfdPrefix = "ofd:";
    
    /// <summary>
    /// 使用命名空间为 http://www.ofdspec.org/2016，其表示符应为 ofd
    /// ——《GB/T 33190-2016》 7.1 命名空间
    /// </summary>
    public static readonly XNamespace OfdNamespace = XNamespace.Get(OfdNamespaceUri);
    
    /// <summary>
    /// 默认OFD命名空间（无前缀）
    /// </summary>
    public static readonly XNamespace OfdNamespaceDefault = XNamespace.Get(OfdNamespaceUri);
    
    /// <summary>
    /// xs:date 类型日期格式
    /// </summary>
    public const string DateFormat = "yyyy-MM-dd";
    
    /// <summary>
    /// xs:dateTime 类型时间日期格式
    /// </summary>
    public const string DateTimeFormat = "yyyy-MM-ddTHH:mm:ss";
    
    /// <summary>
    /// 本地时间日期格式
    /// </summary>
    public const string LocalDateTimeFormat = "yyyy-MM-dd HH:mm:ss";
    
    /// <summary>
    /// OFD索引文件
    /// </summary>
    public const string IndexFile = "OFD.xml";
}