using OfdrwNet.Core.BasicType;
using OfdrwNet.Core.Graph.PathObj;

namespace OfdrwNet.Layout.Element.Canvas
{
    /// <summary>
    /// 画布状态
    /// <para>
    /// 用于保存和恢复绘制状态
    /// </para>
    /// 
    /// 作者: 权观宇
    /// 起始时间: 2020-05-01 11:30:00
    /// </summary>
    public class CanvasState
    {
        /// <summary>
        /// 当前路径数据
        /// </summary>
        public AbbreviatedData? Path { get; set; }

        /// <summary>
        /// 当前变换矩阵 (CTM)
        /// </summary>
        public StArray? Ctm { get; set; }

        /// <summary>
        /// 裁剪区域
        /// </summary>
        public AbbreviatedData? ClipArea { get; set; }

        /// <summary>
        /// 线宽
        /// </summary>
        public double LineWidth { get; set; } = 1.0;

        /// <summary>
        /// 线条端点样式
        /// </summary>
        public string LineCap { get; set; } = "butt";

        /// <summary>
        /// 线条连接样式
        /// </summary>
        public string LineJoin { get; set; } = "miter";

        /// <summary>
        /// 虚线模式
        /// </summary>
        public double[]? LineDash { get; set; }

        /// <summary>
        /// 虚线偏移
        /// </summary>
        public double LineDashOffset { get; set; }

        /// <summary>
        /// 全局透明度
        /// </summary>
        public double GlobalAlpha { get; set; } = 1.0;

        /// <summary>
        /// 全局合成操作
        /// </summary>
        public string GlobalCompositeOperation { get; set; } = "source-over";

        /// <summary>
        /// 克隆当前状态
        /// </summary>
        /// <returns>新的状态对象</returns>
        public CanvasState Clone()
        {
            return new CanvasState
            {
                Path = Path?.Clone(),
                Ctm = Ctm?.Clone(),
                ClipArea = ClipArea?.Clone(),
                LineWidth = LineWidth,
                LineCap = LineCap,
                LineJoin = LineJoin,
                LineDash = LineDash?.Clone() as double[],
                LineDashOffset = LineDashOffset,
                GlobalAlpha = GlobalAlpha,
                GlobalCompositeOperation = GlobalCompositeOperation
            };
        }
    }
}