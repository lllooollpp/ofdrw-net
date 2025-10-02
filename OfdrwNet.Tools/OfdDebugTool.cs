using System.IO.Compression;
using System.Text;
using System.Xml.Linq;
using OfdrwNet.Core.Diagnostics;

namespace OfdrwNet.Tools;

/// <summary>
/// OFD debug tool with structured logging
/// Provides inspection, validation, and diagnostic capabilities
/// </summary>
public class OfdDebugTool
{
    private readonly IStructuredLogger? _logger;

    /// <summary>
    /// Initialize debug tool
    /// </summary>
    /// <param name="logger">Optional structured logger</param>
    public OfdDebugTool(IStructuredLogger? logger = null)
    {
        _logger = logger;
    }

    /// <summary>
    /// Inspect OFD file structure and report contents
    /// </summary>
    /// <param name="ofdPath">Path to OFD file</param>
    /// <returns>Inspection result with file tree and metadata</returns>
    public OfdInspectionResult Inspect(string ofdPath)
    {
        _logger?.LogInfo("debug.inspect", new
        {
            action = "start",
            file = ofdPath
        });

        var result = new OfdInspectionResult
        {
            FilePath = ofdPath,
            Timestamp = DateTime.UtcNow
        };

        try
        {
            using var archive = ZipFile.OpenRead(ofdPath);
            result.FileSize = new FileInfo(ofdPath).Length;
            result.EntryCount = archive.Entries.Count;

            foreach (var entry in archive.Entries)
            {
                var entryInfo = new OfdEntryInfo
                {
                    Path = entry.FullName,
                    CompressedSize = entry.CompressedLength,
                    UncompressedSize = entry.Length,
                    CompressionRatio = entry.Length > 0
                        ? (double)entry.CompressedLength / entry.Length
                        : 0
                };

                // Parse XML metadata for key files
                if (entry.FullName.EndsWith(".xml", StringComparison.OrdinalIgnoreCase))
                {
                    try
                    {
                        using var stream = entry.Open();
                        var doc = XDocument.Load(stream);
                        entryInfo.XmlRootElement = doc.Root?.Name.LocalName ?? "unknown";
                        entryInfo.XmlNamespace = doc.Root?.Name.NamespaceName ?? string.Empty;
                    }
                    catch (Exception ex)
                    {
                        entryInfo.ParseError = ex.Message;
                    }
                }

                result.Entries.Add(entryInfo);
            }

            // Extract OFD.xml metadata
            var ofdEntry = archive.GetEntry("OFD.xml");
            if (ofdEntry != null)
            {
                try
                {
                    using var stream = ofdEntry.Open();
                    var doc = XDocument.Load(stream);
                    result.OfdVersion = doc.Root?.Attribute("Version")?.Value ?? "unknown";
                    result.DocCount = doc.Root?.Element("DocBody")?.Elements("DocRoot").Count() ?? 0;
                }
                catch { }
            }

            _logger?.LogInfo("debug.inspect", new
            {
                action = "complete",
                file = ofdPath,
                entryCount = result.EntryCount,
                fileSize = result.FileSize,
                version = result.OfdVersion
            });

            result.Success = true;
        }
        catch (Exception ex)
        {
            result.Success = false;
            result.Error = ex.Message;

            _logger?.LogError(LogEvents.Error, new
            {
                action = "inspect_failed",
                file = ofdPath,
                error = ex.Message
            });
        }

        return result;
    }

    /// <summary>
    /// Extract specific file from OFD package
    /// </summary>
    /// <param name="ofdPath">OFD file path</param>
    /// <param name="entryPath">Entry path within OFD</param>
    /// <param name="outputPath">Output file path</param>
    public void ExtractFile(string ofdPath, string entryPath, string outputPath)
    {
        _logger?.LogInfo("debug.extract", new
        {
            action = "start",
            ofd = ofdPath,
            entry = entryPath,
            output = outputPath
        });

        try
        {
            using var archive = ZipFile.OpenRead(ofdPath);
            var entry = archive.GetEntry(entryPath);

            if (entry == null)
            {
                _logger?.LogWarn("debug.extract", new
                {
                    action = "entry_not_found",
                    entry = entryPath
                });
                return;
            }

            entry.ExtractToFile(outputPath, overwrite: true);

            _logger?.LogInfo("debug.extract", new
            {
                action = "complete",
                entry = entryPath,
                size = entry.Length
            });
        }
        catch (Exception ex)
        {
            _logger?.LogError(LogEvents.Error, new
            {
                action = "extract_failed",
                entry = entryPath,
                error = ex.Message
            });
            throw;
        }
    }

    /// <summary>
    /// List all resources in OFD package
    /// </summary>
    /// <param name="ofdPath">OFD file path</param>
    /// <returns>List of resource entries</returns>
    public List<ResourceEntry> ListResources(string ofdPath)
    {
        _logger?.LogInfo("debug.list_resources", new
        {
            action = "start",
            file = ofdPath
        });

        var resources = new List<ResourceEntry>();

        try
        {
            using var archive = ZipFile.OpenRead(ofdPath);

            foreach (var entry in archive.Entries)
            {
                var path = entry.FullName.ToLowerInvariant();

                // Detect resource type by path and extension
                ResourceType type = ResourceType.Other;

                if (path.Contains("/res/") || path.Contains("/publicres/") || path.Contains("/documentres/"))
                {
                    if (path.EndsWith(".ttf") || path.EndsWith(".otf") || path.EndsWith(".ttc"))
                        type = ResourceType.Font;
                    else if (path.EndsWith(".jpg") || path.EndsWith(".jpeg") || path.EndsWith(".png") ||
                             path.EndsWith(".jb2") || path.EndsWith(".gbig2"))
                        type = ResourceType.Image;
                    else if (path.EndsWith(".icc") || path.EndsWith(".icm"))
                        type = ResourceType.ColorProfile;
                    else if (path.EndsWith(".xml"))
                        type = ResourceType.Metadata;
                }

                if (type != ResourceType.Other)
                {
                    resources.Add(new ResourceEntry
                    {
                        Path = entry.FullName,
                        Type = type,
                        Size = entry.Length,
                        CompressedSize = entry.CompressedLength
                    });
                }
            }

            _logger?.LogInfo("debug.list_resources", new
            {
                action = "complete",
                totalResources = resources.Count,
                fonts = resources.Count(r => r.Type == ResourceType.Font),
                images = resources.Count(r => r.Type == ResourceType.Image),
                colorProfiles = resources.Count(r => r.Type == ResourceType.ColorProfile)
            });
        }
        catch (Exception ex)
        {
            _logger?.LogError(LogEvents.Error, new
            {
                action = "list_resources_failed",
                error = ex.Message
            });
        }

        return resources;
    }

    /// <summary>
    /// Validate OFD structure and report issues
    /// </summary>
    /// <param name="ofdPath">OFD file path</param>
    /// <returns>Validation result with issues</returns>
    public ValidationResult Validate(string ofdPath)
    {
        _logger?.LogInfo("debug.validate", new
        {
            action = "start",
            file = ofdPath
        });

        var result = new ValidationResult
        {
            FilePath = ofdPath,
            Timestamp = DateTime.UtcNow
        };

        try
        {
            using var archive = ZipFile.OpenRead(ofdPath);

            // Check required files
            if (archive.GetEntry("OFD.xml") == null)
            {
                result.Issues.Add(new ValidationIssue
                {
                    Severity = "error",
                    Code = "MISSING_OFD_XML",
                    Message = "Required OFD.xml not found"
                });
            }

            // Check XML well-formedness
            foreach (var entry in archive.Entries.Where(e =>
                e.FullName.EndsWith(".xml", StringComparison.OrdinalIgnoreCase)))
            {
                try
                {
                    using var stream = entry.Open();
                    XDocument.Load(stream);
                }
                catch (Exception ex)
                {
                    result.Issues.Add(new ValidationIssue
                    {
                        Severity = "error",
                        Code = "MALFORMED_XML",
                        Message = $"Invalid XML in {entry.FullName}: {ex.Message}",
                        Context = entry.FullName
                    });
                }
            }

            result.IsValid = result.Issues.Count(i => i.Severity == "error") == 0;

            _logger?.LogInfo("debug.validate", new
            {
                action = "complete",
                isValid = result.IsValid,
                errorCount = result.Issues.Count(i => i.Severity == "error"),
                warningCount = result.Issues.Count(i => i.Severity == "warning")
            });
        }
        catch (Exception ex)
        {
            result.IsValid = false;
            result.Issues.Add(new ValidationIssue
            {
                Severity = "error",
                Code = "VALIDATION_FAILED",
                Message = $"Validation failed: {ex.Message}"
            });

            _logger?.LogError(LogEvents.Error, new
            {
                action = "validate_failed",
                error = ex.Message
            });
        }

        return result;
    }

    /// <summary>
    /// Generate diagnostic report
    /// </summary>
    /// <param name="ofdPath">OFD file path</param>
    /// <returns>Diagnostic report text</returns>
    public string GenerateReport(string ofdPath)
    {
        var report = new StringBuilder();
        report.AppendLine("=== OFD Debug Report ===");
        report.AppendLine($"File: {ofdPath}");
        report.AppendLine($"Generated: {DateTime.UtcNow:O}");
        report.AppendLine();

        // Inspection
        var inspection = Inspect(ofdPath);
        report.AppendLine("Structure:");
        report.AppendLine($"  Version: {inspection.OfdVersion}");
        report.AppendLine($"  Documents: {inspection.DocCount}");
        report.AppendLine($"  Entries: {inspection.EntryCount}");
        report.AppendLine($"  Size: {inspection.FileSize:N0} bytes");
        report.AppendLine();

        // Resources
        var resources = ListResources(ofdPath);
        report.AppendLine("Resources:");
        report.AppendLine($"  Total: {resources.Count}");
        report.AppendLine($"  Fonts: {resources.Count(r => r.Type == ResourceType.Font)}");
        report.AppendLine($"  Images: {resources.Count(r => r.Type == ResourceType.Image)}");
        report.AppendLine($"  Color Profiles: {resources.Count(r => r.Type == ResourceType.ColorProfile)}");
        report.AppendLine();

        // Validation
        var validation = Validate(ofdPath);
        report.AppendLine("Validation:");
        report.AppendLine($"  Status: {(validation.IsValid ? "VALID" : "INVALID")}");
        if (validation.Issues.Any())
        {
            report.AppendLine("  Issues:");
            foreach (var issue in validation.Issues)
            {
                report.AppendLine($"    [{issue.Severity.ToUpper()}] {issue.Code}: {issue.Message}");
            }
        }

        return report.ToString();
    }
}

#region Data Types

public class OfdInspectionResult
{
    public string FilePath { get; set; } = string.Empty;
    public DateTime Timestamp { get; set; }
    public bool Success { get; set; }
    public string? Error { get; set; }
    public long FileSize { get; set; }
    public int EntryCount { get; set; }
    public string OfdVersion { get; set; } = string.Empty;
    public int DocCount { get; set; }
    public List<OfdEntryInfo> Entries { get; set; } = new();
}

public class OfdEntryInfo
{
    public string Path { get; set; } = string.Empty;
    public long CompressedSize { get; set; }
    public long UncompressedSize { get; set; }
    public double CompressionRatio { get; set; }
    public string? XmlRootElement { get; set; }
    public string? XmlNamespace { get; set; }
    public string? ParseError { get; set; }
}

public class ResourceEntry
{
    public string Path { get; set; } = string.Empty;
    public ResourceType Type { get; set; }
    public long Size { get; set; }
    public long CompressedSize { get; set; }
}

public enum ResourceType
{
    Font,
    Image,
    ColorProfile,
    Metadata,
    Other
}

public class ValidationResult
{
    public string FilePath { get; set; } = string.Empty;
    public DateTime Timestamp { get; set; }
    public bool IsValid { get; set; }
    public List<ValidationIssue> Issues { get; set; } = new();
}

public class ValidationIssue
{
    public string Severity { get; set; } = string.Empty; // "error", "warning", "info"
    public string Code { get; set; } = string.Empty;
    public string Message { get; set; } = string.Empty;
    public string? Context { get; set; }
}

#endregion
