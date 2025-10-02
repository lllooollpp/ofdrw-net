using System;

namespace OfdrwNet.Converter.Domain;

/// <summary>
/// 版本链条目
/// </summary>
public sealed class VersionEntry
{
    /// <summary>
    /// 版本标识符
    /// </summary>
    public required string VersionId { get; init; }

    /// <summary>
    /// 基准文件哈希（完整 OFD 包的 SHA256）
    /// </summary>
    public required string BaseHash { get; init; }

    /// <summary>
    /// 增量大小（diff 目录的总大小，字节）
    /// </summary>
    public required long DeltaSize { get; init; }

    /// <summary>
    /// 创建时间
    /// </summary>
    public DateTime CreatedAt { get; init; } = DateTime.UtcNow;

    /// <summary>
    /// 版本描述
    /// </summary>
    public string? Description { get; init; }

    /// <summary>
    /// 父版本 ID（如果有）
    /// </summary>
    public string? ParentVersionId { get; init; }

    /// <summary>
    /// 创建者标识
    /// </summary>
    public string? CreatedBy { get; init; }

    /// <summary>
    /// 版本标签（如 v1.0, v2.0）
    /// </summary>
    public string? Tag { get; init; }
}
