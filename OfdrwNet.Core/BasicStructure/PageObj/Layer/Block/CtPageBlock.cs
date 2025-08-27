using System.Xml.Linq;
using OfdrwNet.Core.BasicType;

namespace OfdrwNet.Core.BasicStructure.PageObj.Layer.Block;

/// <summary>
/// 页面块
/// 
/// 对应Java版本的 org.ofdrw.core.basicStructure.pageObj.layer.block.CT_PageBlock
/// </summary>
public class CtPageBlock : BlockType
{
    public CtPageBlock() : base("PageBlock")
    {
    }

    public CtPageBlock(XElement element) : base(element)
    {
    }

    public CtPageBlock(StRefId id) : this()
    {
        this.SetObjID(id);
    }

    public CtPageBlock(long id) : this()
    {
        this.SetObjID(new StRefId(id));
    }
}