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
    
    /// <summary>
    /// 兼容方法：添加页面块（适配布局引擎调用）
    /// </summary>
    /// <param name="pageBlock">页面块对象</param>
    /// <returns>this</returns>
    public CtPageBlock AddPageBlock(OfdElement pageBlock)
    {
        Add(pageBlock);
        return this;
    }
}