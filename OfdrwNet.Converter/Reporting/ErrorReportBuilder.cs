using System.Text.Json;
using System.Text.Json.Serialization;
using OfdrwNet.Core.Diagnostics;

namespace OfdrwNet.Converter.Reporting;

/// <summary>
/// Conversion report structure
/// </summary>
public class ConversionReport
{
    /// <summary>
    /// Conversion job ID
    /// </summary>
    [JsonPropertyName("jobId")]
    public string JobId { get; set; } = string.Empty;

    /// <summary>
    /// Source PDF file
    /// </summary>
    [JsonPropertyName("source")]
    public string Source { get; set; } = string.Empty;

    /// <summary>
    /// Output OFD file
    /// </summary>
    [JsonPropertyName("output")]
    public string Output { get; set; } = string.Empty;

    /// <summary>
    /// Conversion timestamp
    /// </summary>
    [JsonPropertyName("timestamp")]
    public DateTime Timestamp { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// Conversion duration in seconds
    /// </summary>
    [JsonPropertyName("durationSeconds")]
    public double DurationSeconds { get; set; }

    /// <summary>
    /// Error records
    /// </summary>
    [JsonPropertyName("errors")]
    public List<ErrorReportEntry> Errors { get; set; } = new();

    /// <summary>
    /// Conversion statistics
    /// </summary>
    [JsonPropertyName("stats")]
    public ConversionStatistics Stats { get; set; } = new();

    /// <summary>
    /// Color delta statistics
    /// </summary>
    [JsonPropertyName("colorDelta")]
    public ColorDeltaStats? ColorDelta { get; set; }
}

/// <summary>
/// Error report entry
/// </summary>
public class ErrorReportEntry
{
    [JsonPropertyName("severity")]
    public string Severity { get; set; } = string.Empty;

    [JsonPropertyName("code")]
    public string Code { get; set; } = string.Empty;

    [JsonPropertyName("message")]
    public string Message { get; set; } = string.Empty;

    [JsonPropertyName("page")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public int? Page { get; set; }

    [JsonPropertyName("feature")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? Feature { get; set; }

    [JsonPropertyName("context")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? Context { get; set; }

    [JsonPropertyName("timestamp")]
    public DateTime Timestamp { get; set; }
}

/// <summary>
/// Conversion statistics
/// </summary>
public class ConversionStatistics
{
    [JsonPropertyName("pages")]
    public int Pages { get; set; }

    [JsonPropertyName("tablesRecognized")]
    public int TablesRecognized { get; set; }

    [JsonPropertyName("formulasRecognized")]
    public int FormulasRecognized { get; set; }

    [JsonPropertyName("imagesEmbedded")]
    public int ImagesEmbedded { get; set; }

    [JsonPropertyName("fontsEmbedded")]
    public int FontsEmbedded { get; set; }

    [JsonPropertyName("downgradedFeatures")]
    public int DowngradedFeatures { get; set; }

    [JsonPropertyName("annotationsMapped")]
    public int AnnotationsMapped { get; set; }

    [JsonPropertyName("formFieldsConverted")]
    public int FormFieldsConverted { get; set; }

    [JsonPropertyName("jsScriptsRemoved")]
    public int JsScriptsRemoved { get; set; }

    [JsonPropertyName("xfaFormsDetected")]
    public int XfaFormsDetected { get; set; }

    [JsonPropertyName("segmentsCreated")]
    public int SegmentsCreated { get; set; }

    [JsonPropertyName("peakMemoryMB")]
    public int PeakMemoryMB { get; set; }
}

/// <summary>
/// Color delta statistics
/// </summary>
public class ColorDeltaStats
{
    [JsonPropertyName("avgDeltaE")]
    public double AvgDeltaE { get; set; }

    [JsonPropertyName("maxDeltaE")]
    public double MaxDeltaE { get; set; }

    [JsonPropertyName("samplesAboveThreshold")]
    public int SamplesAboveThreshold { get; set; }

    [JsonPropertyName("threshold")]
    public double Threshold { get; set; }
}

/// <summary>
/// Error report builder
/// </summary>
public class ErrorReportBuilder
{
    private readonly List<ErrorRecord> _errors = new();
    private readonly ConversionStatistics _stats = new();
    private ColorDeltaStats? _colorDelta;

    private string _jobId = string.Empty;
    private string _source = string.Empty;
    private string _output = string.Empty;
    private DateTime _startTime = DateTime.UtcNow;

    /// <summary>
    /// Set job metadata
    /// </summary>
    public ErrorReportBuilder WithJob(string jobId, string source, string output)
    {
        _jobId = jobId;
        _source = source;
        _output = output;
        return this;
    }

    /// <summary>
    /// Set start time for duration calculation
    /// </summary>
    public ErrorReportBuilder WithStartTime(DateTime startTime)
    {
        _startTime = startTime;
        return this;
    }

    /// <summary>
    /// Add error record
    /// </summary>
    public ErrorReportBuilder AddError(ErrorRecord error)
    {
        _errors.Add(error);
        return this;
    }

    /// <summary>
    /// Add multiple error records
    /// </summary>
    public ErrorReportBuilder AddErrors(IEnumerable<ErrorRecord> errors)
    {
        _errors.AddRange(errors);
        return this;
    }

    /// <summary>
    /// Update statistics
    /// </summary>
    public ErrorReportBuilder WithStats(Action<ConversionStatistics> configure)
    {
        configure(_stats);
        return this;
    }

    /// <summary>
    /// Set color delta statistics
    /// </summary>
    public ErrorReportBuilder WithColorDelta(double avgDeltaE, double maxDeltaE, int samplesAboveThreshold, double threshold)
    {
        _colorDelta = new ColorDeltaStats
        {
            AvgDeltaE = avgDeltaE,
            MaxDeltaE = maxDeltaE,
            SamplesAboveThreshold = samplesAboveThreshold,
            Threshold = threshold
        };
        return this;
    }

    /// <summary>
    /// Build conversion report
    /// </summary>
    public ConversionReport Build()
    {
        return new ConversionReport
        {
            JobId = _jobId,
            Source = _source,
            Output = _output,
            Timestamp = _startTime,
            DurationSeconds = (DateTime.UtcNow - _startTime).TotalSeconds,
            Errors = _errors.Select(e => new ErrorReportEntry
            {
                Severity = e.Severity.ToString(),
                Code = e.Code,
                Message = e.Message,
                Page = e.Page,
                Feature = e.Feature,
                Context = e.Context,
                Timestamp = e.Timestamp
            }).ToList(),
            Stats = _stats,
            ColorDelta = _colorDelta
        };
    }

    /// <summary>
    /// Build and serialize to JSON
    /// </summary>
    public string BuildJson(bool indented = true)
    {
        var report = Build();
        var options = new JsonSerializerOptions
        {
            WriteIndented = indented,
            DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
        };
        return JsonSerializer.Serialize(report, options);
    }

    /// <summary>
    /// Build and write to file
    /// </summary>
    public void BuildToFile(string outputPath, bool indented = true)
    {
        var json = BuildJson(indented);
        File.WriteAllText(outputPath, json);
    }

    /// <summary>
    /// Get error count by severity
    /// </summary>
    public Dictionary<ErrorSeverity, int> GetErrorCountBySeverity()
    {
        return _errors.GroupBy(e => e.Severity)
            .ToDictionary(g => g.Key, g => g.Count());
    }

    /// <summary>
    /// Get error count by code
    /// </summary>
    public Dictionary<string, int> GetErrorCountByCode()
    {
        return _errors.GroupBy(e => e.Code)
            .ToDictionary(g => g.Key, g => g.Count());
    }
}
