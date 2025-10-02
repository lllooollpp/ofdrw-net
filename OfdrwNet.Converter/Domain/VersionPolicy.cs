namespace OfdrwNet.Converter.Domain;

/// <summary>
/// 版本策略配置
/// </summary>
public sealed class VersionPolicy
{
    /// <summary>
    /// 最大版本链长度（默认 30）
    /// </summary>
    public int MaxChain { get; init; } = 30;

    /// <summary>
    /// 大小限制比率（diff 总大小不超过基准文件的此倍数，默认 3）
    /// </summary>
    public double SizeLimitRatio { get; init; } = 3.0;

    /// <summary>
    /// 是否启用自动压实
    /// </summary>
    public bool AutoCompact { get; init; } = true;

    /// <summary>
    /// 压实触发阈值（版本链长度达到此值时触发压实）
    /// </summary>
    public int CompactThreshold { get; init; } = 20;

    /// <summary>
    /// 验证版本策略配置的有效性
    /// </summary>
    public void Validate()
    {
        if (MaxChain <= 0)
        {
            throw new ArgumentException("MaxChain must be positive");
        }

        if (MaxChain > 30)
        {
            throw new ArgumentException("MaxChain cannot exceed 30");
        }

        if (SizeLimitRatio <= 0)
        {
            throw new ArgumentException("SizeLimitRatio must be positive");
        }

        if (SizeLimitRatio > 3.0)
        {
            throw new ArgumentException("SizeLimitRatio cannot exceed 3.0");
        }

        if (CompactThreshold > MaxChain)
        {
            throw new ArgumentException("CompactThreshold cannot exceed MaxChain");
        }
    }
}
