namespace OfdrwNet.Converter.Domain;

/// <summary>
/// JavaScript 脚本信息
/// </summary>
public sealed class JsScriptInfo
{
    /// <summary>
    /// 关联的 PDF 对象 ID
    /// </summary>
    public required int ObjectId { get; init; }

    /// <summary>
    /// 脚本长度（字节）
    /// </summary>
    public required int Length { get; init; }

    /// <summary>
    /// 脚本 SHA256 哈希
    /// </summary>
    public required string Sha256 { get; init; }

    /// <summary>
    /// 脚本代码片段（用于日志记录，截取前 200 字符）
    /// </summary>
    public string? Snippet { get; init; }

    /// <summary>
    /// 是否已应用快照执行
    /// </summary>
    public bool SnapshotApplied { get; set; }

    /// <summary>
    /// 脚本类型（Document, Page, Field, Action等）
    /// </summary>
    public string? ScriptType { get; init; }

    /// <summary>
    /// 执行结果（如果已执行）
    /// </summary>
    public string? ExecutionResult { get; set; }

    /// <summary>
    /// 执行错误信息（如果执行失败）
    /// </summary>
    public string? ExecutionError { get; set; }
}
