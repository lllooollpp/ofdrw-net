using System;

namespace OfdrwNet.Converter.Domain;

/// <summary>
/// 内存快照
/// </summary>
public sealed class MemorySnapshot
{
    /// <summary>
    /// 快照时间戳
    /// </summary>
    public DateTime Timestamp { get; init; } = DateTime.UtcNow;

    /// <summary>
    /// 已分配内存（MB）
    /// </summary>
    public required double AllocatedMB { get; init; }

    /// <summary>
    /// 内存阈值（MB）
    /// </summary>
    public required double ThresholdMB { get; init; }

    /// <summary>
    /// 触发的操作
    /// </summary>
    public required MemoryAction Action { get; init; }

    /// <summary>
    /// 额外信息（如触发页码、操作详情等）
    /// </summary>
    public string? AdditionalInfo { get; init; }

    /// <summary>
    /// GC 堆大小（MB）
    /// </summary>
    public double? GcHeapMB { get; init; }

    /// <summary>
    /// 工作集大小（MB）
    /// </summary>
    public double? WorkingSetMB { get; init; }

    /// <summary>
    /// 检查是否超过阈值
    /// </summary>
    public bool IsOverThreshold => AllocatedMB > ThresholdMB;

    /// <summary>
    /// 计算内存使用率
    /// </summary>
    public double UsageRatio => ThresholdMB > 0 ? AllocatedMB / ThresholdMB : 0.0;
}

/// <summary>
/// 内存操作类型
/// </summary>
public enum MemoryAction
{
    /// <summary>
    /// 常规检查，无操作
    /// </summary>
    None,

    /// <summary>
    /// 触发 GC
    /// </summary>
    GarbageCollect,

    /// <summary>
    /// 刷新资源到磁盘
    /// </summary>
    FlushToDisk,

    /// <summary>
    /// 中止处理
    /// </summary>
    Abort,

    /// <summary>
    /// 降低并行度
    /// </summary>
    ReduceParallelism
}
