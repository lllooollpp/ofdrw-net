using OfdrwNet.Converter.Domain;
using OfdrwNet.Core.Color;
using DomainColorDeltaStats = OfdrwNet.Converter.Domain.ColorDeltaStats;
using DomainColorProfile = OfdrwNet.Converter.Domain.ColorProfile;

namespace OfdrwNet.Converter.ColorManagement;

/// <summary>
/// 颜色配置文件管理器接口,提供配置文件加载、缓存和 ΔE 统计功能。
/// </summary>
/// <remarks>
/// 此接口负责:
/// - ICC 配置文件的加载和缓存管理
/// - 图像对比的 ΔE 评估(像素级色差统计)
/// - 配置文件元数据管理
///
/// 性能要求 (DR-18):
/// - 单次 ΔE 评估应在 100ms 内完成(1024x768 图像)
/// - 配置文件缓存命中率应 > 90%
/// - ΔE 平均值 ≤ 0.8, 最大值 ≤ 2.0
/// </remarks>
public interface IColorProfileManager
{
    /// <summary>
    /// 评估两幅图像之间的 ΔE 色差统计。
    /// </summary>
    /// <param name="original">原始图像引用</param>
    /// <param name="transformed">转换后图像引用</param>
    /// <returns>ΔE 统计数据,包含平均值、最大值和样本数</returns>
    /// <exception cref="ArgumentNullException">当任一图像引用为 null 时抛出</exception>
    /// <exception cref="FileNotFoundException">当图像文件不存在时抛出</exception>
    /// <exception cref="InvalidOperationException">当图像尺寸不匹配时抛出</exception>
    /// <remarks>
    /// 此方法执行像素级 ΔE2000 计算:
    /// 1. 加载两幅图像的像素数据
    /// 2. 确保尺寸匹配,否则抛出异常
    /// 3. 对每个像素计算 ΔE2000 值
    /// 4. 返回平均值、最大值和总样本数
    ///
    /// 性能考虑:
    /// - 对于大图像,考虑采样策略(如每 N 个像素采样)
    /// - 使用并行计算加速处理
    /// - 缓存中间结果以避免重复计算
    /// </remarks>
    DomainColorDeltaStats EvaluateDeltaE(ImageReference original, ImageReference transformed);

    /// <summary>
    /// 加载指定路径的 ICC 配置文件。
    /// </summary>
    /// <param name="path">配置文件路径(支持绝对路径、相对路径或预定义名称如 "sRGB")</param>
    /// <returns>ICC 配置文件对象,如果加载失败则返回 null</returns>
    /// <remarks>
    /// 此方法支持:
    /// - 绝对路径: "C:\Profiles\AdobeRGB.icc"
    /// - 相对路径: "ColorProfiles\sRGB.icc" (相对于工作目录)
    /// - 预定义名称: "sRGB", "AdobeRGB", "DisplayP3" (映射到系统配置文件)
    /// - 系统配置文件: 自动搜索 Windows System32\spool\drivers\color 或 Linux /usr/share/color/icc
    ///
    /// 缓存策略:
    /// - 首次加载后缓存到内存(键为规范化路径)
    /// - 后续调用直接返回缓存对象
    /// - 缓存不设过期时间(假设配置文件不会运行时变更)
    ///
    /// 错误处理:
    /// - 文件不存在: 返回 null 并记录警告日志
    /// - 解析失败: 返回 null 并记录错误日志
    /// - 不影响主流程,调用方应检查返回值并使用回退策略
    /// </remarks>
    IccProfile? Load(string path);
}

/// <summary>
/// ICC 配置文件封装,扩展自 ColorProfile 领域模型。
/// </summary>
/// <remarks>
/// 此类是 ColorProfile 的运行时视图,增加了:
/// - 解析后的 ICC 二进制数据(未来可集成 lcmsNET)
/// - 加载时间戳
/// - 缓存键
///
/// 当前版本为简化实现,仅包装 ColorProfile。
/// 完整实现应包含 ICC 解析逻辑(色彩空间、白点、转换矩阵等)。
/// </remarks>
public sealed class IccProfile
{
    /// <summary>
    /// 创建 ICC 配置文件实例。
    /// </summary>
    public IccProfile(DomainColorProfile profile)
    {
        Profile = profile ?? throw new ArgumentNullException(nameof(profile));
        LoadedAt = DateTime.UtcNow;
    }

    /// <summary>
    /// 底层颜色配置文件。
    /// </summary>
    public DomainColorProfile Profile { get; }

    /// <summary>
    /// 加载时间戳(UTC)。
    /// </summary>
    public DateTime LoadedAt { get; }

    /// <summary>
    /// 配置文件规范化路径(用作缓存键)。
    /// </summary>
    public string CacheKey => Path.GetFullPath(Profile.Path);

    /// <summary>
    /// 指示是否为回退配置文件。
    /// </summary>
    public bool IsFallback => Profile.Fallback;

    /// <summary>
    /// 获取 ΔE 统计信息(如果可用)。
    /// </summary>
    public DomainColorDeltaStats? DeltaEStats => Profile.DeltaEStats;
}
