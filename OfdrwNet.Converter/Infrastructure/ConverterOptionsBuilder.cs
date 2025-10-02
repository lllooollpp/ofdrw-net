using OfdrwNet.Core.Conversion;
using OfdrwNet.Core.Security;
using OfdrwNet.Core.Versioning;

namespace OfdrwNet.Converter.Infrastructure;

/// <summary>
/// Builder for constructing ConverterOptions from CLI arguments or API calls.
/// Aggregates all conversion configuration parameters with validation.
/// </summary>
public sealed class ConverterOptionsBuilder
{
    private string? _inputPath;
    private string? _outputDir;
    private float _tableRecognitionThreshold = 0.8f;
    private float _formulaRecognitionThreshold = 0.8f;
    private string _renderIntent = "perceptual";
    private string _compatLevel = "Std2020";
    private string? _targetReader;
    private int _maxMemoryMB = 512;
    private int _pagesPerSegment = 100;
    private bool _enableStructuredLog = false;
    private bool _runJsSnapshot = false;
    private Dictionary<string, bool> _permissions = new();
    private VersionPolicyConfig? _versionPolicyConfig;

    /// <summary>
    /// Creates a new builder instance.
    /// </summary>
    public static ConverterOptionsBuilder Create() => new();

    /// <summary>
    /// Sets the input PDF file path (required).
    /// </summary>
    public ConverterOptionsBuilder WithInput(string path)
    {
        if (string.IsNullOrWhiteSpace(path))
            throw new ArgumentException("Input path cannot be empty", nameof(path));
        _inputPath = path;
        return this;
    }

    /// <summary>
    /// Sets the output directory for OFD file (required).
    /// </summary>
    public ConverterOptionsBuilder WithOutputDir(string dir)
    {
        if (string.IsNullOrWhiteSpace(dir))
            throw new ArgumentException("Output directory cannot be empty", nameof(dir));
        _outputDir = dir;
        return this;
    }

    /// <summary>
    /// Sets table recognition confidence threshold (0-1).
    /// </summary>
    public ConverterOptionsBuilder WithTableThreshold(float threshold)
    {
        if (threshold < 0 || threshold > 1)
            throw new ArgumentOutOfRangeException(nameof(threshold), "Threshold must be between 0 and 1");
        _tableRecognitionThreshold = threshold;
        return this;
    }

    /// <summary>
    /// Sets formula recognition confidence threshold (0-1).
    /// </summary>
    public ConverterOptionsBuilder WithFormulaThreshold(float threshold)
    {
        if (threshold < 0 || threshold > 1)
            throw new ArgumentOutOfRangeException(nameof(threshold), "Threshold must be between 0 and 1");
        _formulaRecognitionThreshold = threshold;
        return this;
    }

    /// <summary>
    /// Sets rendering intent for color management.
    /// </summary>
    public ConverterOptionsBuilder WithRenderIntent(string intent)
    {
        var validIntents = new[] { "perceptual", "relative", "saturation", "absolute" };
        if (!validIntents.Contains(intent.ToLowerInvariant()))
            throw new ArgumentException($"Invalid render intent. Must be one of: {string.Join(", ", validIntents)}", nameof(intent));
        _renderIntent = intent.ToLowerInvariant();
        return this;
    }

    /// <summary>
    /// Sets OFD compatibility level.
    /// </summary>
    public ConverterOptionsBuilder WithCompatLevel(string level)
    {
        if (string.IsNullOrWhiteSpace(level))
            throw new ArgumentException("Compatibility level cannot be empty", nameof(level));
        _compatLevel = level;
        return this;
    }

    /// <summary>
    /// Sets target reader for compatibility profiling.
    /// </summary>
    public ConverterOptionsBuilder WithTargetReader(string? reader)
    {
        _targetReader = reader;
        return this;
    }

    /// <summary>
    /// Sets maximum memory threshold in MB before triggering segmentation.
    /// </summary>
    public ConverterOptionsBuilder WithMaxMemoryMB(int maxMB)
    {
        if (maxMB < 64)
            throw new ArgumentOutOfRangeException(nameof(maxMB), "Max memory must be at least 64MB");
        _maxMemoryMB = maxMB;
        return this;
    }

    /// <summary>
    /// Sets pages per segment when memory threshold is exceeded.
    /// </summary>
    public ConverterOptionsBuilder WithPagesPerSegment(int pages)
    {
        if (pages < 1)
            throw new ArgumentOutOfRangeException(nameof(pages), "Pages per segment must be at least 1");
        _pagesPerSegment = pages;
        return this;
    }

    /// <summary>
    /// Enables structured JSON logging.
    /// </summary>
    public ConverterOptionsBuilder EnableStructuredLog(bool enable = true)
    {
        _enableStructuredLog = enable;
        return this;
    }

    /// <summary>
    /// Enables JavaScript snapshot execution (experimental).
    /// </summary>
    public ConverterOptionsBuilder EnableJsSnapshot(bool enable = true)
    {
        _runJsSnapshot = enable;
        return this;
    }

    /// <summary>
    /// Sets permissions from key-value pairs (e.g., "print=true,modify=false").
    /// </summary>
    public ConverterOptionsBuilder WithPermissions(string permissionString)
    {
        if (string.IsNullOrWhiteSpace(permissionString))
            return this;

        var pairs = permissionString.Split(',', StringSplitOptions.RemoveEmptyEntries);
        foreach (var pair in pairs)
        {
            var parts = pair.Split('=', 2, StringSplitOptions.TrimEntries);
            if (parts.Length == 2 && bool.TryParse(parts[1], out var value))
            {
                _permissions[parts[0].ToLowerInvariant()] = value;
            }
        }
        return this;
    }

    /// <summary>
    /// Sets version policy configuration from string (e.g., "maxChain=30,sizeLimit=3x").
    /// </summary>
    public ConverterOptionsBuilder WithVersionPolicy(string policyString)
    {
        if (string.IsNullOrWhiteSpace(policyString))
            return this;

        int maxChain = 30;
        float sizeLimit = 3.0f;

        var pairs = policyString.Split(',', StringSplitOptions.RemoveEmptyEntries);
        foreach (var pair in pairs)
        {
            var parts = pair.Split('=', 2, StringSplitOptions.TrimEntries);
            if (parts.Length == 2)
            {
                if (parts[0].Equals("maxChain", StringComparison.OrdinalIgnoreCase) && int.TryParse(parts[1], out var mc))
                    maxChain = mc;
                else if (parts[0].Equals("sizeLimit", StringComparison.OrdinalIgnoreCase))
                {
                    var limitStr = parts[1].TrimEnd('x', 'X');
                    if (float.TryParse(limitStr, out var sl))
                        sizeLimit = sl;
                }
            }
        }

        _versionPolicyConfig = new VersionPolicyConfig(maxChain, sizeLimit);
        return this;
    }

    /// <summary>
    /// Builds the ConverterOptions instance with validation.
    /// </summary>
    /// <returns>Validated ConverterOptions instance.</returns>
    /// <exception cref="InvalidOperationException">When required fields are missing.</exception>
    public ConverterOptions Build()
    {
        if (string.IsNullOrEmpty(_inputPath))
            throw new InvalidOperationException("Input path is required");
        if (string.IsNullOrEmpty(_outputDir))
            throw new InvalidOperationException("Output directory is required");

        // Build permission bits string
        var permString = _permissions.Count > 0
            ? string.Join(",", _permissions.Select(kv => $"{kv.Key}={kv.Value}"))
            : string.Empty;

        // Build version policy string
        var verPolicyString = _versionPolicyConfig != null
            ? $"maxChain={_versionPolicyConfig.MaxChain},sizeLimit={_versionPolicyConfig.SizeLimit}x"
            : string.Empty;

        var options = new ConverterOptions
        {
            InputPath = _inputPath,
            OutputPath = _outputDir,
            TableRecognitionThreshold = _tableRecognitionThreshold,
            FormulaRecognitionThreshold = _formulaRecognitionThreshold,
            RenderIntent = _renderIntent,
            CompatibilityLevel = _compatLevel,
            TargetReader = _targetReader ?? "Foxit",
            MaxMemoryMB = _maxMemoryMB,
            PagesPerSegment = _pagesPerSegment,
            StructuredLogPath = _enableStructuredLog ? "conversion.log" : string.Empty,
            RunJavaScriptSnapshot = _runJsSnapshot,
            PermissionBits = permString,
            VersionPolicyString = verPolicyString
        };

        // Parse permissions and version policy
        if (!string.IsNullOrEmpty(permString))
            options.ParsePermissions();

        if (!string.IsNullOrEmpty(verPolicyString))
            options.ParseVersionPolicy();

        return options;
    }

    private bool GetPermission(string key, bool defaultValue)
    {
        return _permissions.TryGetValue(key, out var value) ? value : defaultValue;
    }

    private record VersionPolicyConfig(int MaxChain, float SizeLimit);
}
