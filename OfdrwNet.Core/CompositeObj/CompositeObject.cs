using System.Xml.Linq;
using OfdrwNet.Core.BasicType;
using OfdrwNet.Core.PageDescription;

namespace OfdrwNet.Core.CompositeObj;

/// <summary>
/// 复合对象
/// 
/// 复合对象是一种特殊的图元对象，拥有图元对象的一切特性，
/// 但其内容在ResourceID指向的矢量图像资源中进行描述，
/// 一个资源可以被多个复合对象所引用。通过这种方式可实现对
/// 文档内矢量图文内容的服用。
/// 
/// 对应 Java 版本的 org.ofdrw.core.compositeObj.CT_Composite
/// 13 复合对象 图 71 表 49
/// </summary>
public class CompositeObject : CtGraphicUnit<CompositeObject>
{
    /// <summary>
    /// 从现有元素构造复合对象
    /// </summary>
    /// <param name="element">XML元素</param>
    public CompositeObject(XElement element) : base(element)
    {
    }

    /// <summary>
    /// 构造新的复合对象
    /// </summary>
    public CompositeObject() : base("CompositeObject")
    {
    }

    /// <summary>
    /// 构造带名称的复合对象
    /// </summary>
    /// <param name="name">元素名称</param>
    protected CompositeObject(string name) : base(name)
    {
    }

    /// <summary>
    /// 【必选 属性】
    /// 设置引用资源文件中定义的矢量图像的标识
    /// 
    /// 复合对象引用的资源时 Res 中的矢量图像（CompositeGraphUnit），
    /// 其类型为 CT_VectorG
    /// </summary>
    /// <param name="resourceId">矢量图像资源标识</param>
    /// <returns>this</returns>
    public CompositeObject SetResourceId(StRefId resourceId)
    {
        SetAttribute("ResourceID", resourceId.ToString());
        return this;
    }

    /// <summary>
    /// 【必选 属性】
    /// 获取引用资源文件中定义的矢量图像的标识
    /// 
    /// 复合对象引用的资源时 Res 中的矢量图像（CompositeGraphUnit），
    /// 其类型为 CT_VectorG
    /// </summary>
    /// <returns>矢量图像资源标识</returns>
    public StRefId? GetResourceId()
    {
        var value = GetAttributeValue("ResourceID");
        return string.IsNullOrEmpty(value) ? null : StRefId.Parse(value);
    }

    /// <summary>
    /// 【可选 属性】
    /// 设置引用对象的可见性
    /// </summary>
    /// <param name="visible">是否可见，默认为true</param>
    /// <returns>this</returns>
    public new CompositeObject SetVisible(bool visible)
    {
        SetAttribute("Visible", visible.ToString().ToLower());
        return this;
    }

    /// <summary>
    /// 【可选 属性】
    /// 获取引用对象的可见性
    /// </summary>
    /// <returns>是否可见，默认为true</returns>
    public new bool GetVisible()
    {
        var value = GetAttributeValue("Visible");
        return string.IsNullOrEmpty(value) || bool.Parse(value);
    }

    /// <summary>
    /// 创建复合对象并设置必要属性
    /// </summary>
    /// <param name="id">对象ID</param>
    /// <param name="resourceId">矢量图像资源ID</param>
    /// <param name="boundary">边界框</param>
    /// <returns>复合对象</returns>
    public static CompositeObject Create(StId id, StRefId resourceId, StBox boundary)
    {
        var composite = new CompositeObject();
        composite.SetObjId(id);
        composite.SetResourceId(resourceId);
        composite.SetBoundary(boundary);
        return composite;
    }

    /// <summary>
    /// 验证复合对象是否有效
    /// </summary>
    /// <returns>是否有效</returns>
    public new bool IsValid()
    {
        return GetResourceId() != null && GetBoundary() != null;
    }

    /// <summary>
    /// 获取复合对象描述信息
    /// </summary>
    /// <returns>描述字符串</returns>
    public override string ToString()
    {
        var resourceId = GetResourceId();
        var boundary = GetBoundary();
        return $"CompositeObject[ResourceID={resourceId}, Boundary={boundary}]";
    }
}
