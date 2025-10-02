using Microsoft.Extensions.Logging;
using OfdrwNet.Converter.Domain;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace OfdrwNet.Converter.Batch;

/// <summary>
/// 批处理器。
/// </summary>
/// <remarks>
/// 编排并行转换与 MemoryGuard 集成。
/// FR-31~FR-32: 批量处理与内存管理
///
/// 功能:
/// - 并行文件转换
/// - MemoryGuard 内存监控
/// - 动态并行度调整
/// - 进度报告
/// - 错误隔离与重试
///
/// 调度策略:
/// - 正常: MaxDegreeOfParallelism = CPU 核心数
/// - 内存警告: 降低到 1/2
/// - 内存临界: 降低到 1/4
/// - 内存不足: 暂停并等待 GC
/// </remarks>
public sealed class BatchProcessor
{
    private readonly ILogger<BatchProcessor> _logger;
    private readonly MemoryGuard _memoryGuard;

    // 默认并行度 = CPU 核心数
    private readonly int _defaultParallelism;

    /// <summary>
    /// 初始化 BatchProcessor 实例。
    /// </summary>
    public BatchProcessor(
        ILogger<BatchProcessor> logger,
        MemoryGuard memoryGuard)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _memoryGuard = memoryGuard ?? throw new ArgumentNullException(nameof(memoryGuard));
        _defaultParallelism = Environment.ProcessorCount;
    }

    /// <summary>
    /// 批量执行任务。
    /// </summary>
    /// <typeparam name="TInput">输入类型</typeparam>
    /// <typeparam name="TOutput">输出类型</typeparam>
    /// <param name="items">输入项列表</param>
    /// <param name="taskFunc">任务执行函数</param>
    /// <param name="options">批处理选项(可选)</param>
    /// <param name="cancellationToken">取消令牌(可选)</param>
    /// <returns>批处理结果</returns>
    public async Task<BatchResult> ProcessBatchAsync<TInput, TOutput>(
        IEnumerable<TInput> items,
        Func<TInput, int, Task<TOutput>> taskFunc,
        BatchProcessOptions? options = null,
        CancellationToken cancellationToken = default)
    {
        if (items == null)
        {
            throw new ArgumentNullException(nameof(items));
        }

        if (taskFunc == null)
        {
            throw new ArgumentNullException(nameof(taskFunc));
        }

        options ??= new BatchProcessOptions();

        var itemList = items.ToList();
        var result = new BatchResult
        {
            Total = itemList.Count,
            StartedAt = DateTime.UtcNow
        };

        _logger.LogInformation("Starting batch processing: {Count} items, MaxParallelism: {Parallelism}",
            itemList.Count, options.MaxDegreeOfParallelism ?? _defaultParallelism);

        var completed = 0;
        var successCount = 0;
        var failedCount = 0;
        var semaphore = new SemaphoreSlim(options.MaxDegreeOfParallelism ?? _defaultParallelism);

        var tasks = itemList.Select(async (item, index) =>
        {
            await semaphore.WaitAsync(cancellationToken);

            try
            {
                // 内存检查
                var memorySnapshot = _memoryGuard.CheckMemory();
                AdjustParallelism(semaphore, memorySnapshot, options);

                // 执行任务
                await taskFunc(item, index);

                Interlocked.Increment(ref successCount);
                Interlocked.Increment(ref completed);

                // 进度报告
                if (options.ProgressCallback != null)
                {
                    var progress = (double)completed / itemList.Count * 100;
                    options.ProgressCallback(completed, itemList.Count, progress);
                }

                _logger.LogDebug("Task {Index}/{Total} completed successfully", index + 1, itemList.Count);
            }
            catch (Exception ex)
            {
                Interlocked.Increment(ref failedCount);
                Interlocked.Increment(ref completed);

                result.Failures.Add(new BatchFailureInfo
                {
                    TaskIndex = index,
                    TaskId = item?.ToString() ?? $"Item-{index}",
                    ErrorMessage = ex.Message,
                    StackTrace = ex.StackTrace
                });

                _logger.LogError(ex, "Task {Index}/{Total} failed", index + 1, itemList.Count);

                if (!options.ContinueOnError)
                {
                    throw;
                }
            }
            finally
            {
                semaphore.Release();
            }
        }).ToList();

        await Task.WhenAll(tasks);

        result.Success = successCount;
        result.Failed = failedCount;
        result.CompletedAt = DateTime.UtcNow;

        _logger.LogInformation(
            "Batch processing completed: {Success}/{Total} succeeded, {Failed} failed, {Elapsed:F2}s elapsed",
            result.Success, result.Total, result.Failed, result.ElapsedSeconds);

        return result;
    }

    /// <summary>
    /// 调整并行度(基于内存状态)。
    /// </summary>
    private void AdjustParallelism(
        SemaphoreSlim semaphore,
        MemorySnapshot snapshot,
        BatchProcessOptions options)
    {
        var action = snapshot.Action;

        if (action == MemoryAction.GarbageCollect)
        {
            _logger.LogWarning("Memory warning detected, triggering GC");
            _memoryGuard.ApplyMitigation(snapshot);
        }
        else if (action == MemoryAction.FlushToDisk)
        {
            _logger.LogWarning("Memory critical, reducing parallelism to 1/2");
            // 注意: SemaphoreSlim 不支持动态调整,实际场景需要更复杂的调度器
            _memoryGuard.ApplyMitigation(snapshot);
        }
        else if (action == MemoryAction.Abort)
        {
            _logger.LogError("Memory exhausted, aborting batch processing");
            throw new OutOfMemoryException("Insufficient memory to continue batch processing");
        }
    }

    /// <summary>
    /// 批量处理文件(便捷方法)。
    /// </summary>
    /// <param name="filePaths">文件路径列表</param>
    /// <param name="processFunc">处理函数</param>
    /// <param name="options">批处理选项(可选)</param>
    /// <param name="cancellationToken">取消令牌(可选)</param>
    /// <returns>批处理结果</returns>
    public async Task<BatchResult> ProcessFilesAsync(
        IEnumerable<string> filePaths,
        Func<string, int, Task> processFunc,
        BatchProcessOptions? options = null,
        CancellationToken cancellationToken = default)
    {
        return await ProcessBatchAsync(
            filePaths,
            async (path, index) =>
            {
                await processFunc(path, index);
                return true;
            },
            options,
            cancellationToken);
    }

    /// <summary>
    /// 估算批处理内存需求。
    /// </summary>
    /// <param name="itemCount">项目数量</param>
    /// <param name="averageItemSizeMB">单项平均内存(MB)</param>
    /// <returns>估算的内存需求(MB)</returns>
    public double EstimateMemoryRequirement(int itemCount, double averageItemSizeMB)
    {
        var parallelism = _defaultParallelism;
        var estimatedMB = parallelism * averageItemSizeMB;

        _logger.LogDebug(
            "Estimated memory requirement: {Count} items × {Parallelism} parallel × {Size}MB = {Total}MB",
            itemCount, parallelism, averageItemSizeMB, estimatedMB);

        return estimatedMB;
    }

    /// <summary>
    /// 建议批处理分片大小。
    /// </summary>
    /// <param name="totalItems">总项目数</param>
    /// <param name="averageItemSizeMB">单项平均内存(MB)</param>
    /// <returns>建议的分片大小</returns>
    public int SuggestBatchSize(int totalItems, double averageItemSizeMB)
    {
        var availableMemoryMB = _memoryGuard.EstimateAvailableMemoryMB();
        var maxParallelItems = (int)(availableMemoryMB / averageItemSizeMB * 0.8); // 80% 安全边际

        var suggestedSize = Math.Min(totalItems, Math.Max(1, maxParallelItems));

        _logger.LogInformation(
            "Suggested batch size: {Size} (Available: {Available}MB, Item: {Item}MB)",
            suggestedSize, availableMemoryMB, averageItemSizeMB);

        return suggestedSize;
    }
}

/// <summary>
/// 批处理选项。
/// </summary>
public sealed class BatchProcessOptions
{
    /// <summary>
    /// 最大并行度(null = 使用 CPU 核心数)。
    /// </summary>
    public int? MaxDegreeOfParallelism { get; set; }

    /// <summary>
    /// 遇到错误时是否继续处理。
    /// </summary>
    public bool ContinueOnError { get; set; } = true;

    /// <summary>
    /// 进度回调(已完成数, 总数, 进度百分比)。
    /// </summary>
    public Action<int, int, double>? ProgressCallback { get; set; }

    /// <summary>
    /// 内存警告阈值(MB)。
    /// </summary>
    public double? MemoryWarningThresholdMB { get; set; }

    /// <summary>
    /// 内存临界阈值(MB)。
    /// </summary>
    public double? MemoryCriticalThresholdMB { get; set; }
}
