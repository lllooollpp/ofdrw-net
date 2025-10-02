namespace OfdrwNet.Converter.Domain;

/// <summary>
/// 颜色配置文件描述
/// </summary>
public sealed class ColorProfile
{
    /// <summary>
    /// 配置文件唯一标识符
    /// </summary>
    public required string ProfileId { get; init; }

    /// <summary>
    /// ICC 配置文件路径
    /// </summary>
    public required string Path { get; init; }

    /// <summary>
    /// 是否使用回退配置（sRGB）
    /// </summary>
    public bool Fallback { get; init; }

    /// <summary>
    /// ΔE 统计信息
    /// </summary>
    public ColorDeltaStats? DeltaEStats { get; init; }

    /// <summary>
    /// 配置文件类型（Input, Display, Output, DeviceLink等）
    /// </summary>
    public string? ProfileType { get; init; }

    /// <summary>
    /// 色彩空间（RGB, CMYK, Lab等）
    /// </summary>
    public string? ColorSpace { get; init; }
}

/// <summary>
/// ΔE 统计数据
/// </summary>
public sealed class ColorDeltaStats
{
    /// <summary>
    /// 平均 ΔE 值
    /// </summary>
    public required double Average { get; init; }

    /// <summary>
    /// 最大 ΔE 值
    /// </summary>
    public required double Max { get; init; }

    /// <summary>
    /// 样本数量
    /// </summary>
    public int SampleCount { get; init; }

    /// <summary>
    /// 验证 ΔE 值是否在可接受范围内
    /// </summary>
    public bool IsAcceptable(double avgThreshold = 0.8, double maxThreshold = 2.0)
    {
        return Average <= avgThreshold && Max <= maxThreshold;
    }
}
