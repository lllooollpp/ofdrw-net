using System.Xml.Linq;
using OfdrwNet.Core.BasicType;

namespace OfdrwNet.Core.BasicStructure.PageObj.Layer.Block;

/// <summary>
/// 页面块类型基类
/// 
/// 为各种页面块对象（文字、图像、路径等）提供基础功能
/// 
/// 对应Java版本中各种block对象的共同功能
/// </summary>
public abstract class BlockType : OfdElement, IPageBlockType
{
    /// <summary>
    /// 从现有元素构造块对象
    /// </summary>
    /// <param name="element">XML元素</param>
    protected BlockType(XElement element) : base(element)
    {
    }

    /// <summary>
    /// 构造新的块对象
    /// </summary>
    /// <param name="name">元素名称</param>
    protected BlockType(string name) : base(name)
    {
    }

    /// <summary>
    /// 【必选 属性】
    /// 设置对象标识
    /// </summary>
    /// <param name="objID">对象标识</param>
    /// <returns>this</returns>
    public virtual BlockType SetObjID(StRefId objID)
    {
        if (objID == null)
        {
            throw new ArgumentException("对象标识不能为空");
        }
        this.SetAttribute("ID", objID.ToString());
        return this;
    }

    /// <summary>
    /// 【必选 属性】
    /// 获取对象标识
    /// </summary>
    /// <returns>对象标识</returns>
    public virtual StRefId? GetObjID()
    {
        return StRefId.Parse(this.GetAttributeValue("ID"));
    }

    /// <summary>
    /// 设置边界框
    /// </summary>
    /// <param name="boundary">边界框</param>
    /// <returns>this</returns>
    public virtual BlockType SetBoundary(StArray boundary)
    {
        if (boundary != null)
        {
            this.SetAttribute("Boundary", boundary.ToString());
        }
        return this;
    }

    /// <summary>
    /// 获取边界框
    /// </summary>
    /// <returns>边界框</returns>
    public virtual StArray? GetBoundary()
    {
        var value = this.GetAttributeValue("Boundary");
        return string.IsNullOrEmpty(value) ? null : StArray.Parse(value);
    }

    /// <summary>
    /// 设置是否可见
    /// </summary>
    /// <param name="visible">是否可见</param>
    /// <returns>this</returns>
    public virtual BlockType SetVisible(bool visible)
    {
        this.SetAttribute("Visible", visible.ToString().ToLowerInvariant());
        return this;
    }

    /// <summary>
    /// 获取是否可见
    /// </summary>
    /// <returns>是否可见，默认true</returns>
    public virtual bool IsVisible()
    {
        var value = this.GetAttributeValue("Visible");
        return string.IsNullOrEmpty(value) || bool.Parse(value);
    }
}