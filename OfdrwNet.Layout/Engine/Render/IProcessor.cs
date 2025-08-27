using System;
using OfdrwNet.Core.BasicStructure.PageObj.Layer.Block;
using OfdrwNet.Core.BasicType;
using OfdrwNet.Layout.Element;
using OfdrwNet.Layout.Engine;

namespace OfdrwNet.Layout.Engine.Render
{
    /// <summary>
    /// 元素绘制器
    /// <para>
    /// 用于实现OFDRW元素到OFD图元的转换，并处理OFD虚拟容器以及资源管理。
    /// </para>
    /// <para>
    /// 绘制器的选择由 VPageParseEngine 实现，您需要向 VPageParseEngine 通过名称注册绘制器。
    /// </para>
    /// 
    /// 作者: 权观宇
    /// 起始时间: 2024-5-27 19:22:48
    /// </summary>
    public interface IProcessor
    {
        /// <summary>
        /// 处理OFDRW元素转换为OFD元素
        /// </summary>
        /// <param name="pageLoc">页面路径</param>
        /// <param name="layer">图片将要放置的图层</param>
        /// <param name="resManager">资源管理器</param>
        /// <param name="e">OFDRW元素</param>
        /// <param name="maxUnitIDProvider">最大元素ID提供器</param>
        /// <exception cref="RenderException">渲染发生错误</exception>
        void Render(StLoc pageLoc, CtPageBlock layer, ResManager resManager, Div e, Func<int> maxUnitIDProvider);
    }
}