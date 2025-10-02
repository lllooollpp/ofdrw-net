using System;

namespace OfdrwNet.Core.Color;

/// <summary>
/// ICC 颜色配置文件描述。
/// </summary>
public sealed class ColorProfile
{
    /// <summary>
    /// 创建颜色配置文件描述。
    /// </summary>
    public ColorProfile(string profileId, string? filePath, string fallback = "sRGB")
    {
        if (string.IsNullOrWhiteSpace(profileId))
        {
            throw new ArgumentException("Profile id cannot be null or empty.", nameof(profileId));
        }

        if (string.IsNullOrWhiteSpace(fallback))
        {
            throw new ArgumentException("Fallback profile cannot be empty.", nameof(fallback));
        }

        ProfileId = profileId;
        FilePath = filePath;
        Fallback = fallback;
    }

    /// <summary>
    /// 颜色配置文件标识。
    /// </summary>
    public string ProfileId { get; }

    /// <summary>
    /// 文件路径，若为 <c>null</c> 则表示使用回退配置。
    /// </summary>
    public string? FilePath { get; }

    /// <summary>
    /// 回退配置（默认 sRGB）。
    /// </summary>
    public string Fallback { get; }

    /// <summary>
    /// 最近一次 ΔE 统计。
    /// </summary>
    public ColorDeltaStats DeltaEStats { get; private set; } = new();

    /// <summary>
    /// 配置是否为回退配置。
    /// </summary>
    public bool IsFallback => string.IsNullOrEmpty(FilePath);

    /// <summary>
    /// 最近一次评估的时间。
    /// </summary>
    public DateTime? LastEvaluatedAt { get; private set; }

    /// <summary>
    /// 更新 ΔE 统计。
    /// </summary>
    public void UpdateDeltaE(ColorDeltaStats stats, DateTime? evaluatedAt = null)
    {
        DeltaEStats = stats ?? throw new ArgumentNullException(nameof(stats));
        LastEvaluatedAt = evaluatedAt ?? DateTime.UtcNow;
    }

    /// <inheritdoc />
    public override string ToString()
    {
        var profile = IsFallback ? $"fallback={Fallback}" : FilePath;
        return $"ColorProfile[{ProfileId}, {profile}, ΔE(avg={DeltaEStats.Average:F2}, max={DeltaEStats.Max:F2})]";
    }

    /// <summary>
    /// 创建回退配置。
    /// </summary>
    public static ColorProfile CreateFallback(string fallback = "sRGB")
    {
        return new ColorProfile($"fallback::{fallback}", null, fallback);
    }
}
