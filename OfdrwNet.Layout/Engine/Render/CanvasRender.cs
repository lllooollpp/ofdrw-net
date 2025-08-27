using System;
using System.IO;
using OfdrwNet.Core.BasicStructure.PageObj.Layer.Block;
using OfdrwNet.Core.BasicType;
using OfdrwNet.Layout.Element;
using OfdrwNet.Layout.Element.Canvas;
using OfdrwNet.Layout.Engine;

namespace OfdrwNet.Layout.Engine.Render
{
    /// <summary>
    /// Canvas渲染器
    /// <para>
    /// Canvas 的渲染器
    /// </para>
    /// 
    /// 作者: 权观宇
    /// 起始时间: 2020-05-01 11:35:00
    /// </summary>
    public class CanvasRender : IProcessor
    {
        /// <summary>
        /// 执行Canvas元素渲染
        /// </summary>
        /// <param name="pageLoc">页面在虚拟容器中绝对路径。</param>
        /// <param name="layer">图层</param>
        /// <param name="resManager">资源管理器</param>
        /// <param name="e">元素</param>
        /// <param name="maxUnitIDProvider">最大ID提供器</param>
        /// <exception cref="RenderException">渲染发生错误</exception>
        public void Render(StLoc pageLoc, CtPageBlock layer, ResManager resManager, Div e, Func<int> maxUnitIDProvider)
        {
            if (e is Canvas canvas)
            {
                Render(layer, resManager, canvas, maxUnitIDProvider);
            }
        }

        /// <summary>
        /// 渲染Canvas元素到指定图层
        /// </summary>
        /// <param name="layer">图层</param>
        /// <param name="resManager">资源管理器</param>
        /// <param name="canvas">Canvas对象</param>
        /// <param name="maxUnitIDProvider">对象ID提供者</param>
        public static void Render(CtPageBlock layer, ResManager resManager, Canvas canvas, Func<int> maxUnitIDProvider)
        {
            if (canvas == null)
            {
                return;
            }

            // 首先渲染Div的基本样式（背景、边框等）
            DivRender.Render(layer, canvas, maxUnitIDProvider);

            // 获取绘制器
            var drawer = canvas.GetDrawer();
            if (drawer == null)
            {
                return;
            }

            // 计算Canvas的实际绘制区域
            var x = canvas.GetX() + canvas.GetMarginLeft() + canvas.GetBorderLeft() + canvas.GetPaddingLeft();
            var y = canvas.GetY() + canvas.GetMarginTop() + canvas.GetBorderTop() + canvas.GetPaddingTop();
            var width = canvas.GetWidth();
            var height = canvas.GetHeight() ?? 0;

            // 创建页面块用于容纳Canvas绘制内容
            CtPageBlock canvasBlock;
            if (canvas.GetPreferBlock() != null)
            {
                canvasBlock = canvas.GetPreferBlock();
            }
            else
            {
                canvasBlock = new CtPageBlock(new StId(maxUnitIDProvider()));
                canvas.SetPreferBlock(canvasBlock);
                layer.Add(canvasBlock);
            }

            // 创建绘制上下文
            var boundary = new StBox(x, y, width, height);
            var drawContext = new DrawContext(canvasBlock, boundary, maxUnitIDProvider, resManager);

            try
            {
                // 执行绘制
                drawer.Draw(drawContext);
            }
            catch (Exception ex)
            {
                throw new RenderException($"Canvas绘制过程中发生错误: {ex.Message}", ex);
            }
            finally
            {
                // 清理绘制上下文
                drawContext?.Dispose();
            }
        }
    }
}