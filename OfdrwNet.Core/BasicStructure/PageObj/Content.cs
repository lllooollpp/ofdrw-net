using System;
using System.Collections.Generic;
using System.Linq;
using System.Xml.Linq;
using OfdrwNet.Core.BasicStructure.PageObj.Layer;
using OfdrwNet.Core.BasicType;

namespace OfdrwNet.Core.BasicStructure.PageObj
{
    /// <summary>
    /// 页面内容描述，该节点不存在时，表示空白页面
    /// 
    /// 7.7 页面对象 表 12
    /// 
    /// 作者：权观宇
    /// 时间：2019-10-10 09:55:20
    /// </summary>
    public class Content : OfdElement
    {
        public Content(XElement proxy) : base(proxy)
        {
        }

        public Content() : base("Content")
        {
        }

        /// <summary>
        /// 【必选】
        /// 增加层节点
        /// 一页可以包含一个或多个层
        /// 注意：每个加入的层节点必须设置 ID属性。
        /// </summary>
        /// <param name="layer">层节点</param>
        /// <returns>this</returns>
        /// <exception cref="ArgumentException">加入的图层对象（CtLayer）没有设置ID属性</exception>
        public Content AddLayer(CtLayer layer)
        {
            var id = layer.GetObjId();
            if (id == null)
            {
                throw new ArgumentException("加入的图层对象（CtLayer）没有设置ID属性");
            }
            Add(layer);
            return this;
        }

        /// <summary>
        /// 【必选】
        /// 获取层节点列表
        /// 一页可以包含一个或多个层
        /// 注意：每个加入的层节点必须设置 ID属性。
        /// </summary>
        /// <returns>层节点</returns>
        public List<CtLayer> GetLayers()
        {
            return GetOfdElements("Layer", e => new CtLayer(e));
        }

        /// <summary>
        /// 【必选】
        /// 获取排序后的层节点列表
        /// 一页可以包含一个或多个层
        /// 注意：每个加入的层节点必须设置 ID属性，排序如下：
        /// 背景模板
        /// 背景层
        /// 正文模板
        /// 正文层
        /// 前景模板
        /// 前景层
        /// </summary>
        /// <returns>层节点</returns>
        public List<CtLayer> GetOrderedLayers()
        {
            var listLayers = GetOfdElements("Layer", e => new CtLayer(e));
            return listLayers.OrderBy(p => p.GetLayerType()).ToList(); // 修复：移除GetOrder()调用，直接使用GetLayerType()
        }
    }
}