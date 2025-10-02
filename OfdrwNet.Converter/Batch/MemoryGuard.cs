using Microsoft.Extensions.Logging;
using OfdrwNet.Converter.Domain;
using System;
using System.Diagnostics;

namespace OfdrwNet.Converter.Batch;

/// <summary>
/// 内存保护服务。
/// </summary>
/// <remarks>
/// 监控内存使用情况并应用分段计划。
/// DR-24~DR-27: 内存优化与分段策略
///
/// 功能：
/// - 实时监控工作集内存
/// - 检测超过阈值时触发分段
/// - 生成 MemorySnapshot 记录
/// - 建议缓解措施（GC/分段/延迟）
///
/// 当前为占位实现，使用 Process.WorkingSet64 监控。
/// 实际部署可集成更精细的内存分析器。
/// </remarks>
public sealed class MemoryGuard
{
    private readonly ILogger<MemoryGuard> _logger;
    private readonly double _warningThresholdMB;
    private readonly double _criticalThresholdMB;
    private readonly Process _currentProcess;

    /// <summary>
    /// 默认警告阈值（2000 MB）。
    /// </summary>
    private const double _defaultWarningThresholdMB = 2000.0;

    /// <summary>
    /// 默认临界阈值（3000 MB）。
    /// </summary>
    private const double _defaultCriticalThresholdMB = 3000.0;

    /// <summary>
    /// 初始化 MemoryGuard 实例。
    /// </summary>
    /// <param name="logger">日志记录器</param>
    /// <param name="warningThresholdMB">警告阈值（MB），默认 2000MB</param>
    /// <param name="criticalThresholdMB">临界阈值（MB），默认 3000MB</param>
    public MemoryGuard(
        ILogger<MemoryGuard> logger,
        double? warningThresholdMB = null,
        double? criticalThresholdMB = null)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _warningThresholdMB = warningThresholdMB ?? _defaultWarningThresholdMB;
        _criticalThresholdMB = criticalThresholdMB ?? _defaultCriticalThresholdMB;
        _currentProcess = Process.GetCurrentProcess();

        if (_warningThresholdMB >= _criticalThresholdMB)
        {
            throw new ArgumentException("Warning threshold must be less than critical threshold");
        }

        _logger.LogInformation(
            "MemoryGuard initialized: Warning={WarningMB}MB, Critical={CriticalMB}MB",
            _warningThresholdMB,
            _criticalThresholdMB);
    }

    /// <summary>
    /// 检查当前内存状态并生成快照。
    /// </summary>
    /// <returns>内存快照</returns>
    public MemorySnapshot CheckMemory()
    {
        try
        {
            _currentProcess.Refresh();
            var workingSetMB = _currentProcess.WorkingSet64 / (1024.0 * 1024.0);
            var privateMemoryMB = _currentProcess.PrivateMemorySize64 / (1024.0 * 1024.0);
            var gcHeapMB = GC.GetTotalMemory(forceFullCollection: false) / (1024.0 * 1024.0);

            var (action, threshold) = DetermineAction(workingSetMB);

            var snapshot = new MemorySnapshot
            {
                Timestamp = DateTime.UtcNow,
                AllocatedMB = workingSetMB,
                ThresholdMB = threshold,
                Action = action,
                WorkingSetMB = workingSetMB,
                GcHeapMB = gcHeapMB,
                AdditionalInfo = $"Private={privateMemoryMB:F2}MB"
            };

            LogMemoryStatus(snapshot);

            return snapshot;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to check memory status");

            return new MemorySnapshot
            {
                Timestamp = DateTime.UtcNow,
                AllocatedMB = 0,
                ThresholdMB = _criticalThresholdMB,
                Action = MemoryAction.None,
                AdditionalInfo = "Error checking memory"
            };
        }
    }

    /// <summary>
    /// 确定内存操作。
    /// </summary>
    private (MemoryAction action, double threshold) DetermineAction(double workingSetMB)
    {
        if (workingSetMB >= _criticalThresholdMB)
        {
            _logger.LogWarning(
                "Critical memory threshold exceeded: {CurrentMB}MB >= {ThresholdMB}MB",
                workingSetMB,
                _criticalThresholdMB);
            return (MemoryAction.FlushToDisk, _criticalThresholdMB);
        }

        if (workingSetMB >= _warningThresholdMB)
        {
            _logger.LogWarning(
                "Warning memory threshold exceeded: {CurrentMB}MB >= {ThresholdMB}MB",
                workingSetMB,
                _warningThresholdMB);
            return (MemoryAction.GarbageCollect, _warningThresholdMB);
        }

        return (MemoryAction.None, _criticalThresholdMB);
    }

    /// <summary>
    /// 记录内存状态日志。
    /// </summary>
    private void LogMemoryStatus(MemorySnapshot snapshot)
    {
        if (snapshot.Action != MemoryAction.None)
        {
            _logger.LogWarning(
                "Memory snapshot: Allocated={AllocatedMB}MB, Threshold={ThresholdMB}MB, Action={Action}, {AdditionalInfo}",
                snapshot.AllocatedMB,
                snapshot.ThresholdMB,
                snapshot.Action,
                snapshot.AdditionalInfo);
        }
        else
        {
            _logger.LogDebug(
                "Memory snapshot: Allocated={AllocatedMB}MB, GcHeap={GcHeapMB}MB",
                snapshot.AllocatedMB,
                snapshot.GcHeapMB);
        }
    }

    /// <summary>
    /// 应用缓解措施。
    /// </summary>
    /// <param name="snapshot">内存快照</param>
    /// <returns>是否成功应用缓解措施</returns>
    public bool ApplyMitigation(MemorySnapshot snapshot)
    {
        if (snapshot == null)
        {
            throw new ArgumentNullException(nameof(snapshot));
        }

        try
        {
            switch (snapshot.Action)
            {
                case MemoryAction.GarbageCollect:
                    _logger.LogInformation("Applying mitigation: triggering GC collection");
                    GC.Collect(2, GCCollectionMode.Forced, blocking: true, compacting: true);
                    GC.WaitForPendingFinalizers();
                    _logger.LogInformation("GC collection completed");
                    return true;

                case MemoryAction.FlushToDisk:
                    _logger.LogWarning("Mitigation action 'FlushToDisk' requires caller intervention (pause/flush resources)");
                    return false;

                case MemoryAction.ReduceParallelism:
                    _logger.LogWarning("Mitigation action 'ReduceParallelism' requires caller intervention");
                    return false;

                case MemoryAction.Abort:
                    _logger.LogError("Mitigation action 'Abort' requested - critical memory situation");
                    return false;

                case MemoryAction.None:
                    _logger.LogDebug("No mitigation action required");
                    return true;

                default:
                    _logger.LogWarning("Unknown mitigation action: {Action}", snapshot.Action);
                    return false;
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to apply mitigation action: {Action}", snapshot.Action);
            return false;
        }
    }

    /// <summary>
    /// 检查是否应分段处理。
    /// </summary>
    /// <returns>如果超过临界阈值则返回 true</returns>
    public bool ShouldSegment()
    {
        var snapshot = CheckMemory();
        return snapshot.Action == MemoryAction.FlushToDisk || snapshot.Action == MemoryAction.Abort;
    }

    /// <summary>
    /// 估算剩余可用内存（MB）。
    /// </summary>
    public double EstimateAvailableMemoryMB()
    {
        _currentProcess.Refresh();
        var workingSetMB = _currentProcess.WorkingSet64 / (1024.0 * 1024.0);
        var remainingMB = _criticalThresholdMB - workingSetMB;

        return Math.Max(0, remainingMB);
    }

    /// <summary>
    /// 生成分段建议。
    /// </summary>
    /// <param name="totalPages">总页数</param>
    /// <param name="averagePageMemoryMB">平均每页内存占用（MB）</param>
    /// <returns>建议的每批次页数</returns>
    public int SuggestSegmentSize(int totalPages, double averagePageMemoryMB)
    {
        if (totalPages <= 0)
        {
            throw new ArgumentException("Total pages must be positive", nameof(totalPages));
        }

        if (averagePageMemoryMB <= 0)
        {
            throw new ArgumentException("Average page memory must be positive", nameof(averagePageMemoryMB));
        }

        var availableMemoryMB = EstimateAvailableMemoryMB();

        // 预留 20% 安全边际
        var safeMemoryMB = availableMemoryMB * 0.8;

        var maxPagesPerSegment = (int)(safeMemoryMB / averagePageMemoryMB);

        // 至少处理 1 页
        maxPagesPerSegment = Math.Max(1, maxPagesPerSegment);

        // 不超过总页数
        maxPagesPerSegment = Math.Min(totalPages, maxPagesPerSegment);

        _logger.LogInformation(
            "Segment size suggestion: {MaxPages} pages ({AvailableMB}MB available, {AvgPageMB}MB per page)",
            maxPagesPerSegment,
            availableMemoryMB,
            averagePageMemoryMB);

        return maxPagesPerSegment;
    }
}
