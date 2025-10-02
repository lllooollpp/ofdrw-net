using System;
using System.IO;
using OfdrwNet.Core.Diagnostics;

namespace OfdrwNet.Core.Color;

/// <summary>
/// Color profile manager with ΔE calculation (LittleCMS integration placeholder)
/// </summary>
public class ColorProfileManager
{
    private readonly IStructuredLogger? _logger;

    /// <summary>
    /// 初始化颜色配置管理器。
    /// </summary>
    /// <param name="logger">可选的结构化日志记录器。</param>
    public ColorProfileManager(IStructuredLogger? logger = null)
    {
        _logger = logger;
    }

    /// <summary>
    /// Calculate Delta E between two colors
    /// </summary>
    public double CalculateDeltaE(ColorProfile source, ColorProfile target, byte[] sourceRgb, byte[] targetRgb)
    {
        // Placeholder: actual LittleCMS integration in optimization phase
        // For now use simple Euclidean distance in RGB space
        var deltaR = sourceRgb[0] - targetRgb[0];
        var deltaG = sourceRgb[1] - targetRgb[1];
        var deltaB = sourceRgb[2] - targetRgb[2];

        var deltaE = Math.Sqrt(deltaR * deltaR + deltaG * deltaG + deltaB * deltaB);

        _logger?.LogInfo(LogEvents.ColorDelta, new
        {
            sourceProfile = source.ProfileId,
            targetProfile = target.ProfileId,
            deltaE
        });

        return deltaE;
    }

    /// <summary>
    /// Load ICC profile from file
    /// </summary>
    public ColorProfile LoadProfile(string path)
    {
        var profileId = Path.GetFileNameWithoutExtension(path);
        return new ColorProfile(profileId, path);
    }
}
