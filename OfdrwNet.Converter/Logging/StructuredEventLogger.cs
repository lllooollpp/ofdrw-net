using System.Text.Json;
using OfdrwNet.Converter.Domain;
using OfdrwNet.Converter.Batch;

namespace OfdrwNet.Converter.Logging;

/// <summary>
/// Structured event logger for MemoryGuard and performance metrics (FR-31, FR-32)
/// </summary>
public class StructuredEventLogger
{
    private readonly List<StructuredEvent> _events = new();
    private readonly object _lock = new();
    private readonly string? _outputPath;
    private readonly bool _autoFlush;

    /// <summary>
    /// Initializes structured event logger
    /// </summary>
    /// <param name="outputPath">Optional output file path for event log</param>
    /// <param name="autoFlush">Auto flush events to file after each log</param>
    public StructuredEventLogger(string? outputPath = null, bool autoFlush = false)
    {
        _outputPath = outputPath;
        _autoFlush = autoFlush;
    }

    /// <summary>
    /// Log memory guard event
    /// </summary>
    /// <param name="snapshot">Memory snapshot</param>
    /// <param name="action">Recommended action</param>
    /// <param name="context">Additional context</param>
    public void LogMemoryEvent(MemorySnapshot snapshot, MemoryAction action, string? context = null)
    {
        var evt = new StructuredEvent
        {
            Timestamp = DateTime.UtcNow,
            Category = "Memory",
            Level = action switch
            {
                MemoryAction.None => "Info",
                MemoryAction.GarbageCollect => "Warning",
                MemoryAction.FlushToDisk => "Warning",
                MemoryAction.Abort => "Error",
                MemoryAction.ReduceParallelism => "Warning",
                _ => "Info"
            },
            EventType = "MemoryGuard",
            Data = new Dictionary<string, object>
            {
                ["allocatedMB"] = snapshot.AllocatedMB,
                ["thresholdMB"] = snapshot.ThresholdMB,
                ["workingSetMB"] = snapshot.WorkingSetMB ?? 0,
                ["gcHeapMB"] = snapshot.GcHeapMB ?? 0,
                ["action"] = action.ToString(),
                ["isOverThreshold"] = snapshot.IsOverThreshold,
                ["usageRatio"] = snapshot.UsageRatio
            }
        };

        if (!string.IsNullOrWhiteSpace(context))
            evt.Data["context"] = context;

        LogEvent(evt);
    }

    /// <summary>
    /// Log performance metrics
    /// </summary>
    /// <param name="operation">Operation name</param>
    /// <param name="durationMs">Duration in milliseconds</param>
    /// <param name="metrics">Additional metrics</param>
    public void LogPerformance(string operation, double durationMs, Dictionary<string, object>? metrics = null)
    {
        var evt = new StructuredEvent
        {
            Timestamp = DateTime.UtcNow,
            Category = "Performance",
            Level = "Info",
            EventType = "PerformanceMetric",
            Data = new Dictionary<string, object>
            {
                ["operation"] = operation,
                ["durationMs"] = durationMs,
                ["durationSeconds"] = durationMs / 1000.0
            }
        };

        if (metrics != null)
        {
            foreach (var kvp in metrics)
                evt.Data[kvp.Key] = kvp.Value;
        }

        LogEvent(evt);
    }

    /// <summary>
    /// Log batch processing event
    /// </summary>
    /// <param name="batchId">Batch identifier</param>
    /// <param name="totalItems">Total items in batch</param>
    /// <param name="processedItems">Items processed so far</param>
    /// <param name="successCount">Successful items</param>
    /// <param name="failedCount">Failed items</param>
    /// <param name="parallelism">Current parallelism level</param>
    public void LogBatchProgress(
        string batchId,
        int totalItems,
        int processedItems,
        int successCount,
        int failedCount,
        int parallelism)
    {
        var evt = new StructuredEvent
        {
            Timestamp = DateTime.UtcNow,
            Category = "Batch",
            Level = "Info",
            EventType = "BatchProgress",
            Data = new Dictionary<string, object>
            {
                ["batchId"] = batchId,
                ["totalItems"] = totalItems,
                ["processedItems"] = processedItems,
                ["successCount"] = successCount,
                ["failedCount"] = failedCount,
                ["parallelism"] = parallelism,
                ["progressPercent"] = totalItems > 0 ? (processedItems * 100.0 / totalItems) : 0,
                ["successRate"] = processedItems > 0 ? (successCount * 100.0 / processedItems) : 0
            }
        };

        LogEvent(evt);
    }

    /// <summary>
    /// Log conversion event
    /// </summary>
    /// <param name="sourcePath">Source file path</param>
    /// <param name="outputPath">Output file path</param>
    /// <param name="status">Conversion status</param>
    /// <param name="durationMs">Duration in milliseconds</param>
    /// <param name="errorCount">Number of errors</param>
    /// <param name="warningCount">Number of warnings</param>
    public void LogConversion(
        string sourcePath,
        string outputPath,
        string status,
        double durationMs,
        int errorCount = 0,
        int warningCount = 0)
    {
        var evt = new StructuredEvent
        {
            Timestamp = DateTime.UtcNow,
            Category = "Conversion",
            Level = status.ToLower() switch
            {
                "success" => "Info",
                "warning" => "Warning",
                "error" => "Error",
                "failed" => "Error",
                _ => "Info"
            },
            EventType = "ConversionComplete",
            Data = new Dictionary<string, object>
            {
                ["source"] = sourcePath,
                ["output"] = outputPath,
                ["status"] = status,
                ["durationMs"] = durationMs,
                ["errorCount"] = errorCount,
                ["warningCount"] = warningCount
            }
        };

        LogEvent(evt);
    }

    /// <summary>
    /// Log validation event
    /// </summary>
    /// <param name="filePath">File path being validated</param>
    /// <param name="isValid">Whether validation passed</param>
    /// <param name="errorCount">Number of validation errors</param>
    /// <param name="warningCount">Number of validation warnings</param>
    /// <param name="durationMs">Validation duration</param>
    public void LogValidation(
        string filePath,
        bool isValid,
        int errorCount,
        int warningCount,
        double durationMs)
    {
        var evt = new StructuredEvent
        {
            Timestamp = DateTime.UtcNow,
            Category = "Validation",
            Level = isValid ? "Info" : "Warning",
            EventType = "ValidationComplete",
            Data = new Dictionary<string, object>
            {
                ["file"] = filePath,
                ["isValid"] = isValid,
                ["errorCount"] = errorCount,
                ["warningCount"] = warningCount,
                ["durationMs"] = durationMs
            }
        };

        LogEvent(evt);
    }

    /// <summary>
    /// Log generic structured event
    /// </summary>
    /// <param name="evt">Structured event</param>
    public void LogEvent(StructuredEvent evt)
    {
        lock (_lock)
        {
            _events.Add(evt);

            if (_autoFlush && !string.IsNullOrWhiteSpace(_outputPath))
                FlushToFile();
        }
    }

    /// <summary>
    /// Get all logged events
    /// </summary>
    /// <returns>List of structured events</returns>
    public List<StructuredEvent> GetEvents()
    {
        lock (_lock)
        {
            return new List<StructuredEvent>(_events);
        }
    }

    /// <summary>
    /// Get events by category
    /// </summary>
    /// <param name="category">Event category</param>
    /// <returns>Filtered events</returns>
    public List<StructuredEvent> GetEventsByCategory(string category)
    {
        lock (_lock)
        {
            return _events.Where(e => e.Category == category).ToList();
        }
    }

    /// <summary>
    /// Get events by level
    /// </summary>
    /// <param name="level">Event level</param>
    /// <returns>Filtered events</returns>
    public List<StructuredEvent> GetEventsByLevel(string level)
    {
        lock (_lock)
        {
            return _events.Where(e => e.Level == level).ToList();
        }
    }

    /// <summary>
    /// Get event statistics
    /// </summary>
    /// <returns>Event statistics</returns>
    public EventStatistics GetStatistics()
    {
        lock (_lock)
        {
            return new EventStatistics
            {
                TotalEvents = _events.Count,
                EventsByCategory = _events.GroupBy(e => e.Category)
                    .ToDictionary(g => g.Key, g => g.Count()),
                EventsByLevel = _events.GroupBy(e => e.Level)
                    .ToDictionary(g => g.Key, g => g.Count()),
                EventsByType = _events.GroupBy(e => e.EventType)
                    .ToDictionary(g => g.Key, g => g.Count()),
                FirstEvent = _events.FirstOrDefault()?.Timestamp,
                LastEvent = _events.LastOrDefault()?.Timestamp
            };
        }
    }

    /// <summary>
    /// Flush events to file
    /// </summary>
    /// <param name="outputPath">Output file path (overrides constructor path)</param>
    public void FlushToFile(string? outputPath = null)
    {
        var path = outputPath ?? _outputPath;
        if (string.IsNullOrWhiteSpace(path))
            throw new InvalidOperationException("No output path specified");

        lock (_lock)
        {
            var options = new JsonSerializerOptions
            {
                WriteIndented = true,
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase
            };

            var json = JsonSerializer.Serialize(_events, options);
            File.WriteAllText(path, json);
        }
    }

    /// <summary>
    /// Clear all logged events
    /// </summary>
    public void Clear()
    {
        lock (_lock)
        {
            _events.Clear();
        }
    }
}

/// <summary>
/// Structured event entry
/// </summary>
public class StructuredEvent
{
    /// <summary>
    /// Event timestamp (UTC)
    /// </summary>
    public DateTime Timestamp { get; set; }

    /// <summary>
    /// Event category (Memory, Performance, Batch, Conversion, Validation)
    /// </summary>
    public string Category { get; set; } = string.Empty;

    /// <summary>
    /// Event level (Info, Warning, Error)
    /// </summary>
    public string Level { get; set; } = "Info";

    /// <summary>
    /// Event type (MemoryGuard, PerformanceMetric, BatchProgress, etc.)
    /// </summary>
    public string EventType { get; set; } = string.Empty;

    /// <summary>
    /// Event data dictionary
    /// </summary>
    public Dictionary<string, object> Data { get; set; } = new();
}

/// <summary>
/// Event statistics
/// </summary>
public class EventStatistics
{
    /// <summary>
    /// Total event count
    /// </summary>
    public int TotalEvents { get; set; }

    /// <summary>
    /// Events grouped by category
    /// </summary>
    public Dictionary<string, int> EventsByCategory { get; set; } = new();

    /// <summary>
    /// Events grouped by level
    /// </summary>
    public Dictionary<string, int> EventsByLevel { get; set; } = new();

    /// <summary>
    /// Events grouped by type
    /// </summary>
    public Dictionary<string, int> EventsByType { get; set; } = new();

    /// <summary>
    /// First event timestamp
    /// </summary>
    public DateTime? FirstEvent { get; set; }

    /// <summary>
    /// Last event timestamp
    /// </summary>
    public DateTime? LastEvent { get; set; }
}
