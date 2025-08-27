using System.IO;

namespace OfdrwNet.Layout.Element.Canvas
{
    /// <summary>
    /// Canvas 内容绘制器
    /// <para>
    /// 用于绘制Canvas中的实际内容
    /// </para>
    /// 
    /// 作者: 权观宇
    /// 起始时间: 2020-05-01 11:22:05
    /// </summary>
    public interface IDrawer
    {
        /// <summary>
        /// 绘制
        /// </summary>
        /// <param name="ctx">绘制上下文</param>
        /// <exception cref="IOException">图片读取过程中IO异常</exception>
        void Draw(DrawContext ctx);
    }
}