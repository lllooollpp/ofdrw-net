using System;
using System.Collections.Generic;
using System.Drawing;
using System.Threading.Tasks;
using OfdrwNet.Reader.Model;

namespace OfdrwNet.Reader.Rendering
{
    /// <summary>
    /// 页面渲染器接口，处理页面内容的渲染和显示
    /// </summary>
    public interface IPageRenderer
    {
        /// <summary>
        /// 渲染指定页面到图形上下文
        /// </summary>
        /// <param name="pageInfo">页面信息</param>
        /// <param name="graphics">图形上下文</param>
        /// <param name="renderContext">渲染上下文</param>
        /// <returns>渲染结果</returns>
        Task<RenderResult> RenderPageAsync(
            PageInfo pageInfo,
            Graphics graphics,
            RenderContext renderContext);

        /// <summary>
        /// 渲染页面到位图
        /// </summary>
        /// <param name="pageInfo">页面信息</param>
        /// <param name="width">输出位图宽度</param>
        /// <param name="height">输出位图高度</param>
        /// <param name="renderContext">渲染上下文</param>
        /// <returns>渲染后的位图</returns>
        Task<Bitmap> RenderToBitmapAsync(
            PageInfo pageInfo,
            int width,
            int height,
            RenderContext? renderContext = null);

        /// <summary>
        /// 生成页面缩略图
        /// </summary>
        /// <param name="pageInfo">页面信息</param>
        /// <param name="thumbnailSize">缩略图尺寸</param>
        /// <returns>缩略图位图</returns>
        Task<Bitmap> GenerateThumbnailAsync(
            PageInfo pageInfo,
            Size thumbnailSize);

        /// <summary>
        /// 测试指定坐标点击中的对象
        /// </summary>
        /// <param name="pageInfo">页面信息</param>
        /// <param name="point">测试点坐标</param>
        /// <param name="renderContext">渲染上下文</param>
        /// <returns>命中的内容对象</returns>
        Task<ContentObject> HitTestAsync(
            PageInfo pageInfo,
            Point point,
            RenderContext renderContext);
    }

    /// <summary>
    /// 渲染结果
    /// </summary>
    public class RenderResult
    {
        /// <summary>
        /// 渲染是否成功
        /// </summary>
        public bool Success { get; set; }

        /// <summary>
        /// 渲染耗时
        /// </summary>
        public TimeSpan Duration { get; set; }

        /// <summary>
        /// 已渲染对象数量
        /// </summary>
        public int ObjectsRendered { get; set; }

        /// <summary>
        /// 渲染错误列表
        /// </summary>
        public List<RenderError> Errors { get; set; } = new List<RenderError>();

        /// <summary>
        /// 渲染性能指标
        /// </summary>
        public RenderMetrics? Metrics { get; set; }
    }

    /// <summary>
    /// 渲染错误
    /// </summary>
    public class RenderError
    {
        /// <summary>
        /// 对象ID
        /// </summary>
        public string ObjectId { get; set; } = string.Empty;

        /// <summary>
        /// 错误类型
        /// </summary>
        public RenderErrorType Type { get; set; }

        /// <summary>
        /// 错误消息
        /// </summary>
        public string Message { get; set; } = string.Empty;

        /// <summary>
        /// 异常对象
        /// </summary>
        public Exception? Exception { get; set; }
    }

    /// <summary>
    /// 渲染错误类型
    /// </summary>
    public enum RenderErrorType
    {
        /// <summary>
        /// 对象未找到
        /// </summary>
        ObjectNotFound,

        /// <summary>
        /// 资源缺失
        /// </summary>
        ResourceMissing,

        /// <summary>
        /// 数据无效
        /// </summary>
        InvalidData,

        /// <summary>
        /// 渲染失败
        /// </summary>
        RenderingFailed,

        /// <summary>
        /// 内存不足
        /// </summary>
        OutOfMemory
    }
}
