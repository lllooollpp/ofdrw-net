using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;

namespace OfdrwNet.Core.Resources;

/// <summary>
/// 表示一次转换过程中需要引用的字体资源元数据。
/// </summary>
public sealed class FontResource
{
    private readonly IReadOnlyDictionary<uint, ushort> _encodingMap;

    /// <summary>
    /// 初始化 <see cref="FontResource"/> 的新实例。
    /// </summary>
    /// <param name="name">字体名称（必填）。</param>
    /// <param name="subsetTag">PDF 子集标记，例如 <c>ABCDE+</c>。</param>
    /// <param name="encodingMap">Unicode → GlyphId 映射。</param>
    /// <param name="fileRef">字体文件在容器中的相对引用。</param>
    /// <param name="isEmbedded">字体文件是否在输出中嵌入。</param>
    /// <param name="supportsGb18030">是否覆盖 GB18030 字符集。</param>
    public FontResource(
        string name,
        string? subsetTag = null,
        IDictionary<uint, ushort>? encodingMap = null,
        string? fileRef = null,
        bool isEmbedded = true,
        bool supportsGb18030 = false)
    {
        if (string.IsNullOrWhiteSpace(name))
        {
            throw new ArgumentException("Font name cannot be null or whitespace.", nameof(name));
        }

        if (!string.IsNullOrWhiteSpace(subsetTag) && !subsetTag.Contains('+', StringComparison.Ordinal))
        {
            throw new ArgumentException("Subset tag should include '+' according to PDF subset naming rules (e.g. ABCDE+).", nameof(subsetTag));
        }

        if (encodingMap != null && encodingMap.Values.Any(v => v == 0))
        {
            throw new ArgumentException("Encoding map cannot contain glyph id 0 – reserved for .notdef entry.", nameof(encodingMap));
        }

        Name = name.Trim();
        SubsetTag = string.IsNullOrWhiteSpace(subsetTag) ? null : subsetTag.Trim();
        FileRef = string.IsNullOrWhiteSpace(fileRef) ? null : fileRef.Trim();
        IsEmbedded = isEmbedded;
        _encodingMap = new ReadOnlyDictionary<uint, ushort>(encodingMap ?? new Dictionary<uint, ushort>());
        SupportsGb18030 = supportsGb18030;
    }

    /// <summary>
    /// 字体名称。
    /// </summary>
    public string Name { get; }

    /// <summary>
    /// PDF 子集标记（例如 <c>ABCDE+</c>）。
    /// </summary>
    public string? SubsetTag { get; }

    /// <summary>
    /// 获取一个值，指示该字体是否为子集字体。
    /// </summary>
    public bool IsSubset => !string.IsNullOrEmpty(SubsetTag);

    /// <summary>
    /// 字体文件在 OFD 容器中的相对引用。
    /// </summary>
    public string? FileRef { get; }

    /// <summary>
    /// 字体文件是否会在输出中嵌入。
    /// </summary>
    public bool IsEmbedded { get; }

    /// <summary>
    /// Unicode → GlyphId 映射。
    /// </summary>
    public IReadOnlyDictionary<uint, ushort> EncodingMap => _encodingMap;

    /// <summary>
    /// 当前映射中的字形数量。
    /// </summary>
    public int GlyphCount => _encodingMap.Count;

    /// <summary>
    /// 是否满足 GB18030 字符集覆盖。
    /// </summary>
    public bool SupportsGb18030 { get; private set; }

    /// <summary>
    /// 获取包含子集前缀的显示名称。
    /// </summary>
    public string DisplayName => IsSubset ? $"{SubsetTag}{Name}" : Name;

    /// <summary>
    /// 尝试从映射中获取字形索引。
    /// </summary>
    public bool TryGetGlyph(uint codePoint, out ushort glyphId)
    {
        return _encodingMap.TryGetValue(codePoint, out glyphId);
    }

    /// <summary>
    /// 创建具有不同嵌入状态的字体资源副本。
    /// </summary>
    public FontResource WithEmbedded(bool embedded)
    {
        return new FontResource(Name, SubsetTag, _encodingMap.ToDictionary(static kv => kv.Key, static kv => kv.Value), FileRef, embedded, SupportsGb18030);
    }

    /// <summary>
    /// 创建具有不同文件引用的字体资源副本。
    /// </summary>
    public FontResource WithFileRef(string fileRef)
    {
        if (string.IsNullOrWhiteSpace(fileRef))
        {
            throw new ArgumentException("File reference cannot be null or whitespace.", nameof(fileRef));
        }

        return new FontResource(Name, SubsetTag, _encodingMap.ToDictionary(static kv => kv.Key, static kv => kv.Value), fileRef, IsEmbedded, SupportsGb18030);
    }

    /// <summary>
    /// 创建具有不同编码映射的字体资源副本。
    /// </summary>
    public FontResource WithEncoding(IDictionary<uint, ushort> encodingMap)
    {
        if (encodingMap is null)
        {
            throw new ArgumentNullException(nameof(encodingMap));
        }

        return new FontResource(Name, SubsetTag, encodingMap, FileRef, IsEmbedded, SupportsGb18030);
    }

    /// <summary>
    /// 创建带有 GB18030 覆盖标志的字体资源副本。
    /// </summary>
    public FontResource WithGb18030Support(bool supported)
    {
        return new FontResource(Name, SubsetTag, _encodingMap.ToDictionary(static kv => kv.Key, static kv => kv.Value), FileRef, IsEmbedded, supported);
    }

    /// <inheritdoc />
    public override string ToString()
    {
        var embed = IsEmbedded ? "embedded" : "linked";
        return $"FontResource[{DisplayName}, glyphs={GlyphCount}, {embed}]";
    }
}
