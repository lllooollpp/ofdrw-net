using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using OfdrwNet.Converter.Domain;

namespace OfdrwNet.Converter.ColorManagement;

/// <summary>
/// 颜色空间转换器实现 (基于 sRGB 近似算法)
/// </summary>
/// <remarks>
/// 初版使用内建算法,避免依赖 lcmsNET:
/// - CMYK → RGB: 简单减法 (1-CMY)*(1-K)
/// - RGB → Lab: sRGB → XYZ → Lab (D65 白点)
/// - Lab → RGB: Lab → XYZ → sRGB
/// - ΔE2000: CIE2000 标准公式
///
/// 后续优化:
/// - 集成 lcmsNET 进行真实 ICC Profile 转换
/// - 支持自定义 ICC 配置文件
/// </remarks>
public sealed class ColorSpaceConverter : IColorSpaceConverter
{
    private readonly ILogger<ColorSpaceConverter> _logger;
    private readonly Dictionary<string, ColorProfile> _profileCache = new();
    private bool _isFallbackMode;
    private string? _fallbackReason;

    public ColorSpaceConverter(ILogger<ColorSpaceConverter> logger)
    {
        _logger = logger;
        _isFallbackMode = true; // 初版始终使用 sRGB 近似
        _fallbackReason = "lcmsNET integration pending, using sRGB approximation";
    }

    /// <inheritdoc/>
    public async Task<ColorConversionResult> ConvertAsync(
        ColorValue source,
        ColorSpace targetSpace,
        RenderingIntent intent,
        CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(source);
        source.Validate();

        var startTime = DateTime.UtcNow;

        // 如果源和目标相同,直接返回
        if (source.Space == targetSpace)
        {
            return new ColorConversionResult
            {
                ConvertedValue = source,
                DeltaE = 0.0,
                UsedFallback = false,
                ConversionTime = TimeSpan.Zero
            };
        }

        // 转换为 Lab (中间色彩空间)
        var sourceLab = await ConvertToLabAsync(source, ct);

        // 从 Lab 转为目标色彩空间
        var converted = await ConvertFromLabAsync(sourceLab, targetSpace, ct);

        // 计算 ΔE2000
        var convertedLab = await ConvertToLabAsync(converted, ct);
        var deltaE = CalculateDeltaE2000(
            new ColorValue { Space = ColorSpace.Lab, Components = sourceLab },
            new ColorValue { Space = ColorSpace.Lab, Components = convertedLab }
        );

        var elapsed = DateTime.UtcNow - startTime;

        _logger.LogDebug(
            "Color conversion: {Source} → {Target}, ΔE={DeltaE:F3}, Time={Time}ms",
            source, converted, deltaE, elapsed.TotalMilliseconds);

        return new ColorConversionResult
        {
            ConvertedValue = converted,
            DeltaE = deltaE,
            UsedFallback = _isFallbackMode,
            FallbackReason = _fallbackReason,
            ConversionTime = elapsed
        };
    }

    /// <inheritdoc/>
    public double CalculateDeltaE2000(ColorValue a, ColorValue b)
    {
        ArgumentNullException.ThrowIfNull(a);
        ArgumentNullException.ThrowIfNull(b);

        // 转换为 Lab
        var labA = a.Space == ColorSpace.Lab ? a.Components : ConvertToLabAsync(a).Result;
        var labB = b.Space == ColorSpace.Lab ? b.Components : ConvertToLabAsync(b).Result;

        // CIE ΔE2000 算法 (简化版)
        // 参考: http://www.brucelindbloom.com/index.html?Eqn_DeltaE_CIE2000.html
        var L1 = labA[0];
        var a1 = labA[1];
        var b1 = labA[2];

        var L2 = labB[0];
        var a2 = labB[1];
        var b2 = labB[2];

        // 计算色度 C*
        var C1 = Math.Sqrt(a1 * a1 + b1 * b1);
        var C2 = Math.Sqrt(a2 * a2 + b2 * b2);
        var Cavg = (C1 + C2) / 2.0;

        // 计算 a' (旋转修正)
        var G = 0.5 * (1 - Math.Sqrt(Math.Pow(Cavg, 7) / (Math.Pow(Cavg, 7) + Math.Pow(25.0, 7))));
        var a1prime = (1 + G) * a1;
        var a2prime = (1 + G) * a2;

        // 计算 C' 和 h'
        var C1prime = Math.Sqrt(a1prime * a1prime + b1 * b1);
        var C2prime = Math.Sqrt(a2prime * a2prime + b2 * b2);

        var h1prime = Math.Atan2(b1, a1prime) * 180.0 / Math.PI;
        if (h1prime < 0) h1prime += 360.0;

        var h2prime = Math.Atan2(b2, a2prime) * 180.0 / Math.PI;
        if (h2prime < 0) h2prime += 360.0;

        // 计算差值
        var deltaLprime = L2 - L1;
        var deltaCprime = C2prime - C1prime;

        var deltahprime = 0.0;
        if (C1prime * C2prime != 0)
        {
            var hdiff = h2prime - h1prime;
            if (Math.Abs(hdiff) <= 180.0)
                deltahprime = hdiff;
            else if (hdiff > 180.0)
                deltahprime = hdiff - 360.0;
            else
                deltahprime = hdiff + 360.0;
        }

        var deltaHprime = 2.0 * Math.Sqrt(C1prime * C2prime) * Math.Sin(deltahprime * Math.PI / 360.0);

        // 计算平均值
        var Lprimeavg = (L1 + L2) / 2.0;
        var Cprimeavg = (C1prime + C2prime) / 2.0;

        var hprimeavg = 0.0;
        if (C1prime * C2prime != 0)
        {
            var hsum = h1prime + h2prime;
            if (Math.Abs(h1prime - h2prime) <= 180.0)
                hprimeavg = hsum / 2.0;
            else if (hsum < 360.0)
                hprimeavg = (hsum + 360.0) / 2.0;
            else
                hprimeavg = (hsum - 360.0) / 2.0;
        }

        // 加权系数
        var T = 1 - 0.17 * Math.Cos((hprimeavg - 30.0) * Math.PI / 180.0)
                 + 0.24 * Math.Cos(2.0 * hprimeavg * Math.PI / 180.0)
                 + 0.32 * Math.Cos((3.0 * hprimeavg + 6.0) * Math.PI / 180.0)
                 - 0.20 * Math.Cos((4.0 * hprimeavg - 63.0) * Math.PI / 180.0);

        var SL = 1 + (0.015 * Math.Pow(Lprimeavg - 50.0, 2)) / Math.Sqrt(20 + Math.Pow(Lprimeavg - 50.0, 2));
        var SC = 1 + 0.045 * Cprimeavg;
        var SH = 1 + 0.015 * Cprimeavg * T;

        var RT = -2.0 * Math.Sqrt(Math.Pow(Cprimeavg, 7) / (Math.Pow(Cprimeavg, 7) + Math.Pow(25.0, 7)))
                 * Math.Sin(60.0 * Math.Exp(-Math.Pow((hprimeavg - 275.0) / 25.0, 2)) * Math.PI / 180.0);

        // 最终 ΔE2000 (kL=kC=kH=1)
        var deltaE = Math.Sqrt(
            Math.Pow(deltaLprime / SL, 2) +
            Math.Pow(deltaCprime / SC, 2) +
            Math.Pow(deltaHprime / SH, 2) +
            RT * (deltaCprime / SC) * (deltaHprime / SH)
        );

        return deltaE;
    }

    /// <inheritdoc/>
    public async Task<ColorProfile> LoadProfileAsync(string profilePath, CancellationToken ct = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(profilePath);

        // 检查缓存
        if (_profileCache.TryGetValue(profilePath, out var cached))
        {
            _logger.LogDebug("Loaded ICC profile from cache: {Path}", profilePath);
            return cached;
        }

        // 查找配置文件
        var resolvedPath = ResolveProfilePath(profilePath);

        if (!File.Exists(resolvedPath))
        {
            throw new FileNotFoundException($"ICC profile not found: {profilePath}", resolvedPath);
        }

        // 读取配置文件 (初版仅记录元数据,不解析)
        var fileInfo = new FileInfo(resolvedPath);
        var profile = new ColorProfile
        {
            ProfileId = Path.GetFileNameWithoutExtension(profilePath),
            Path = resolvedPath,
            Fallback = true, // 初版始终使用回退 (sRGB)
            ColorSpace = "RGB", // 假设为 RGB (完整实现需解析 ICC header)
            DeltaEStats = null // 初版无统计
        };

        _profileCache[profilePath] = profile;

        _logger.LogInformation(
            "Loaded ICC profile: {ProfileId} ({Size} bytes)",
            profile.ProfileId, fileInfo.Length);

        return await Task.FromResult(profile);
    }

    /// <inheritdoc/>
    public FallbackInfo GetFallbackInfo()
    {
        return new FallbackInfo
        {
            IsFallbackMode = _isFallbackMode,
            Reason = _fallbackReason,
            Strategy = "sRGB approximation"
        };
    }

    #region Private Helper Methods

    /// <summary>
    /// 转换颜色到 Lab 色彩空间
    /// </summary>
    private Task<double[]> ConvertToLabAsync(ColorValue color, CancellationToken ct = default)
    {
        var result = color.Space switch
        {
            ColorSpace.Lab => color.Components,
            ColorSpace.RGB => RgbToLab(color.Components),
            ColorSpace.CMYK => RgbToLab(CmykToRgb(color.Components)),
            ColorSpace.Gray => RgbToLab(new[] { color.Components[0], color.Components[0], color.Components[0] }),
            _ => throw new NotSupportedException($"Conversion from {color.Space} to Lab not supported")
        };

        return Task.FromResult(result);
    }

    /// <summary>
    /// 从 Lab 转换到目标色彩空间
    /// </summary>
    private Task<ColorValue> ConvertFromLabAsync(double[] lab, ColorSpace targetSpace, CancellationToken ct = default)
    {
        var components = targetSpace switch
        {
            ColorSpace.Lab => lab,
            ColorSpace.RGB => LabToRgb(lab),
            ColorSpace.CMYK => RgbToCmyk(LabToRgb(lab)),
            ColorSpace.Gray => new[] { (LabToRgb(lab)[0] + LabToRgb(lab)[1] + LabToRgb(lab)[2]) / 3.0 },
            _ => throw new NotSupportedException($"Conversion from Lab to {targetSpace} not supported")
        };

        var result = new ColorValue
        {
            Space = targetSpace,
            Components = components
        };

        return Task.FromResult(result);
    }

    /// <summary>
    /// CMYK → RGB (简单减法)
    /// </summary>
    private double[] CmykToRgb(double[] cmyk)
    {
        var c = cmyk[0];
        var m = cmyk[1];
        var y = cmyk[2];
        var k = cmyk[3];

        return new[]
        {
            (1 - c) * (1 - k),
            (1 - m) * (1 - k),
            (1 - y) * (1 - k)
        };
    }

    /// <summary>
    /// RGB → CMYK (简单公式)
    /// </summary>
    private double[] RgbToCmyk(double[] rgb)
    {
        var r = rgb[0];
        var g = rgb[1];
        var b = rgb[2];

        var k = 1 - Math.Max(r, Math.Max(g, b));

        if (k >= 1.0)
        {
            return new[] { 0.0, 0.0, 0.0, 1.0 };
        }

        return new[]
        {
            (1 - r - k) / (1 - k),
            (1 - g - k) / (1 - k),
            (1 - b - k) / (1 - k),
            k
        };
    }

    /// <summary>
    /// RGB → XYZ → Lab (D65 白点)
    /// </summary>
    private double[] RgbToLab(double[] rgb)
    {
        // sRGB → 线性 RGB
        var r = GammaToLinear(rgb[0]);
        var g = GammaToLinear(rgb[1]);
        var b = GammaToLinear(rgb[2]);

        // 线性 RGB → XYZ (D65 矩阵)
        var x = r * 0.4124564 + g * 0.3575761 + b * 0.1804375;
        var y = r * 0.2126729 + g * 0.7151522 + b * 0.0721750;
        var z = r * 0.0193339 + g * 0.1191920 + b * 0.9503041;

        // XYZ → Lab
        return XyzToLab(x, y, z);
    }

    /// <summary>
    /// Lab → XYZ → RGB (D65 白点)
    /// </summary>
    private double[] LabToRgb(double[] lab)
    {
        // Lab → XYZ
        var (x, y, z) = LabToXyz(lab[0], lab[1], lab[2]);

        // XYZ → 线性 RGB
        var r = x * 3.2404542 + y * -1.5371385 + z * -0.4985314;
        var g = x * -0.9692660 + y * 1.8760108 + z * 0.0415560;
        var b = x * 0.0556434 + y * -0.2040259 + z * 1.0572252;

        // 线性 RGB → sRGB (伽马校正)
        return new[]
        {
            Clamp(LinearToGamma(r)),
            Clamp(LinearToGamma(g)),
            Clamp(LinearToGamma(b))
        };
    }

    /// <summary>
    /// XYZ → Lab
    /// </summary>
    private double[] XyzToLab(double x, double y, double z)
    {
        // D65 白点
        const double Xn = 0.95047;
        const double Yn = 1.00000;
        const double Zn = 1.08883;

        var fx = LabF(x / Xn);
        var fy = LabF(y / Yn);
        var fz = LabF(z / Zn);

        var L = 116.0 * fy - 16.0;
        var a = 500.0 * (fx - fy);
        var b = 200.0 * (fy - fz);

        return new[] { L, a, b };
    }

    /// <summary>
    /// Lab → XYZ
    /// </summary>
    private (double x, double y, double z) LabToXyz(double L, double a, double b)
    {
        const double Xn = 0.95047;
        const double Yn = 1.00000;
        const double Zn = 1.08883;

        var fy = (L + 16.0) / 116.0;
        var fx = a / 500.0 + fy;
        var fz = fy - b / 200.0;

        var x = Xn * LabFInv(fx);
        var y = Yn * LabFInv(fy);
        var z = Zn * LabFInv(fz);

        return (x, y, z);
    }

    /// <summary>
    /// Lab 转换函数
    /// </summary>
    private double LabF(double t)
    {
        const double delta = 6.0 / 29.0;
        return t > delta * delta * delta
            ? Math.Pow(t, 1.0 / 3.0)
            : t / (3.0 * delta * delta) + 4.0 / 29.0;
    }

    /// <summary>
    /// Lab 逆转换函数
    /// </summary>
    private double LabFInv(double t)
    {
        const double delta = 6.0 / 29.0;
        return t > delta
            ? t * t * t
            : 3.0 * delta * delta * (t - 4.0 / 29.0);
    }

    /// <summary>
    /// sRGB 伽马校正 (线性 → sRGB)
    /// </summary>
    private double LinearToGamma(double linear)
    {
        return linear <= 0.0031308
            ? 12.92 * linear
            : 1.055 * Math.Pow(linear, 1.0 / 2.4) - 0.055;
    }

    /// <summary>
    /// sRGB 伽马去校正 (sRGB → 线性)
    /// </summary>
    private double GammaToLinear(double gamma)
    {
        return gamma <= 0.04045
            ? gamma / 12.92
            : Math.Pow((gamma + 0.055) / 1.055, 2.4);
    }

    /// <summary>
    /// 钳制到 [0, 1]
    /// </summary>
    private double Clamp(double value)
    {
        return Math.Max(0.0, Math.Min(1.0, value));
    }

    /// <summary>
    /// 解析 ICC Profile 路径
    /// </summary>
    private string ResolveProfilePath(string profilePath)
    {
        // 1. 绝对路径
        if (Path.IsPathRooted(profilePath) && File.Exists(profilePath))
        {
            return profilePath;
        }

        // 2. 相对于工作目录的 ColorProfiles/
        var workDirPath = Path.Combine(Directory.GetCurrentDirectory(), "ColorProfiles", profilePath);
        if (File.Exists(workDirPath))
        {
            return workDirPath;
        }

        // 3. 系统 ICC 目录 (Windows)
        if (OperatingSystem.IsWindows())
        {
            var systemPath = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.System),
                "spool", "drivers", "color", profilePath);

            if (File.Exists(systemPath))
            {
                return systemPath;
            }
        }

        // 4. 系统 ICC 目录 (Linux/macOS)
        if (OperatingSystem.IsLinux() || OperatingSystem.IsMacOS())
        {
            var linuxPath = Path.Combine("/usr/share/color/icc", profilePath);
            if (File.Exists(linuxPath))
            {
                return linuxPath;
            }
        }

        // 未找到,返回原路径 (将在后续抛出 FileNotFoundException)
        return profilePath;
    }

    #endregion
}
