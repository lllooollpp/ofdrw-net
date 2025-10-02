using System;

namespace OfdrwNet.Core.Compatibility;

/// <summary>
/// 记录一次兼容性降级操作。
/// </summary>
public sealed class DowngradeAction
{
    /// <summary>
    /// 被降级的特性名称。
    /// </summary>
    public string Feature { get; init; } = string.Empty;

    /// <summary>
    /// 目标阅读器或兼容级别。
    /// </summary>
    public string TargetReader { get; init; } = string.Empty;

    /// <summary>
    /// 降级方法。
    /// </summary>
    public DowngradeMethod Method { get; init; } = DowngradeMethod.Unknown;

    /// <summary>
    /// 降级原因描述。
    /// </summary>
    public string Reason { get; init; } = string.Empty;

    /// <summary>
    /// 发生页码。
    /// </summary>
    public int? Page { get; init; }

    /// <summary>
    /// 附加上下文信息。
    /// </summary>
    public string? Context { get; init; }

    /// <summary>
    /// 时间戳（UTC）。
    /// </summary>
    public DateTime Timestamp { get; init; } = DateTime.UtcNow;

    /// <inheritdoc />
    public override string ToString()
    {
        return $"Downgrade[{Feature} -> {Method} ({TargetReader})]";
    }
}

/// <summary>
/// 降级方式。
/// </summary>
public enum DowngradeMethod
{
    /// <summary>
    /// 未知或未指定。
    /// </summary>
    Unknown,

    /// <summary>
    /// 栅格化。
    /// </summary>
    Rasterize,

    /// <summary>
    /// 删除特性。
    /// </summary>
    Remove,

    /// <summary>
    /// 替换为降级特性。
    /// </summary>
    Replace,

    /// <summary>
    /// 降级分辨率或精度。
    /// </summary>
    ReduceQuality
}
