using System;

namespace OfdrwNet.Core.Versioning;

/// <summary>
/// 版本链策略配置。
/// </summary>
public sealed class VersionPolicy
{
    private int _maxChain = 30;
    private double _sizeLimitRatio = 3.0;

    /// <summary>
    /// 最大允许的版本链长度。
    /// </summary>
    public int MaxChain
    {
        get => _maxChain;
        init
        {
            if (value <= 0)
            {
                throw new ArgumentOutOfRangeException(nameof(value), "MaxChain must be greater than zero.");
            }

            _maxChain = value;
        }
    }

    /// <summary>
    /// 增量大小与基线大小的比例上限。
    /// </summary>
    public double SizeLimitRatio
    {
        get => _sizeLimitRatio;
        init
        {
            if (value <= 0)
            {
                throw new ArgumentOutOfRangeException(nameof(value), "SizeLimitRatio must be positive.");
            }

            _sizeLimitRatio = value;
        }
    }

    /// <summary>
    /// 是否自动压缩版本链。
    /// </summary>
    public bool AutoCompact { get; init; } = true;

    /// <summary>
    /// 版本最大保留时长。
    /// </summary>
    public TimeSpan? MaxAge { get; init; }

    /// <summary>
    /// 检查策略是否满足约束。
    /// </summary>
    public void Validate()
    {
        if (MaxChain > 30)
        {
            throw new InvalidOperationException("MaxChain exceeds DR-30 limit (30).");
        }

        if (SizeLimitRatio > 3.0)
        {
            throw new InvalidOperationException("SizeLimitRatio exceeds DR-31 limit (3x).");
        }
    }

    /// <inheritdoc />
    public override string ToString()
    {
        return $"VersionPolicy[maxChain={MaxChain}, sizeRatio={SizeLimitRatio:0.##}, autoCompact={AutoCompact}]";
    }
}
