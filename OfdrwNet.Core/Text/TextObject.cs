using System;
using System.Xml.Linq;
using OfdrwNet.Core.BasicStructure.PageObj.Layer;
using OfdrwNet.Core.BasicStructure.PageObj.Layer.Block;
using OfdrwNet.Core.BasicType;
using OfdrwNet.Core.PageDescription;

namespace OfdrwNet.Core.Text;

/// <summary>
/// 文字对象
/// 
/// 文本对象的层级容器，包含文字内容和样式定义
/// 
/// 对应Java版本的 org.ofdrw.core.basicStructure.pageObj.layer.block.TextObject
/// 7.7 表 16
/// </summary>
public class TextObject : CtText, IPageBlockType
{
    public TextObject() : base("TextObject")
    {
    }

    public TextObject(XElement element) : base(element)
    {
    }

    /// <summary>
    /// 创建文字对象实例
    /// </summary>
    /// <param name="id">对象ID</param>
    public TextObject(StRefId id) : this()
    {
        this.SetObjId(id);
    }

    /// <summary>
    /// 创建文字对象实例
    /// </summary>
    /// <param name="id">对象ID</param>
    public TextObject(long id) : this()
    {
        this.SetObjId(new StRefId(id));
    }

    /// <summary>
    /// 创建文字对象实例
    /// </summary>
    /// <param name="id">对象ID</param>
    /// <returns>文字对象</returns>
    public static TextObject Create(StRefId id)
    {
        if (id == null)
        {
            throw new ArgumentException("ID 不能为空");
        }
        
        return new TextObject(id);
    }

    /// <summary>
    /// 【必选 属性】
    /// 设置对象ID
    /// </summary>
    /// <param name="id">对象ID</param>
    /// <returns>this</returns>
    public TextObject SetID(StRefId id)
    {
        if (id == null)
        {
            throw new ArgumentException("ID 不能为空");
        }
        this.SetObjId(id);
        return this;
    }

    /// <summary>
    /// 【必选 属性】
    /// 获取对象ID
    /// </summary>
    /// <returns>对象ID</returns>
    public StId? GetID()
    {
        return this.GetObjId();
    }
}