using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Drawing;
using System.Linq;

namespace OfdrwNet.Core.Resources;

/// <summary>
/// 表示多媒体资源（音频/视频等）。
/// </summary>
public sealed class MediaResource
{
    private readonly IReadOnlyDictionary<string, string> _metadata;

    /// <summary>
    /// 初始化 <see cref="MediaResource"/>。
    /// </summary>
    public MediaResource(
        string id,
        string mediaType,
        string? fileRef = null,
        bool autoPlay = false,
        RectangleF? rect = null,
        TimeSpan? duration = null,
        IDictionary<string, string>? metadata = null)
    {
        if (string.IsNullOrWhiteSpace(id))
        {
            throw new ArgumentException("Media id cannot be null or whitespace.", nameof(id));
        }

        if (string.IsNullOrWhiteSpace(mediaType))
        {
            throw new ArgumentException("Media type cannot be null or whitespace.", nameof(mediaType));
        }

        if (!mediaType.Contains('/', StringComparison.Ordinal))
        {
            throw new ArgumentException("Media type should be a valid MIME type.", nameof(mediaType));
        }

        Id = id.Trim();
        MediaType = mediaType.Trim().ToLowerInvariant();
        FileRef = string.IsNullOrWhiteSpace(fileRef) ? null : fileRef.Trim();
        AutoPlay = autoPlay;
        Rectangle = rect;
        Duration = duration;
        _metadata = new ReadOnlyDictionary<string, string>(metadata?.ToDictionary(static kv => kv.Key, static kv => kv.Value) ?? new Dictionary<string, string>());
    }

    /// <summary>
    /// 多媒体资源唯一标识。
    /// </summary>
    public string Id { get; }

    /// <summary>
    /// 媒体类型（MIME）。
    /// </summary>
    public string MediaType { get; }

    /// <summary>
    /// 文件引用。
    /// </summary>
    public string? FileRef { get; }

    /// <summary>
    /// 是否自动播放。
    /// </summary>
    public bool AutoPlay { get; }

    /// <summary>
    /// 呈现区域。
    /// </summary>
    public RectangleF? Rectangle { get; }

    /// <summary>
    /// 媒体时长。
    /// </summary>
    public TimeSpan? Duration { get; }

    /// <summary>
    /// 附加元数据。
    /// </summary>
    public IReadOnlyDictionary<string, string> Metadata => _metadata;

    /// <summary>
    /// 创建附带新文件引用的副本。
    /// </summary>
    public MediaResource WithFileRef(string fileRef)
    {
        if (string.IsNullOrWhiteSpace(fileRef))
        {
            throw new ArgumentException("File reference cannot be null or whitespace.", nameof(fileRef));
        }

        return new MediaResource(Id, MediaType, fileRef, AutoPlay, Rectangle, Duration, _metadata.ToDictionary(static kv => kv.Key, static kv => kv.Value));
    }

    /// <summary>
    /// 创建附带新区域的副本。
    /// </summary>
    public MediaResource WithRectangle(RectangleF rect)
    {
        return new MediaResource(Id, MediaType, FileRef, AutoPlay, rect, Duration, _metadata.ToDictionary(static kv => kv.Key, static kv => kv.Value));
    }

    /// <inheritdoc />
    public override string ToString()
    {
        return $"MediaResource[{Id}, type={MediaType}, autoPlay={AutoPlay}]";
    }
}
