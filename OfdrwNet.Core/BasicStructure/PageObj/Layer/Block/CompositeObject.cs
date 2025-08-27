using System.Xml.Linq;
using OfdrwNet.Core.BasicType;

namespace OfdrwNet.Core.BasicStructure.PageObj.Layer.Block;

/// <summary>
/// 复合对象
/// 
/// 对应Java版本的 org.ofdrw.core.basicStructure.pageObj.layer.block.CompositeObject
/// </summary>
public class CompositeObject : BlockType
{
    public CompositeObject() : base("CompositeObject")
    {
    }

    public CompositeObject(XElement element) : base(element)
    {
    }

    public CompositeObject(StRefId id) : this()
    {
        this.SetObjID(id);
    }

    public CompositeObject(long id) : this()
    {
        this.SetObjID(new StRefId(id));
    }
}