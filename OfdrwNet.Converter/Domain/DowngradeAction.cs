namespace OfdrwNet.Converter.Domain;

/// <summary>
/// 降级行为记录
/// </summary>
public sealed class DowngradeAction
{
    /// <summary>
    /// 被降级的特性名称
    /// </summary>
    public required string Feature { get; init; }

    /// <summary>
    /// 降级原因
    /// </summary>
    public required string Reason { get; init; }

    /// <summary>
    /// 降级方法（删除、替换、简化等）
    /// </summary>
    public required string Method { get; init; }

    /// <summary>
    /// 受影响的页码（如果特定于页面）
    /// </summary>
    public int? Page { get; init; }

    /// <summary>
    /// 降级详情（JSON 格式）
    /// </summary>
    public string? Details { get; init; }

    /// <summary>
    /// 降级发生时间
    /// </summary>
    public DateTime Timestamp { get; init; } = DateTime.UtcNow;

    /// <summary>
    /// 影响程度（High, Medium, Low）
    /// </summary>
    public string? Impact { get; init; }

    /// <summary>
    /// 替代方案描述
    /// </summary>
    public string? Alternative { get; init; }
}
