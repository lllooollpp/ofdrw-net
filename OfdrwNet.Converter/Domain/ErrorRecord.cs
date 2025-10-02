namespace OfdrwNet.Converter.Domain;

/// <summary>
/// 错误/警告记录
/// </summary>
public sealed class ErrorRecord
{
    /// <summary>
    /// 严重级别
    /// </summary>
    public required ErrorSeverity Severity { get; init; }

    /// <summary>
    /// 错误代码
    /// </summary>
    public required string Code { get; init; }

    /// <summary>
    /// 错误消息
    /// </summary>
    public required string Message { get; init; }

    /// <summary>
    /// 错误位置（页码、对象 ID 等）
    /// </summary>
    public string? Location { get; init; }

    /// <summary>
    /// 上下文信息（JSON 格式）
    /// </summary>
    public string? Context { get; init; }

    /// <summary>
    /// 错误发生时间
    /// </summary>
    public DateTime Timestamp { get; init; } = DateTime.UtcNow;

    /// <summary>
    /// 异常类型名称（如果由异常引起）
    /// </summary>
    public string? ExceptionType { get; init; }

    /// <summary>
    /// 堆栈跟踪
    /// </summary>
    public string? StackTrace { get; init; }
}

/// <summary>
/// 错误严重级别
/// </summary>
public enum ErrorSeverity
{
    /// <summary>
    /// 信息
    /// </summary>
    Info,

    /// <summary>
    /// 警告
    /// </summary>
    Warning,

    /// <summary>
    /// 错误
    /// </summary>
    Error,

    /// <summary>
    /// 致命错误
    /// </summary>
    Fatal
}
