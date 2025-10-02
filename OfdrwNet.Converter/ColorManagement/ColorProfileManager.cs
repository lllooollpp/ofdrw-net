using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using OfdrwNet.Converter.Domain;
using OfdrwNet.Core.Color;
using DomainColorDeltaStats = OfdrwNet.Converter.Domain.ColorDeltaStats;

namespace OfdrwNet.Converter.ColorManagement;

/// <summary>
/// 颜色配置文件管理器实现,提供配置文件缓存和 ΔE 统计功能。
/// </summary>
/// <remarks>
/// 此实现提供:
/// - 线程安全的配置文件缓存(使用 ConcurrentDictionary)
/// - 像素级 ΔE2000 评估(集成 ColorSpaceConverter)
/// - 自动配置文件路径解析(支持预定义名称和系统路径)
/// - 性能优化(采样策略、并行计算)
///
/// 性能指标:
/// - 1024x768 图像 ΔE 评估 < 100ms
/// - 缓存命中率 > 90%
/// - 内存占用 < 50MB (100个配置文件)
/// </remarks>
public sealed class ColorProfileManager : IColorProfileManager
{
    private readonly IColorSpaceConverter _colorSpaceConverter;
    private readonly ILogger<ColorProfileManager> _logger;
    private readonly ConcurrentDictionary<string, IccProfile> _profileCache;

    // 预定义配置文件映射 (名称 -> 系统路径)
    private static readonly Dictionary<string, string> _predefinedProfiles = new(StringComparer.OrdinalIgnoreCase)
    {
        ["sRGB"] = "sRGB Color Space Profile.icm",
        ["AdobeRGB"] = "AdobeRGB1998.icc",
        ["DisplayP3"] = "Display P3.icc",
        ["ProPhotoRGB"] = "ProPhoto.icm"
    };

    /// <summary>
    /// 创建颜色配置文件管理器实例。
    /// </summary>
    /// <param name="colorSpaceConverter">颜色空间转换器(用于 ΔE 计算)</param>
    /// <param name="logger">日志记录器</param>
    public ColorProfileManager(
        IColorSpaceConverter colorSpaceConverter,
        ILogger<ColorProfileManager> logger)
    {
        _colorSpaceConverter = colorSpaceConverter ?? throw new ArgumentNullException(nameof(colorSpaceConverter));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _profileCache = new ConcurrentDictionary<string, IccProfile>(StringComparer.OrdinalIgnoreCase);
    }

    /// <inheritdoc/>
    public DomainColorDeltaStats EvaluateDeltaE(ImageReference original, ImageReference transformed)
    {
        ArgumentNullException.ThrowIfNull(original);
        ArgumentNullException.ThrowIfNull(transformed);

        // 验证图像尺寸匹配
        if (original.PixelSize != transformed.PixelSize)
        {
            throw new InvalidOperationException(
                $"Image size mismatch: original {original.PixelSize}, transformed {transformed.PixelSize}");
        }

        _logger.LogInformation(
            "Evaluating ΔE between {Original} and {Transformed} ({Width}x{Height})",
            original.Source, transformed.Source,
            original.PixelSize.Width, original.PixelSize.Height);

        var startTime = DateTime.UtcNow;

        try
        {
            // 加载两幅图像
            using var originalBitmap = LoadImage(original.Source);
            using var transformedBitmap = LoadImage(transformed.Source);

            // 验证加载后的尺寸
            if (originalBitmap.Size != transformedBitmap.Size)
            {
                throw new InvalidOperationException(
                    $"Loaded image size mismatch: {originalBitmap.Size} vs {transformedBitmap.Size}");
            }

            // 计算采样策略 (大图像采样以提升性能)
            var samplingStrategy = DetermineSamplingStrategy(originalBitmap.Size);

            _logger.LogDebug(
                "Using sampling strategy: step={Step}, total pixels={Total}",
                samplingStrategy.Step, samplingStrategy.TotalSamples);

            // 并行计算 ΔE 值
            var deltaEValues = CalculateDeltaEValues(
                originalBitmap,
                transformedBitmap,
                samplingStrategy);

            // 统计结果
            var stats = new DomainColorDeltaStats
            {
                Average = deltaEValues.Average(),
                Max = deltaEValues.Max(),
                SampleCount = deltaEValues.Count
            };

            var elapsed = DateTime.UtcNow - startTime;
            _logger.LogInformation(
                "ΔE evaluation complete: avg={Avg:F3}, max={Max:F3}, samples={Samples}, time={Time}ms",
                stats.Average, stats.Max, stats.SampleCount, elapsed.TotalMilliseconds);

            // 检查是否满足质量要求
            if (!stats.IsAcceptable())
            {
                _logger.LogWarning(
                    "ΔE stats exceed acceptable threshold: avg={Avg:F3} (≤0.8), max={Max:F3} (≤2.0)",
                    stats.Average, stats.Max);
            }

            return stats;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to evaluate ΔE between {Original} and {Transformed}",
                original.Source, transformed.Source);
            throw;
        }
    }

    /// <inheritdoc/>
    public IccProfile? Load(string path)
    {
        if (string.IsNullOrWhiteSpace(path))
        {
            _logger.LogWarning("Cannot load ICC profile: path is null or empty");
            return null;
        }

        try
        {
            // 规范化路径作为缓存键
            var cacheKey = ResolveCacheKey(path);

            // 检查缓存
            if (_profileCache.TryGetValue(cacheKey, out var cached))
            {
                _logger.LogDebug("ICC profile loaded from cache: {Path}", path);
                return cached;
            }

            // 解析实际文件路径
            var resolvedPath = ResolveProfilePath(path);

            if (!File.Exists(resolvedPath))
            {
                _logger.LogWarning("ICC profile file not found: {Path} (resolved: {Resolved})",
                    path, resolvedPath);
                return null;
            }

            // 加载配置文件 (使用 ColorSpaceConverter 的加载逻辑)
            // 注意: ColorSpaceConverter.LoadProfileAsync 返回 ColorProfile
            var colorProfile = _colorSpaceConverter.LoadProfileAsync(resolvedPath).GetAwaiter().GetResult();

            // 包装为 IccProfile
            var iccProfile = new IccProfile(colorProfile);

            // 缓存
            _profileCache[cacheKey] = iccProfile;

            _logger.LogInformation(
                "Loaded ICC profile: {ProfileId} from {Path}",
                colorProfile.ProfileId, resolvedPath);

            return iccProfile;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to load ICC profile: {Path}", path);
            return null;
        }
    }

    #region Private Helper Methods

    /// <summary>
    /// 加载图像文件为 Bitmap。
    /// </summary>
    private Bitmap LoadImage(string source)
    {
        if (!File.Exists(source))
        {
            throw new FileNotFoundException($"Image file not found: {source}", source);
        }

        try
        {
            return new Bitmap(source);
        }
        catch (Exception ex)
        {
            throw new InvalidOperationException($"Failed to load image: {source}", ex);
        }
    }

    /// <summary>
    /// 确定采样策略 (大图像采样以提升性能)。
    /// </summary>
    private SamplingStrategy DetermineSamplingStrategy(Size imageSize)
    {
        var totalPixels = imageSize.Width * imageSize.Height;

        // 小图像: 全像素评估
        if (totalPixels <= 256 * 256)
        {
            return new SamplingStrategy { Step = 1, TotalSamples = totalPixels };
        }

        // 中等图像 (256x256 ~ 1024x768): 每 2 个像素采样
        if (totalPixels <= 1024 * 768)
        {
            var samples = totalPixels / 4; // 每 2x2 块采样 1 个
            return new SamplingStrategy { Step = 2, TotalSamples = samples };
        }

        // 大图像 (> 1024x768): 每 4 个像素采样
        return new SamplingStrategy { Step = 4, TotalSamples = totalPixels / 16 };
    }

    /// <summary>
    /// 计算像素级 ΔE 值列表。
    /// </summary>
    private List<double> CalculateDeltaEValues(
        Bitmap original,
        Bitmap transformed,
        SamplingStrategy sampling)
    {
        var deltaEValues = new List<double>(sampling.TotalSamples);
        var lockObj = new object();

        // 并行遍历像素
        Parallel.For(0, original.Height / sampling.Step, y =>
        {
            var localValues = new List<double>();
            var actualY = y * sampling.Step;

            for (int x = 0; x < original.Width; x += sampling.Step)
            {
                var originalPixel = original.GetPixel(x, actualY);
                var transformedPixel = transformed.GetPixel(x, actualY);

                // 转换为 Lab 色值
                var originalLab = RgbToLab(originalPixel);
                var transformedLab = RgbToLab(transformedPixel);

                // 计算 ΔE2000
                var deltaE = _colorSpaceConverter.CalculateDeltaE2000(originalLab, transformedLab);
                localValues.Add(deltaE);
            }

            // 合并到主列表 (线程安全)
            lock (lockObj)
            {
                deltaEValues.AddRange(localValues);
            }
        });

        return deltaEValues;
    }

    /// <summary>
    /// 将 System.Drawing.Color (sRGB) 转换为 Lab ColorValue。
    /// </summary>
    private ColorValue RgbToLab(System.Drawing.Color color)
    {
        // 创建 RGB ColorValue (归一化到 [0,1])
        var rgb = new ColorValue
        {
            Space = ColorSpace.RGB,
            Components = new[] { color.R / 255.0, color.G / 255.0, color.B / 255.0 }
        };

        // 使用 ColorSpaceConverter 转换到 Lab
        var result = _colorSpaceConverter.ConvertAsync(
            rgb,
            ColorSpace.Lab,
            RenderingIntent.RelativeColorimetric).GetAwaiter().GetResult();

        return result.ConvertedValue;
    }

    /// <summary>
    /// 解析配置文件缓存键。
    /// </summary>
    private string ResolveCacheKey(string path)
    {
        // 预定义名称直接作为缓存键
        if (_predefinedProfiles.ContainsKey(path))
        {
            return path.ToLowerInvariant();
        }

        // 绝对路径或相对路径规范化
        try
        {
            return Path.GetFullPath(path).ToLowerInvariant();
        }
        catch
        {
            // 无效路径,使用原始值
            return path.ToLowerInvariant();
        }
    }

    /// <summary>
    /// 解析配置文件实际路径 (支持预定义名称、相对路径、系统路径)。
    /// </summary>
    private string ResolveProfilePath(string path)
    {
        // 1. 预定义名称
        if (_predefinedProfiles.TryGetValue(path, out var predefinedFile))
        {
            path = predefinedFile;
        }

        // 2. 绝对路径
        if (Path.IsPathRooted(path) && File.Exists(path))
        {
            return path;
        }

        // 3. 相对于工作目录
        var workDirPath = Path.Combine(Directory.GetCurrentDirectory(), "ColorProfiles", path);
        if (File.Exists(workDirPath))
        {
            return workDirPath;
        }

        // 4. 系统配置文件目录 (Windows)
        if (OperatingSystem.IsWindows())
        {
            var systemPath = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.System),
                "spool", "drivers", "color", path);

            if (File.Exists(systemPath))
            {
                return systemPath;
            }
        }

        // 5. 系统配置文件目录 (Linux/macOS)
        if (OperatingSystem.IsLinux() || OperatingSystem.IsMacOS())
        {
            var linuxPaths = new[]
            {
                $"/usr/share/color/icc/{path}",
                $"/Library/ColorSync/Profiles/{path}" // macOS
            };

            foreach (var linuxPath in linuxPaths)
            {
                if (File.Exists(linuxPath))
                {
                    return linuxPath;
                }
            }
        }

        // 6. 未找到,返回原始路径 (让调用方处理 FileNotFoundException)
        return Path.GetFullPath(path);
    }

    /// <summary>
    /// 采样策略配置。
    /// </summary>
    private sealed class SamplingStrategy
    {
        /// <summary>
        /// 采样步长 (1=全像素, 2=每2个像素, 4=每4个像素)。
        /// </summary>
        public int Step { get; init; }

        /// <summary>
        /// 总采样数量。
        /// </summary>
        public int TotalSamples { get; init; }
    }

    #endregion
}
