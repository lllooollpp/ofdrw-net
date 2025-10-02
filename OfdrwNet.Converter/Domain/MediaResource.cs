namespace OfdrwNet.Converter.Domain;

/// <summary>
/// 多媒体资源描述
/// </summary>
public sealed class MediaResource
{
    /// <summary>
    /// 媒体资源唯一标识符
    /// </summary>
    public required string Id { get; init; }

    /// <summary>
    /// 媒体类型（audio, video, 3d等）
    /// </summary>
    public required string MediaType { get; init; }

    /// <summary>
    /// 资源文件引用路径
    /// </summary>
    public required string FileRef { get; init; }

    /// <summary>
    /// 是否自动播放
    /// </summary>
    public bool AutoPlay { get; init; }

    /// <summary>
    /// 媒体资源在页面上的显示区域
    /// </summary>
    public BoundingBox? Rect { get; init; }

    /// <summary>
    /// MIME 类型
    /// </summary>
    public string? MimeType { get; init; }

    /// <summary>
    /// 媒体时长（秒，仅适用于音频/视频）
    /// </summary>
    public double? Duration { get; init; }

    /// <summary>
    /// 缩略图文件引用（如果有）
    /// </summary>
    public string? ThumbnailRef { get; init; }
}
