using System.Collections.Generic;

namespace OfdrwNet.Converter.Domain;

/// <summary>
/// 批处理结果
/// </summary>
public sealed class BatchResult
{
    /// <summary>
    /// 总任务数
    /// </summary>
    public required int Total { get; init; }

    /// <summary>
    /// 成功任务数
    /// </summary>
    public int Success { get; set; }

    /// <summary>
    /// 失败任务数
    /// </summary>
    public int Failed { get; set; }

    /// <summary>
    /// 失败任务详情
    /// </summary>
    public IList<BatchFailureInfo> Failures { get; init; } = new List<BatchFailureInfo>();

    /// <summary>
    /// 批处理开始时间
    /// </summary>
    public DateTime StartedAt { get; init; } = DateTime.UtcNow;

    /// <summary>
    /// 批处理完成时间
    /// </summary>
    public DateTime? CompletedAt { get; set; }

    /// <summary>
    /// 计算成功率
    /// </summary>
    public double SuccessRate => Total > 0 ? (double)Success / Total : 0.0;

    /// <summary>
    /// 计算耗时（秒）
    /// </summary>
    public double ElapsedSeconds => (CompletedAt ?? DateTime.UtcNow).Subtract(StartedAt).TotalSeconds;
}

/// <summary>
/// 批处理失败信息
/// </summary>
public sealed class BatchFailureInfo
{
    /// <summary>
    /// 任务索引
    /// </summary>
    public required int TaskIndex { get; init; }

    /// <summary>
    /// 任务标识符
    /// </summary>
    public required string TaskId { get; init; }

    /// <summary>
    /// 错误消息
    /// </summary>
    public required string ErrorMessage { get; init; }

    /// <summary>
    /// 错误堆栈跟踪
    /// </summary>
    public string? StackTrace { get; init; }
}
