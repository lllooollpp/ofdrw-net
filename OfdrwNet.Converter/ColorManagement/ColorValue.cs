namespace OfdrwNet.Converter.ColorManagement;

/// <summary>
/// 颜色值表示
/// </summary>
public sealed class ColorValue
{
    /// <summary>
    /// 颜色空间
    /// </summary>
    public required ColorSpace Space { get; init; }

    /// <summary>
    /// 颜色分量 (归一化到 0-1)
    /// </summary>
    /// <remarks>
    /// - RGB: [R, G, B] (3 components)
    /// - CMYK: [C, M, Y, K] (4 components)
    /// - Lab: [L, a, b] (3 components, L: 0-100, a/b: -128 to 127, 需归一化)
    /// - Gray: [Gray] (1 component)
    /// </remarks>
    public required double[] Components { get; init; }

    /// <summary>
    /// 验证颜色分量有效性
    /// </summary>
    public void Validate()
    {
        var expectedComponents = Space switch
        {
            ColorSpace.RGB => 3,
            ColorSpace.CMYK => 4,
            ColorSpace.Lab => 3,
            ColorSpace.Gray => 1,
            _ => throw new NotSupportedException($"Unsupported color space: {Space}")
        };

        if (Components.Length != expectedComponents)
        {
            throw new ArgumentException(
                $"ColorSpace {Space} requires {expectedComponents} components, but got {Components.Length}");
        }

        // 验证分量范围 (归一化到 0-1, Lab 除外)
        if (Space != ColorSpace.Lab)
        {
            foreach (var component in Components)
            {
                if (component < 0.0 || component > 1.0)
                {
                    throw new ArgumentException(
                        $"Component value {component} is out of range [0, 1] for {Space}");
                }
            }
        }
    }

    /// <summary>
    /// 转为调试字符串
    /// </summary>
    public override string ToString()
    {
        var components = string.Join(", ", Components.Select(c => c.ToString("F3")));
        return $"{Space}({components})";
    }
}

/// <summary>
/// 颜色空间枚举
/// </summary>
public enum ColorSpace
{
    /// <summary>
    /// RGB 色彩空间 (3 分量: R, G, B)
    /// </summary>
    RGB = 0,

    /// <summary>
    /// CMYK 色彩空间 (4 分量: C, M, Y, K)
    /// </summary>
    CMYK = 1,

    /// <summary>
    /// CIE Lab 色彩空间 (3 分量: L, a, b)
    /// </summary>
    Lab = 2,

    /// <summary>
    /// 灰度色彩空间 (1 分量: Gray)
    /// </summary>
    Gray = 3
}

/// <summary>
/// 渲染意图 (ICC Profile)
/// </summary>
public enum RenderingIntent
{
    /// <summary>
    /// 感知式 (适用于照片) - 保持整体色彩关系
    /// </summary>
    Perceptual = 0,

    /// <summary>
    /// 相对比色 (适用于 Logo) - 保持在色域内的颜色不变
    /// </summary>
    RelativeColorimetric = 1,

    /// <summary>
    /// 饱和度 (适用于图表) - 保持饱和度
    /// </summary>
    Saturation = 2,

    /// <summary>
    /// 绝对比色 (适用于校样) - 模拟纸张白点
    /// </summary>
    AbsoluteColorimetric = 3
}
