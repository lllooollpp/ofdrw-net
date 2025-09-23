using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;

namespace OfdrwNet.Reader.Model
{
    /// <summary>
    /// 矢量对象基类
    /// </summary>
    public class VectorObject : RenderObject
    {
        /// <summary>
        /// 矢量类型
        /// </summary>
        public VectorType VectorType { get; set; }

        /// <summary>
        /// 路径数据
        /// </summary>
        public string? PathData { get; set; }

        /// <summary>
        /// 点集合
        /// </summary>
        public List<PointF>? Points { get; set; }

        /// <summary>
        /// 填充样式
        /// </summary>
        public FillStyle? FillStyle { get; set; }

        /// <summary>
        /// 描边样式
        /// </summary>
        public StrokeStyle? StrokeStyle { get; set; }

        /// <summary>
        /// 构造函数
        /// </summary>
        public VectorObject()
        {
            Points = new List<PointF>();
        }
    }

    /// <summary>
    /// 矢量类型枚举
    /// </summary>
    public enum VectorType
    {
        /// <summary>路径</summary>
        Path,
        /// <summary>直线</summary>
        Line,
        /// <summary>矩形</summary>
        Rectangle,
        /// <summary>圆形</summary>
        Circle,
        /// <summary>椭圆</summary>
        Ellipse,
        /// <summary>多边形</summary>
        Polygon,
        /// <summary>折线</summary>
        Polyline
    }

    /// <summary>
    /// 填充样式
    /// </summary>
    public class FillStyle
    {
        /// <summary>颜色</summary>
        public ColorInfo? Color { get; set; }

        /// <summary>透明度</summary>
        public float Alpha { get; set; } = 1.0f;

        /// <summary>填充规则</summary>
        public FillRule FillRule { get; set; } = FillRule.NonZero;
    }

    /// <summary>
    /// 描边样式
    /// </summary>
    public class StrokeStyle
    {
        /// <summary>颜色</summary>
        public ColorInfo? Color { get; set; }

        /// <summary>线宽</summary>
        public float Width { get; set; } = 1.0f;

        /// <summary>虚线数组</summary>
        public List<float>? DashArray { get; set; }

        /// <summary>起始端点样式</summary>
        public LineCapType StartCap { get; set; } = LineCapType.Flat;

        /// <summary>结束端点样式</summary>
        public LineCapType EndCap { get; set; } = LineCapType.Flat;

        /// <summary>线条连接样式</summary>
        public LineJoinType LineJoin { get; set; } = LineJoinType.Miter;
    }

    /// <summary>
    /// 填充规则枚举
    /// </summary>
    public enum FillRule
    {
        /// <summary>非零规则</summary>
        NonZero,
        /// <summary>奇偶规则</summary>
        EvenOdd
    }

    /// <summary>
    /// 线条端点样式枚举
    /// </summary>
    public enum LineCapType
    {
        /// <summary>平直</summary>
        Flat,
        /// <summary>圆形</summary>
        Round,
        /// <summary>方形</summary>
        Square
    }

    /// <summary>
    /// 线条连接样式枚举
    /// </summary>
    public enum LineJoinType
    {
        /// <summary>斜接</summary>
        Miter,
        /// <summary>圆形</summary>
        Round,
        /// <summary>斜切</summary>
        Bevel
    }

    /// <summary>
    /// 路径数据
    /// </summary>
    public class PathData
    {
        /// <summary>路径命令列表</summary>
        public List<PathCommand>? Commands { get; set; }

        /// <summary>构造函数</summary>
        public PathData()
        {
            Commands = new List<PathCommand>();
        }

        /// <summary>
        /// 从字符串解析路径数据
        /// </summary>
        /// <param name="data">路径数据字符串</param>
        /// <returns>路径数据对象</returns>
        public static PathData Parse(string data)
        {
            var pathData = new PathData();
            // TODO: 实现路径数据解析逻辑
            return pathData;
        }
    }

    /// <summary>
    /// 路径命令
    /// </summary>
    public class PathCommand
    {
        /// <summary>命令类型</summary>
        public PathCommandType Type { get; set; }

        /// <summary>点集合</summary>
        public List<PointF>? Points { get; set; }

        /// <summary>构造函数</summary>
        public PathCommand()
        {
            Points = new List<PointF>();
        }
    }

    /// <summary>
    /// 路径命令类型枚举
    /// </summary>
    public enum PathCommandType
    {
        /// <summary>移动到</summary>
        MoveTo,
        /// <summary>直线到</summary>
        LineTo,
        /// <summary>曲线到</summary>
        CurveTo,
        /// <summary>关闭路径</summary>
        ClosePath
    }
}
