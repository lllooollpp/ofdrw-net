using System.Collections.Generic;

namespace OfdrwNet.Converter.Domain;

/// <summary>
/// 单页处理上下文，跟踪每页的资源和识别结果
/// </summary>
public sealed class PageContext
{
    /// <summary>
    /// 页码（从 1 开始）
    /// </summary>
    public required int PageNumber { get; init; }

    /// <summary>
    /// 源对象列表（文本、路径、图像等）
    /// </summary>
    public IList<object> SourceObjects { get; init; } = new List<object>();

    /// <summary>
    /// 提取的矢量图形
    /// </summary>
    public IList<object> ExtractedVectors { get; init; } = new List<object>();

    /// <summary>
    /// 使用的字体资源
    /// </summary>
    public IList<FontResource> FontsUsed { get; init; } = new List<FontResource>();

    /// <summary>
    /// 使用的图像资源
    /// </summary>
    public IList<ImageResource> ImagesUsed { get; init; } = new List<ImageResource>();

    /// <summary>
    /// 复合对象识别结果（表格、公式等）
    /// </summary>
    public IList<CompositeResult> CompositeResults { get; init; } = new List<CompositeResult>();

    /// <summary>
    /// 表单字段集合
    /// </summary>
    public IList<FormField> FormFields { get; init; } = new List<FormField>();

    /// <summary>
    /// JavaScript 脚本信息集合
    /// </summary>
    public IList<JsScriptInfo> JavaScripts { get; init; } = new List<JsScriptInfo>();

    /// <summary>
    /// 多媒体资源集合
    /// </summary>
    public IList<MediaResource> MediaResources { get; init; } = new List<MediaResource>();

    /// <summary>
    /// 页面宽度 (mm)
    /// </summary>
    public double? Width { get; init; }

    /// <summary>
    /// 页面高度 (mm)
    /// </summary>
    public double? Height { get; init; }

    /// <summary>
    /// 页面处理是否成功
    /// </summary>
    public bool IsSuccess { get; set; } = true;

    /// <summary>
    /// 页面处理错误消息（如果失败）
    /// </summary>
    public string? ErrorMessage { get; set; }
}
