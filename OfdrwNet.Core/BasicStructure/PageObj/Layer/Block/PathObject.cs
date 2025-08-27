using System.Xml.Linq;
using OfdrwNet.Core.BasicType;

namespace OfdrwNet.Core.BasicStructure.PageObj.Layer.Block;

/// <summary>
/// 路径对象
/// 
/// 对应Java版本的 org.ofdrw.core.basicStructure.pageObj.layer.block.PathObject
/// </summary>
public class PathObject : BlockType
{
    public PathObject() : base("PathObject")
    {
    }

    public PathObject(XElement element) : base(element)
    {
    }

    public PathObject(StRefId id) : this()
    {
        this.SetObjID(id);
    }

    public PathObject(long id) : this()
    {
        this.SetObjID(new StRefId(id));
    }
}