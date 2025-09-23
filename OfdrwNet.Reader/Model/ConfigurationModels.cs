using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Text;
using System.IO;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace OfdrwNet.Reader.Model
{
    /// <summary>
    /// OFD阅读器配置管理
    /// 统一管理渲染、缓存、导航和性能等相关配置
    /// </summary>
    public class OfdrwConfiguration
    {
        /// <summary>
        /// 渲染配置
        /// </summary>
        public RenderConfiguration Rendering { get; set; } = new RenderConfiguration();

        /// <summary>
        /// 缓存配置
        /// </summary>
        public CacheConfiguration Caching { get; set; } = new CacheConfiguration();

        /// <summary>
        /// 导航配置
        /// </summary>
        public NavigationConfiguration Navigation { get; set; } = new NavigationConfiguration();

        /// <summary>
        /// 性能配置
        /// </summary>
        public PerformanceConfiguration Performance { get; set; } = new PerformanceConfiguration();

        /// <summary>
        /// 用户界面配置
        /// </summary>
        public UIConfiguration UI { get; set; } = new UIConfiguration();

        /// <summary>
        /// 调试配置
        /// </summary>
        public DebugConfiguration Debug { get; set; } = new DebugConfiguration();

        /// <summary>
        /// 配置文件版本
        /// </summary>
        public string Version { get; set; } = "1.0.0";

        /// <summary>
        /// 最后修改时间
        /// </summary>
        public DateTime LastModified { get; set; } = DateTime.UtcNow;

        /// <summary>
        /// 序列化为JSON
        /// </summary>
        /// <returns>JSON字符串</returns>
        public string ToJson()
        {
            var options = new JsonSerializerOptions
            {
                WriteIndented = true,
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
                Converters = { new JsonStringEnumConverter() }
            };
            return JsonSerializer.Serialize(this, options);
        }

        /// <summary>
        /// 从JSON反序列化
        /// </summary>
        /// <param name="json">JSON字符串</param>
        /// <returns>配置对象</returns>
        public static OfdrwConfiguration? FromJson(string json)
        {
            try
            {
                var options = new JsonSerializerOptions
                {
                    PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
                    Converters = { new JsonStringEnumConverter() }
                };
                return JsonSerializer.Deserialize<OfdrwConfiguration>(json, options);
            }
            catch
            {
                return null;
            }
        }

        /// <summary>
        /// 保存到文件
        /// </summary>
        /// <param name="filePath">文件路径</param>
        public void SaveToFile(string filePath)
        {
            LastModified = DateTime.UtcNow;
            var json = ToJson();
            File.WriteAllText(filePath, json);
        }

        /// <summary>
        /// 从文件加载
        /// </summary>
        /// <param name="filePath">文件路径</param>
        /// <returns>配置对象</returns>
        public static OfdrwConfiguration? LoadFromFile(string filePath)
        {
            try
            {
                if (!File.Exists(filePath))
                    return new OfdrwConfiguration();

                var json = File.ReadAllText(filePath);
                return FromJson(json) ?? new OfdrwConfiguration();
            }
            catch
            {
                return new OfdrwConfiguration();
            }
        }

        /// <summary>
        /// 重置为默认配置
        /// </summary>
        public void ResetToDefaults()
        {
            Rendering = new RenderConfiguration();
            Caching = new CacheConfiguration();
            Navigation = new NavigationConfiguration();
            Performance = new PerformanceConfiguration();
            UI = new UIConfiguration();
            Debug = new DebugConfiguration();
            LastModified = DateTime.UtcNow;
        }

        /// <summary>
        /// 验证配置有效性
        /// </summary>
        /// <returns>验证结果</returns>
        public ConfigurationValidationResult Validate()
        {
            var result = new ConfigurationValidationResult();

            // 验证渲染配置
            if (Rendering.DefaultDpi <= 0)
                result.AddError("Rendering.DefaultDpi", "DPI值必须大于0");

            if (Rendering.MaxZoomLevel <= Rendering.MinZoomLevel)
                result.AddError("Rendering.ZoomLevel", "最大缩放级别必须大于最小缩放级别");

            // 验证缓存配置
            if (Caching.MaxMemoryUsage <= 0)
                result.AddError("Caching.MaxMemoryUsage", "最大内存使用量必须大于0");

            if (Caching.PageCacheSize <= 0)
                result.AddError("Caching.PageCacheSize", "页面缓存大小必须大于0");

            // 验证性能配置
            if (Performance.MaxWorkerThreads <= 0)
                result.AddError("Performance.MaxWorkerThreads", "最大工作线程数必须大于0");

            return result;
        }
    }

    /// <summary>
    /// 渲染配置
    /// </summary>
    public class RenderConfiguration
    {
        /// <summary>
        /// 默认DPI
        /// </summary>
        public int DefaultDpi { get; set; } = 96;

        /// <summary>
        /// 默认缩放级别
        /// </summary>
        public double DefaultZoomLevel { get; set; } = 1.0;

        /// <summary>
        /// 最小缩放级别
        /// </summary>
        public double MinZoomLevel { get; set; } = 0.1;

        /// <summary>
        /// 最大缩放级别
        /// </summary>
        public double MaxZoomLevel { get; set; } = 10.0;

        /// <summary>
        /// 缩放步长
        /// </summary>
        public double ZoomStep { get; set; } = 0.25;

        /// <summary>
        /// 平滑模式
        /// </summary>
        public SmoothingMode SmoothingMode { get; set; } = SmoothingMode.AntiAlias;

        /// <summary>
        /// 插值模式
        /// </summary>
        public InterpolationMode InterpolationMode { get; set; } = InterpolationMode.HighQualityBicubic;

        /// <summary>
        /// 文本渲染提示
        /// </summary>
        public TextRenderingHint TextRenderingHint { get; set; } = TextRenderingHint.AntiAlias;

        /// <summary>
        /// 合成质量
        /// </summary>
        public CompositingQuality CompositingQuality { get; set; } = CompositingQuality.HighQuality;

        /// <summary>
        /// 像素偏移模式
        /// </summary>
        public PixelOffsetMode PixelOffsetMode { get; set; } = PixelOffsetMode.HighQuality;

        /// <summary>
        /// 背景颜色
        /// </summary>
        public ColorConfiguration BackgroundColor { get; set; } = new ColorConfiguration { R = 255, G = 255, B = 255, A = 255 };

        /// <summary>
        /// 是否启用子像素字体渲染
        /// </summary>
        public bool EnableSubPixelRendering { get; set; } = true;

        /// <summary>
        /// 是否启用硬件加速
        /// </summary>
        public bool EnableHardwareAcceleration { get; set; } = true;

        /// <summary>
        /// 渲染质量配置
        /// </summary>
        public Dictionary<RenderQuality, RenderQualitySettings> QualitySettings { get; set; } =
            new Dictionary<RenderQuality, RenderQualitySettings>
            {
                { RenderQuality.Low, new RenderQualitySettings { ScaleFactor = 0.5, SmoothingMode = SmoothingMode.HighSpeed } },
                { RenderQuality.Medium, new RenderQualitySettings { ScaleFactor = 1.0, SmoothingMode = SmoothingMode.AntiAlias } },
                { RenderQuality.High, new RenderQualitySettings { ScaleFactor = 1.0, SmoothingMode = SmoothingMode.HighQuality } },
                { RenderQuality.Print, new RenderQualitySettings { ScaleFactor = 2.0, SmoothingMode = SmoothingMode.HighQuality } }
            };
    }

    /// <summary>
    /// 缓存配置
    /// </summary>
    public class CacheConfiguration
    {
        /// <summary>
        /// 最大内存使用量（字节）
        /// </summary>
        public long MaxMemoryUsage { get; set; } = 500L * 1024 * 1024; // 500MB

        /// <summary>
        /// 页面缓存大小（页面数量）
        /// </summary>
        public int PageCacheSize { get; set; } = 50;

        /// <summary>
        /// 资源缓存大小（MB）
        /// </summary>
        public long ResourceCacheSize { get; set; } = 100L * 1024 * 1024; // 100MB

        /// <summary>
        /// 缓存过期时间（分钟）
        /// </summary>
        public int ExpirationTimeMinutes { get; set; } = 30;

        /// <summary>
        /// 是否启用预缓存
        /// </summary>
        public bool EnablePreCaching { get; set; } = true;

        /// <summary>
        /// 预缓存页面范围
        /// </summary>
        public int PreCacheRange { get; set; } = 3;

        /// <summary>
        /// 是否启用磁盘缓存
        /// </summary>
        public bool EnableDiskCache { get; set; } = false;

        /// <summary>
        /// 磁盘缓存路径
        /// </summary>
        public string DiskCachePath { get; set; } = Path.Combine(Path.GetTempPath(), "OfdrwCache");

        /// <summary>
        /// 磁盘缓存大小限制（MB）
        /// </summary>
        public long DiskCacheSizeLimit { get; set; } = 1024L * 1024 * 1024; // 1GB

        /// <summary>
        /// 清理策略
        /// </summary>
        public CacheCleanupStrategy CleanupStrategy { get; set; } = CacheCleanupStrategy.LeastRecentlyUsed;
    }

    /// <summary>
    /// 导航配置
    /// </summary>
    public class NavigationConfiguration
    {
        /// <summary>
        /// 默认导航模式
        /// </summary>
        public NavigationType DefaultNavigationType { get; set; } = NavigationType.SinglePage;

        /// <summary>
        /// 是否启用平滑滚动
        /// </summary>
        public bool EnableSmoothScrolling { get; set; } = true;

        /// <summary>
        /// 滚动速度
        /// </summary>
        public double ScrollSpeed { get; set; } = 1.0;

        /// <summary>
        /// 键盘快捷键配置
        /// </summary>
        public Dictionary<string, string> KeyboardShortcuts { get; set; } = new Dictionary<string, string>
        {
            { "NextPage", "PageDown" },
            { "PreviousPage", "PageUp" },
            { "ZoomIn", "Ctrl+Plus" },
            { "ZoomOut", "Ctrl+Minus" },
            { "FitToWidth", "Ctrl+1" },
            { "FitToHeight", "Ctrl+2" },
            { "ActualSize", "Ctrl+0" }
        };

        /// <summary>
        /// 鼠标手势配置
        /// </summary>
        public MouseGestureConfiguration MouseGestures { get; set; } = new MouseGestureConfiguration();

        /// <summary>
        /// 历史记录大小
        /// </summary>
        public int HistorySize { get; set; } = 100;

        /// <summary>
        /// 是否自动保存导航状态
        /// </summary>
        public bool AutoSaveNavigationState { get; set; } = true;
    }

    /// <summary>
    /// 性能配置
    /// </summary>
    public class PerformanceConfiguration
    {
        /// <summary>
        /// 最大工作线程数
        /// </summary>
        public int MaxWorkerThreads { get; set; } = Environment.ProcessorCount;

        /// <summary>
        /// 最大并发渲染数
        /// </summary>
        public int MaxConcurrentRenders { get; set; } = Environment.ProcessorCount;

        /// <summary>
        /// 渲染超时时间（秒）
        /// </summary>
        public int RenderTimeoutSeconds { get; set; } = 30;

        /// <summary>
        /// 是否启用性能监控
        /// </summary>
        public bool EnablePerformanceMonitoring { get; set; } = false;

        /// <summary>
        /// 性能监控采样间隔（毫秒）
        /// </summary>
        public int MonitoringSampleInterval { get; set; } = 1000;

        /// <summary>
        /// 内存回收阈值
        /// </summary>
        public double MemoryPressureThreshold { get; set; } = 0.8;

        /// <summary>
        /// 是否启用低内存模式
        /// </summary>
        public bool EnableLowMemoryMode { get; set; } = false;

        /// <summary>
        /// CPU使用率阈值
        /// </summary>
        public double CpuUsageThreshold { get; set; } = 0.9;
    }

    /// <summary>
    /// 用户界面配置
    /// </summary>
    public class UIConfiguration
    {
        /// <summary>
        /// 主题名称
        /// </summary>
        public string Theme { get; set; } = "Default";

        /// <summary>
        /// 语言设置
        /// </summary>
        public string Language { get; set; } = "zh-CN";

        /// <summary>
        /// 工具栏是否可见
        /// </summary>
        public bool ShowToolbar { get; set; } = true;

        /// <summary>
        /// 状态栏是否可见
        /// </summary>
        public bool ShowStatusBar { get; set; } = true;

        /// <summary>
        /// 导航面板是否可见
        /// </summary>
        public bool ShowNavigationPanel { get; set; } = true;

        /// <summary>
        /// 书签面板是否可见
        /// </summary>
        public bool ShowBookmarkPanel { get; set; } = false;

        /// <summary>
        /// 默认窗口大小
        /// </summary>
        public SizeConfiguration DefaultWindowSize { get; set; } = new SizeConfiguration { Width = 1024, Height = 768 };

        /// <summary>
        /// 是否记住窗口状态
        /// </summary>
        public bool RememberWindowState { get; set; } = true;

        /// <summary>
        /// 颜色配置
        /// </summary>
        public UIColorScheme ColorScheme { get; set; } = new UIColorScheme();
    }

    /// <summary>
    /// 调试配置
    /// </summary>
    public class DebugConfiguration
    {
        /// <summary>
        /// 是否启用调试模式
        /// </summary>
        public bool EnableDebugMode { get; set; } = false;

        /// <summary>
        /// 日志级别
        /// </summary>
        public LogLevel LogLevel { get; set; } = LogLevel.Information;

        /// <summary>
        /// 日志文件路径
        /// </summary>
        public string LogFilePath { get; set; } = Path.Combine(Path.GetTempPath(), "ofdrw-debug.log");

        /// <summary>
        /// 是否显示渲染边界
        /// </summary>
        public bool ShowRenderBounds { get; set; } = false;

        /// <summary>
        /// 是否显示性能统计
        /// </summary>
        public bool ShowPerformanceStats { get; set; } = false;

        /// <summary>
        /// 是否启用内存泄漏检测
        /// </summary>
        public bool EnableMemoryLeakDetection { get; set; } = false;

        /// <summary>
        /// 详细渲染信息
        /// </summary>
        public bool VerboseRenderingInfo { get; set; } = false;
    }

    // 辅助配置类

    /// <summary>
    /// 颜色配置
    /// </summary>
    public class ColorConfiguration
    {
        public byte R { get; set; }
        public byte G { get; set; }
        public byte B { get; set; }
        public byte A { get; set; } = 255;

        public Color ToColor()
        {
            return Color.FromArgb(A, R, G, B);
        }

        public static ColorConfiguration FromColor(Color color)
        {
            return new ColorConfiguration { R = color.R, G = color.G, B = color.B, A = color.A };
        }
    }

    /// <summary>
    /// 尺寸配置
    /// </summary>
    public class SizeConfiguration
    {
        public int Width { get; set; }
        public int Height { get; set; }

        public Size ToSize()
        {
            return new Size(Width, Height);
        }
    }

    /// <summary>
    /// 渲染质量设置
    /// </summary>
    public class RenderQualitySettings
    {
        public double ScaleFactor { get; set; } = 1.0;
        public SmoothingMode SmoothingMode { get; set; } = SmoothingMode.AntiAlias;
        public InterpolationMode InterpolationMode { get; set; } = InterpolationMode.HighQualityBicubic;
        public TextRenderingHint TextRenderingHint { get; set; } = TextRenderingHint.AntiAlias;
    }

    /// <summary>
    /// 鼠标手势配置
    /// </summary>
    public class MouseGestureConfiguration
    {
        public bool EnableMouseGestures { get; set; } = true;
        public bool EnablePanning { get; set; } = true;
        public bool EnableZoomOnWheel { get; set; } = true;
        public double WheelZoomFactor { get; set; } = 1.2;
    }

    /// <summary>
    /// UI颜色方案
    /// </summary>
    public class UIColorScheme
    {
        public ColorConfiguration BackgroundColor { get; set; } = new ColorConfiguration { R = 240, G = 240, B = 240 };
        public ColorConfiguration ForegroundColor { get; set; } = new ColorConfiguration { R = 0, G = 0, B = 0 };
        public ColorConfiguration AccentColor { get; set; } = new ColorConfiguration { R = 0, G = 120, B = 215 };
        public ColorConfiguration BorderColor { get; set; } = new ColorConfiguration { R = 200, G = 200, B = 200 };
    }

    /// <summary>
    /// 缓存清理策略
    /// </summary>
    public enum CacheCleanupStrategy
    {
        LeastRecentlyUsed,
        LeastFrequentlyUsed,
        FirstInFirstOut,
        TimeBasedExpiration
    }

    /// <summary>
    /// 日志级别
    /// </summary>
    public enum LogLevel
    {
        Trace,
        Debug,
        Information,
        Warning,
        Error,
        Critical
    }

    /// <summary>
    /// 配置验证结果
    /// </summary>
    public class ConfigurationValidationResult
    {
        public bool IsValid => Errors.Count == 0;
        public List<ConfigurationError> Errors { get; } = new List<ConfigurationError>();

        public void AddError(string property, string message)
        {
            Errors.Add(new ConfigurationError { Property = property, Message = message });
        }
    }

    /// <summary>
    /// 配置错误
    /// </summary>
    public class ConfigurationError
    {
        public string Property { get; set; } = string.Empty;
        public string Message { get; set; } = string.Empty;
    }
}
