using System;
using Microsoft.Extensions.Logging;

namespace OfdrwNet.Converter.Options;

/// <summary>
/// Playwright PDF 转换选项配置类
/// </summary>
public class PlaywrightConvertOptions
{
    /// <summary>是否在转换后自动清理临时文件（默认 true）</summary>
    public bool AutoCleanup { get; set; } = true;

    /// <summary>转换超时时间（默认 30 秒）</summary>
    public TimeSpan Timeout { get; set; } = TimeSpan.FromSeconds(30);

    /// <summary>日志记录器</summary>
    public ILogger? Logger { get; set; }

    /// <summary>浏览器页面选项</summary>
    public BrowserPageOptions PageOptions { get; set; } = new();
}

/// <summary>
/// 浏览器页面选项
/// </summary>
public class BrowserPageOptions
{
    /// <summary>页面视口宽度（默认 1920）</summary>
    public int ViewportWidth { get; set; } = 1920;

    /// <summary>页面视口高度（默认 1080）</summary>
    public int ViewportHeight { get; set; } = 1080;

    /// <summary>是否启用 JavaScript（默认 true）</summary>
    public bool JavaScriptEnabled { get; set; } = true;
}
