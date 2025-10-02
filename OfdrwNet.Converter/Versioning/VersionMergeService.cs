using Microsoft.Extensions.Logging;
using OfdrwNet.Converter.Domain;
using System;
using System.Collections.Generic;
using System.Linq;

namespace OfdrwNet.Converter.Versioning;

/// <summary>
/// 版本合并服务。
/// </summary>
/// <remarks>
/// 压缩版本链当超过阈值时。
/// FR-33~FR-34: 版本链自动合并
///
/// 功能：
/// - 检测合并触发条件
/// - 压缩版本链（全量替换）
/// - 生成合并报告
/// - 保留关键版本元数据
///
/// 当前为占位实现，模拟版本链压缩逻辑。
/// 实际部署需要集成 OFD 包重写和差分清理。
/// </remarks>
public sealed class VersionMergeService
{
    private readonly ILogger<VersionMergeService> _logger;
    private readonly DiffBasedVersionManager _versionManager;

    /// <summary>
    /// 初始化 VersionMergeService 实例。
    /// </summary>
    public VersionMergeService(
        ILogger<VersionMergeService> logger,
        DiffBasedVersionManager versionManager)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _versionManager = versionManager ?? throw new ArgumentNullException(nameof(versionManager));
    }

    /// <summary>
    /// 检查并执行版本链合并（如果需要）。
    /// </summary>
    public MergeResult CheckAndMerge(string documentPath)
    {
        if (string.IsNullOrWhiteSpace(documentPath))
        {
            throw new ArgumentException("Document path cannot be null or empty", nameof(documentPath));
        }

        try
        {
            if (!_versionManager.RequiresMerge())
            {
                _logger.LogDebug("Version chain does not require merge");
                return new MergeResult
                {
                    MergePerformed = false,
                    Reason = "Below threshold"
                };
            }

            _logger.LogInformation("Version chain requires merge, performing compaction");

            return PerformMerge(documentPath);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to check and merge version chain");
            return new MergeResult
            {
                MergePerformed = false,
                Reason = "Error",
                ErrorMessage = ex.Message
            };
        }
    }

    /// <summary>
    /// 执行版本链合并。
    /// </summary>
    private MergeResult PerformMerge(string documentPath)
    {
        var history = _versionManager.GetVersionHistory();
        var latestVersion = _versionManager.GetLatestVersion();

        if (latestVersion == null)
        {
            _logger.LogWarning("No versions to merge");
            return new MergeResult
            {
                MergePerformed = false,
                Reason = "Empty chain"
            };
        }

        var beforeChainLength = history.Count;
        var beforeChainSize = _versionManager.GetTotalChainSize();

        _logger.LogInformation(
            "Merging version chain: {VersionCount} versions, {TotalSizeMB}MB total",
            beforeChainLength,
            beforeChainSize / (1024 * 1024));

        try
        {
            // 占位实现：模拟合并过程
            // 实际实现应：
            // 1. 读取最新版本的完整文档
            // 2. 创建新的基线版本
            // 3. 清除旧的差分链
            // 4. 重写 OFD 包结构（移除 Versions/ 目录）

            // 清除旧链
            _versionManager.ClearChain();

            // 创建新基线版本
            var mergeDescription = $"Merged from {beforeChainLength} versions";
            var newBaselineVersion = _versionManager.AppendVersion(
                documentPath,
                mergeDescription,
                "VersionMergeService");

            var afterChainLength = _versionManager.GetVersionHistory().Count;
            var afterChainSize = _versionManager.GetTotalChainSize();

            var savedBytes = beforeChainSize - afterChainSize;

            _logger.LogInformation(
                "Version chain merged successfully: {Before} versions → {After} version, saved {SavedMB}MB",
                beforeChainLength,
                afterChainLength,
                savedBytes / (1024 * 1024));

            return new MergeResult
            {
                MergePerformed = true,
                Reason = "Threshold exceeded",
                BeforeVersionCount = beforeChainLength,
                AfterVersionCount = afterChainLength,
                SpaceSavedBytes = savedBytes,
                NewBaselineVersionId = newBaselineVersion.VersionId
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to perform version merge");
            return new MergeResult
            {
                MergePerformed = false,
                Reason = "Merge failed",
                ErrorMessage = ex.Message
            };
        }
    }

    /// <summary>
    /// 强制合并版本链（忽略阈值检查）。
    /// </summary>
    public MergeResult ForceMerge(string documentPath)
    {
        if (string.IsNullOrWhiteSpace(documentPath))
        {
            throw new ArgumentException("Document path cannot be null or empty", nameof(documentPath));
        }

        _logger.LogWarning("Forcing version chain merge (ignoring thresholds)");

        try
        {
            return PerformMerge(documentPath);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to force merge version chain");
            return new MergeResult
            {
                MergePerformed = false,
                Reason = "Force merge failed",
                ErrorMessage = ex.Message
            };
        }
    }

    /// <summary>
    /// 获取合并建议。
    /// </summary>
    public MergeRecommendation GetRecommendation()
    {
        var history = _versionManager.GetVersionHistory();
        var totalSize = _versionManager.GetTotalChainSize();
        var requiresMerge = _versionManager.RequiresMerge();

        var recommendation = new MergeRecommendation
        {
            CurrentVersionCount = history.Count,
            TotalChainSizeBytes = totalSize,
            RequiresMerge = requiresMerge
        };

        if (requiresMerge)
        {
            var estimatedSavings = EstimateSavings(history, totalSize);
            recommendation.EstimatedSpaceSavingsBytes = estimatedSavings;
            recommendation.Recommendation = $"Merge recommended: save ~{estimatedSavings / (1024 * 1024)}MB";
        }
        else
        {
            recommendation.Recommendation = "No merge needed at this time";
        }

        return recommendation;
    }

    /// <summary>
    /// 估算合并后节省的空间。
    /// </summary>
    private long EstimateSavings(IReadOnlyList<VersionEntry> history, long totalSize)
    {
        if (history.Count == 0)
        {
            return 0;
        }

        // 占位启发式：假设合并后只保留最新版本的完整大小
        var latestVersionSize = history.Last().DeltaSize;
        var estimatedSavings = totalSize - latestVersionSize;

        return Math.Max(0, estimatedSavings);
    }
}

/// <summary>
/// 合并结果。
/// </summary>
public sealed class MergeResult
{
    public bool MergePerformed { get; set; }
    public string Reason { get; set; } = string.Empty;
    public int BeforeVersionCount { get; set; }
    public int AfterVersionCount { get; set; }
    public long SpaceSavedBytes { get; set; }
    public string? NewBaselineVersionId { get; set; }
    public string? ErrorMessage { get; set; }
}

/// <summary>
/// 合并建议。
/// </summary>
public sealed class MergeRecommendation
{
    public int CurrentVersionCount { get; set; }
    public long TotalChainSizeBytes { get; set; }
    public bool RequiresMerge { get; set; }
    public long EstimatedSpaceSavingsBytes { get; set; }
    public string Recommendation { get; set; } = string.Empty;
}
