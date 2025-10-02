using Microsoft.Extensions.Logging;
using OfdrwNet.Converter.Domain;
using System;
using System.Collections.Generic;
using System.Linq;

namespace OfdrwNet.Converter.Compatibility;

/// <summary>
/// 功能降级器。
/// </summary>
/// <remarks>
/// 应用降级操作以确保阅读器兼容性。
/// FR-37: 功能降级与替代方案
///
/// 功能：
/// - 检测不兼容功能
/// - 应用降级策略
/// - 记录降级操作
/// - 生成兼容性报告
///
/// 降级策略：
/// - 视频/音频 → 占位图像
/// - JavaScript → 移除 + 警告
/// - 3D 模型 → 2D 截图
/// - 附件 → 提取到外部
/// </remarks>
public sealed class FeatureDowngrader
{
    private readonly ILogger<FeatureDowngrader> _logger;
    private readonly JsonCompatibilityProfileProvider _profileProvider;
    private readonly List<DowngradeAction> _downgrades;

    /// <summary>
    /// 初始化 FeatureDowngrader 实例。
    /// </summary>
    public FeatureDowngrader(
        ILogger<FeatureDowngrader> logger,
        JsonCompatibilityProfileProvider profileProvider)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _profileProvider = profileProvider ?? throw new ArgumentNullException(nameof(profileProvider));
        _downgrades = new List<DowngradeAction>();
    }

    /// <summary>
    /// 检查并降级功能。
    /// </summary>
    /// <param name="profileName">目标阅读器配置名称</param>
    /// <param name="featureName">功能名称</param>
    /// <param name="page">页码（可选）</param>
    /// <returns>降级操作</returns>
    public DowngradeAction CheckAndDowngrade(string profileName, string featureName, int? page = null)
    {
        if (string.IsNullOrWhiteSpace(profileName))
        {
            throw new ArgumentException("Profile name cannot be null or empty", nameof(profileName));
        }

        if (string.IsNullOrWhiteSpace(featureName))
        {
            throw new ArgumentException("Feature name cannot be null or empty", nameof(featureName));
        }

        try
        {
            var isUnsupported = _profileProvider.IsFeatureUnsupported(profileName, featureName);

            if (!isUnsupported)
            {
                _logger.LogDebug("Feature {Feature} is supported by {Profile}", featureName, profileName);

                // 返回"无操作"降级记录
                return new DowngradeAction
                {
                    Feature = featureName,
                    Reason = "Supported by target profile",
                    Method = "None",
                    Page = page,
                    Impact = "Low"
                };
            }

            _logger.LogInformation(
                "Feature {Feature} is unsupported by {Profile}, applying downgrade",
                featureName,
                profileName);

            var downgrade = DetermineDowngrade(featureName, profileName, page);
            _downgrades.Add(downgrade);

            return downgrade;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to check and downgrade feature {Feature}", featureName);
            throw;
        }
    }

    /// <summary>
    /// 确定降级策略。
    /// </summary>
    private DowngradeAction DetermineDowngrade(string featureName, string profileName, int? page)
    {
        var action = featureName.ToLowerInvariant() switch
        {
            "video" => new DowngradeAction
            {
                Feature = featureName,
                Reason = $"Video not supported by {profileName}",
                Method = "ReplaceWithPlaceholder",
                Page = page,
                Alternative = "Static image placeholder with play icon",
                Impact = "Medium",
                Details = "{\"type\":\"media\",\"original\":\"video\",\"replacement\":\"image\"}"
            },
            "audio" => new DowngradeAction
            {
                Feature = featureName,
                Reason = $"Audio not supported by {profileName}",
                Method = "ReplaceWithPlaceholder",
                Page = page,
                Alternative = "Static image placeholder with speaker icon",
                Impact = "Medium",
                Details = "{\"type\":\"media\",\"original\":\"audio\",\"replacement\":\"image\"}"
            },
            "javascript" => new DowngradeAction
            {
                Feature = featureName,
                Reason = $"JavaScript not supported by {profileName}",
                Method = "Remove",
                Page = page,
                Alternative = "Script removed, see conversion report for details",
                Impact = "High",
                Details = "{\"type\":\"script\",\"action\":\"removed\"}"
            },
            "3d" => new DowngradeAction
            {
                Feature = featureName,
                Reason = $"3D models not supported by {profileName}",
                Method = "ConvertTo2D",
                Page = page,
                Alternative = "2D screenshot or wireframe representation",
                Impact = "High",
                Details = "{\"type\":\"3d\",\"conversion\":\"2d_snapshot\"}"
            },
            "attachment" => new DowngradeAction
            {
                Feature = featureName,
                Reason = $"Attachments not supported by {profileName}",
                Method = "ExtractExternal",
                Page = page,
                Alternative = "Extracted to external files in attachments/ directory",
                Impact = "Medium",
                Details = "{\"type\":\"attachment\",\"action\":\"external_extraction\"}"
            },
            "form" when profileName.Equals("Baseline", StringComparison.OrdinalIgnoreCase) => new DowngradeAction
            {
                Feature = featureName,
                Reason = "Interactive forms not supported in baseline profile",
                Method = "FlattenToText",
                Page = page,
                Alternative = "Form fields converted to static text",
                Impact = "High",
                Details = "{\"type\":\"form\",\"action\":\"flattened\"}"
            },
            _ => new DowngradeAction
            {
                Feature = featureName,
                Reason = $"Feature not supported by {profileName}, no alternative available",
                Method = "Remove",
                Page = page,
                Alternative = "Feature removed",
                Impact = "Medium",
                Details = "{\"type\":\"unknown\",\"action\":\"removed\"}"
            }
        };

        _logger.LogWarning(
            "Downgrade applied: {Feature} → {Method} (Impact: {Impact})",
            featureName,
            action.Method,
            action.Impact);

        return action;
    }

    /// <summary>
    /// 批量检查功能列表。
    /// </summary>
    public IList<DowngradeAction> CheckFeatures(string profileName, IEnumerable<string> features)
    {
        if (features == null)
        {
            throw new ArgumentNullException(nameof(features));
        }

        var results = new List<DowngradeAction>();

        foreach (var feature in features)
        {
            if (!string.IsNullOrWhiteSpace(feature))
            {
                var result = CheckAndDowngrade(profileName, feature);
                results.Add(result);
            }
        }

        return results;
    }

    /// <summary>
    /// 获取所有降级操作。
    /// </summary>
    public IReadOnlyList<DowngradeAction> GetDowngrades()
    {
        return _downgrades.AsReadOnly();
    }

    /// <summary>
    /// 获取降级统计。
    /// </summary>
    public DowngradeStatistics GetStatistics()
    {
        var total = _downgrades.Count;
        var byMethod = _downgrades
            .GroupBy(d => d.Method)
            .ToDictionary(g => g.Key, g => g.Count());
        var byImpact = _downgrades
            .Where(d => d.Impact != null)
            .GroupBy(d => d.Impact!)
            .ToDictionary(g => g.Key, g => g.Count());

        return new DowngradeStatistics
        {
            TotalDowngrades = total,
            DowngradesByMethod = byMethod,
            DowngradesByImpact = byImpact
        };
    }

    /// <summary>
    /// 清除降级记录。
    /// </summary>
    public void ClearDowngrades()
    {
        _logger.LogInformation("Clearing {Count} downgrade records", _downgrades.Count);
        _downgrades.Clear();
    }
}

/// <summary>
/// 降级统计。
/// </summary>
public sealed class DowngradeStatistics
{
    public int TotalDowngrades { get; set; }
    public Dictionary<string, int> DowngradesByMethod { get; set; } = new();
    public Dictionary<string, int> DowngradesByImpact { get; set; } = new();
}
