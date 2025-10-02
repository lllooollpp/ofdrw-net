using System.Collections.Generic;

namespace OfdrwNet.Converter.Domain;

/// <summary>
/// 字体资源描述
/// </summary>
public sealed class FontResource
{
    /// <summary>
    /// 字体名称
    /// </summary>
    public required string Name { get; init; }

    /// <summary>
    /// 子集标签（用于字体子集化）
    /// </summary>
    public string? SubsetTag { get; init; }

    /// <summary>
    /// 字符编码映射表
    /// </summary>
    public IReadOnlyDictionary<int, int>? EncodingMap { get; init; }

    /// <summary>
    /// 字体文件引用路径
    /// </summary>
    public string? FileRef { get; init; }

    /// <summary>
    /// 是否嵌入字体文件
    /// </summary>
    public bool IsEmbedded { get; init; }

    /// <summary>
    /// 字体系列
    /// </summary>
    public string? Family { get; init; }

    /// <summary>
    /// 字体样式（粗体、斜体等）
    /// </summary>
    public string? Style { get; init; }

    /// <summary>
    /// 字体使用的字符集
    /// </summary>
    public IReadOnlySet<char>? UsedCharacters { get; init; }
}
