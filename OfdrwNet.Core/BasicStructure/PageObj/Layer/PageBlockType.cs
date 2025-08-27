using System.Xml.Linq;
using OfdrwNet.Core.Text;

namespace OfdrwNet.Core.BasicStructure.PageObj.Layer;

/// <summary>
/// 用于表示页块类型的接口
/// 
/// 逻辑层面表示
/// 
/// 对应Java版本的 org.ofdrw.core.basicStructure.pageObj.layer.PageBlockType
/// </summary>
public interface IPageBlockType
{
    /// <summary>
    /// 获取XML元素
    /// </summary>
    XElement Element { get; }
}

/// <summary>
/// PageBlockType工厂类
/// </summary>
public static class PageBlockTypeFactory
{
    /// <summary>
    /// 解析元素并获取对应的PageBlock子类实例
    /// </summary>
    /// <param name="element">XML元素</param>
    /// <returns>子类实例，若无法转换则返回null</returns>
    public static IPageBlockType? GetInstance(XElement element)
    {
        var qName = element.Name.LocalName;
        
        return qName switch
        {
            "TextObject" => new Text.TextObject(element),
            "PathObject" => new Block.PathObject(element),
            "ImageObject" => new Block.ImageObject(element),
            "CompositeObject" => new Block.CompositeObject(element),
            "PageBlock" => new Block.CtPageBlock(element),
            "Layer" => new CtLayer(element),
            _ => null
        };
    }
}