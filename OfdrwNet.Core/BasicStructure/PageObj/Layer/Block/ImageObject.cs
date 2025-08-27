using System.Xml.Linq;
using OfdrwNet.Core.BasicType;

namespace OfdrwNet.Core.BasicStructure.PageObj.Layer.Block;

/// <summary>
/// 图像对象
/// 
/// 对应Java版本的 org.ofdrw.core.basicStructure.pageObj.layer.block.ImageObject
/// </summary>
public class ImageObject : BlockType
{
    public ImageObject() : base("ImageObject")
    {
    }

    public ImageObject(XElement element) : base(element)
    {
    }

    public ImageObject(StRefId id) : this()
    {
        this.SetObjID(id);
    }

    public ImageObject(long id) : this()
    {
        this.SetObjID(new StRefId(id));
    }
}