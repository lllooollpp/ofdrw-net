using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using OfdrwNet.Core.BasicStructure.PageObj.Layer.Block;
using OfdrwNet.Core.BasicType;
using OfdrwNet.Core.Graph.PathObj;
using OfdrwNet.Core.PageDescription.Color;
using OfdrwNet.Core.PageDescription.DrawParam;
using OfdrwNet.Core.Text.Font;
using OfdrwNet.Core.Text;
using CtColor = OfdrwNet.Core.PageDescription.Color.CtColor;
using OfdrwNet.Layout.Element.Canvas;
using OfdrwNet.Layout.Engine;

namespace OfdrwNet.Layout.Element.Canvas
{
    /// <summary>
    /// 绘制器绘制上下文
    /// <para>
    /// 上下文中提供系列的绘制方法供绘制
    /// </para>
    /// <para>
    /// 一个路径对象只允许出现一种描边和填充颜色
    /// 重复设置，取最后一次设置的颜色。
    /// </para>
    /// <para>
    /// 关于路径：
    /// 1. beginPath 清空路径。
    /// 2. 所有路径在 fill 和 stroke 是才应用图元效果。
    /// 3. 路径数据 与 绘图数据分开。
    /// 4. 除了 beginPath 之外所有数据均认为是 向已经存在的路径追加新的路径。
    /// </para>
    /// 
    /// 作者: 权观宇
    /// 起始时间: 2020-05-01 11:29:20
    /// </summary>
    public class DrawContext : IDisposable
    {
        private static readonly StArray ONE = StArray.UnitCTM();

        /// <summary>
        /// 用于容纳所绘制的所有图像的容器
        /// </summary>
        private CtPageBlock container;

        /// <summary>
        /// 对象ID提供器
        /// </summary>
        private readonly Func<int> maxUnitIDProvider;

        /// <summary>
        /// 资源管理器
        /// </summary>
        private ResManager resManager;

        /// <summary>
        /// 边框位置，也就是画布大小以及位置
        /// </summary>
        private StBox boundary;

        /// <summary>
        /// 画布状态
        /// </summary>
        private CanvasState state;

        /// <summary>
        /// 绘制参数栈
        /// <para>
        /// save() 时将当前绘制参数压栈
        /// </para>
        /// <para>
        /// restore() 时将当前绘制参数出栈
        /// </para>
        /// </summary>
        private readonly Stack<CanvasState> stack;

        /// <summary>
        /// 填充颜色
        /// <para>
        /// 支持：
        /// </para>
        /// <para>
        /// string 16进制颜色值，#000000、rgb(0,0,0)、rgba(0,0,0,1)
        /// </para>
        /// <para>
        /// CtColor OFD颜色对象
        /// </para>
        /// <para>
        /// ColorClusterType 颜色族
        /// </para>
        /// <para>
        /// CanvasPattern 图案
        /// </para>
        /// <para>
        /// CanvasGradient 渐变
        /// </para>
        /// <para>
        /// CanvasRadialGradient 径向渐变
        /// </para>
        /// </summary>
        public object? FillStyle { get; set; }

        /// <summary>
        /// 描边颜色
        /// <para>
        /// 支持：
        /// </para>
        /// <para>
        /// string 16进制颜色值，#000000、rgb(0,0,0)、rgba(0,0,0,1)
        /// </para>
        /// <para>
        /// CtColor OFD颜色对象
        /// </para>
        /// <para>
        /// ColorClusterType 颜色族
        /// </para>
        /// <para>
        /// CanvasPattern 图案
        /// </para>
        /// <para>
        /// CanvasGradient 渐变
        /// </para>
        /// <para>
        /// CanvasRadialGradient 径向渐变
        /// </para>
        /// </summary>
        public object? StrokeStyle { get; set; }

        /// <summary>
        /// 字体描述 格式与CSS3格式一致
        /// <para>
        /// [font-style] [font-weight] font-size font-family
        /// </para>
        /// <para>
        /// 它必需包含 font-size font-family，[]内容为可选
        /// </para>
        /// <para>
        /// 详见 https://developer.mozilla.org/en-US/docs/Web/CSS/font
        /// </para>
        /// <para>
        /// font-style: normal | italic
        /// </para>
        /// <para>
        /// font-weight: normal | bold | bolder | lighter | 100 | 200 | 300 | 400 | 500 | 600 | 700 | 800 | 900
        /// </para>
        /// <para>
        /// font-size: 12px | 3.17mm （默认单位为 mm）
        /// </para>
        /// <para>
        /// font-family: 宋体 | SimSun | Times New Roman | Times | serif | sans-serif | monospace | cursive | fantasy
        /// </para>
        /// <para>
        /// font-family 为必选项，其他为可选项
        /// </para>
        /// <para>
        /// font-size 和 line-height 可以使用 px 或 mm 作为单位，若不指定单位则默认为 mm
        /// </para>
        /// <para>
        /// 锚点：
        /// - fillText
        /// - measureText
        /// </para>
        /// </summary>
        public string? Font { get; set; }

        /// <summary>
        /// 每毫米像素数量 pixel per millimeter
        /// </summary>
        public double PPM { get; set; }

        /// <summary>
        /// 创建绘制上下文
        /// </summary>
        /// <param name="container">绘制内容缩所放置容器</param>
        /// <param name="boundary">画布大小以及位置</param>
        /// <param name="maxUnitIDProvider">自增的对象ID</param>
        /// <param name="resManager">资源管理器</param>
        public DrawContext(CtPageBlock container, StBox boundary, Func<int> maxUnitIDProvider, ResManager resManager)
        {
            this.container = container;
            this.boundary = boundary;
            this.maxUnitIDProvider = maxUnitIDProvider;
            this.resManager = resManager;
            state = new CanvasState();
            stack = new Stack<CanvasState>();
            PPM = 3.78;
            // 初始化颜色默认为黑色
            FillStyle = "#000000";
            StrokeStyle = "#000000";
        }

        /// <summary>
        /// 开启一段新的路径
        /// <para>
        /// 如果已经存在路径，那么将会清除已经存在的所有路径。
        /// </para>
        /// </summary>
        /// <returns>this</returns>
        public DrawContext BeginPath()
        {
            state.Path = new AbbreviatedData();
            return this;
        }

        /// <summary>
        /// 关闭路径
        /// <para>
        /// 如果路径存在描边或者填充，那么改路径将会被加入到图形容器中进行渲染
        /// </para>
        /// <para>
        /// 路径关闭后将会清空上下文中的路径对象
        /// </para>
        /// </summary>
        /// <returns>this</returns>
        public DrawContext ClosePath()
        {
            if (state.Path == null)
            {
                return this;
            }
            state.Path.Close();
            return this;
        }

        /// <summary>
        /// 从原始画布中剪切任意形状和尺寸
        /// <para>
        /// 裁剪路径以当前的路径作为裁剪参数
        /// </para>
        /// <para>
        /// 裁剪区域受变换矩阵影响
        /// </para>
        /// </summary>
        /// <returns>this</returns>
        public DrawContext Clip()
        {
            if (state.Path == null)
            {
                return this;
            }

            state.ClipArea = state.Path.Clone();
            if (state.Ctm != null && !ONE.Equals(state.Ctm))
            {
                // 受到CTM的影响形变
                Transform(state.ClipArea, state.Ctm);
            }
            return this;
        }

        /// <summary>
        /// 移动绘制点到指定位置
        /// </summary>
        /// <param name="x">X坐标</param>
        /// <param name="y">Y坐标</param>
        /// <returns>this</returns>
        public DrawContext MoveTo(double x, double y)
        {
            state.Path ??= new AbbreviatedData();
            state.Path.MoveTo(x, y);
            return this;
        }

        /// <summary>
        /// 从当前点连线到指定点
        /// <para>
        /// 请在调用前创建路径
        /// </para>
        /// </summary>
        /// <param name="x">X坐标</param>
        /// <param name="y">Y坐标</param>
        /// <returns>this</returns>
        public DrawContext LineTo(double x, double y)
        {
            if (state.Path == null)
            {
                return this;
            }
            state.Path.LineTo(x, y);
            return this;
        }

        /// <summary>
        /// 通过二次贝塞尔曲线的指定控制点，向当前路径添加一个点。
        /// </summary>
        /// <param name="cpx">贝塞尔控制点的 x 坐标</param>
        /// <param name="cpy">贝塞尔控制点的 y 坐标</param>
        /// <param name="x">结束点的 x 坐标</param>
        /// <param name="y">结束点的 y 坐标</param>
        /// <returns>this</returns>
        public DrawContext QuadraticCurveTo(double cpx, double cpy, double x, double y)
        {
            state.Path ??= new AbbreviatedData();
            state.Path.QuadraticBezier(cpx, cpy, x, y);
            return this;
        }

        /// <summary>
        /// 方法三次贝塞尔曲线的指定控制点，向当前路径添加一个点。
        /// </summary>
        /// <param name="cp1x">第一个贝塞尔控制点的 x 坐标</param>
        /// <param name="cp1y">第一个贝塞尔控制点的 y 坐标</param>
        /// <param name="cp2x">第二个贝塞尔控制点的 x 坐标</param>
        /// <param name="cp2y">第二个贝塞尔控制点的 y 坐标</param>
        /// <param name="x">结束点的 x 坐标</param>
        /// <param name="y">结束点的 y 坐标</param>
        /// <returns>this</returns>
        public DrawContext BezierCurveTo(double cp1x, double cp1y, double cp2x, double cp2y, double x, double y)
        {
            state.Path ??= new AbbreviatedData();
            state.Path.CubicBezier(cp1x, cp1y, cp2x, cp2y, x, y);
            return this;
        }

        /// <summary>
        /// 从当前点连接到点（x，y）的圆弧
        /// </summary>
        /// <param name="a">椭圆长轴长度</param>
        /// <param name="b">椭圆短轴长度</param>
        /// <param name="angle">旋转角度，正值 - 顺时针，负值 - 逆时针</param>
        /// <param name="large">true表示对应度数大于 180°的弧，false 表示对应度数小于 180°的弧</param>
        /// <param name="sweep">true 表示由圆弧起始点到结束点是顺时针旋转，false表示由圆弧起始点到结束点是逆时针旋转。</param>
        /// <param name="x">目标点 x</param>
        /// <param name="y">目标点 y</param>
        /// <returns>this</returns>
        public DrawContext Arc(double a, double b, double angle, bool large, bool sweep, double x, double y)
        {
            state.Path ??= new AbbreviatedData();
            state.Path.Arc(a, b, angle % 360, large ? 1 : 0, sweep ? 1 : 0, x, y);
            return this;
        }

        /// <summary>
        /// 创建弧/曲线（用于创建圆或部分圆）
        /// </summary>
        /// <param name="x">圆的中心的 x 坐标。</param>
        /// <param name="y">圆的中心的 y 坐标。</param>
        /// <param name="r">圆的半径。</param>
        /// <param name="sAngle">起始角，单位度（弧的圆形的三点钟位置是 0 度）。</param>
        /// <param name="eAngle">结束角，单位度</param>
        /// <param name="counterclockwise">规定应该逆时针还是顺时针绘图。false = 顺时针，true = 逆时针。</param>
        /// <returns>this</returns>
        public DrawContext Arc(double x, double y, double r, double sAngle, double eAngle, bool counterclockwise = true)
        {
            state.Path ??= new AbbreviatedData();

            // 首先移动点到起始位置
            var x1 = x + r * Math.Cos(sAngle * Math.PI / 180);
            var y1 = y + r * Math.Sin(sAngle * Math.PI / 180);
            MoveTo(x1, y1);

            var angle = eAngle - sAngle;
            if (Math.Abs(angle - 360) < 0.001)
            {
                // 整个圆的时候需要分为两次路径进行绘制
                state.Path.Arc(r, r, angle, 1, counterclockwise ? 1 : 0, x - r, y)
                          .Arc(r, r, angle, 1, counterclockwise ? 1 : 0, x1, y1);
            }
            else
            {
                // 绘制结束位置起始位置
                var x2 = x + r * Math.Cos(eAngle * Math.PI / 180);
                var y2 = y + r * Math.Sin(eAngle * Math.PI / 180);
                state.Path.Arc(r, r, angle, angle > 180 ? 1 : 0, counterclockwise ? 1 : 0, x2, y2);
            }

            return this;
        }

        /// <summary>
        /// 创建矩形路径
        /// </summary>
        /// <param name="x">左上角X坐标</param>
        /// <param name="y">左上角Y坐标</param>
        /// <param name="width">宽度</param>
        /// <param name="height">高度</param>
        /// <returns>this</returns>
        public DrawContext Rect(double x, double y, double width, double height)
        {
            state.Path ??= new AbbreviatedData();
            state.Path.MoveTo(x, y).LineTo(x + width, y).LineTo(x + width, y + height).LineTo(x, y + height).Close();
            return this;
        }

        /// <summary>
        /// 创建并填充矩形路径
        /// <para>
        /// 填充矩形不会导致影响上下文中的路径。
        /// </para>
        /// <para>
        /// 如果已经存在路径那么改路径将会提前关闭，并创建新的路径。
        /// </para>
        /// </summary>
        /// <param name="x">左上角X坐标</param>
        /// <param name="y">左上角Y坐标</param>
        /// <param name="width">宽度</param>
        /// <param name="height">高度</param>
        /// <returns>this</returns>
        public DrawContext FillRect(double x, double y, double width, double height)
        {
            var abData = new AbbreviatedData()
                .MoveTo(x, y).LineTo(x + width, y).LineTo(x + width, y + height).LineTo(x, y + height).Close();

            var p = new PathObject(new StId(maxUnitIDProvider()));
            p.SetAbbreviatedData(abData);
            p.SetFill(true);
            ApplyDrawParam(p);
            container.Add(p);
            return this;
        }

        /// <summary>
        /// 创建并描边矩形路径
        /// <para>
        /// 描边矩形不会导致影响上下文中的路径。
        /// </para>
        /// <para>
        /// 默认描边颜色为黑色
        /// </para>
        /// </summary>
        /// <param name="x">左上角X坐标</param>
        /// <param name="y">左上角Y坐标</param>
        /// <param name="width">宽度</param>
        /// <param name="height">高度</param>
        /// <returns>this</returns>
        public DrawContext StrokeRect(double x, double y, double width, double height)
        {
            var abData = new AbbreviatedData()
                .MoveTo(x, y).LineTo(x + width, y).LineTo(x + width, y + height).LineTo(x, y + height).Close();

            var p = new PathObject(new StId(maxUnitIDProvider()));
            p.SetAbbreviatedData(abData);
            p.SetStroke(true);
            ApplyDrawParam(p);
            container.Add(p);
            return this;
        }

        /// <summary>
        /// 绘制已定义的路径
        /// </summary>
        /// <returns>this</returns>
        public DrawContext Stroke()
        {
            if (state.Path == null)
            {
                return this;
            }

            var p = new PathObject(new StId(maxUnitIDProvider()));
            p.SetAbbreviatedData(state.Path.Clone());
            p.SetStroke(true);
            ApplyDrawParam(p);
            container.Add(p);
            return this;
        }

        /// <summary>
        /// 填充已定义路径
        /// <para>
        /// 默认的填充颜色是黑色。
        /// </para>
        /// </summary>
        /// <returns>this</returns>
        public DrawContext Fill()
        {
            if (state.Path == null)
            {
                return this;
            }

            var p = new PathObject(new StId(maxUnitIDProvider()));
            p.SetAbbreviatedData(state.Path.Clone());
            p.SetFill(true);
            p.SetLineWidth(0d);
            ApplyDrawParam(p);
            container.Add(p);
            return this;
        }

        /// <summary>
        /// 缩放当前绘图，更大或更小
        /// </summary>
        /// <param name="scalewidth">缩放当前绘图的宽度 (1=100%, 0.5=50%, 2=200%, 依次类推)</param>
        /// <param name="scaleheight">缩放当前绘图的高度 (1=100%, 0.5=50%, 2=200%, 依次类推)</param>
        /// <returns>this</returns>
        public DrawContext Scale(double scalewidth, double scaleheight)
        {
            state.Ctm ??= StArray.UnitCTM();
            var scale = new StArray(scalewidth, 0, 0, scaleheight, 0, 0);
            state.Ctm = scale.MtxMul(state.Ctm);
            return this;
        }

        /// <summary>
        /// 旋转当前的绘图
        /// </summary>
        /// <param name="angle">旋转角度（0~360）</param>
        /// <returns>this</returns>
        public DrawContext Rotate(double angle)
        {
            state.Ctm ??= StArray.UnitCTM();
            var alpha = angle * Math.PI / 180d;
            var r = new StArray(Math.Cos(alpha), Math.Sin(alpha), -Math.Sin(alpha), Math.Cos(alpha), 0, 0);
            state.Ctm = r.MtxMul(state.Ctm);
            return this;
        }

        /// <summary>
        /// 重新映射画布上的 (0,0) 位置
        /// </summary>
        /// <param name="x">添加到水平坐标（x）上的值</param>
        /// <param name="y">添加到垂直坐标（y）上的值</param>
        /// <returns>this</returns>
        public DrawContext Translate(double x, double y)
        {
            state.Ctm ??= StArray.UnitCTM();
            var r = new StArray(1, 0, 0, 1, x, y);
            state.Ctm = r.MtxMul(state.Ctm);
            return this;
        }

        /// <summary>
        /// 变换矩阵
        /// <para>
        /// 每次变换矩阵都会在前一个变换的基础上进行
        /// </para>
        /// </summary>
        /// <param name="a">水平缩放绘图</param>
        /// <param name="b">水平倾斜绘图</param>
        /// <param name="c">垂直倾斜绘图</param>
        /// <param name="d">垂直缩放绘图</param>
        /// <param name="e">水平移动绘图</param>
        /// <param name="f">垂直移动绘图</param>
        /// <returns>this</returns>
        public DrawContext Transform(double a, double b, double c, double d, double e, double f)
        {
            state.Ctm ??= StArray.UnitCTM();
            var r = new StArray(a, b, c, d, e, f);
            state.Ctm = r.MtxMul(state.Ctm);
            return this;
        }

        /// <summary>
        /// 设置变换矩阵
        /// <para>
        /// 每当调用 setTransform() 时，它都会重置前一个变换矩阵然后构建新的矩阵
        /// </para>
        /// </summary>
        /// <param name="a">水平缩放绘图</param>
        /// <param name="b">水平倾斜绘图</param>
        /// <param name="c">垂直倾斜绘图</param>
        /// <param name="d">垂直缩放绘图</param>
        /// <param name="e">水平移动绘图</param>
        /// <param name="f">垂直移动绘图</param>
        /// <returns>this</returns>
        public DrawContext SetTransform(double a, double b, double c, double d, double e, double f)
        {
            state.Ctm = new StArray(a, b, c, d, e, f);
            return this;
        }

        /// <summary>
        /// 保存当前的绘制状态
        /// </summary>
        /// <returns>this</returns>
        public DrawContext Save()
        {
            stack.Push(state.Clone());
            return this;
        }

        /// <summary>
        /// 恢复之前保存的绘制状态
        /// </summary>
        /// <returns>this</returns>
        public DrawContext Restore()
        {
            if (stack.Count > 0)
            {
                state = stack.Pop();
            }
            return this;
        }

        /// <summary>
        /// 应用绘制参数到路径对象
        /// </summary>
        /// <param name="pathObj">路径对象</param>
        private void ApplyDrawParam(PathObject pathObj)
        {
            // 应用填充颜色
            if (pathObj.GetFill() == true && FillStyle != null)
            {
                ApplyFillStyle(pathObj);
            }

            // 应用描边颜色  
            if (pathObj.GetStroke() == true && StrokeStyle != null)
            {
                ApplyStrokeStyle(pathObj);
            }

            // 应用变换矩阵
            if (state.Ctm != null && !ONE.Equals(state.Ctm))
            {
                pathObj.SetCTM(state.Ctm);
            }

            // 应用裁剪区域
            if (state.ClipArea != null)
            {
                // 实现裁剪逻辑
            }
        }

        /// <summary>
        /// 应用填充样式
        /// </summary>
        /// <param name="pathObj">路径对象</param>
        private void ApplyFillStyle(PathObject pathObj)
        {
            switch (FillStyle)
            {
                case string colorStr:
                    var fillColor = ParseColor(colorStr);
                    if (fillColor != null)
                    {
                        pathObj.SetFillColor(fillColor);
                    }
                    break;
                case CtColor ctColor:
                    pathObj.SetFillColor(ctColor);
                    break;
                // 可以继续添加其他类型的处理
            }
        }

        /// <summary>
        /// 应用描边样式
        /// </summary>
        /// <param name="pathObj">路径对象</param>
        private void ApplyStrokeStyle(PathObject pathObj)
        {
            switch (StrokeStyle)
            {
                case string colorStr:
                    var strokeColor = ParseColor(colorStr);
                    if (strokeColor != null)
                    {
                        pathObj.SetStrokeColor(strokeColor);
                    }
                    break;
                case CtColor ctColor:
                    pathObj.SetStrokeColor(ctColor);
                    break;
                // 可以继续添加其他类型的处理
            }
        }

        /// <summary>
        /// 解析颜色字符串
        /// </summary>
        /// <param name="colorStr">颜色字符串</param>
        /// <returns>OFD颜色对象</returns>
        private CtColor? ParseColor(string colorStr)
        {
            // 简化的颜色解析，实际应该支持更多格式
            if (colorStr.StartsWith("#") && colorStr.Length == 7)
            {
                var r = Convert.ToInt32(colorStr.Substring(1, 2), 16);
                var g = Convert.ToInt32(colorStr.Substring(3, 2), 16);
                var b = Convert.ToInt32(colorStr.Substring(5, 2), 16);
                return new CtColor(r, g, b);
            }
            return null;
        }

        /// <summary>
        /// 变换路径数据
        /// </summary>
        /// <param name="pathData">路径数据</param>
        /// <param name="ctm">变换矩阵</param>
        private void Transform(AbbreviatedData pathData, StArray ctm)
        {
            // 实现路径数据的变换
            // 这里应该遍历路径数据中的所有坐标点，应用变换矩阵
            // 简化实现，实际需要更复杂的逻辑
        }

        /// <summary>
        /// 绘制文本
        /// </summary>
        /// <param name="text">要绘制的文本</param>
        /// <param name="x">文本起始位置的 x 坐标</param>
        /// <param name="y">文本起始位置的 y 坐标</param>
        /// <returns>this</returns>
        public DrawContext FillText(string text, double x, double y)
        {
            // 创建文本对象并添加到容器中
            // 这里是简化实现，实际需要更复杂的文本处理逻辑
            return this;
        }

        /// <summary>
        /// 绘制图像
        /// </summary>
        /// <param name="imagePath">图像路径</param>
        /// <param name="x">图像左上角的 x 坐标</param>
        /// <param name="y">图像左上角的 y 坐标</param>
        /// <param name="width">图像宽度</param>
        /// <param name="height">图像高度</param>
        /// <returns>this</returns>
        public DrawContext DrawImage(string imagePath, double x, double y, double width, double height)
        {
            // 创建图像对象并添加到容器中
            // 这里是简化实现，实际需要图像处理逻辑
            return this;
        }

        /// <summary>
        /// 释放资源
        /// </summary>
        public void Dispose()
        {
            // 清理资源
        }
    }
}