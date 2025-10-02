using System;
using System.Threading;
using System.Threading.Tasks;

namespace OfdrwNet.Converter.ColorManagement;

/// <summary>
/// 颜色空间转换与ΔE2000计算契约
/// </summary>
/// <remarks>
/// 实现此接口以提供颜色空间转换能力,支持:
/// - ICC配置文件加载与解析
/// - CMYK↔RGB↔Lab转换
/// - CIE ΔE2000色差计算
/// - 回退策略 (ICC不可用时使用sRGB近似)
/// </remarks>
public interface IColorSpaceConverter
{
    /// <summary>
    /// 转换颜色值到目标色彩空间
    /// </summary>
    /// <param name="source">源颜色值</param>
    /// <param name="targetSpace">目标色彩空间</param>
    /// <param name="intent">渲染意图</param>
    /// <param name="ct">取消令牌</param>
    /// <returns>转换结果,包含转换后的颜色值和ΔE2000</returns>
    /// <exception cref="ArgumentNullException">source为null</exception>
    /// <exception cref="NotSupportedException">不支持的色彩空间组合</exception>
    /// <remarks>
    /// 转换过程:
    /// 1. 将源颜色转为Lab色彩空间 (使用ICC或近似算法)
    /// 2. 根据渲染意图调整Lab值
    /// 3. 转为目标色彩空间
    /// 4. 计算原始Lab与转换后Lab的ΔE2000
    ///
    /// 性能要求 (DR-18):
    /// - 单次转换 < 1ms
    /// - ΔE2000最大值 ≤ 2.0
    /// - ΔE2000平均值 ≤ 0.8
    /// </remarks>
    Task<ColorConversionResult> ConvertAsync(
        ColorValue source,
        ColorSpace targetSpace,
        RenderingIntent intent,
        CancellationToken ct = default);

    /// <summary>
    /// 计算两个颜色值之间的CIE ΔE2000色差
    /// </summary>
    /// <param name="a">颜色A</param>
    /// <param name="b">颜色B</param>
    /// <returns>ΔE2000值 (0表示相同,值越大差异越大)</returns>
    /// <exception cref="ArgumentNullException">a或b为null</exception>
    /// <remarks>
    /// 使用CIE ΔE2000算法 (ISO/CIE 11664-6:2014):
    /// - 考虑亮度、色度、色相的感知差异
    /// - 引入旋转项修正蓝色区域
    /// - 加权系数: kL=1, kC=1, kH=1
    ///
    /// 阈值参考:
    /// - ΔE < 1.0: 人眼几乎无法察觉
    /// - ΔE < 2.0: 可接受差异 (本项目要求)
    /// - ΔE > 5.0: 明显差异
    /// </remarks>
    double CalculateDeltaE2000(ColorValue a, ColorValue b);

    /// <summary>
    /// 加载ICC配置文件
    /// </summary>
    /// <param name="profilePath">ICC配置文件路径 (绝对路径或相对路径)</param>
    /// <param name="ct">取消令牌</param>
    /// <returns>解析后的颜色配置对象</returns>
    /// <exception cref="FileNotFoundException">配置文件不存在</exception>
    /// <exception cref="InvalidDataException">ICC文件格式无效</exception>
    /// <remarks>
    /// 支持的ICC版本: v2 (ICC.1:2001-04), v4 (ICC.1:2010)
    ///
    /// 配置文件查找顺序:
    /// 1. profilePath (若为绝对路径)
    /// 2. {WorkDir}/ColorProfiles/{profilePath}
    /// 3. 系统ICC目录 (%WINDIR%\System32\spool\drivers\color 或 /usr/share/color/icc)
    ///
    /// 常用配置:
    /// - sRGB-IEC61966-2.1.icc: 标准sRGB
    /// - CoatedFOGRA39.icc: 印刷CMYK
    /// - AdobeRGB1998.icc: 宽色域RGB
    /// </remarks>
    Task<Domain.ColorProfile> LoadProfileAsync(
        string profilePath,
        CancellationToken ct = default);

    /// <summary>
    /// 获取回退策略信息
    /// </summary>
    /// <returns>当前回退模式和原因</returns>
    /// <remarks>
    /// 回退场景:
    /// - ICC配置文件缺失或损坏 → sRGB近似
    /// - 不支持的色彩空间 → 线性转换
    /// - 内存不足 → 禁用缓存
    /// </remarks>
    FallbackInfo GetFallbackInfo();
}

/// <summary>
/// 颜色转换结果
/// </summary>
public sealed class ColorConversionResult
{
    /// <summary>
    /// 转换后的颜色值
    /// </summary>
    public required ColorValue ConvertedValue { get; init; }

    /// <summary>
    /// ΔE2000色差 (源颜色与转换后颜色)
    /// </summary>
    public double DeltaE { get; init; }

    /// <summary>
    /// 是否使用了回退策略
    /// </summary>
    public bool UsedFallback { get; init; }

    /// <summary>
    /// 回退原因 (若UsedFallback=true)
    /// </summary>
    public string? FallbackReason { get; init; }

    /// <summary>
    /// 转换耗时 (仅调试)
    /// </summary>
    public TimeSpan? ConversionTime { get; init; }
}

/// <summary>
/// 回退信息
/// </summary>
public sealed class FallbackInfo
{
    /// <summary>
    /// 是否处于回退模式
    /// </summary>
    public bool IsFallbackMode { get; init; }

    /// <summary>
    /// 回退原因
    /// </summary>
    public string? Reason { get; init; }

    /// <summary>
    /// 当前使用的策略
    /// </summary>
    public required string Strategy { get; init; }
}

/// <summary>
/// 契约测试辅助 - 预定义测试颜色
/// </summary>
public static class TestColors
{
    /// <summary>
    /// 纯黑 (RGB)
    /// </summary>
    public static ColorValue Black => new()
    {
        Space = ColorSpace.RGB,
        Components = [0.0, 0.0, 0.0]
    };

    /// <summary>
    /// 纯白 (RGB)
    /// </summary>
    public static ColorValue White => new()
    {
        Space = ColorSpace.RGB,
        Components = [1.0, 1.0, 1.0]
    };

    /// <summary>
    /// 纯红 (RGB)
    /// </summary>
    public static ColorValue Red => new()
    {
        Space = ColorSpace.RGB,
        Components = [1.0, 0.0, 0.0]
    };

    /// <summary>
    /// 50%灰 (RGB)
    /// </summary>
    public static ColorValue Gray50 => new()
    {
        Space = ColorSpace.RGB,
        Components = [0.5, 0.5, 0.5]
    };

    /// <summary>
    /// 纯青 (CMYK)
    /// </summary>
    public static ColorValue Cyan => new()
    {
        Space = ColorSpace.CMYK,
        Components = [1.0, 0.0, 0.0, 0.0]
    };
}
