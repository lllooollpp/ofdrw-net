using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;

namespace OfdrwNet.Core.Batch;

/// <summary>
/// 批量转换结果摘要。
/// </summary>
public sealed class BatchResult
{
    private readonly IReadOnlyList<string> _failedFiles;

    /// <summary>
    /// 初始化 <see cref="BatchResult"/>。
    /// </summary>
    public BatchResult(int total = 0, int success = 0, IEnumerable<string>? failedFiles = null)
    {
        if (total < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(total));
        }

        if (success < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(success));
        }

        if (success > total)
        {
            throw new ArgumentException("Success count cannot exceed total count.", nameof(success));
        }

        Total = total;
        Success = success;
        Failed = Math.Max(0, total - success);
        _failedFiles = new ReadOnlyCollection<string>((failedFiles ?? Array.Empty<string>()).Select(f => f.Trim()).Where(f => f.Length > 0).ToList());
    }

    /// <summary>
    /// 总任务数。
    /// </summary>
    public int Total { get; init; }

    /// <summary>
    /// 成功数。
    /// </summary>
    public int Success { get; init; }

    /// <summary>
    /// 失败数。
    /// </summary>
    public int Failed { get; init; }

    /// <summary>
    /// 失败文件列表。
    /// </summary>
    public IReadOnlyList<string> FailedFiles => _failedFiles;

    /// <summary>
    /// 是否全部成功。
    /// </summary>
    public bool IsSuccessful => Failed == 0;

    /// <summary>
    /// 创建附带新失败列表的副本。
    /// </summary>
    public BatchResult WithFailures(IEnumerable<string> failedFiles)
    {
        var list = failedFiles?.ToList() ?? new List<string>();
        return new BatchResult(Total, Success, list);
    }

    /// <inheritdoc />
    public override string ToString()
    {
        return $"BatchResult[total={Total}, success={Success}, failed={Failed}]";
    }
}
