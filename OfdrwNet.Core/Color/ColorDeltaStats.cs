using System;
using System.Collections.Generic;
using System.Linq;

namespace OfdrwNet.Core.Color;

/// <summary>
/// ΔE2000 统计信息。
/// </summary>
public sealed class ColorDeltaStats
{
    /// <summary>
    /// 创建默认统计信息，平均值和最大值均为 0。
    /// </summary>
    public ColorDeltaStats()
    {
    }

    /// <summary>
    /// ΔE 平均值。
    /// </summary>
    public double Average { get; init; }

    /// <summary>
    /// ΔE 最大值。
    /// </summary>
    public double Max { get; init; }

    /// <summary>
    /// 从样本集合构建统计信息。
    /// </summary>
    public static ColorDeltaStats FromSamples(IEnumerable<double> samples)
    {
        if (samples is null)
        {
            throw new ArgumentNullException(nameof(samples));
        }

        var list = samples as IList<double> ?? samples.ToList();
        if (list.Count == 0)
        {
            return new ColorDeltaStats();
        }

        var average = list.Average();
        var max = list.Max();
        return new ColorDeltaStats
        {
            Average = average,
            Max = max
        };
    }
}
