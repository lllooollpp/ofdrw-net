using Microsoft.Extensions.Logging;
using OfdrwNet.Converter.Domain;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;

namespace OfdrwNet.Converter.Compatibility;

/// <summary>
/// JSON 兼容性配置文件提供者。
/// </summary>
/// <remarks>
/// 从 JSON 文件加载阅读器兼容性配置。
/// FR-37: 阅读器兼容性配置
///
/// 功能：
/// - 加载配置文件（profiles.json）
/// - 解析阅读器配置
/// - 提供降级规则查询
/// - 缓存配置数据
///
/// 配置文件格式：
/// {
///   "profiles": [
///     {
///       "name": "Suwell",
///       "version": "9.x",
///       "features": ["basic", "annotation", "form"],
///       "unsupported": ["video", "javascript"]
///     }
///   ]
/// }
/// </remarks>
public sealed class JsonCompatibilityProfileProvider
{
    private readonly ILogger<JsonCompatibilityProfileProvider> _logger;
    private readonly Dictionary<string, CompatibilityProfile> _profiles;

    /// <summary>
    /// 初始化 JsonCompatibilityProfileProvider 实例。
    /// </summary>
    /// <param name="logger">日志记录器</param>
    /// <param name="profilePath">配置文件路径（可选）</param>
    public JsonCompatibilityProfileProvider(ILogger<JsonCompatibilityProfileProvider> logger, string? profilePath = null)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _profiles = new Dictionary<string, CompatibilityProfile>(StringComparer.OrdinalIgnoreCase);

        if (!string.IsNullOrWhiteSpace(profilePath) && File.Exists(profilePath))
        {
            LoadProfiles(profilePath);
        }
        else
        {
            _logger.LogWarning("No profile path provided or file not found, using default profiles");
            LoadDefaultProfiles();
        }
    }

    /// <summary>
    /// 加载配置文件。
    /// </summary>
    private void LoadProfiles(string profilePath)
    {
        try
        {
            _logger.LogInformation("Loading compatibility profiles from: {Path}", profilePath);

            var json = File.ReadAllText(profilePath);
            var document = JsonDocument.Parse(json);

            if (document.RootElement.TryGetProperty("profiles", out var profilesElement))
            {
                foreach (var profileElement in profilesElement.EnumerateArray())
                {
                    var profile = ParseProfile(profileElement);
                    if (profile != null)
                    {
                        _profiles[profile.Name] = profile;
                        _logger.LogDebug("Loaded profile: {Name}", profile.Name);
                    }
                }
            }

            _logger.LogInformation("Loaded {Count} compatibility profiles", _profiles.Count);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to load profiles from {Path}, using defaults", profilePath);
            LoadDefaultProfiles();
        }
    }

    /// <summary>
    /// 解析单个配置。
    /// </summary>
    private CompatibilityProfile? ParseProfile(JsonElement element)
    {
        try
        {
            if (!element.TryGetProperty("name", out var nameElement))
            {
                return null;
            }

            var name = nameElement.GetString();
            if (string.IsNullOrWhiteSpace(name))
            {
                return null;
            }

            var version = element.TryGetProperty("version", out var versionElement)
                ? versionElement.GetString()
                : "unknown";

            var features = new List<string>();
            if (element.TryGetProperty("features", out var featuresElement))
            {
                features.AddRange(featuresElement.EnumerateArray().Select(e => e.GetString() ?? ""));
            }

            var unsupported = new List<string>();
            if (element.TryGetProperty("unsupported", out var unsupportedElement))
            {
                unsupported.AddRange(unsupportedElement.EnumerateArray().Select(e => e.GetString() ?? ""));
            }

            return new CompatibilityProfile
            {
                Name = name,
                Version = version ?? "unknown",
                SupportedFeatures = features,
                UnsupportedFeatures = unsupported
            };
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to parse profile");
            return null;
        }
    }

    /// <summary>
    /// 加载默认配置。
    /// </summary>
    private void LoadDefaultProfiles()
    {
        _logger.LogInformation("Loading default compatibility profiles");

        // 数科 OFD 阅读器
        _profiles["Suwell"] = new CompatibilityProfile
        {
            Name = "Suwell",
            Version = "9.x",
            SupportedFeatures = new List<string> { "basic", "annotation", "form", "signature" },
            UnsupportedFeatures = new List<string> { "video", "audio", "javascript", "3d" }
        };

        // 福昕 OFD 阅读器
        _profiles["Foxit"] = new CompatibilityProfile
        {
            Name = "Foxit",
            Version = "11.x",
            SupportedFeatures = new List<string> { "basic", "annotation", "form", "signature", "bookmark" },
            UnsupportedFeatures = new List<string> { "video", "audio", "javascript" }
        };

        // 通用基线配置
        _profiles["Baseline"] = new CompatibilityProfile
        {
            Name = "Baseline",
            Version = "1.0",
            SupportedFeatures = new List<string> { "basic", "text", "image", "vector" },
            UnsupportedFeatures = new List<string> { "video", "audio", "javascript", "3d", "attachment", "form" }
        };

        _logger.LogInformation("Loaded {Count} default profiles", _profiles.Count);
    }

    /// <summary>
    /// 获取指定配置。
    /// </summary>
    /// <param name="profileName">配置名称</param>
    /// <returns>配置对象，如果不存在则返回 null</returns>
    public CompatibilityProfile? GetProfile(string profileName)
    {
        if (string.IsNullOrWhiteSpace(profileName))
        {
            return null;
        }

        return _profiles.TryGetValue(profileName, out var profile) ? profile : null;
    }

    /// <summary>
    /// 获取所有配置名称。
    /// </summary>
    public IReadOnlyList<string> GetProfileNames()
    {
        return _profiles.Keys.ToList().AsReadOnly();
    }

    /// <summary>
    /// 检查功能是否支持。
    /// </summary>
    /// <param name="profileName">配置名称</param>
    /// <param name="featureName">功能名称</param>
    /// <returns>如果支持则返回 true</returns>
    public bool IsFeatureSupported(string profileName, string featureName)
    {
        var profile = GetProfile(profileName);
        if (profile == null)
        {
            _logger.LogWarning("Profile not found: {ProfileName}", profileName);
            return false;
        }

        return profile.SupportedFeatures.Contains(featureName, StringComparer.OrdinalIgnoreCase);
    }

    /// <summary>
    /// 检查功能是否不支持。
    /// </summary>
    public bool IsFeatureUnsupported(string profileName, string featureName)
    {
        var profile = GetProfile(profileName);
        if (profile == null)
        {
            return false;
        }

        return profile.UnsupportedFeatures.Contains(featureName, StringComparer.OrdinalIgnoreCase);
    }
}

/// <summary>
/// 兼容性配置。
/// </summary>
public sealed class CompatibilityProfile
{
    /// <summary>
    /// 配置名称（阅读器名称）。
    /// </summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// 版本信息。
    /// </summary>
    public string Version { get; set; } = string.Empty;

    /// <summary>
    /// 支持的功能列表。
    /// </summary>
    public List<string> SupportedFeatures { get; set; } = new();

    /// <summary>
    /// 不支持的功能列表。
    /// </summary>
    public List<string> UnsupportedFeatures { get; set; } = new();
}
