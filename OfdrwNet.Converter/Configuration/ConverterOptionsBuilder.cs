using System;
using System.IO;
using System.Text.Json;
using OfdrwNet.Converter.Domain;

namespace OfdrwNet.Converter.Configuration;

/// <summary>
/// Builder for constructing ConverterOptions from JSON configuration files with environment variable overrides.
/// </summary>
/// <remarks>
/// This builder provides a fluent API for configuring the PDF to OFD converter with support for:
/// - Loading defaults from JSON configuration files
/// - Environment variable overrides (prefixed with "OFDRW_")
/// - Programmatic configuration via fluent methods
/// - Validation before building the final options object
///
/// Configuration precedence (highest to lowest):
/// 1. Programmatic configuration (WithXxx methods)
/// 2. Environment variables (OFDRW_TABLE_THRESHOLD, etc.)
/// 3. JSON configuration file
/// 4. Hard-coded defaults
///
/// Example JSON configuration file:
/// ```json
/// {
///   "tableThreshold": 0.85,
///   "formulaThreshold": 0.80,
///   "renderIntent": "relative",
///   "compatLevel": "Std2020",
///   "maxMemMB": 512,
///   "pagesPerSegment": 100,
///   "runJsSnapshot": false,
///   "appendVersion": false
/// }
/// ```
///
/// Example environment variables:
/// - OFDRW_TABLE_THRESHOLD=0.9
/// - OFDRW_MAX_MEM_MB=1024
/// - OFDRW_COMPAT_LEVEL=Full
/// </remarks>
public sealed class ConverterOptionsBuilder
{
    private float? _tableThreshold;
    private float? _formulaThreshold;
    private string? _renderIntent;
    private CompatLevel? _compatLevel;
    private string? _targetReader;
    private int? _maxMemMB;
    private int? _pagesPerSegment;
    private PermissionConfig? _permissions;
    private VersionPolicy? _versionPolicy;
    private bool? _runJsSnapshot;
    private bool? _appendVersion;

    // Default values (used when no other source provides a value)
    private const float _defaultTableThreshold = 0.8f;
    private const float _defaultFormulaThreshold = 0.8f;
    private const string _defaultRenderIntent = "perceptual";
    private const CompatLevel _defaultCompatLevel = CompatLevel.Std2020;
    private const int _defaultMaxMemMB = 512;
    private const int _defaultPagesPerSegment = 100;

    /// <summary>
    /// Creates a new builder instance with default values.
    /// </summary>
    public ConverterOptionsBuilder()
    {
    }

    /// <summary>
    /// Creates a new builder instance.
    /// </summary>
    public static ConverterOptionsBuilder Create() => new();

    /// <summary>
    /// Loads configuration from a JSON file.
    /// </summary>
    /// <param name="jsonPath">Path to the JSON configuration file</param>
    /// <returns>This builder instance for chaining</returns>
    /// <exception cref="FileNotFoundException">When the JSON file does not exist</exception>
    /// <exception cref="JsonException">When the JSON is malformed</exception>
    public ConverterOptionsBuilder LoadFromJson(string jsonPath)
    {
        if (!File.Exists(jsonPath))
        {
            throw new FileNotFoundException($"Configuration file not found: {jsonPath}", jsonPath);
        }

        var json = File.ReadAllText(jsonPath);
        var doc = JsonDocument.Parse(json);
        var root = doc.RootElement;

        // Parse each configuration value if present in JSON
        if (root.TryGetProperty("tableThreshold", out var tableThreshold) && tableThreshold.ValueKind == JsonValueKind.Number)
        {
            _tableThreshold = (float)tableThreshold.GetDouble();
        }

        if (root.TryGetProperty("formulaThreshold", out var formulaThreshold) && formulaThreshold.ValueKind == JsonValueKind.Number)
        {
            _formulaThreshold = (float)formulaThreshold.GetDouble();
        }

        if (root.TryGetProperty("renderIntent", out var renderIntent) && renderIntent.ValueKind == JsonValueKind.String)
        {
            _renderIntent = renderIntent.GetString();
        }

        if (root.TryGetProperty("compatLevel", out var compatLevel) && compatLevel.ValueKind == JsonValueKind.String)
        {
            var levelStr = compatLevel.GetString();
            if (Enum.TryParse<CompatLevel>(levelStr, ignoreCase: true, out var level))
            {
                _compatLevel = level;
            }
        }

        if (root.TryGetProperty("targetReader", out var targetReader) && targetReader.ValueKind == JsonValueKind.String)
        {
            _targetReader = targetReader.GetString();
        }

        if (root.TryGetProperty("maxMemMB", out var maxMemMB) && maxMemMB.ValueKind == JsonValueKind.Number)
        {
            _maxMemMB = maxMemMB.GetInt32();
        }

        if (root.TryGetProperty("pagesPerSegment", out var pagesPerSegment) && pagesPerSegment.ValueKind == JsonValueKind.Number)
        {
            _pagesPerSegment = pagesPerSegment.GetInt32();
        }

        if (root.TryGetProperty("runJsSnapshot", out var runJsSnapshot) && runJsSnapshot.ValueKind == JsonValueKind.True || runJsSnapshot.ValueKind == JsonValueKind.False)
        {
            _runJsSnapshot = runJsSnapshot.GetBoolean();
        }

        if (root.TryGetProperty("appendVersion", out var appendVersion) && appendVersion.ValueKind == JsonValueKind.True || appendVersion.ValueKind == JsonValueKind.False)
        {
            _appendVersion = appendVersion.GetBoolean();
        }

        // Parse nested permissions
        if (root.TryGetProperty("permissions", out var permissionsObj) && permissionsObj.ValueKind == JsonValueKind.Object)
        {
            _permissions = ParsePermissionsFromJson(permissionsObj);
        }

        // Parse nested version policy
        if (root.TryGetProperty("versionPolicy", out var versionPolicyObj) && versionPolicyObj.ValueKind == JsonValueKind.Object)
        {
            _versionPolicy = ParseVersionPolicyFromJson(versionPolicyObj);
        }

        return this;
    }

    /// <summary>
    /// Loads configuration from environment variables (prefixed with "OFDRW_").
    /// </summary>
    /// <returns>This builder instance for chaining</returns>
    /// <remarks>
    /// Supported environment variables:
    /// - OFDRW_TABLE_THRESHOLD: float value (0.0-1.0)
    /// - OFDRW_FORMULA_THRESHOLD: float value (0.0-1.0)
    /// - OFDRW_RENDER_INTENT: string (perceptual, relative, saturation, absolute)
    /// - OFDRW_COMPAT_LEVEL: string (Base, Std2020, Full)
    /// - OFDRW_TARGET_READER: string
    /// - OFDRW_MAX_MEM_MB: integer
    /// - OFDRW_PAGES_PER_SEGMENT: integer
    /// - OFDRW_RUN_JS_SNAPSHOT: boolean (true/false)
    /// - OFDRW_APPEND_VERSION: boolean (true/false)
    /// </remarks>
    public ConverterOptionsBuilder LoadFromEnvironment()
    {
        var tableThresholdEnv = Environment.GetEnvironmentVariable("OFDRW_TABLE_THRESHOLD");
        if (!string.IsNullOrEmpty(tableThresholdEnv) && float.TryParse(tableThresholdEnv, out var tableThreshold))
        {
            _tableThreshold = tableThreshold;
        }

        var formulaThresholdEnv = Environment.GetEnvironmentVariable("OFDRW_FORMULA_THRESHOLD");
        if (!string.IsNullOrEmpty(formulaThresholdEnv) && float.TryParse(formulaThresholdEnv, out var formulaThreshold))
        {
            _formulaThreshold = formulaThreshold;
        }

        var renderIntentEnv = Environment.GetEnvironmentVariable("OFDRW_RENDER_INTENT");
        if (!string.IsNullOrEmpty(renderIntentEnv))
        {
            _renderIntent = renderIntentEnv;
        }

        var compatLevelEnv = Environment.GetEnvironmentVariable("OFDRW_COMPAT_LEVEL");
        if (!string.IsNullOrEmpty(compatLevelEnv) && Enum.TryParse<CompatLevel>(compatLevelEnv, ignoreCase: true, out var compatLevel))
        {
            _compatLevel = compatLevel;
        }

        var targetReaderEnv = Environment.GetEnvironmentVariable("OFDRW_TARGET_READER");
        if (!string.IsNullOrEmpty(targetReaderEnv))
        {
            _targetReader = targetReaderEnv;
        }

        var maxMemMBEnv = Environment.GetEnvironmentVariable("OFDRW_MAX_MEM_MB");
        if (!string.IsNullOrEmpty(maxMemMBEnv) && int.TryParse(maxMemMBEnv, out var maxMemMB))
        {
            _maxMemMB = maxMemMB;
        }

        var pagesPerSegmentEnv = Environment.GetEnvironmentVariable("OFDRW_PAGES_PER_SEGMENT");
        if (!string.IsNullOrEmpty(pagesPerSegmentEnv) && int.TryParse(pagesPerSegmentEnv, out var pagesPerSegment))
        {
            _pagesPerSegment = pagesPerSegment;
        }

        var runJsSnapshotEnv = Environment.GetEnvironmentVariable("OFDRW_RUN_JS_SNAPSHOT");
        if (!string.IsNullOrEmpty(runJsSnapshotEnv) && bool.TryParse(runJsSnapshotEnv, out var runJsSnapshot))
        {
            _runJsSnapshot = runJsSnapshot;
        }

        var appendVersionEnv = Environment.GetEnvironmentVariable("OFDRW_APPEND_VERSION");
        if (!string.IsNullOrEmpty(appendVersionEnv) && bool.TryParse(appendVersionEnv, out var appendVersion))
        {
            _appendVersion = appendVersion;
        }

        return this;
    }

    /// <summary>
    /// Sets the table recognition confidence threshold (0.0-1.0).
    /// </summary>
    public ConverterOptionsBuilder WithTableThreshold(float threshold)
    {
        if (threshold < 0.0f || threshold > 1.0f)
        {
            throw new ArgumentOutOfRangeException(nameof(threshold), "Table threshold must be between 0.0 and 1.0");
        }
        _tableThreshold = threshold;
        return this;
    }

    /// <summary>
    /// Sets the formula recognition confidence threshold (0.0-1.0).
    /// </summary>
    public ConverterOptionsBuilder WithFormulaThreshold(float threshold)
    {
        if (threshold < 0.0f || threshold > 1.0f)
        {
            throw new ArgumentOutOfRangeException(nameof(threshold), "Formula threshold must be between 0.0 and 1.0");
        }
        _formulaThreshold = threshold;
        return this;
    }

    /// <summary>
    /// Sets the rendering intent for color management.
    /// </summary>
    /// <param name="intent">One of: perceptual, relative, saturation, absolute</param>
    public ConverterOptionsBuilder WithRenderIntent(string intent)
    {
        if (string.IsNullOrWhiteSpace(intent))
        {
            throw new ArgumentException("Render intent cannot be empty", nameof(intent));
        }

        var validIntents = new[] { "perceptual", "relative", "saturation", "absolute" };
        if (!validIntents.Contains(intent.ToLowerInvariant()))
        {
            throw new ArgumentException(
                $"Invalid render intent '{intent}'. Valid values: {string.Join(", ", validIntents)}",
                nameof(intent));
        }

        _renderIntent = intent.ToLowerInvariant();
        return this;
    }

    /// <summary>
    /// Sets the OFD compatibility level.
    /// </summary>
    public ConverterOptionsBuilder WithCompatLevel(CompatLevel level)
    {
        _compatLevel = level;
        return this;
    }

    /// <summary>
    /// Sets the target reader for compatibility profiling.
    /// </summary>
    public ConverterOptionsBuilder WithTargetReader(string? reader)
    {
        _targetReader = reader;
        return this;
    }

    /// <summary>
    /// Sets the maximum memory threshold in MB before triggering segmentation.
    /// </summary>
    public ConverterOptionsBuilder WithMaxMemoryMB(int maxMB)
    {
        if (maxMB <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(maxMB), "Max memory must be positive");
        }
        _maxMemMB = maxMB;
        return this;
    }

    /// <summary>
    /// Sets the pages per segment when memory threshold is exceeded.
    /// </summary>
    public ConverterOptionsBuilder WithPagesPerSegment(int pages)
    {
        if (pages <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(pages), "Pages per segment must be positive");
        }
        _pagesPerSegment = pages;
        return this;
    }

    /// <summary>
    /// Sets the permissions configuration.
    /// </summary>
    public ConverterOptionsBuilder WithPermissions(PermissionConfig permissions)
    {
        _permissions = permissions ?? throw new ArgumentNullException(nameof(permissions));
        return this;
    }

    /// <summary>
    /// Sets the version policy configuration.
    /// </summary>
    public ConverterOptionsBuilder WithVersionPolicy(VersionPolicy versionPolicy)
    {
        _versionPolicy = versionPolicy ?? throw new ArgumentNullException(nameof(versionPolicy));
        return this;
    }

    /// <summary>
    /// Enables or disables JavaScript snapshot execution.
    /// </summary>
    public ConverterOptionsBuilder WithJsSnapshot(bool enable)
    {
        _runJsSnapshot = enable;
        return this;
    }

    /// <summary>
    /// Enables or disables version appending.
    /// </summary>
    public ConverterOptionsBuilder WithAppendVersion(bool enable)
    {
        _appendVersion = enable;
        return this;
    }

    /// <summary>
    /// Builds the ConverterOptions instance with all configured values.
    /// </summary>
    /// <returns>Validated ConverterOptions instance</returns>
    /// <exception cref="ArgumentException">When validation fails</exception>
    public ConverterOptions Build()
    {
        var options = new ConverterOptions
        {
            TableThreshold = _tableThreshold ?? _defaultTableThreshold,
            FormulaThreshold = _formulaThreshold ?? _defaultFormulaThreshold,
            RenderIntent = _renderIntent ?? _defaultRenderIntent,
            CompatLevel = _compatLevel ?? _defaultCompatLevel,
            TargetReader = _targetReader,
            MaxMemMB = _maxMemMB ?? _defaultMaxMemMB,
            PagesPerSegment = _pagesPerSegment ?? _defaultPagesPerSegment,
            Permissions = _permissions,
            VersionPolicy = _versionPolicy,
            RunJsSnapshot = _runJsSnapshot ?? false,
            AppendVersion = _appendVersion ?? false
        };

        // Validate the built options
        options.Validate();

        return options;
    }

    #region Private Helper Methods

    private PermissionConfig ParsePermissionsFromJson(JsonElement permissionsObj)
    {
        var print = true;
        var printHQ = true;
        var modify = true;
        var annotate = true;
        var export = true;
        string? owner = null;

        if (permissionsObj.TryGetProperty("print", out var printProp))
        {
            print = printProp.GetBoolean();
        }

        if (permissionsObj.TryGetProperty("printHQ", out var printHQProp))
        {
            printHQ = printHQProp.GetBoolean();
        }

        if (permissionsObj.TryGetProperty("modify", out var modifyProp))
        {
            modify = modifyProp.GetBoolean();
        }

        if (permissionsObj.TryGetProperty("annotate", out var annotateProp))
        {
            annotate = annotateProp.GetBoolean();
        }

        if (permissionsObj.TryGetProperty("export", out var exportProp))
        {
            export = exportProp.GetBoolean();
        }

        if (permissionsObj.TryGetProperty("owner", out var ownerProp) && ownerProp.ValueKind == JsonValueKind.String)
        {
            owner = ownerProp.GetString();
        }

        return new PermissionConfig
        {
            Print = print,
            PrintHQ = printHQ,
            Modify = modify,
            Annotate = annotate,
            Export = export,
            Owner = owner
        };
    }

    private VersionPolicy ParseVersionPolicyFromJson(JsonElement versionPolicyObj)
    {
        var maxChain = 30;
        var sizeLimitRatio = 3.0;
        var autoCompact = true;
        var compactThreshold = 20;

        if (versionPolicyObj.TryGetProperty("maxChain", out var maxChainProp) && maxChainProp.ValueKind == JsonValueKind.Number)
        {
            maxChain = maxChainProp.GetInt32();
        }

        if (versionPolicyObj.TryGetProperty("sizeLimitRatio", out var sizeLimitRatioProp) && sizeLimitRatioProp.ValueKind == JsonValueKind.Number)
        {
            sizeLimitRatio = sizeLimitRatioProp.GetDouble();
        }

        if (versionPolicyObj.TryGetProperty("autoCompact", out var autoCompactProp))
        {
            autoCompact = autoCompactProp.GetBoolean();
        }

        if (versionPolicyObj.TryGetProperty("compactThreshold", out var compactThresholdProp) && compactThresholdProp.ValueKind == JsonValueKind.Number)
        {
            compactThreshold = compactThresholdProp.GetInt32();
        }

        return new VersionPolicy
        {
            MaxChain = maxChain,
            SizeLimitRatio = sizeLimitRatio,
            AutoCompact = autoCompact,
            CompactThreshold = compactThreshold
        };
    }

    #endregion
}
