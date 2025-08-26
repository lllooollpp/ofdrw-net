using System;
using System.Collections.Generic;

namespace OfdrwNet;

/// <summary>
/// OFDRW.NET 全局配置类
/// 管理库的全局设置和配置选项
/// </summary>
public static class OfdrwConfiguration
{
    #region 版本信息

    /// <summary>
    /// OFDRW.NET 版本
    /// </summary>
    public const string Version = "1.0.0";

    /// <summary>
    /// 支持的OFD标准版本
    /// </summary>
    public const string OfdStandardVersion = "GB/T 33190-2016";

    #endregion

    #region 默认配置

    /// <summary>
    /// 默认字体
    /// </summary>
    public static string DefaultFont { get; set; } = "SimSun";

    /// <summary>
    /// 默认字体大小 (单位：mm)
    /// </summary>
    public static double DefaultFontSize { get; set; } = 3.5;

    /// <summary>
    /// 默认页面大小
    /// </summary>
    public static PageLayout DefaultPageLayout { get; set; } = PageLayout.A4();

    /// <summary>
    /// 默认文档创建者
    /// </summary>
    public static string DefaultCreator { get; set; } = "OFDRW.NET";

    /// <summary>
    /// 默认文档创建工具版本
    /// </summary>
    public static string DefaultCreatorVersion { get; set; } = Version;

    #endregion

    #region 字体配置

    /// <summary>
    /// 字体映射字典 - 将字体名称映射到系统字体
    /// </summary>
    public static Dictionary<string, string> FontMappings { get; } = new()
    {
        // 常用中文字体映射
        { "宋体", "SimSun" },
        { "黑体", "SimHei" },
        { "楷体", "KaiTi" },
        { "仿宋", "FangSong" },
        { "微软雅黑", "Microsoft YaHei" },
        { "新宋体", "NSimSun" },
        
        // 英文字体映射
        { "Times New Roman", "Times New Roman" },
        { "Arial", "Arial" },
        { "Calibri", "Calibri" },
        { "Tahoma", "Tahoma" },
        
        // 通用映射
        { "serif", "SimSun" },
        { "sans-serif", "Microsoft YaHei" },
        { "monospace", "Consolas" }
    };

    /// <summary>
    /// 添加字体映射
    /// </summary>
    /// <param name="fontName">字体名称</param>
    /// <param name="systemFontName">系统字体名称</param>
    public static void AddFontMapping(string fontName, string systemFontName)
    {
        FontMappings[fontName] = systemFontName;
    }

    /// <summary>
    /// 获取映射后的字体名称
    /// </summary>
    /// <param name="fontName">原始字体名称</param>
    /// <returns>映射后的字体名称</returns>
    public static string GetMappedFont(string fontName)
    {
        return FontMappings.TryGetValue(fontName, out string? mappedFont) ? mappedFont : fontName;
    }

    #endregion

    #region 性能配置

    /// <summary>
    /// 是否启用并行处理
    /// </summary>
    public static bool EnableParallelProcessing { get; set; } = true;

    /// <summary>
    /// 最大并行度
    /// </summary>
    public static int MaxDegreeOfParallelism { get; set; } = Environment.ProcessorCount;

    /// <summary>
    /// 内存缓存大小 (MB)
    /// </summary>
    public static int MemoryCacheSizeMB { get; set; } = 100;

    /// <summary>
    /// 是否启用文件缓存
    /// </summary>
    public static bool EnableFileCache { get; set; } = true;

    /// <summary>
    /// 临时文件目录
    /// </summary>
    public static string TempDirectory { get; set; } = Path.GetTempPath();

    #endregion

    #region 渲染配置

    /// <summary>
    /// 默认DPI (用于图片渲染)
    /// </summary>
    public static int DefaultDpi { get; set; } = 300;

    /// <summary>
    /// 图片压缩质量 (0-100)
    /// </summary>
    public static int ImageCompressionQuality { get; set; } = 85;

    /// <summary>
    /// 是否启用抗锯齿
    /// </summary>
    public static bool EnableAntiAliasing { get; set; } = true;

    /// <summary>
    /// 文本渲染提示
    /// </summary>
    public static TextRenderingHint TextRenderingHint { get; set; } = TextRenderingHint.AntiAlias;

    #endregion

    #region 日志配置

    /// <summary>
    /// 是否启用日志
    /// </summary>
    public static bool EnableLogging { get; set; } = false;

    /// <summary>
    /// 日志级别
    /// </summary>
    public static LogLevel LogLevel { get; set; } = LogLevel.Information;

    /// <summary>
    /// 日志输出目录
    /// </summary>
    public static string? LogDirectory { get; set; }

    #endregion

    #region 安全配置

    /// <summary>
    /// 是否启用严格模式 (更严格的标准符合性检查)
    /// </summary>
    public static bool StrictMode { get; set; } = false;

    /// <summary>
    /// 允许的最大文档大小 (MB)
    /// </summary>
    public static long MaxDocumentSizeMB { get; set; } = 1024; // 1GB

    /// <summary>
    /// 允许的最大页面数
    /// </summary>
    public static int MaxPageCount { get; set; } = 10000;

    #endregion

    #region 扩展配置

    /// <summary>
    /// 自定义配置字典
    /// </summary>
    public static Dictionary<string, object> CustomSettings { get; } = new();

    /// <summary>
    /// 设置自定义配置
    /// </summary>
    /// <typeparam name="T">配置值类型</typeparam>
    /// <param name="key">配置键</param>
    /// <param name="value">配置值</param>
    public static void SetCustomSetting<T>(string key, T value)
    {
        CustomSettings[key] = value!;
    }

    /// <summary>
    /// 获取自定义配置
    /// </summary>
    /// <typeparam name="T">配置值类型</typeparam>
    /// <param name="key">配置键</param>
    /// <param name="defaultValue">默认值</param>
    /// <returns>配置值</returns>
    public static T GetCustomSetting<T>(string key, T defaultValue = default!)
    {
        if (CustomSettings.TryGetValue(key, out object? value) && value is T typedValue)
        {
            return typedValue;
        }
        return defaultValue;
    }

    #endregion

    #region 初始化方法

    /// <summary>
    /// 重置所有配置为默认值
    /// </summary>
    public static void ResetToDefaults()
    {
        DefaultFont = "SimSun";
        DefaultFontSize = 3.5;
        DefaultPageLayout = PageLayout.A4();
        DefaultCreator = "OFDRW.NET";
        DefaultCreatorVersion = Version;
        
        EnableParallelProcessing = true;
        MaxDegreeOfParallelism = Environment.ProcessorCount;
        MemoryCacheSizeMB = 100;
        EnableFileCache = true;
        TempDirectory = Path.GetTempPath();
        
        DefaultDpi = 300;
        ImageCompressionQuality = 85;
        EnableAntiAliasing = true;
        TextRenderingHint = TextRenderingHint.AntiAlias;
        
        EnableLogging = false;
        LogLevel = LogLevel.Information;
        LogDirectory = null;
        
        StrictMode = false;
        MaxDocumentSizeMB = 1024;
        MaxPageCount = 10000;
        
        CustomSettings.Clear();
    }

    /// <summary>
    /// 从配置文件加载配置
    /// </summary>
    /// <param name="configFilePath">配置文件路径</param>
    public static void LoadFromFile(string configFilePath)
    {
        // 实现从配置文件加载配置的逻辑
        // 可以支持JSON、XML或其他格式
    }

    /// <summary>
    /// 保存配置到文件
    /// </summary>
    /// <param name="configFilePath">配置文件路径</param>
    public static void SaveToFile(string configFilePath)
    {
        // 实现保存配置到文件的逻辑
    }

    #endregion
}

/// <summary>
/// 文本渲染提示枚举
/// </summary>
public enum TextRenderingHint
{
    /// <summary>
    /// 系统默认
    /// </summary>
    SystemDefault,
    
    /// <summary>
    /// 单色
    /// </summary>
    SingleBitPerPixel,
    
    /// <summary>
    /// 单色网格拟合
    /// </summary>
    SingleBitPerPixelGridFit,
    
    /// <summary>
    /// 抗锯齿
    /// </summary>
    AntiAlias,
    
    /// <summary>
    /// 抗锯齿网格拟合
    /// </summary>
    AntiAliasGridFit,
    
    /// <summary>
    /// ClearType网格拟合
    /// </summary>
    ClearTypeGridFit
}

/// <summary>
/// 日志级别枚举
/// </summary>
public enum LogLevel
{
    /// <summary>
    /// 跟踪
    /// </summary>
    Trace,
    
    /// <summary>
    /// 调试
    /// </summary>
    Debug,
    
    /// <summary>
    /// 信息
    /// </summary>
    Information,
    
    /// <summary>
    /// 警告
    /// </summary>
    Warning,
    
    /// <summary>
    /// 错误
    /// </summary>
    Error,
    
    /// <summary>
    /// 严重错误
    /// </summary>
    Critical,
    
    /// <summary>
    /// 无日志
    /// </summary>
    None
}