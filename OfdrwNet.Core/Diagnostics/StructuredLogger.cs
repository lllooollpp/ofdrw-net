using System.Text.Json;

namespace OfdrwNet.Core.Diagnostics;

/// <summary>
/// Structured logger interface for consistent, machine-readable logging.
/// Outputs JSON lines for downstream analysis and monitoring.
/// </summary>
public interface IStructuredLogger
{
    /// <summary>
    /// Logs an event with structured data.
    /// </summary>
    void Log(string level, string eventName, object? data = null, string? jobId = null, int? page = null, string? feature = null, long? durationMs = null);

    /// <summary>
    /// Logs an INFO event.
    /// </summary>
    void LogInfo(string eventName, object? data = null, string? jobId = null, int? page = null, string? feature = null, long? durationMs = null);

    /// <summary>
    /// Logs a WARN event.
    /// </summary>
    void LogWarn(string eventName, object? data = null, string? jobId = null, int? page = null, string? feature = null, long? durationMs = null);

    /// <summary>
    /// Logs an ERROR event.
    /// </summary>
    void LogError(string eventName, object? data = null, string? jobId = null, int? page = null, string? feature = null, long? durationMs = null);
}

/// <summary>
/// Default implementation of IStructuredLogger that writes JSON lines to TextWriter.
/// </summary>
public sealed class StructuredLogger : IStructuredLogger, IDisposable
{
    private readonly TextWriter _writer;
    private readonly bool _ownsWriter;
    private readonly object _lock = new();

    /// <summary>
    /// Creates a logger writing to the specified TextWriter.
    /// </summary>
    public StructuredLogger(TextWriter writer, bool ownsWriter = false)
    {
        _writer = writer ?? throw new ArgumentNullException(nameof(writer));
        _ownsWriter = ownsWriter;
    }

    /// <summary>
    /// Creates a logger writing to the specified file path.
    /// </summary>
    public static StructuredLogger CreateFile(string filePath)
    {
        var writer = new StreamWriter(filePath, append: true);
        return new StructuredLogger(writer, ownsWriter: true);
    }

    /// <summary>
    /// Creates a logger writing to Console.Out.
    /// </summary>
    public static StructuredLogger CreateConsole()
    {
        return new StructuredLogger(Console.Out, ownsWriter: false);
    }

    public void Log(string level, string eventName, object? data = null, string? jobId = null, int? page = null, string? feature = null, long? durationMs = null)
    {
        var logEntry = new
        {
            ts = DateTime.UtcNow.ToString("o"),
            level = level.ToUpperInvariant(),
            @event = eventName,
            jobId = jobId,
            page = page,
            feature = feature,
            data = data,
            durationMs = durationMs
        };

        var json = JsonSerializer.Serialize(logEntry, new JsonSerializerOptions
        {
            DefaultIgnoreCondition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull
        });

        lock (_lock)
        {
            _writer.WriteLine(json);
            _writer.Flush();
        }
    }

    public void LogInfo(string eventName, object? data = null, string? jobId = null, int? page = null, string? feature = null, long? durationMs = null)
        => Log("INFO", eventName, data, jobId, page, feature, durationMs);

    public void LogWarn(string eventName, object? data = null, string? jobId = null, int? page = null, string? feature = null, long? durationMs = null)
        => Log("WARN", eventName, data, jobId, page, feature, durationMs);

    public void LogError(string eventName, object? data = null, string? jobId = null, int? page = null, string? feature = null, long? durationMs = null)
        => Log("ERROR", eventName, data, jobId, page, feature, durationMs);

    public void Dispose()
    {
        if (_ownsWriter)
        {
            _writer.Dispose();
        }
    }
}

/// <summary>
/// Standard event names for structured logging.
/// </summary>
public static class LogEvents
{
    // Container & Structure
    public const string BuildContainer = "BuildContainer";
    public const string ResourceEmbedded = "ResourceEmbedded";

    // Vector & Graphics
    public const string VectorTranslate = "VectorTranslate";

    // Recognition
    public const string CompositeRecognized = "CompositeRecognized";
    public const string CompositeFallback = "CompositeFallback";
    public const string TableRecognition = "TableRecognition";
    public const string FormulaRecognition = "FormulaRecognition";

    // Interaction
    public const string AnnotationMapped = "AnnotationMapped";
    public const string FormConverted = "FormConverted";

    // Scripting
    public const string JsScan = "JsScan";
    public const string JsSnapshot = "JsSnapshot";
    public const string XfaDetected = "XfaDetected";

    // Color Management
    public const string ColorDelta = "ColorDelta";

    // Compatibility
    public const string Downgrade = "Downgrade";

    // Memory & Performance
    public const string Memory = "Memory";
    public const string Segment = "Segment";
    public const string PerformanceSample = "PerformanceSample";

    // Versioning
    public const string VersionChain = "VersionChain";

    // Security
    public const string SecurityApplied = "SecurityApplied";

    // Validation
    public const string ValidationRun = "ValidationRun";

    // Batch
    public const string BatchFileDone = "BatchFileDone";

    // General
    public const string Error = "Error";
}
