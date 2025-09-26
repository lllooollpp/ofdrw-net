using System;
using System.Drawing;
using System.Drawing.Drawing2D;

namespace OfdrwNet.Reader.Model
{
    /// <summary>
    /// 可渲染对象基类
    /// </summary>
    public abstract class RenderObject
    {
        /// <summary>
        /// 对象ID
        /// </summary>
        public string Id { get; set; } = string.Empty;

        /// <summary>
        /// 边界矩形
        /// </summary>
        public RectangleF Boundary { get; set; }

        /// <summary>
        /// 是否可见
        /// </summary>
        public bool Visible { get; set; } = true;

        /// <summary>
        /// 透明度 (0-1)
        /// </summary>
        public double Alpha { get; set; } = 1.0;

        /// <summary>
        /// 层级顺序
        /// </summary>
        public int ZOrder { get; set; } = 0;

        /// <summary>
        /// Z轴索引（绘制深度）
        /// </summary>
        public int ZIndex { get; set; } = 0;

        /// <summary>
        /// 变换矩阵
        /// </summary>
        public Matrix? Transform { get; set; }

        /// <summary>
        /// 是否需要重新渲染
        /// </summary>
        public virtual bool RequiresRedraw { get; set; } = true;

        /// <summary>
        /// 获取有效边界矩形（应用变换后）
        /// </summary>
        /// <returns>变换后的边界矩形</returns>
        public virtual RectangleF GetTransformedBoundary()
        {
            if (Transform == null)
                return Boundary;

            var points = new PointF[]
            {
                new PointF(Boundary.Left, Boundary.Top),
                new PointF(Boundary.Right, Boundary.Top),
                new PointF(Boundary.Right, Boundary.Bottom),
                new PointF(Boundary.Left, Boundary.Bottom)
            };

            Transform.TransformPoints(points);

            var minX = Math.Min(Math.Min(points[0].X, points[1].X), Math.Min(points[2].X, points[3].X));
            var minY = Math.Min(Math.Min(points[0].Y, points[1].Y), Math.Min(points[2].Y, points[3].Y));
            var maxX = Math.Max(Math.Max(points[0].X, points[1].X), Math.Max(points[2].X, points[3].X));
            var maxY = Math.Max(Math.Max(points[0].Y, points[1].Y), Math.Max(points[2].Y, points[3].Y));

            return new RectangleF(minX, minY, maxX - minX, maxY - minY);
        }
    }
}
