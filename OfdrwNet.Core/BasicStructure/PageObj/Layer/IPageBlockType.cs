using System.Xml.Linq;

namespace OfdrwNet.Core.BasicStructure.PageObj.Layer
{
    /// <summary>
    /// 用于表示页块类型的接口
    /// 逻辑层面表示
    /// 
    /// 作者：权观宇
    /// 时间：2019-10-10 10:12:35
    /// </summary>
    public interface IPageBlockType
    {
        /// <summary>
        /// 获取XML元素
        /// </summary>
        XElement Element { get; }
    }

    public static class PageBlockTypeFactory
    {
        /// <summary>
        /// 解析元素并获取对应的PageBlock子类实例
        /// </summary>
        /// <param name="element">实例</param>
        /// <returns>子类实例，若无法转换则返回null</returns>
        public static IPageBlockType? GetInstance(XElement element)
        {
            var qName = element.Name.LocalName;
            IPageBlockType? res = null;
            switch (qName)
            {
                case "TextObject":
                    // TODO: 需要等待TextObject类实现
                    // res = new TextObject(element);
                    break;
                case "PathObject":
                    // TODO: 需要等待PathObject类实现
                    // res = new PathObject(element);
                    break;
                case "ImageObject":
                    // TODO: 需要等待ImageObject类实现
                    // res = new ImageObject(element);
                    break;
                case "CompositeObject":
                    // TODO: 需要等待CompositeObject类实现
                    // res = new CompositeObject(element);
                    break;
                case "PageBlock":
                    res = new Block.CtPageBlock(element);
                    break;
                case "Layer":
                    res = new CtLayer(element);
                    break;
                default:
                    res = null;
                    break;
            }
            return res;
        }
    }
}