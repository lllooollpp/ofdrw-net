using System;
using System.Collections.Generic;
using System.Drawing;

namespace OfdrwNet.WinFormsDemo.Viewer.Models
{
    /// <summary>
    /// OFD文档模型，表示已加载的文档信息
    /// </summary>
    public record DocumentModel
    {
        /// <summary>
        /// 文档标识符
        /// </summary>
        public string Id { get; init; } = string.Empty;

        /// <summary>
        /// 页面总数
        /// </summary>
        public int PageCount { get; init; }

        /// <summary>
        /// 文档元数据
        /// </summary>
        public Dictionary<string, object> Metadata { get; init; } = new();

        /// <summary>
        /// 文档路径
        /// </summary>
        public string FilePath { get; init; } = string.Empty;
    }

    /// <summary>
    /// 页面模型，表示单个页面的基本信息
    /// </summary>
    public record PageModel
    {
        /// <summary>
        /// 页面索引（从0开始）
        /// </summary>
        public int Index { get; init; }

        /// <summary>
        /// 页面尺寸
        /// </summary>
        public SizeF Size { get; init; }

        /// <summary>
        /// 层信息摘要
        /// </summary>
        public List<string> Layers { get; init; } = new();

        /// <summary>
        /// 对象数量摘要
        /// </summary>
        public Dictionary<string, int> ObjectCounts { get; init; } = new();
    }

    /// <summary>
    /// 渲染请求，描述一次页面渲染所需的参数
    /// </summary>
    public record RenderRequest
    {
        /// <summary>
        /// 页面索引
        /// </summary>
        public int PageIndex { get; init; }

        /// <summary>
        /// 缩放比例
        /// </summary>
        public float Zoom { get; init; } = 1.0f;

        /// <summary>
        /// 视窗尺寸
        /// </summary>
        public Size ViewportSize { get; init; }

        /// <summary>
        /// 缓存策略
        /// </summary>
        public CachePolicy CachePolicy { get; init; } = CachePolicy.Default;

        /// <summary>
        /// 取消令牌
        /// </summary>
        public System.Threading.CancellationToken Token { get; init; } = default;
    }

    /// <summary>
    /// 渲染结果，包含渲染输出和相关指标
    /// </summary>
    public record RenderResult
    {
        /// <summary>
        /// 渲染的位图引用
        /// </summary>
        public Bitmap? RenderedBitmap { get; init; }

        /// <summary>
        /// 渲染指标
        /// </summary>
        public RenderMetrics Metrics { get; init; } = new();

        /// <summary>
        /// 诊断信息
        /// </summary>
        public List<string> Diagnostics { get; init; } = new();
    }

    /// <summary>
    /// 缓存条目，表示已缓存的页面数据
    /// </summary>
    public record CacheEntry
    {
        /// <summary>
        /// 缓存键（页面+缩放）
        /// </summary>
        public string Key { get; init; } = string.Empty;

        /// <summary>
        /// 位图引用
        /// </summary>
        public Bitmap? BitmapRef { get; init; }

        /// <summary>
        /// 创建时间
        /// </summary>
        public DateTime CreatedAt { get; init; } = DateTime.Now;

        /// <summary>
        /// 内存占用字节数
        /// </summary>
        public long MemoryBytes { get; init; }
    }

    /// <summary>
    /// 遥测事件，用于性能和行为跟踪
    /// </summary>
    public record TelemetryEvent
    {
        /// <summary>
        /// 事件名称
        /// </summary>
        public string Name { get; init; } = string.Empty;

        /// <summary>
        /// 时间戳
        /// </summary>
        public DateTime Timestamp { get; init; } = DateTime.Now;

        /// <summary>
        /// 持续时间（毫秒）
        /// </summary>
        public double Duration { get; init; }

        /// <summary>
        /// 事件标签
        /// </summary>
        public Dictionary<string, object> Tags { get; init; } = new();
    }

    /// <summary>
    /// 字体回退规则
    /// </summary>
    public record FontFallbackRule
    {
        /// <summary>
        /// 字体模式匹配
        /// </summary>
        public string Pattern { get; init; } = string.Empty;

        /// <summary>
        /// 替代字体
        /// </summary>
        public string SubstituteFont { get; init; } = string.Empty;
    }

    /// <summary>
    /// 渲染指标
    /// </summary>
    public record RenderMetrics
    {
        /// <summary>
        /// 解析耗时（毫秒）
        /// </summary>
        public double ParseTime { get; init; }

        /// <summary>
        /// 布局耗时（毫秒）
        /// </summary>
        public double LayoutTime { get; init; }

        /// <summary>
        /// 绘制耗时（毫秒）
        /// </summary>
        public double PaintTime { get; init; }

        /// <summary>
        /// 总耗时（毫秒）
        /// </summary>
        public double TotalTime { get; init; }
    }

    /// <summary>
    /// 缓存策略枚举
    /// </summary>
    public enum CachePolicy
    {
        /// <summary>
        /// 默认缓存策略
        /// </summary>
        Default,

        /// <summary>
        /// 强制不使用缓存
        /// </summary>
        NoCache,

        /// <summary>
        /// 强制使用缓存
        /// </summary>
        ForceCache
    }
}
