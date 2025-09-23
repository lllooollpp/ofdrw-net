using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Text;
using System.Threading.Tasks;

namespace OfdrwNet.Reader
{
    /// <summary>
    /// 文档查看器配置管理接口
    /// </summary>
    public interface IDocumentViewerConfiguration
    {
        /// <summary>
        /// 渲染配置
        /// </summary>
        RenderingConfiguration Rendering { get; set; }

        /// <summary>
        /// 缓存配置
        /// </summary>
        CacheConfiguration Cache { get; set; }

        /// <summary>
        /// 导航配置
        /// </summary>
        NavigationConfiguration Navigation { get; set; }

        /// <summary>
        /// 性能配置
        /// </summary>
        PerformanceConfiguration Performance { get; set; }

        /// <summary>
        /// 从配置文件加载
        /// </summary>
        /// <param name="configPath">配置文件路径</param>
        Task LoadFromFileAsync(string configPath);

        /// <summary>
        /// 保存到配置文件
        /// </summary>
        /// <param name="configPath">配置文件路径</param>
        Task SaveToFileAsync(string configPath);
    }

    /// <summary>
    /// 渲染配置
    /// </summary>
    public class RenderingConfiguration
    {
        /// <summary>
        /// 文本渲染提示
        /// </summary>
        public TextRenderingHint TextRenderingHint { get; set; } = TextRenderingHint.AntiAlias;

        /// <summary>
        /// 平滑模式
        /// </summary>
        public SmoothingMode SmoothingMode { get; set; } = SmoothingMode.HighQuality;

        /// <summary>
        /// 插值模式
        /// </summary>
        public InterpolationMode InterpolationMode { get; set; } = InterpolationMode.HighQualityBicubic;

        /// <summary>
        /// 启用矢量缓存
        /// </summary>
        public bool EnableVectorCaching { get; set; } = true;

        /// <summary>
        /// 默认DPI
        /// </summary>
        public float DefaultDpi { get; set; } = 96.0f;

        /// <summary>
        /// 背景颜色
        /// </summary>
        public Color BackgroundColor { get; set; } = Color.White;

        /// <summary>
        /// 启用高DPI支持
        /// </summary>
        public bool EnableHighDpiSupport { get; set; } = true;
    }

    /// <summary>
    /// 缓存配置
    /// </summary>
    public class CacheConfiguration
    {
        /// <summary>
        /// 最大内存使用量(字节)
        /// </summary>
        public long MaxMemoryUsage { get; set; } = 200 * 1024 * 1024; // 200MB

        /// <summary>
        /// 最大缓存页面数
        /// </summary>
        public int MaxCachedPages { get; set; } = 10;

        /// <summary>
        /// 缩略图缓存大小
        /// </summary>
        public int ThumbnailCacheSize { get; set; } = 50;

        /// <summary>
        /// 自动清理间隔
        /// </summary>
        public TimeSpan AutoCleanupInterval { get; set; } = TimeSpan.FromMinutes(5);

        /// <summary>
        /// 启用磁盘缓存
        /// </summary>
        public bool EnableDiskCache { get; set; } = false;

        /// <summary>
        /// 磁盘缓存路径
        /// </summary>
        public string DiskCachePath { get; set; } = string.Empty;

        /// <summary>
        /// 磁盘缓存大小限制(字节)
        /// </summary>
        public long DiskCacheSizeLimit { get; set; } = 1024 * 1024 * 1024; // 1GB
    }

    /// <summary>
    /// 导航配置
    /// </summary>
    public class NavigationConfiguration
    {
        /// <summary>
        /// 启用页面预加载
        /// </summary>
        public bool EnablePagePreload { get; set; } = true;

        /// <summary>
        /// 预加载页面数量
        /// </summary>
        public int PreloadPageCount { get; set; } = 2;

        /// <summary>
        /// 启用缩略图
        /// </summary>
        public bool EnableThumbnails { get; set; } = true;

        /// <summary>
        /// 缩略图尺寸
        /// </summary>
        public Size ThumbnailSize { get; set; } = new Size(150, 200);

        /// <summary>
        /// 导航动画持续时间
        /// </summary>
        public TimeSpan AnimationDuration { get; set; } = TimeSpan.FromMilliseconds(300);

        /// <summary>
        /// 启用鼠标滚轮缩放
        /// </summary>
        public bool EnableMouseWheelZoom { get; set; } = true;

        /// <summary>
        /// 缩放步长
        /// </summary>
        public double ZoomStep { get; set; } = 0.25;

        /// <summary>
        /// 最小缩放级别
        /// </summary>
        public double MinZoomLevel { get; set; } = 0.1;

        /// <summary>
        /// 最大缩放级别
        /// </summary>
        public double MaxZoomLevel { get; set; } = 5.0;
    }

    /// <summary>
    /// 性能配置
    /// </summary>
    public class PerformanceConfiguration
    {
        /// <summary>
        /// 启用并行渲染
        /// </summary>
        public bool EnableParallelRendering { get; set; } = true;

        /// <summary>
        /// 渲染线程数
        /// </summary>
        public int RenderingThreads { get; set; } = Environment.ProcessorCount;

        /// <summary>
        /// 启用后台预加载
        /// </summary>
        public bool BackgroundPreloading { get; set; } = true;

        /// <summary>
        /// 渲染超时时间
        /// </summary>
        public TimeSpan RenderTimeout { get; set; } = TimeSpan.FromSeconds(10);

        /// <summary>
        /// 启用性能监控
        /// </summary>
        public bool EnablePerformanceMonitoring { get; set; } = false;

        /// <summary>
        /// 内存压力阈值(百分比)
        /// </summary>
        public double MemoryPressureThreshold { get; set; } = 0.8;

        /// <summary>
        /// 启用低内存模式
        /// </summary>
        public bool EnableLowMemoryMode { get; set; } = false;

        /// <summary>
        /// 垃圾回收间隔
        /// </summary>
        public TimeSpan GarbageCollectionInterval { get; set; } = TimeSpan.FromMinutes(2);
    }
}
