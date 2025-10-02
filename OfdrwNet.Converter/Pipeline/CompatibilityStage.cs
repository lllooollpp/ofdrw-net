using Microsoft.Extensions.Logging;
using OfdrwNet.Converter.Compatibility;
using OfdrwNet.Converter.Domain;
using System;
using System.Collections.Generic;
using System.Linq;

namespace OfdrwNet.Converter.Pipeline;

/// <summary>
/// 兼容性处理阶段。
/// </summary>
/// <remarks>
/// T077: 集成兼容性策略 + 降级日志
///
/// 功能：
/// - 检查目标阅读器兼容性
/// - 应用功能降级策略
/// - 记录降级操作到 DowngradeAction 列表
/// - 生成兼容性警告日志
///
/// 使用场景：
/// - 在 PDF→OFD 转换完成后调用
/// - 验证生成的 OFD 功能是否符合目标阅读器
/// - 对不支持的功能应用降级
/// </remarks>
public sealed class CompatibilityStage
{
    private readonly FeatureDowngrader _downgrader;
    private readonly ILogger<CompatibilityStage> _logger;
    private readonly List<DowngradeAction> _actions;

    /// <summary>
    /// 初始化 CompatibilityStage 实例。
    /// </summary>
    public CompatibilityStage(
        FeatureDowngrader downgrader,
        ILogger<CompatibilityStage> logger)
    {
        _downgrader = downgrader ?? throw new ArgumentNullException(nameof(downgrader));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _actions = new List<DowngradeAction>();
    }

    /// <summary>
    /// 执行兼容性检查和降级。
    /// </summary>
    /// <param name="profileName">目标阅读器配置名称（如 "Suwell 9.x"）</param>
    /// <param name="features">需要检查的功能列表</param>
    /// <returns>执行的降级操作列表</returns>
    public List<DowngradeAction> Execute(string profileName, IEnumerable<FeatureCheckRequest> features)
    {
        if (string.IsNullOrWhiteSpace(profileName))
        {
            throw new ArgumentException("Profile name cannot be null or empty", nameof(profileName));
        }

        if (features == null)
        {
            throw new ArgumentNullException(nameof(features));
        }

        _logger.LogInformation(
            "[CompatibilityStage] Starting compatibility check for profile: {Profile}",
            profileName);

        _actions.Clear();

        foreach (var feature in features)
        {
            try
            {
                var action = _downgrader.CheckAndDowngrade(
                    profileName,
                    feature.FeatureName,
                    feature.Page);

                _actions.Add(action);

                if (action.Method != "None")
                {
                    _logger.LogWarning(
                        "[CompatibilityStage] Feature '{Feature}' downgraded on page {Page}: {Method} - {Reason}",
                        feature.FeatureName,
                        feature.Page ?? -1,
                        action.Method,
                        action.Reason);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(
                    ex,
                    "[CompatibilityStage] Failed to process feature '{Feature}' on page {Page}",
                    feature.FeatureName,
                    feature.Page ?? -1);
            }
        }

        var downgradedCount = _actions.Count(a => a.Method != "None");
        _logger.LogInformation(
            "[CompatibilityStage] Compatibility check complete. {Total} features checked, {Downgraded} downgraded",
            _actions.Count,
            downgradedCount);

        return _actions;
    }

    /// <summary>
    /// 获取所有降级操作记录。
    /// </summary>
    public IReadOnlyList<DowngradeAction> GetDowngradeActions() => _actions.AsReadOnly();

    /// <summary>
    /// 获取降级统计信息。
    /// </summary>
    public DowngradeStatistics GetStatistics()
    {
        return new DowngradeStatistics
        {
            TotalFeatures = _actions.Count,
            DowngradedFeatures = _actions.Count(a => a.Method != "None"),
            HighImpact = _actions.Count(a => a.Impact == "High"),
            MediumImpact = _actions.Count(a => a.Impact == "Medium"),
            LowImpact = _actions.Count(a => a.Impact == "Low")
        };
    }
}

/// <summary>
/// 功能检查请求。
/// </summary>
public record FeatureCheckRequest
{
    /// <summary>
    /// 功能名称（如 "Video", "JavaScript", "3D", "Attachment"）
    /// </summary>
    public required string FeatureName { get; init; }

    /// <summary>
    /// 页码（可选，某些功能可能不关联页面）
    /// </summary>
    public int? Page { get; init; }

    /// <summary>
    /// 附加上下文信息（可选）
    /// </summary>
    public string? Context { get; init; }
}

/// <summary>
/// 降级统计信息。
/// </summary>
public record DowngradeStatistics
{
    public int TotalFeatures { get; init; }
    public int DowngradedFeatures { get; init; }
    public int HighImpact { get; init; }
    public int MediumImpact { get; init; }
    public int LowImpact { get; init; }
}
