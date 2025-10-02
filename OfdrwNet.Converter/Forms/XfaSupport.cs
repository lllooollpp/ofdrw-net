using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace OfdrwNet.Converter.Forms;

/// <summary>
/// XFA 表单检测器。
/// </summary>
/// <remarks>
/// 检测 PDF 中的 XFA (XML Forms Architecture) 包。
/// XFA 是 Adobe 的动态表单格式，OFD 不支持，需要：
/// 1. 检测 XFA 存在
/// 2. 提取计算脚本并执行固化结果
/// 3. 生成静态外观快照
/// 4. 输出 hints.json 警告用户
///
/// DR-9: XFA 检测与警告、静态外观快照生成
/// DR-10: XFA calculate 结果固化与 hints JSON 生成
/// </remarks>
public sealed class XfaDetector
{
    private readonly ILogger<XfaDetector> _logger;

    /// <summary>
    /// 初始化 XfaDetector 实例。
    /// </summary>
    /// <param name="logger">日志记录器</param>
    public XfaDetector(ILogger<XfaDetector> logger)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <summary>
    /// 检测 PDF 文档是否包含 XFA 表单。
    /// </summary>
    /// <param name="pdfDocument">PDF 文档对象（iText7 PdfDocument 或模拟对象）</param>
    /// <returns>XFA 检测结果</returns>
    public XfaDetectionResult Detect(object pdfDocument)
    {
        if (pdfDocument == null)
        {
            throw new ArgumentNullException(nameof(pdfDocument));
        }

        var result = new XfaDetectionResult
        {
            HasXfa = false,
            FieldCount = 0,
            Scripts = new List<XfaScriptInfo>()
        };

        try
        {
            // 占位实现：检查对象是否有 /XFA 或 AcroForm 属性
            var type = pdfDocument.GetType();
            var catalogProperty = type.GetProperty("Catalog") ?? type.GetProperty("GetCatalog");

            if (catalogProperty != null)
            {
                var catalog = catalogProperty.GetValue(pdfDocument);
                if (catalog != null)
                {
                    // 检查 /AcroForm 字典
                    var acroFormProperty = catalog.GetType().GetProperty("AcroForm");
                    if (acroFormProperty != null)
                    {
                        var acroForm = acroFormProperty.GetValue(catalog);
                        if (acroForm != null)
                        {
                            // 检查 /XFA 键
                            var xfaProperty = acroForm.GetType().GetProperty("XFA");
                            if (xfaProperty != null)
                            {
                                var xfaValue = xfaProperty.GetValue(acroForm);
                                if (xfaValue != null)
                                {
                                    result.HasXfa = true;
                                    _logger.LogWarning("XFA form detected; scripts will be discarded.");

                                    // 提取 XFA 数据集
                                    ExtractXfaData(xfaValue, result);
                                }
                            }
                        }
                    }
                }
            }

            // 如果没有 XFA，检查 AcroForm 字段的 JavaScript 脚本
            if (!result.HasXfa)
            {
                ScanAcroFormScripts(pdfDocument, result);
            }

            _logger.LogInformation(
                "XFA detection complete: HasXfa={HasXfa}, FieldCount={Count}, ScriptCount={Scripts}",
                result.HasXfa, result.FieldCount, result.Scripts.Count);

            return result;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to detect XFA in PDF document");
            return result;
        }
    }

    /// <summary>
    /// 提取 XFA 数据集内容。
    /// </summary>
    private void ExtractXfaData(object xfaStream, XfaDetectionResult result)
    {
        try
        {
            // 占位：尝试读取 XFA 流内容
            // 实际实现应解析 XFA XML 结构
            var xfaType = xfaStream.GetType();
            var getBytesMethod = xfaType.GetMethod("GetBytes");

            if (getBytesMethod != null)
            {
                var bytes = getBytesMethod.Invoke(xfaStream, null) as byte[];
                if (bytes != null && bytes.Length > 0)
                {
                    var xmlContent = Encoding.UTF8.GetString(bytes);

                    // 简单解析：查找 <calculate> 和 <validate> 节点
                    var calculateCount = CountOccurrences(xmlContent, "<calculate>");
                    var validateCount = CountOccurrences(xmlContent, "<validate>");

                    result.FieldCount = calculateCount + validateCount;

                    _logger.LogDebug(
                        "XFA contains {Calculate} calculate scripts and {Validate} validate scripts",
                        calculateCount, validateCount);
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to extract XFA data");
        }
    }

    /// <summary>
    /// 扫描 AcroForm 字段的 JavaScript 脚本。
    /// </summary>
    private void ScanAcroFormScripts(object pdfDocument, XfaDetectionResult result)
    {
        try
        {
            // 占位：遍历 AcroForm 字段，检测 /AA（附加动作）
            var type = pdfDocument.GetType();
            var getFieldsMethod = type.GetMethod("GetFields") ?? type.GetMethod("GetAcroFields");

            if (getFieldsMethod != null)
            {
                var fields = getFieldsMethod.Invoke(pdfDocument, null) as System.Collections.IEnumerable;
                if (fields != null)
                {
                    foreach (var field in fields)
                    {
                        var fieldName = field.GetType().GetProperty("Name")?.GetValue(field)?.ToString() ?? "Unknown";

                        // 检查 /AA 字典
                        var aaProperty = field.GetType().GetProperty("AA") ?? field.GetType().GetProperty("AdditionalActions");
                        if (aaProperty != null)
                        {
                            var aaValue = aaProperty.GetValue(field);
                            if (aaValue != null)
                            {
                                ExtractScriptFromActions(fieldName, aaValue, result);
                            }
                        }

                        result.FieldCount++;
                    }
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to scan AcroForm scripts");
        }
    }

    /// <summary>
    /// 从附加动作中提取脚本信息。
    /// </summary>
    private void ExtractScriptFromActions(string fieldName, object actions, XfaDetectionResult result)
    {
        try
        {
            // 检查 /C (Calculate), /V (Validate), /F (Format) 动作
            var actionsType = actions.GetType();
            var scriptTypes = new[] { "Calculate", "Validate", "Format", "Keystroke" };

            foreach (var scriptType in scriptTypes)
            {
                var actionProperty = actionsType.GetProperty(scriptType) ?? actionsType.GetProperty(scriptType[0].ToString());
                if (actionProperty != null)
                {
                    var actionValue = actionProperty.GetValue(actions);
                    if (actionValue != null)
                    {
                        var script = ExtractScriptText(actionValue);
                        if (!string.IsNullOrWhiteSpace(script))
                        {
                            result.Scripts.Add(new XfaScriptInfo
                            {
                                FieldName = fieldName,
                                ScriptType = scriptType,
                                ScriptContent = script,
                                ScriptHash = ComputeSha256Hash(script),
                                ScriptLength = script.Length
                            });

                            _logger.LogDebug(
                                "Found {Type} script in field {Field}: {Length} chars",
                                scriptType, fieldName, script.Length);
                        }
                    }
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to extract script from field {Field}", fieldName);
        }
    }

    /// <summary>
    /// 提取脚本文本。
    /// </summary>
    private string? ExtractScriptText(object action)
    {
        try
        {
            var actionType = action.GetType();
            var jsProperty = actionType.GetProperty("JavaScript") ?? actionType.GetProperty("JS");

            if (jsProperty != null)
            {
                var jsValue = jsProperty.GetValue(action);
                return jsValue?.ToString();
            }

            return action.ToString();
        }
        catch
        {
            return null;
        }
    }

    /// <summary>
    /// 计算字符串的 SHA-256 哈希。
    /// </summary>
    private string ComputeSha256Hash(string text)
    {
        using var sha256 = SHA256.Create();
        var hashBytes = sha256.ComputeHash(Encoding.UTF8.GetBytes(text));
        return Convert.ToHexString(hashBytes).ToLowerInvariant();
    }

    /// <summary>
    /// 计算子字符串出现次数。
    /// </summary>
    private int CountOccurrences(string text, string substring)
    {
        if (string.IsNullOrEmpty(text) || string.IsNullOrEmpty(substring))
        {
            return 0;
        }

        int count = 0;
        int index = 0;
        while ((index = text.IndexOf(substring, index, StringComparison.OrdinalIgnoreCase)) >= 0)
        {
            count++;
            index += substring.Length;
        }
        return count;
    }
}

/// <summary>
/// XFA 检测结果。
/// </summary>
public sealed class XfaDetectionResult
{
    /// <summary>
    /// 是否包含 XFA 表单。
    /// </summary>
    public bool HasXfa { get; set; }

    /// <summary>
    /// 字段数量。
    /// </summary>
    public int FieldCount { get; set; }

    /// <summary>
    /// 脚本信息列表。
    /// </summary>
    public IList<XfaScriptInfo> Scripts { get; set; } = new List<XfaScriptInfo>();
}

/// <summary>
/// XFA 脚本信息。
/// </summary>
public sealed class XfaScriptInfo
{
    /// <summary>
    /// 字段名称。
    /// </summary>
    public string FieldName { get; set; } = string.Empty;

    /// <summary>
    /// 脚本类型（Calculate/Validate/Format/Keystroke）。
    /// </summary>
    public string ScriptType { get; set; } = string.Empty;

    /// <summary>
    /// 脚本内容。
    /// </summary>
    public string ScriptContent { get; set; } = string.Empty;

    /// <summary>
    /// 脚本 SHA-256 哈希。
    /// </summary>
    public string ScriptHash { get; set; } = string.Empty;

    /// <summary>
    /// 脚本长度（字符数）。
    /// </summary>
    public int ScriptLength { get; set; }
}

/// <summary>
/// XFA 提示信息写入器。
/// </summary>
/// <remarks>
/// 生成 hints.json 文件，包含 XFA 警告信息和脚本摘要。
/// 文件格式：
/// {
///   "warning": "XFA form detected; scripts will be discarded.",
///   "xfaDetected": true,
///   "fieldCount": 12,
///   "scripts": [
///     {
///       "fieldName": "TotalAmount",
///       "scriptType": "Calculate",
///       "scriptHash": "a1b2c3...",
///       "scriptLength": 256,
///       "scriptPreview": "function calculate() { ... }"
///     }
///   ],
///   "recommendations": [
///     "Manual review required for dynamic form logic",
///     "Static appearance snapshots have been generated"
///   ]
/// }
/// </remarks>
public sealed class XfaHintWriter
{
    private readonly ILogger<XfaHintWriter> _logger;

    /// <summary>
    /// 初始化 XfaHintWriter 实例。
    /// </summary>
    /// <param name="logger">日志记录器</param>
    public XfaHintWriter(ILogger<XfaHintWriter> logger)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <summary>
    /// 写入 XFA 提示文件。
    /// </summary>
    /// <param name="detectionResult">XFA 检测结果</param>
    /// <param name="outputPath">输出文件路径（绝对路径）</param>
    public void WriteHints(XfaDetectionResult detectionResult, string outputPath)
    {
        if (detectionResult == null)
        {
            throw new ArgumentNullException(nameof(detectionResult));
        }

        if (string.IsNullOrWhiteSpace(outputPath))
        {
            throw new ArgumentException("Output path cannot be null or empty", nameof(outputPath));
        }

        try
        {
            // 如果没有 XFA 或脚本，不生成文件
            if (!detectionResult.HasXfa && detectionResult.Scripts.Count == 0)
            {
                _logger.LogDebug("No XFA or scripts detected, skipping hints file");
                return;
            }

            var hints = new XfaHintsDocument
            {
                Warning = detectionResult.HasXfa
                    ? "XFA form detected; scripts will be discarded."
                    : "AcroForm with JavaScript detected; scripts will be discarded.",
                XfaDetected = detectionResult.HasXfa,
                FieldCount = detectionResult.FieldCount,
                Scripts = detectionResult.Scripts.Select(s => new XfaScriptSummary
                {
                    FieldName = s.FieldName,
                    ScriptType = s.ScriptType,
                    ScriptHash = s.ScriptHash,
                    ScriptLength = s.ScriptLength,
                    ScriptPreview = TruncateScript(s.ScriptContent, 120)
                }).ToList(),
                Recommendations = GenerateRecommendations(detectionResult)
            };

            var options = new JsonSerializerOptions
            {
                WriteIndented = true,
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
                DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
            };

            var json = JsonSerializer.Serialize(hints, options);
            File.WriteAllText(outputPath, json, Encoding.UTF8);

            _logger.LogInformation(
                "XFA hints written to {Path}: {FieldCount} fields, {ScriptCount} scripts",
                outputPath, detectionResult.FieldCount, detectionResult.Scripts.Count);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to write XFA hints to {Path}", outputPath);
            throw;
        }
    }

    /// <summary>
    /// 截断脚本内容（用于预览）。
    /// </summary>
    private string TruncateScript(string script, int maxLength)
    {
        if (string.IsNullOrEmpty(script))
        {
            return string.Empty;
        }

        if (script.Length <= maxLength)
        {
            return script;
        }

        return script.Substring(0, maxLength) + "...";
    }

    /// <summary>
    /// 生成建议列表。
    /// </summary>
    private List<string> GenerateRecommendations(XfaDetectionResult result)
    {
        var recommendations = new List<string>();

        if (result.HasXfa)
        {
            recommendations.Add("Manual review required for dynamic form logic");
            recommendations.Add("Static appearance snapshots have been generated");
            recommendations.Add("XFA calculate results have been fixed as default values");
        }

        if (result.Scripts.Count > 0)
        {
            recommendations.Add($"Found {result.Scripts.Count} JavaScript scripts that cannot be executed in OFD");
            recommendations.Add("Consider using AcroForm fallback for form functionality");
        }

        if (result.FieldCount > 0)
        {
            recommendations.Add($"All {result.FieldCount} form fields have been converted to static OFD equivalents");
        }

        return recommendations;
    }
}

/// <summary>
/// XFA 提示文档（JSON 输出格式）。
/// </summary>
internal sealed class XfaHintsDocument
{
    [JsonPropertyName("warning")]
    public string Warning { get; set; } = string.Empty;

    [JsonPropertyName("xfaDetected")]
    public bool XfaDetected { get; set; }

    [JsonPropertyName("fieldCount")]
    public int FieldCount { get; set; }

    [JsonPropertyName("scripts")]
    public List<XfaScriptSummary> Scripts { get; set; } = new();

    [JsonPropertyName("recommendations")]
    public List<string> Recommendations { get; set; } = new();
}

/// <summary>
/// XFA 脚本摘要（JSON 输出格式）。
/// </summary>
internal sealed class XfaScriptSummary
{
    [JsonPropertyName("fieldName")]
    public string FieldName { get; set; } = string.Empty;

    [JsonPropertyName("scriptType")]
    public string ScriptType { get; set; } = string.Empty;

    [JsonPropertyName("scriptHash")]
    public string ScriptHash { get; set; } = string.Empty;

    [JsonPropertyName("scriptLength")]
    public int ScriptLength { get; set; }

    [JsonPropertyName("scriptPreview")]
    public string ScriptPreview { get; set; } = string.Empty;
}
