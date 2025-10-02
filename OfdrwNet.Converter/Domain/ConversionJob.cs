using System;
using System.Collections.Generic;

namespace OfdrwNet.Converter.Domain;

/// <summary>
/// 表示一次 PDF→OFD 转换请求（可批量）
/// </summary>
public sealed class ConversionJob
{
    /// <summary>
    /// 作业唯一标识符
    /// </summary>
    public required string Id { get; init; }

    /// <summary>
    /// 输入 PDF 文件路径
    /// </summary>
    public required string InputPath { get; init; }

    /// <summary>
    /// 输出目录路径
    /// </summary>
    public required string OutputDir { get; init; }

    /// <summary>
    /// 转换选项配置
    /// </summary>
    public required ConverterOptions Options { get; init; }

    /// <summary>
    /// 当前作业状态
    /// </summary>
    public ConversionStatus Status { get; set; } = ConversionStatus.Draft;

    /// <summary>
    /// 页面处理上下文集合
    /// </summary>
    public IList<PageContext> Pages { get; init; } = new List<PageContext>();

    /// <summary>
    /// 错误/警告记录集合
    /// </summary>
    public IList<ErrorRecord> Errors { get; init; } = new List<ErrorRecord>();

    /// <summary>
    /// 降级行为记录集合
    /// </summary>
    public IList<DowngradeAction> DowngradeActions { get; init; } = new List<DowngradeAction>();

    /// <summary>
    /// 版本链条目集合
    /// </summary>
    public IList<VersionEntry> VersionEntries { get; init; } = new List<VersionEntry>();

    /// <summary>
    /// 内存快照集合
    /// </summary>
    public IList<MemorySnapshot> MemorySnapshots { get; init; } = new List<MemorySnapshot>();

    /// <summary>
    /// 作业创建时间
    /// </summary>
    public DateTime CreatedAt { get; init; } = DateTime.UtcNow;

    /// <summary>
    /// 作业开始时间
    /// </summary>
    public DateTime? StartedAt { get; set; }

    /// <summary>
    /// 作业完成时间
    /// </summary>
    public DateTime? CompletedAt { get; set; }

    /// <summary>
    /// 转换到 Running 状态
    /// </summary>
    public void Start()
    {
        if (Status != ConversionStatus.Draft)
        {
            throw new InvalidOperationException($"Cannot start job in {Status} status");
        }

        Status = ConversionStatus.Running;
        StartedAt = DateTime.UtcNow;
    }

    /// <summary>
    /// 转换到完成状态
    /// </summary>
    public void Complete(bool hasFailures = false)
    {
        if (Status != ConversionStatus.Running)
        {
            throw new InvalidOperationException($"Cannot complete job in {Status} status");
        }

        Status = hasFailures ? ConversionStatus.PartiallyCompleted : ConversionStatus.Completed;
        CompletedAt = DateTime.UtcNow;
    }

    /// <summary>
    /// 转换到失败状态
    /// </summary>
    public void Fail()
    {
        Status = ConversionStatus.Failed;
        CompletedAt = DateTime.UtcNow;
    }
}

/// <summary>
/// 转换作业状态
/// </summary>
public enum ConversionStatus
{
    /// <summary>
    /// 草稿状态，等待执行
    /// </summary>
    Draft,

    /// <summary>
    /// 运行中
    /// </summary>
    Running,

    /// <summary>
    /// 已完成
    /// </summary>
    Completed,

    /// <summary>
    /// 部分完成（部分页面失败）
    /// </summary>
    PartiallyCompleted,

    /// <summary>
    /// 失败
    /// </summary>
    Failed
}
