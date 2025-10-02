using System;

namespace OfdrwNet.Core.Diagnostics;

/// <summary>
/// 转换过程中的错误或警告记录。
/// </summary>
public sealed class ErrorRecord
{
    /// <summary>
    /// 严重级别。
    /// </summary>
    public ErrorSeverity Severity { get; init; } = ErrorSeverity.Info;

    /// <summary>
    /// 错误代码。
    /// </summary>
    public string Code { get; init; } = string.Empty;

    /// <summary>
    /// 错误消息。
    /// </summary>
    public string Message { get; init; } = string.Empty;

    /// <summary>
    /// 关联页码。
    /// </summary>
    public int? Page { get; init; }

    /// <summary>
    /// 关联功能模块。
    /// </summary>
    public string? Feature { get; init; }

    /// <summary>
    /// 捕获的异常（若存在）。
    /// </summary>
    public Exception? Exception { get; init; }

    /// <summary>
    /// 上下文信息（JSON 或键值）。
    /// </summary>
    public string? Context { get; init; }

    /// <summary>
    /// 记录时间（UTC）。
    /// </summary>
    public DateTime Timestamp { get; init; } = DateTime.UtcNow;

    /// <summary>
    /// 转换为可读格式。
    /// </summary>
    public override string ToString()
    {
        return $"[{Timestamp:O}] {Severity} {Code}: {Message}";
    }
}

/// <summary>
/// 错误严重性。
/// </summary>
public enum ErrorSeverity
{
    /// <summary>
    /// 信息。
    /// </summary>
    Info,

    /// <summary>
    /// 警告。
    /// </summary>
    Warning,

    /// <summary>
    /// 错误。
    /// </summary>
    Error,

    /// <summary>
    /// 致命错误。
    /// </summary>
    Fatal
}
