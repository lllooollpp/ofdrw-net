using System;

namespace OfdrwNet.Core.Versioning;

/// <summary>
/// 表示版本链中的一个版本条目。
/// </summary>
public sealed class VersionEntry
{
    /// <summary>
    /// 版本标识符。
    /// </summary>
    public string VersionId { get; init; } = string.Empty;

    /// <summary>
    /// 基准版本哈希（上一版本或合并后的基线）。
    /// </summary>
    public string BaseHash { get; init; } = string.Empty;

    /// <summary>
    /// 累计大小（字节）。
    /// </summary>
    public long CumulativeSizeBytes { get; init; }

    /// <summary>
    /// 创建时间（UTC）。
    /// </summary>
    public DateTime CreatedAt { get; init; }

    /// <summary>
    /// 是否通过合并产生。
    /// </summary>
    public bool IsMergeCommit { get; init; }

    /// <summary>
    /// 可选描述。
    /// </summary>
    public string? Description { get; init; }

    /// <summary>
    /// 版本作者。
    /// </summary>
    public string? Author { get; init; }

    /// <summary>
    /// 关联的增量大小（字节）。
    /// </summary>
    public long DeltaSizeBytes { get; init; }

    /// <summary>
    /// 转换为易读字符串。
    /// </summary>
    public override string ToString()
    {
        return $"VersionEntry[{VersionId}, base={BaseHash}, size={CumulativeSizeBytes}, merge={IsMergeCommit}]";
    }
}
