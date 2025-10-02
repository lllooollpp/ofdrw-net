using System;

namespace OfdrwNet.Core.Diagnostics;

/// <summary>
/// 内存快照记录。
/// </summary>
public sealed class MemorySnapshot
{
    /// <summary>
    /// 采集时间（UTC）。
    /// </summary>
    public DateTime Timestamp { get; init; } = DateTime.UtcNow;

    /// <summary>
    /// 当前分配内存（MB）。
    /// </summary>
    public double AllocatedMB { get; init; }

    /// <summary>
    /// 阈值（MB）。
    /// </summary>
    public double ThresholdMB { get; init; }

    /// <summary>
    /// 当次决策。
    /// </summary>
    public MemoryAction Action { get; init; } = MemoryAction.Sampled;

    /// <summary>
    /// 备注信息。
    /// </summary>
    public string? Notes { get; init; }

    /// <summary>
    /// 当前负载是否超出阈值。
    /// </summary>
    public bool ExceedsThreshold => AllocatedMB > ThresholdMB;

    /// <summary>
    /// 计算相对占比。
    /// </summary>
    public double Ratio => ThresholdMB <= 0 ? 0 : AllocatedMB / ThresholdMB;

    /// <inheritdoc />
    public override string ToString()
    {
        return $"MemorySnapshot[{AllocatedMB:0.##}MB/{ThresholdMB:0.##}MB, action={Action}]";
    }
}

/// <summary>
/// 内存守护动作。
/// </summary>
public enum MemoryAction
{
    /// <summary>
    /// 仅采样记录。
    /// </summary>
    Sampled,

    /// <summary>
    /// 触发分片。
    /// </summary>
    Segment,

    /// <summary>
    /// 请求终止。
    /// </summary>
    Abort
}
