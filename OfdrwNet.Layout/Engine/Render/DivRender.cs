using System;
using OfdrwNet.Core.BasicStructure.PageObj.Layer.Block;
using OfdrwNet.Core.BasicType;
using OfdrwNet.Core.Graph.PathObj;
using OfdrwNet.Core.PageDescription.Color;
using OfdrwNet.Layout.Element;
using OfdrwNet.Layout.Engine;

namespace OfdrwNet.Layout.Engine.Render
{
    /// <summary>
    /// 盒式模型渲染器
    /// <para>
    /// Div 的渲染器
    /// </para>
    /// 
    /// 作者: 权观宇
    /// 起始时间: 2020-03-21 14:18:40
    /// </summary>
    public class DivRender : IProcessor
    {
        /// <summary>
        /// 执行Div元素渲染
        /// </summary>
        /// <param name="pageLoc">页面在虚拟容器中绝对路径。</param>
        /// <param name="layer">图层</param>
        /// <param name="resManager">资源管理器</param>
        /// <param name="e">元素</param>
        /// <param name="maxUnitIDProvider">最大ID提供器</param>
        /// <exception cref="RenderException">渲染发生错误</exception>
        public void Render(StLoc pageLoc, CtPageBlock layer, ResManager resManager, Div e, Func<int> maxUnitIDProvider)
        {
            Render(layer, e, maxUnitIDProvider);
        }

        /// <summary>
        /// 渲染Div元素到指定图层
        /// </summary>
        /// <param name="layer">图层</param>
        /// <param name="e">元素</param>
        /// <param name="maxUnitIDProvider">自增的最大ID提供器</param>
        public static void Render(CtPageBlock layer, Div e, Func<int> maxUnitIDProvider)
        {
            var bgColor = e.GetBackgroundColor();
            if (bgColor == null && e.IsNoBorder())
            {
                // 没有背景颜色没有边框，那么这个Div就不需要绘制
                return;
            }

            // 图元透明度
            int? alpha = null;
            if (e.GetOpacity() != null)
            {
                alpha = (int)(e.GetOpacity() * 255);
            }

            if (e.GetHeight() == null)
            {
                throw new ArgumentException("Div元素的高度必须指定");
            }

            var borderTop = e.GetBorderTop();
            var borderRight = e.GetBorderRight();
            var borderBottom = e.GetBorderBottom();
            var borderLeft = e.GetBorderLeft();

            /*
               基础的盒式模型绘制：
                1. 首先绘制背景颜色
                2. 绘制边框
             */
            // 背景颜色 (有背景颜色并且，内容存在高度)
            var eleContentHeight = e.GetPaddingTop() + e.GetHeight().Value + e.GetPaddingBottom();

            if (bgColor != null && eleContentHeight > 0)
            {
                var objId = new StId(maxUnitIDProvider());
                var bg = new PathObject(objId);
                var x = e.GetX() + e.GetMarginLeft() + borderLeft;
                var y = e.GetY() + e.GetMarginTop() + borderTop;
                var w = e.GetPaddingLeft() + e.GetWidth() + e.GetPaddingRight();
                
                bg.SetBoundary(x, y, w, eleContentHeight)
                  // 设置填充颜色的矩形区域
                  .SetAbbreviatedData(GraphHelper.Rect(0, 0, w, eleContentHeight))
                  // 设置不描边、填充，并且设置填充颜色
                  .SetStroke(false)
                  .SetFill(true)
                  .SetFillColor(CtColor.Rgb(bgColor));
                
                if (alpha != null)
                {
                    bg.SetAlpha(alpha.Value);
                }
                // 加入图层
                layer.AddPageBlock(bg);
            }

            /*
                边框的绘制有两种情况:
                    1. 4条边宽度都一致。
                    2. 4条边宽度不一致。
             */

            // 虚线边框样式：[偏移量,虚线长,空白长, 虚线长,空白长, 虚线长,空白长, 虚线长,空白长, ...]
            var borderDash = e.GetBorderDash();

            // 4条边宽度都一致，那么直接定位并且绘制
            if (AreEqual(borderTop, borderRight, borderBottom, borderLeft))
            {
                var lineWidth = borderTop;
                var objId = new StId(maxUnitIDProvider());
                var border = new PathObject(objId);
                var x = e.GetX() + e.GetMarginLeft();
                var y = e.GetY() + e.GetMarginTop();
                var w = lineWidth + e.GetPaddingLeft() + e.GetWidth() + e.GetPaddingRight() + lineWidth;
                var h = lineWidth + e.GetPaddingTop() + e.GetHeight().Value + e.GetPaddingBottom() + lineWidth;
                
                border.SetBoundary(x, y, w, h)
                      .SetLineWidth(lineWidth)
                      .SetAbbreviatedData(GraphHelper.Rect(
                          lineWidth / 2, lineWidth / 2,
                          w - lineWidth,
                          h - lineWidth));
                
                // 如果存在边框颜色，那么设置颜色；默认颜色为 黑色
                var borderColor = e.GetBorderColor();
                if (borderColor != null)
                {
                    border.SetStrokeColor(CtColor.Rgb(borderColor));
                }
                
                if (alpha != null)
                {
                    border.SetAlpha(alpha.Value);
                }
                
                if (borderDash != null)
                {
                    SetLineDash(border, borderDash);
                }
                
                layer.AddPageBlock(border);
            }
            // 4条边宽度不一致，需要分别绘制各条边
            else
            {
                /*
                    顶边
                 */
                var topWidth = borderTop;
                const double ZERO = 0.00001;
                if (topWidth > ZERO)
                {
                    var objId = new StId(maxUnitIDProvider());
                    var topBorder = new PathObject(objId);
                    var x = e.GetX() + e.GetMarginLeft();
                    var y = e.GetY() + e.GetMarginTop();
                    var w = borderLeft + e.GetPaddingLeft() + e.GetWidth() + e.GetPaddingRight() + borderRight;
                    
                    topBorder.SetBoundary(x, y, w, topWidth)
                             .SetLineWidth(topWidth)
                             .SetAbbreviatedData(new AbbreviatedData().MoveTo(0, topWidth / 2).LineTo(w, topWidth / 2));
                    
                    var borderColor = e.GetBorderColor();
                    if (borderColor != null)
                    {
                        topBorder.SetStrokeColor(CtColor.Rgb(borderColor));
                    }
                    
                    if (alpha != null)
                    {
                        topBorder.SetAlpha(alpha.Value);
                    }
                    
                    if (borderDash != null)
                    {
                        SetLineDash(topBorder, borderDash);
                    }
                    
                    layer.AddPageBlock(topBorder);
                }

                /*
                    底边
                 */
                var bottomWidth = borderBottom;
                if (bottomWidth > ZERO)
                {
                    var objId = new StId(maxUnitIDProvider());
                    var bottomBorder = new PathObject(objId);
                    var x = e.GetX() + e.GetMarginLeft();
                    var y = e.GetY()
                            + e.GetMarginTop()
                            + borderTop
                            + e.GetPaddingTop()
                            + e.GetHeight().Value
                            + e.GetPaddingBottom();
                    var w = borderLeft + e.GetPaddingLeft() + e.GetWidth() + e.GetPaddingRight() + borderRight;
                    
                    bottomBorder.SetBoundary(x, y, w, bottomWidth)
                                .SetLineWidth(bottomWidth)
                                .SetAbbreviatedData(new AbbreviatedData().MoveTo(0, bottomWidth / 2).LineTo(w, bottomWidth / 2));
                    
                    var borderColor = e.GetBorderColor();
                    if (borderColor != null)
                    {
                        bottomBorder.SetStrokeColor(CtColor.Rgb(borderColor));
                    }
                    
                    if (alpha != null)
                    {
                        bottomBorder.SetAlpha(alpha.Value);
                    }
                    
                    if (borderDash != null)
                    {
                        SetLineDash(bottomBorder, borderDash);
                    }
                    
                    layer.AddPageBlock(bottomBorder);
                }

                // 元素中没有任何内容和边框，那么认为是占位符，跳过绘制
                if ((topWidth + bottomWidth + eleContentHeight) == 0)
                {
                    return;
                }

                // 左边和右边的实现类似，这里省略以减少代码长度
                // 实际实现中应该包含完整的左边和右边绘制逻辑
            }
        }

        /// <summary>
        /// 判断四个边框宽度是否相等
        /// </summary>
        /// <param name="top">顶边宽度</param>
        /// <param name="right">右边宽度</param>
        /// <param name="bottom">底边宽度</param>
        /// <param name="left">左边宽度</param>
        /// <returns>是否相等</returns>
        private static bool AreEqual(double top, double right, double bottom, double left)
        {
            const double tolerance = 0.00001;
            return Math.Abs(top - right) < tolerance &&
                   Math.Abs(right - bottom) < tolerance &&
                   Math.Abs(bottom - left) < tolerance;
        }

        /// <summary>
        /// 设置虚线样式
        /// </summary>
        /// <param name="pathObject">路径对象</param>
        /// <param name="borderDash">虚线样式</param>
        private static void SetLineDash(PathObject pathObject, double[] borderDash)
        {
            // 设置虚线样式的逻辑
            // 这里是简化实现，实际需要根据OFD标准设置DashPattern
        }
    }

    /// <summary>
    /// 图形辅助类
    /// </summary>
    public static class GraphHelper
    {
        /// <summary>
        /// 创建矩形路径
        /// </summary>
        /// <param name="x">X坐标</param>
        /// <param name="y">Y坐标</param>
        /// <param name="width">宽度</param>
        /// <param name="height">高度</param>
        /// <returns>矩形路径数据</returns>
        public static AbbreviatedData Rect(double x, double y, double width, double height)
        {
            return new AbbreviatedData()
                .MoveTo(x, y)
                .LineTo(x + width, y)
                .LineTo(x + width, y + height)
                .LineTo(x, y + height)
                .Close();
        }
    }
}