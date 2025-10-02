using System.Diagnostics;
using OfdrwNet.Core.Diagnostics;

namespace OfdrwNet.Converter.Memory;

/// <summary>
/// Memory guard interface for segmentation logic
/// </summary>
public interface IMemoryGuard
{
    /// <summary>
    /// Current memory usage in MB
    /// </summary>
    long CurrentMemoryMB { get; }

    /// <summary>
    /// Memory threshold in MB
    /// </summary>
    long ThresholdMB { get; }

    /// <summary>
    /// Check if memory threshold exceeded
    /// </summary>
    bool IsThresholdExceeded();

    /// <summary>
    /// Suggest segmenting when pressure exceeds configured ratio
    /// </summary>
    bool ShouldSegment();

    /// <summary>
    /// Get memory pressure level relative to threshold (0.0 - 1.0+)
    /// </summary>
    double GetMemoryPressure();

    /// <summary>
    /// Force garbage collection
    /// </summary>
    void ForceGC();

    /// <summary>
    /// Take memory snapshot
    /// </summary>
    MemorySnapshot TakeSnapshot();
}

/// <summary>
/// Memory snapshot data
/// </summary>
public class MemorySnapshot
{
    /// <summary>
    /// Snapshot timestamp
    /// </summary>
    public DateTime Timestamp { get; init; } = DateTime.UtcNow;

    /// <summary>
    /// Working set memory (MB)
    /// </summary>
    public long WorkingSetMB { get; init; }

    /// <summary>
    /// Private memory (MB)
    /// </summary>
    public long PrivateMemoryMB { get; init; }

    /// <summary>
    /// GC heap size (MB)
    /// </summary>
    public long GcHeapMB { get; init; }

    /// <summary>
    /// Generation 0 collections
    /// </summary>
    public int Gen0Collections { get; init; }

    /// <summary>
    /// Generation 1 collections
    /// </summary>
    public int Gen1Collections { get; init; }

    /// <summary>
    /// Generation 2 collections
    /// </summary>
    public int Gen2Collections { get; init; }
}

/// <summary>
/// Segment memory guard implementation
/// </summary>
public class SegmentMemoryGuard : IMemoryGuard
{
    private readonly long _thresholdMB;
    private readonly IStructuredLogger? _logger;
    private readonly Process _process = Process.GetCurrentProcess();

    public long ThresholdMB => _thresholdMB;

    public long CurrentMemoryMB
    {
        get
        {
            _process.Refresh();
            return _process.WorkingSet64 / (1024 * 1024);
        }
    }

    /// <summary>
    /// Initialize memory guard with threshold
    /// </summary>
    /// <param name="thresholdMB">Memory threshold in MB</param>
    /// <param name="logger">Optional structured logger</param>
    public SegmentMemoryGuard(long thresholdMB, IStructuredLogger? logger = null)
    {
        _thresholdMB = thresholdMB;
        _logger = logger;
    }

    public bool IsThresholdExceeded()
    {
        var current = CurrentMemoryMB;
        var exceeded = current > _thresholdMB;

        if (exceeded)
        {
            _logger?.LogWarn(LogEvents.Memory, new
            {
                currentMB = current,
                thresholdMB = _thresholdMB,
                exceeded = true
            });
        }

        return exceeded;
    }

    public void ForceGC()
    {
        var beforeMB = CurrentMemoryMB;

        GC.Collect();
        GC.WaitForPendingFinalizers();
        GC.Collect();

        var afterMB = CurrentMemoryMB;

        _logger?.LogInfo(LogEvents.Memory, new
        {
            action = "force_gc",
            beforeMB,
            afterMB,
            freedMB = beforeMB - afterMB
        });
    }

    public MemorySnapshot TakeSnapshot()
    {
        _process.Refresh();

        var snapshot = new MemorySnapshot
        {
            WorkingSetMB = _process.WorkingSet64 / (1024 * 1024),
            PrivateMemoryMB = _process.PrivateMemorySize64 / (1024 * 1024),
            GcHeapMB = GC.GetTotalMemory(false) / (1024 * 1024),
            Gen0Collections = GC.CollectionCount(0),
            Gen1Collections = GC.CollectionCount(1),
            Gen2Collections = GC.CollectionCount(2)
        };

        _logger?.LogInfo(LogEvents.Memory, new
        {
            action = "snapshot",
            workingSetMB = snapshot.WorkingSetMB,
            privateMB = snapshot.PrivateMemoryMB,
            gcHeapMB = snapshot.GcHeapMB,
            gen0 = snapshot.Gen0Collections,
            gen1 = snapshot.Gen1Collections,
            gen2 = snapshot.Gen2Collections
        });

        return snapshot;
    }

    public double GetMemoryPressure() => (double)CurrentMemoryMB / _thresholdMB;

    public bool ShouldSegment() => GetMemoryPressure() >= 0.8; // 80% threshold
}

/// <summary>
/// Segment manager for handling large documents
/// </summary>
public class SegmentManager
{
    private readonly IMemoryGuard _guard;
    private readonly IStructuredLogger? _logger;
    private readonly List<string> _segments = new();

    /// <summary>
    /// Initialize segment manager
    /// </summary>
    public SegmentManager(IMemoryGuard guard, IStructuredLogger? logger = null)
    {
        _guard = guard;
        _logger = logger;
    }

    /// <summary>
    /// Create new segment file
    /// </summary>
    /// <param name="segmentIndex">Segment index</param>
    /// <param name="outputDir">Output directory</param>
    /// <returns>Segment file path</returns>
    public string CreateSegment(int segmentIndex, string outputDir)
    {
        var segmentPath = Path.Combine(outputDir, $"segment_{segmentIndex:D4}.ofd");
        _segments.Add(segmentPath);

        _logger?.LogInfo(LogEvents.Segment, new
        {
            action = "create",
            index = segmentIndex,
            path = segmentPath,
            memoryMB = _guard.CurrentMemoryMB
        });

        return segmentPath;
    }

    /// <summary>
    /// Merge all segments into final OFD
    /// </summary>
    /// <param name="finalPath">Final output path</param>
    public void MergeSegments(string finalPath)
    {
        _logger?.LogInfo(LogEvents.Segment, new
        {
            action = "merge_start",
            segmentCount = _segments.Count,
            output = finalPath
        });

        // Placeholder: actual merge logic in Phase B (batch)
        // For now just copy first segment
        if (_segments.Count > 0)
        {
            File.Copy(_segments[0], finalPath, true);
        }

        _logger?.LogInfo(LogEvents.Segment, new
        {
            action = "merge_complete",
            segmentCount = _segments.Count,
            output = finalPath
        });
    }

    /// <summary>
    /// Get all segment paths
    /// </summary>
    public List<string> GetSegments() => new(_segments);

    /// <summary>
    /// Clean up segment files
    /// </summary>
    public void CleanupSegments()
    {
        foreach (var segment in _segments)
        {
            try
            {
                if (File.Exists(segment))
                    File.Delete(segment);
            }
            catch (Exception ex)
            {
                _logger?.LogWarn(LogEvents.Segment, new
                {
                    action = "cleanup_failed",
                    segment,
                    error = ex.Message
                });
            }
        }

        _segments.Clear();
    }
}
