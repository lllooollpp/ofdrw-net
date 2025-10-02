using Microsoft.Extensions.Logging;
using OfdrwNet.Converter.Domain;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography;

namespace OfdrwNet.Converter.Versioning;

/// <summary>
/// 基于差分的版本管理器。
/// </summary>
/// <remarks>
/// 管理 OFD 文档版本链。
/// FR-33~FR-34: 版本链追加与合并
///
/// 功能：
/// - 版本链追加（增量模式）
/// - 计算差分大小和哈希
/// - 版本历史查询
/// - 链长度监控
///
/// 当前为占位实现，模拟版本链管理逻辑。
/// 实际部署需要集成 OFD 包结构和差分算法。
/// </remarks>
public sealed class DiffBasedVersionManager
{
    private readonly ILogger<DiffBasedVersionManager> _logger;
    private readonly VersionPolicy _policy;
    private readonly List<VersionEntry> _versionChain;

    /// <summary>
    /// 初始化 DiffBasedVersionManager 实例。
    /// </summary>
    /// <param name="logger">日志记录器</param>
    /// <param name="policy">版本策略</param>
    public DiffBasedVersionManager(ILogger<DiffBasedVersionManager> logger, VersionPolicy policy)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _policy = policy ?? throw new ArgumentNullException(nameof(policy));
        _versionChain = new List<VersionEntry>();

        _policy.Validate();

        _logger.LogInformation(
            "DiffBasedVersionManager initialized: MaxChain={MaxChain}, AutoCompact={AutoCompact}",
            _policy.MaxChain,
            _policy.AutoCompact);
    }

    /// <summary>
    /// 追加新版本到链。
    /// </summary>
    /// <param name="documentPath">文档路径</param>
    /// <param name="description">版本描述</param>
    /// <param name="createdBy">创建者</param>
    /// <returns>新创建的版本条目</returns>
    public VersionEntry AppendVersion(string documentPath, string? description = null, string? createdBy = null)
    {
        if (string.IsNullOrWhiteSpace(documentPath))
        {
            throw new ArgumentException("Document path cannot be null or empty", nameof(documentPath));
        }

        if (!File.Exists(documentPath))
        {
            throw new FileNotFoundException("Document file not found", documentPath);
        }

        try
        {
            _logger.LogInformation("Appending version for document: {Path}", documentPath);

            // 计算文档哈希
            var documentHash = ComputeFileHash(documentPath);
            var fileInfo = new FileInfo(documentPath);

            // 计算差分大小
            long deltaSize;
            string? parentVersionId = null;

            if (_versionChain.Count > 0)
            {
                var previousVersion = _versionChain[_versionChain.Count - 1];
                parentVersionId = previousVersion.VersionId;
                deltaSize = EstimateDeltaSize(previousVersion.BaseHash, documentHash, fileInfo.Length);
            }
            else
            {
                deltaSize = fileInfo.Length; // 第一个版本，完整大小
            }

            var versionId = $"v{_versionChain.Count + 1:D3}_{DateTime.UtcNow:yyyyMMddHHmmss}";

            var entry = new VersionEntry
            {
                VersionId = versionId,
                BaseHash = documentHash,
                DeltaSize = deltaSize,
                CreatedAt = DateTime.UtcNow,
                Description = description,
                ParentVersionId = parentVersionId,
                CreatedBy = createdBy,
                Tag = $"v{_versionChain.Count + 1}"
            };

            _versionChain.Add(entry);

            _logger.LogInformation(
                "Version appended: VersionId={VersionId}, Hash={Hash}, DeltaSize={DeltaKB}KB",
                entry.VersionId,
                entry.BaseHash.Substring(0, 8) + "...",
                entry.DeltaSize / 1024);

            CheckPolicyLimits();

            return entry;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to append version for document: {Path}", documentPath);
            throw;
        }
    }

    /// <summary>
    /// 获取版本历史。
    /// </summary>
    public IReadOnlyList<VersionEntry> GetVersionHistory()
    {
        return _versionChain.AsReadOnly();
    }

    /// <summary>
    /// 获取指定版本。
    /// </summary>
    public VersionEntry? GetVersion(string versionId)
    {
        return _versionChain.FirstOrDefault(v => v.VersionId == versionId);
    }

    /// <summary>
    /// 获取最新版本。
    /// </summary>
    public VersionEntry? GetLatestVersion()
    {
        return _versionChain.Count > 0 ? _versionChain[_versionChain.Count - 1] : null;
    }

    /// <summary>
    /// 计算版本链总大小（字节）。
    /// </summary>
    public long GetTotalChainSize()
    {
        return _versionChain.Sum(v => v.DeltaSize);
    }

    /// <summary>
    /// 检查是否需要合并（超过策略限制）。
    /// </summary>
    public bool RequiresMerge()
    {
        var requiresMerge = _versionChain.Count >= _policy.MaxChain;

        if (requiresMerge)
        {
            _logger.LogWarning(
                "Version chain requires merge: ChainLength={Length}/{MaxChain}",
                _versionChain.Count,
                _policy.MaxChain);
        }

        return requiresMerge;
    }

    /// <summary>
    /// 清除所有版本（重置链）。
    /// </summary>
    public void ClearChain()
    {
        _logger.LogWarning("Clearing version chain ({Count} versions)", _versionChain.Count);
        _versionChain.Clear();
    }

    /// <summary>
    /// 检查策略限制并记录警告。
    /// </summary>
    private void CheckPolicyLimits()
    {
        if (_versionChain.Count >= _policy.MaxChain)
        {
            _logger.LogWarning(
                "Version chain length limit reached: {Current}/{Max}",
                _versionChain.Count,
                _policy.MaxChain);
        }

        if (_policy.AutoCompact && _versionChain.Count >= _policy.CompactThreshold)
        {
            _logger.LogWarning(
                "Version chain compact threshold reached: {Current}/{Threshold}",
                _versionChain.Count,
                _policy.CompactThreshold);
        }
    }

    /// <summary>
    /// 计算文件 SHA-256 哈希。
    /// </summary>
    private string ComputeFileHash(string filePath)
    {
        using var stream = File.OpenRead(filePath);
        using var sha256 = SHA256.Create();
        var hashBytes = sha256.ComputeHash(stream);
        return Convert.ToHexString(hashBytes).ToLowerInvariant();
    }

    /// <summary>
    /// 估算差分大小（占位实现）。
    /// </summary>
    private long EstimateDeltaSize(string previousHash, string currentHash, long currentFileSize)
    {
        if (previousHash == currentHash)
        {
            return 0; // 哈希相同，无变化
        }

        // 占位启发式：假设差分为文件大小的 30%
        var estimatedDelta = (long)(currentFileSize * 0.3);

        _logger.LogDebug(
            "Estimated delta size: {DeltaKB}KB (30% of {FileSizeKB}KB)",
            estimatedDelta / 1024,
            currentFileSize / 1024);

        return estimatedDelta;
    }
}
