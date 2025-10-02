using OfdrwNet.Core.Diagnostics;

namespace OfdrwNet.Converter.Validation;

/// <summary>
/// Composite validation engine combining schema and semantic validation
/// </summary>
public class CompositeValidationEngine
{
    private readonly SchemaValidator _schemaValidator;
    private readonly SemanticRuleEngine _semanticValidator;
    private readonly ValidationOptions _options;

    /// <summary>
    /// Initializes composite validation engine
    /// </summary>
    /// <param name="schemaValidator">Schema validator (null to skip schema validation)</param>
    /// <param name="semanticValidator">Semantic validator (null to skip semantic validation)</param>
    /// <param name="options">Validation options</param>
    public CompositeValidationEngine(
        SchemaValidator? schemaValidator = null,
        SemanticRuleEngine? semanticValidator = null,
        ValidationOptions? options = null)
    {
        _schemaValidator = schemaValidator ?? new SchemaValidator();
        _semanticValidator = semanticValidator ?? new SemanticRuleEngine();
        _options = options ?? new ValidationOptions();
    }

    /// <summary>
    /// Validate OFD document with both schema and semantic validation
    /// </summary>
    /// <param name="ofdPath">Path to OFD file</param>
    /// <returns>Composite validation result</returns>
    public CompositeValidationResult Validate(string ofdPath)
    {
        var result = new CompositeValidationResult
        {
            OfdPath = ofdPath,
            StartTime = DateTime.UtcNow
        };

        // Step 1: Schema validation (if enabled)
        if (_options.EnableSchemaValidation)
        {
            try
            {
                result.SchemaErrors = _schemaValidator.Validate(ofdPath);
                result.SchemaValidationPerformed = true;
            }
            catch (Exception ex)
            {
                result.SchemaErrors.Add(new ErrorRecord
                {
                    Code = "VAL_SCHEMA_EXCEPTION",
                    Message = $"Schema validation failed: {ex.Message}",
                    Context = ofdPath,
                    Severity = ErrorSeverity.Fatal,
                    Exception = ex
                });
            }
        }

        // If schema validation found fatal errors and StopOnSchemaError is enabled, skip semantic validation
        if (_options.StopOnSchemaError && result.SchemaErrors.Any(e => e.Severity == ErrorSeverity.Fatal))
        {
            result.EndTime = DateTime.UtcNow;
            result.SemanticValidationSkipped = true;
            result.SkipReason = "Schema validation found fatal errors";
            return result;
        }

        // Step 2: Semantic validation (if enabled)
        if (_options.EnableSemanticValidation)
        {
            try
            {
                var context = BuildValidationContext(ofdPath);
                result.SemanticErrors = _semanticValidator.Validate(context);
                result.SemanticValidationPerformed = true;
            }
            catch (Exception ex)
            {
                result.SemanticErrors.Add(new ErrorRecord
                {
                    Code = "VAL_SEMANTIC_EXCEPTION",
                    Message = $"Semantic validation failed: {ex.Message}",
                    Context = ofdPath,
                    Severity = ErrorSeverity.Error,
                    Exception = ex
                });
            }
        }

        result.EndTime = DateTime.UtcNow;
        return result;
    }

    /// <summary>
    /// Validate OFD document and return aggregated errors
    /// </summary>
    /// <param name="ofdPath">Path to OFD file</param>
    /// <returns>Aggregated error list</returns>
    public List<ErrorRecord> ValidateAndGetErrors(string ofdPath)
    {
        var result = Validate(ofdPath);
        return result.GetAllErrors();
    }

    /// <summary>
    /// Validate OFD document and check if it's valid
    /// </summary>
    /// <param name="ofdPath">Path to OFD file</param>
    /// <returns>True if document is valid (no errors)</returns>
    public bool IsValid(string ofdPath)
    {
        var result = Validate(ofdPath);
        return result.IsValid;
    }

    /// <summary>
    /// Get validation statistics
    /// </summary>
    /// <param name="ofdPath">Path to OFD file</param>
    /// <returns>Validation statistics</returns>
    public ValidationStatistics GetStatistics(string ofdPath)
    {
        var result = Validate(ofdPath);
        return new ValidationStatistics
        {
            TotalErrors = result.GetAllErrors().Count,
            SchemaErrors = result.SchemaErrors.Count,
            SemanticErrors = result.SemanticErrors.Count,
            FatalErrors = result.GetAllErrors().Count(e => e.Severity == ErrorSeverity.Fatal),
            Errors = result.GetAllErrors().Count(e => e.Severity == ErrorSeverity.Error),
            Warnings = result.GetAllErrors().Count(e => e.Severity == ErrorSeverity.Warning),
            InfoMessages = result.GetAllErrors().Count(e => e.Severity == ErrorSeverity.Info),
            ValidationDuration = result.EndTime - result.StartTime
        };
    }

    /// <summary>
    /// Build validation context from OFD file
    /// </summary>
    /// <param name="ofdPath">Path to OFD file</param>
    /// <returns>Validation context</returns>
    private ValidationContext BuildValidationContext(string ofdPath)
    {
        var context = new ValidationContext
        {
            OfdPath = ofdPath
        };

        try
        {
            // Extract OFD structure
            using var archive = System.IO.Compression.ZipFile.OpenRead(ofdPath);

            // Parse OFD.xml
            var ofdEntry = archive.GetEntry("OFD.xml");
            if (ofdEntry != null)
            {
                using var stream = ofdEntry.Open();
                var reader = System.Xml.Linq.XDocument.Load(stream);

                // Extract document structure
                // This is a simplified version - full implementation requires complete OFD parsing
                var root = reader.Root;
                if (root != null)
                {
                    // Parse document references
                    var docRefs = root.Descendants().Where(e => e.Name.LocalName == "DocBody");
                    foreach (var docRef in docRefs)
                    {
                        var docRoot = docRef.Element(System.Xml.Linq.XName.Get("DocRoot", root.Name.NamespaceName));
                        if (docRoot != null)
                        {
                            context.AddElement("Document", docRoot.Value);
                        }
                    }
                }
            }

            // Parse Document.xml for page structure
            var docEntry = archive.Entries.FirstOrDefault(e => e.FullName.EndsWith("Document.xml"));
            if (docEntry != null)
            {
                using var stream = docEntry.Open();
                var reader = System.Xml.Linq.XDocument.Load(stream);

                var root = reader.Root;
                if (root != null)
                {
                    // Parse pages
                    var pages = root.Descendants().Where(e => e.Name.LocalName == "Page");
                    int pageIndex = 0;
                    foreach (var page in pages)
                    {
                        var id = page.Attribute("ID")?.Value ?? $"Page_{pageIndex}";
                        context.AddElement($"Page_{pageIndex}", page);

                        // Parse page physical box
                        var physicalBox = page.Descendants().FirstOrDefault(e => e.Name.LocalName == "PhysicalBox");
                        if (physicalBox != null)
                        {
                            var coords = physicalBox.Value.Split(' ');
                            if (coords.Length >= 4)
                            {
                                if (double.TryParse(coords[2], out var width) && double.TryParse(coords[3], out var height))
                                {
                                    context.PageBounds[pageIndex] = (width, height);
                                }
                            }
                        }

                        // Parse resource references
                        var resourceRefs = page.Descendants().Where(e => e.Name.LocalName == "Resource");
                        foreach (var resRef in resourceRefs)
                        {
                            var resId = resRef.Value;
                            if (!string.IsNullOrWhiteSpace(resId))
                            {
                                context.AddReference($"Page_{pageIndex}", resId);
                            }
                        }

                        pageIndex++;
                    }
                }
            }
        }
        catch (Exception ex)
        {
            // Add error to context but continue validation
            context.AddElement("ParseError", ex.Message);
        }

        return context;
    }
}

/// <summary>
/// Composite validation result
/// </summary>
public class CompositeValidationResult
{
    /// <summary>
    /// OFD file path
    /// </summary>
    public string OfdPath { get; set; } = string.Empty;

    /// <summary>
    /// Schema validation errors
    /// </summary>
    public List<ErrorRecord> SchemaErrors { get; set; } = new();

    /// <summary>
    /// Semantic validation errors
    /// </summary>
    public List<ErrorRecord> SemanticErrors { get; set; } = new();

    /// <summary>
    /// Whether schema validation was performed
    /// </summary>
    public bool SchemaValidationPerformed { get; set; }

    /// <summary>
    /// Whether semantic validation was performed
    /// </summary>
    public bool SemanticValidationPerformed { get; set; }

    /// <summary>
    /// Whether semantic validation was skipped
    /// </summary>
    public bool SemanticValidationSkipped { get; set; }

    /// <summary>
    /// Reason for skipping semantic validation
    /// </summary>
    public string? SkipReason { get; set; }

    /// <summary>
    /// Validation start time
    /// </summary>
    public DateTime StartTime { get; set; }

    /// <summary>
    /// Validation end time
    /// </summary>
    public DateTime EndTime { get; set; }

    /// <summary>
    /// Whether document is valid (no errors)
    /// </summary>
    public bool IsValid => !GetAllErrors().Any(e => e.Severity == ErrorSeverity.Error || e.Severity == ErrorSeverity.Fatal);

    /// <summary>
    /// Get all errors from both schema and semantic validation
    /// </summary>
    /// <returns>Aggregated error list sorted by severity</returns>
    public List<ErrorRecord> GetAllErrors()
    {
        var allErrors = new List<ErrorRecord>();
        allErrors.AddRange(SchemaErrors);
        allErrors.AddRange(SemanticErrors);

        // Sort by severity (Fatal -> Error -> Warning -> Info)
        return allErrors.OrderByDescending(e => e.Severity).ToList();
    }

    /// <summary>
    /// Get errors grouped by severity
    /// </summary>
    /// <returns>Dictionary of severity -> error list</returns>
    public Dictionary<ErrorSeverity, List<ErrorRecord>> GetErrorsBySeverity()
    {
        return GetAllErrors()
            .GroupBy(e => e.Severity)
            .ToDictionary(g => g.Key, g => g.ToList());
    }

    /// <summary>
    /// Get summary of validation result
    /// </summary>
    /// <returns>Summary string</returns>
    public string GetSummary()
    {
        var errors = GetAllErrors();
        var fatalCount = errors.Count(e => e.Severity == ErrorSeverity.Fatal);
        var errorCount = errors.Count(e => e.Severity == ErrorSeverity.Error);
        var warningCount = errors.Count(e => e.Severity == ErrorSeverity.Warning);
        var duration = (EndTime - StartTime).TotalSeconds;

        return $"Validation completed in {duration:F2}s: {fatalCount} fatal, {errorCount} errors, {warningCount} warnings";
    }
}

/// <summary>
/// Validation options
/// </summary>
public class ValidationOptions
{
    /// <summary>
    /// Enable schema validation
    /// </summary>
    public bool EnableSchemaValidation { get; set; } = true;

    /// <summary>
    /// Enable semantic validation
    /// </summary>
    public bool EnableSemanticValidation { get; set; } = true;

    /// <summary>
    /// Stop on schema error (skip semantic validation if schema has fatal errors)
    /// </summary>
    public bool StopOnSchemaError { get; set; } = true;
}

/// <summary>
/// Validation statistics
/// </summary>
public class ValidationStatistics
{
    /// <summary>
    /// Total error count
    /// </summary>
    public int TotalErrors { get; set; }

    /// <summary>
    /// Schema error count
    /// </summary>
    public int SchemaErrors { get; set; }

    /// <summary>
    /// Semantic error count
    /// </summary>
    public int SemanticErrors { get; set; }

    /// <summary>
    /// Fatal error count
    /// </summary>
    public int FatalErrors { get; set; }

    /// <summary>
    /// Error count (severity: Error)
    /// </summary>
    public int Errors { get; set; }

    /// <summary>
    /// Warning count
    /// </summary>
    public int Warnings { get; set; }

    /// <summary>
    /// Info message count
    /// </summary>
    public int InfoMessages { get; set; }

    /// <summary>
    /// Validation duration
    /// </summary>
    public TimeSpan ValidationDuration { get; set; }
}
