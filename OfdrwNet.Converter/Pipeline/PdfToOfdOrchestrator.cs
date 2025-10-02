using OfdrwNet.Core.Conversion;
using OfdrwNet.Core.Diagnostics;
using OfdrwNet.Core.Pages;

namespace OfdrwNet.Converter.Pipeline;

/// <summary>
/// Page processing pipeline stage interface
/// </summary>
public interface IPagePipelineStage
{
    /// <summary>
    /// Stage name for logging
    /// </summary>
    string StageName { get; }

    /// <summary>
    /// Execute stage processing
    /// </summary>
    /// <param name="context">Page context</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>True if successful, false to abort pipeline</returns>
    Task<bool> ExecuteAsync(PageContext context, CancellationToken cancellationToken = default);
}

/// <summary>
/// PDF to OFD conversion orchestrator with pipeline architecture
/// </summary>
public class PdfToOfdOrchestrator
{
    private readonly List<IPagePipelineStage> _stages = new();
    private readonly IStructuredLogger? _logger;
    private readonly Memory.IMemoryGuard? _memoryGuard;

    /// <summary>
    /// Initialize orchestrator
    /// </summary>
    public PdfToOfdOrchestrator(IStructuredLogger? logger = null, Memory.IMemoryGuard? memoryGuard = null)
    {
        _logger = logger;
        _memoryGuard = memoryGuard;
    }

    /// <summary>
    /// Register pipeline stage
    /// </summary>
    public PdfToOfdOrchestrator AddStage(IPagePipelineStage stage)
    {
        _stages.Add(stage);
        return this;
    }

    /// <summary>
    /// Execute conversion with registered pipeline stages
    /// </summary>
    /// <param name="options">Converter options</param>
    /// <param name="cancellationToken">Cancellation token</param>
    public async Task<ConversionResult> ConvertAsync(ConverterOptions options, CancellationToken cancellationToken = default)
    {
        var startTime = DateTime.UtcNow;
        var errors = new List<ErrorRecord>();
        var jobId = Guid.NewGuid().ToString("N");

        _logger?.LogInfo(LogEvents.BuildContainer, new
        {
            action = "start",
            jobId,
            input = options.InputPath,
            output = options.OutputPath
        });

        try
        {
            // Memory check before starting
            if (_memoryGuard is { } guardAtStart && guardAtStart.IsThresholdExceeded())
            {
                _logger?.LogWarn(LogEvents.Memory, new
                {
                    action = "threshold_exceeded_before_start",
                    currentMB = guardAtStart.CurrentMemoryMB,
                    thresholdMB = guardAtStart.ThresholdMB
                });

                guardAtStart.ForceGC();
            }

            // Placeholder: actual PDF parsing + page iteration in Phase R
            // For now simulate single page
            var pageContext = new PageContext
            {
                PageNumber = 1,
                JobId = jobId
            };

            // Execute pipeline stages
            foreach (var stage in _stages)
            {
                _logger?.LogInfo(LogEvents.PerformanceSample, new
                {
                    action = "stage_start",
                    stage = stage.StageName,
                    jobId,
                    page = pageContext.PageNumber
                });

                var stageStart = DateTime.UtcNow;

                try
                {
                    var success = await stage.ExecuteAsync(pageContext, cancellationToken);

                    var duration = (DateTime.UtcNow - stageStart).TotalMilliseconds;

                    _logger?.LogInfo(LogEvents.PerformanceSample, new
                    {
                        action = "stage_complete",
                        stage = stage.StageName,
                        jobId,
                        page = pageContext.PageNumber,
                        durationMs = duration,
                        success
                    });

                    if (!success)
                    {
                        errors.Add(new ErrorRecord
                        {
                            Code = ErrorCodes.GEN_CONVERSION_FAILED,
                            Message = $"Stage {stage.StageName} failed",
                            Severity = ErrorSeverity.Error,
                            Page = pageContext.PageNumber,
                            Feature = stage.StageName
                        });
                        break;
                    }
                }
                catch (Exception ex)
                {
                    var duration = (DateTime.UtcNow - stageStart).TotalMilliseconds;

                    _logger?.LogError(LogEvents.Error, new
                    {
                        action = "stage_error",
                        stage = stage.StageName,
                        jobId,
                        page = pageContext.PageNumber,
                        error = ex.Message,
                        durationMs = duration
                    });

                    errors.Add(new ErrorRecord
                    {
                        Code = ErrorCodes.GEN_UNEXPECTED_ERROR,
                        Message = $"Stage {stage.StageName} threw exception: {ex.Message}",
                        Severity = ErrorSeverity.Fatal,
                        Page = pageContext.PageNumber,
                        Feature = stage.StageName,
                        Exception = ex
                    });

                    throw;
                }

                // Memory check after each stage
                if (_memoryGuard is { } guard && guard.ShouldSegment())
                {
                    var pressure = guard.GetMemoryPressure();

                    _logger?.LogWarn(LogEvents.Segment, new
                    {
                        action = "segment_recommended",
                        jobId,
                        page = pageContext.PageNumber,
                        zeroBasedPage = pageContext.ZeroBasedIndex,
                        pressure
                    });

                    guard.ForceGC();
                }
            }

            var totalDuration = (DateTime.UtcNow - startTime).TotalSeconds;

            _logger?.LogInfo(LogEvents.BuildContainer, new
            {
                action = "complete",
                jobId,
                durationSeconds = totalDuration,
                errorCount = errors.Count
            });

            return new ConversionResult
            {
                Success = errors.Count(e => e.Severity >= ErrorSeverity.Error) == 0,
                Errors = errors,
                DurationSeconds = totalDuration,
                JobId = jobId
            };
        }
        catch (Exception ex)
        {
            _logger?.LogError(LogEvents.Error, new
            {
                action = "conversion_failed",
                jobId,
                error = ex.Message
            });

            errors.Add(new ErrorRecord
            {
                Code = ErrorCodes.GEN_CONVERSION_FAILED,
                Message = $"Conversion failed: {ex.Message}",
                Severity = ErrorSeverity.Fatal,
                Exception = ex
            });

            return new ConversionResult
            {
                Success = false,
                Errors = errors,
                DurationSeconds = (DateTime.UtcNow - startTime).TotalSeconds,
                JobId = jobId
            };
        }
    }
}

/// <summary>
/// Conversion result
/// </summary>
public class ConversionResult
{
    public bool Success { get; init; }
    public List<ErrorRecord> Errors { get; init; } = new();
    public double DurationSeconds { get; init; }
    public string JobId { get; init; } = string.Empty;
}
