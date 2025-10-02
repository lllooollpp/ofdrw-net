using System.Xml;
using System.Xml.Schema;
using OfdrwNet.Core.Diagnostics;

namespace OfdrwNet.Converter.Validation;

/// <summary>
/// OFD XML schema validator using OFD.xsd
/// </summary>
public class SchemaValidator
{
    private readonly XmlSchemaSet _schemaSet;
    private readonly List<ErrorRecord> _validationErrors = new();

    /// <summary>
    /// Initializes validator with OFD schema
    /// </summary>
    public SchemaValidator()
    {
        _schemaSet = new XmlSchemaSet();
        // Load OFD.xsd from embedded resource or file system
        // For now use minimal setup, actual XSD integration in resource phase
        _schemaSet.ValidationEventHandler += OnValidationEvent;
    }

    /// <summary>
    /// Validate OFD document against OFD.xsd schema
    /// </summary>
    /// <param name="ofdPath">Path to OFD file (ZIP container)</param>
    /// <returns>List of validation errors (empty if valid)</returns>
    public List<ErrorRecord> Validate(string ofdPath)
    {
        _validationErrors.Clear();

        try
        {
            // Extract OFD.xml from ZIP
            using var archive = System.IO.Compression.ZipFile.OpenRead(ofdPath);
            var ofdEntry = archive.GetEntry("OFD.xml");

            if (ofdEntry == null)
            {
                _validationErrors.Add(new ErrorRecord
                {
                    Code = "STR_MISSING_RESOURCE",
                    Message = "Missing OFD.xml in root",
                    Context = "OFD.xml",
                    Severity = ErrorSeverity.Fatal
                });
                return _validationErrors;
            }

            // Parse and validate XML
            using var stream = ofdEntry.Open();
            var settings = new XmlReaderSettings
            {
                ValidationType = ValidationType.Schema,
                Schemas = _schemaSet,
                ValidationFlags = XmlSchemaValidationFlags.ReportValidationWarnings
            };
            settings.ValidationEventHandler += OnValidationEvent;

            using var reader = XmlReader.Create(stream, settings);
            while (reader.Read()) { }
        }
        catch (Exception ex)
        {
            _validationErrors.Add(new ErrorRecord
            {
                Code = "GEN_IO_ERROR",
                Message = $"Failed to read OFD: {ex.Message}",
                Context = ofdPath,
                Severity = ErrorSeverity.Fatal,
                Exception = ex
            });
        }

        return _validationErrors;
    }

    /// <summary>
    /// Validate individual XML file against schema
    /// </summary>
    /// <param name="xmlPath">Path to XML file</param>
    /// <param name="schemaNamespace">Target namespace</param>
    /// <returns>List of validation errors</returns>
    public List<ErrorRecord> ValidateXml(string xmlPath, string schemaNamespace)
    {
        _validationErrors.Clear();

        try
        {
            var settings = new XmlReaderSettings
            {
                ValidationType = ValidationType.Schema,
                Schemas = _schemaSet,
                ValidationFlags = XmlSchemaValidationFlags.ReportValidationWarnings
            };
            settings.ValidationEventHandler += OnValidationEvent;

            using var reader = XmlReader.Create(xmlPath, settings);
            while (reader.Read()) { }
        }
        catch (Exception ex)
        {
            _validationErrors.Add(new ErrorRecord
            {
                Code = "VAL_SCHEMA_ERROR",
                Message = $"XML parsing failed: {ex.Message}",
                Context = xmlPath,
                Severity = ErrorSeverity.Error,
                Exception = ex
            });
        }

        return _validationErrors;
    }

    private void OnValidationEvent(object? sender, ValidationEventArgs e)
    {
        var severity = e.Severity == XmlSeverityType.Error
            ? ErrorSeverity.Error
            : ErrorSeverity.Warning;

        _validationErrors.Add(new ErrorRecord
        {
            Code = "VAL_SCHEMA_ERROR",
            Message = e.Message,
            Context = $"{e.Exception?.SourceUri ?? "unknown"}:L{e.Exception?.LineNumber}:C{e.Exception?.LinePosition}",
            Severity = severity,
            Exception = e.Exception
        });
    }

    /// <summary>
    /// Load custom XSD schema from stream
    /// </summary>
    /// <param name="stream">XSD schema stream</param>
    /// <param name="targetNamespace">Target namespace URI</param>
    public void LoadSchema(Stream stream, string targetNamespace)
    {
        var schema = XmlSchema.Read(stream, OnValidationEvent);
        if (schema != null)
        {
            _schemaSet.Add(schema);
            _schemaSet.Compile();
        }
    }

    /// <summary>
    /// Load XSD schema from file path
    /// </summary>
    /// <param name="xsdPath">Path to XSD file</param>
    public void LoadSchemaFromFile(string xsdPath)
    {
        using var stream = File.OpenRead(xsdPath);
        var schema = XmlSchema.Read(stream, OnValidationEvent);
        if (schema != null)
        {
            _schemaSet.Add(schema);
            _schemaSet.Compile();
        }
    }
}
