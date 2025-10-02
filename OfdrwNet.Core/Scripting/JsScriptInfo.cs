using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;

namespace OfdrwNet.Core.Scripting;

/// <summary>
/// 表示 PDF 中提取的 JavaScript 脚本元信息。
/// </summary>
public sealed class JsScriptInfo
{
    private readonly IReadOnlyDictionary<string, string> _metadata;

    /// <summary>
    /// 初始化 <see cref="JsScriptInfo"/>。
    /// </summary>
    public JsScriptInfo(
        string objectId,
        int length,
        string sha256,
        ScriptScope scope,
        string? snippet = null,
        bool snapshotApplied = false,
        IEnumerable<string>? triggers = null,
        IDictionary<string, string>? metadata = null)
    {
        if (string.IsNullOrWhiteSpace(objectId))
        {
            throw new ArgumentException("Object id cannot be null or whitespace.", nameof(objectId));
        }

        if (length < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(length), "Length must be non-negative.");
        }

        if (string.IsNullOrWhiteSpace(sha256))
        {
            throw new ArgumentException("SHA256 hash cannot be null or whitespace.", nameof(sha256));
        }

        if (sha256.Length != 64)
        {
            throw new ArgumentException("SHA256 hash must be 64 characters long.", nameof(sha256));
        }

        ObjectId = objectId.Trim();
        Length = length;
        Sha256 = sha256.ToLowerInvariant();
        Scope = scope;
        Snippet = snippet;
        SnapshotApplied = snapshotApplied;
        Triggers = new ReadOnlyCollection<string>((triggers ?? Array.Empty<string>()).Select(t => t.Trim()).Where(t => t.Length > 0).ToList());
        _metadata = new ReadOnlyDictionary<string, string>(metadata?.ToDictionary(static kv => kv.Key, static kv => kv.Value) ?? new Dictionary<string, string>());
    }

    /// <summary>
    /// 脚本所属对象编号。
    /// </summary>
    public string ObjectId { get; }

    /// <summary>
    /// 脚本长度（字节）。
    /// </summary>
    public int Length { get; }

    /// <summary>
    /// 脚本 SHA256 哈希。
    /// </summary>
    public string Sha256 { get; }

    /// <summary>
    /// 脚本作用域（文档/页面/字段）。
    /// </summary>
    public ScriptScope Scope { get; }

    /// <summary>
    /// 脚本片段（用于报告）。
    /// </summary>
    public string? Snippet { get; }

    /// <summary>
    /// 是否已执行 QuickJS snapshot。
    /// </summary>
    public bool SnapshotApplied { get; }

    /// <summary>
    /// 触发器集合，例如 Open、Keystroke 等。
    /// </summary>
    public IReadOnlyList<string> Triggers { get; }

    /// <summary>
    /// 附加元数据。
    /// </summary>
    public IReadOnlyDictionary<string, string> Metadata => _metadata;

    /// <summary>
    /// 判断脚本是否超过指定长度。
    /// </summary>
    public bool ExceedsLength(int threshold) => Length > threshold;

    /// <inheritdoc />
    public override string ToString()
    {
        return $"JsScriptInfo[{ObjectId}, scope={Scope}, length={Length}, snapshot={SnapshotApplied}]";
    }
}

/// <summary>
/// 脚本作用域。
/// </summary>
public enum ScriptScope
{
    /// <summary>
    /// 未知。
    /// </summary>
    Unknown,

    /// <summary>
    /// 文档级脚本。
    /// </summary>
    Document,

    /// <summary>
    /// 页面级脚本。
    /// </summary>
    Page,

    /// <summary>
    /// 字段级脚本。
    /// </summary>
    Field
}
