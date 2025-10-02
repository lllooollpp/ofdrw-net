using Microsoft.Extensions.Logging;
using OfdrwNet.Abstractions.Forms;
using OfdrwNet.Converter.Domain;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Text;

namespace OfdrwNet.Converter.Forms;

/// <summary>
/// PDF 表单字段映射器实现。
/// </summary>
/// <remarks>
/// 将 PDF AcroForm 字段映射到 OFD 表单字段。
/// 当前为占位实现，生成 FormField 域对象，不处理实际 PDF 结构。
/// </remarks>
public sealed class PdfFormMapper : IFormFieldMapper
{
    private readonly ILogger<PdfFormMapper> _logger;

    /// <summary>
    /// 初始化 PdfFormMapper 实例。
    /// </summary>
    /// <param name="logger">日志记录器</param>
    public PdfFormMapper(ILogger<PdfFormMapper> logger)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <summary>
    /// 从 PDF 对象映射表单字段。
    /// </summary>
    /// <param name="pdfField">PDF 字段对象（iText7 PdfFormField 或模拟对象）</param>
    /// <returns>映射后的 FormField 或 null</returns>
    public object? MapField(object pdfField)
    {
        if (pdfField == null)
        {
            _logger.LogWarning("PDF field is null, skipping");
            return null;
        }

        try
        {
            // 当前为占位实现：尝试从动态对象提取属性
            // 实际实现应使用 iText7 API 直接读取 PdfFormField
            var fieldType = GetFieldType(pdfField);
            var fieldName = GetFieldName(pdfField);

            if (string.IsNullOrEmpty(fieldName))
            {
                _logger.LogWarning("Field has no name, skipping");
                return null;
            }

            var formField = new FormField
            {
                Name = fieldName,
                Type = fieldType,
                DefaultValue = GetDefaultValue(pdfField),
                ReadOnly = IsReadOnly(pdfField),
                MaxLength = GetMaxLength(pdfField),
                FormatCategory = GetFormatCategory(pdfField),
                Options = GetOptions(pdfField),
                XfaScriptHash = GenerateXfaScriptHash(pdfField),
                BoundingBox = GetBoundingBox(pdfField),
                Required = IsRequired(pdfField)
            };

            _logger.LogInformation(
                "Mapped PDF field: {Name} ({Type}) XfaScriptHash={Hash}",
                fieldName, fieldType, formField.XfaScriptHash ?? "(none)");

            return formField;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to map PDF field");
            return null;
        }
    }

    /// <summary>
    /// 批量映射表单字段。
    /// </summary>
    public IList<object> MapFields(IEnumerable<object> pdfFields)
    {
        var results = new List<object>();

        foreach (var pdfField in pdfFields)
        {
            var mapped = MapField(pdfField);
            if (mapped != null)
            {
                results.Add(mapped);
            }
        }

        _logger.LogInformation("Mapped {Count} PDF fields", results.Count);
        return results;
    }

    /// <summary>
    /// 检测字段是否包含 JavaScript。
    /// </summary>
    public bool HasJavaScript(object pdfField)
    {
        // 检测 /AA（附加动作）字典中的 /Calc、/Validate、/Format 动作
        var script = ExtractJavaScript(pdfField);
        return !string.IsNullOrWhiteSpace(script);
    }

    /// <summary>
    /// 提取字段的 JavaScript 脚本内容。
    /// </summary>
    public string? ExtractJavaScript(object pdfField)
    {
        if (pdfField == null)
        {
            return null;
        }

        try
        {
            // 占位实现：检查对象是否有 AA 或 JavaScript 属性
            var type = pdfField.GetType();
            var aaProperty = type.GetProperty("AA") ?? type.GetProperty("AdditionalActions");
            var jsProperty = type.GetProperty("JavaScript") ?? type.GetProperty("JS");

            if (aaProperty != null)
            {
                var aaValue = aaProperty.GetValue(pdfField);
                if (aaValue != null)
                {
                    return aaValue.ToString();
                }
            }

            if (jsProperty != null)
            {
                var jsValue = jsProperty.GetValue(pdfField);
                if (jsValue != null)
                {
                    return jsValue.ToString();
                }
            }

            _logger.LogDebug("No JavaScript found in field");
            return null;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to extract JavaScript from field");
            return null;
        }
    }

    // -------------------- 私有辅助方法 --------------------

    /// <summary>
    /// 获取字段类型（Text/CheckBox/RadioButton 等）。
    /// </summary>
    private FormFieldType GetFieldType(object pdfField)
    {
        // 占位逻辑：尝试读取 FieldType 或 Type 属性
        var type = pdfField.GetType();
        var typeProperty = type.GetProperty("FieldType") ?? type.GetProperty("Type");

        if (typeProperty != null)
        {
            var typeValue = typeProperty.GetValue(pdfField)?.ToString()?.ToLowerInvariant();

            return typeValue switch
            {
                "text" or "tx" => FormFieldType.Text,
                "checkbox" or "btn" => FormFieldType.CheckBox, // PDF Btn 字段可能是 CheckBox 或 RadioButton
                "radiobutton" or "radio" => FormFieldType.RadioButton,
                "combobox" or "ch" => FormFieldType.ComboBox,
                "listbox" or "list" => FormFieldType.ListBox,
                "button" or "pushbutton" => FormFieldType.Button,
                "signature" or "sig" => FormFieldType.Signature,
                _ => FormFieldType.Text
            };
        }

        _logger.LogWarning("Cannot determine field type, defaulting to Text");
        return FormFieldType.Text;
    }

    /// <summary>
    /// 获取字段名称。
    /// </summary>
    private string? GetFieldName(object pdfField)
    {
        var type = pdfField.GetType();
        var nameProperty = type.GetProperty("Name") ?? type.GetProperty("FieldName");

        return nameProperty?.GetValue(pdfField)?.ToString();
    }

    /// <summary>
    /// 获取默认值。
    /// </summary>
    private string? GetDefaultValue(object pdfField)
    {
        var type = pdfField.GetType();
        var valueProperty = type.GetProperty("DefaultValue") ?? type.GetProperty("Value");

        return valueProperty?.GetValue(pdfField)?.ToString();
    }

    /// <summary>
    /// 是否只读。
    /// </summary>
    private bool IsReadOnly(object pdfField)
    {
        var type = pdfField.GetType();
        var readOnlyProperty = type.GetProperty("ReadOnly") ?? type.GetProperty("IsReadOnly");

        if (readOnlyProperty != null && readOnlyProperty.PropertyType == typeof(bool))
        {
            return (bool)(readOnlyProperty.GetValue(pdfField) ?? false);
        }

        return false;
    }

    /// <summary>
    /// 获取最大长度（文本字段）。
    /// </summary>
    private int? GetMaxLength(object pdfField)
    {
        var type = pdfField.GetType();
        var maxLenProperty = type.GetProperty("MaxLength") ?? type.GetProperty("MaxLen");

        if (maxLenProperty != null && maxLenProperty.PropertyType == typeof(int))
        {
            var value = (int)(maxLenProperty.GetValue(pdfField) ?? 0);
            return value > 0 ? value : null;
        }

        return null;
    }

    /// <summary>
    /// 获取格式类别（date/phone/email 等）。
    /// </summary>
    private string? GetFormatCategory(object pdfField)
    {
        var type = pdfField.GetType();
        var formatProperty = type.GetProperty("Format") ?? type.GetProperty("FormatCategory");

        return formatProperty?.GetValue(pdfField)?.ToString();
    }

    /// <summary>
    /// 获取选项列表（下拉框/单选框）。
    /// </summary>
    private IList<string>? GetOptions(object pdfField)
    {
        var type = pdfField.GetType();
        var optionsProperty = type.GetProperty("Options") ?? type.GetProperty("Choices");

        if (optionsProperty != null)
        {
            var optionsValue = optionsProperty.GetValue(pdfField);
            if (optionsValue is IEnumerable<string> stringList)
            {
                return stringList.ToList();
            }
        }

        return null;
    }

    /// <summary>
    /// 生成 XFA 脚本哈希（SHA-256）。
    /// </summary>
    private string? GenerateXfaScriptHash(object pdfField)
    {
        var script = ExtractJavaScript(pdfField);
        if (string.IsNullOrWhiteSpace(script))
        {
            return null;
        }

        try
        {
            using var sha256 = SHA256.Create();
            var hashBytes = sha256.ComputeHash(Encoding.UTF8.GetBytes(script));
            return Convert.ToHexString(hashBytes).ToLowerInvariant();
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to generate XFA script hash");
            return null;
        }
    }

    /// <summary>
    /// 获取字段边界框。
    /// </summary>
    private BoundingBox? GetBoundingBox(object pdfField)
    {
        var type = pdfField.GetType();
        var rectProperty = type.GetProperty("Rect") ?? type.GetProperty("BoundingBox");

        if (rectProperty != null)
        {
            var rectValue = rectProperty.GetValue(pdfField);
            if (rectValue != null)
            {
                // 占位：尝试解析边界框
                // 实际实现应读取 PDF Rectangle [x1 y1 x2 y2]
                _logger.LogDebug("BoundingBox extraction not implemented, returning null");
            }
        }

        return null;
    }

    /// <summary>
    /// 是否必填。
    /// </summary>
    private bool IsRequired(object pdfField)
    {
        var type = pdfField.GetType();
        var requiredProperty = type.GetProperty("Required") ?? type.GetProperty("IsRequired");

        if (requiredProperty != null && requiredProperty.PropertyType == typeof(bool))
        {
            return (bool)(requiredProperty.GetValue(pdfField) ?? false);
        }

        return false;
    }
}
