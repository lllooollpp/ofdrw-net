using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;

namespace OfdrwNet.Core.Forms;

/// <summary>
/// OFD 表单字段的基础描述。
/// </summary>
public sealed class FormField
{
    private readonly IReadOnlyList<FormFieldOption> _options;

    /// <summary>
    /// 初始化 <see cref="FormField"/> 的新实例。
    /// </summary>
    public FormField(
        string name,
        FormFieldType type,
        string? defaultValue = null,
        bool readOnly = false,
        int? maxLength = null,
        string? formatCategory = null,
        IEnumerable<FormFieldOption>? options = null,
        string? xfaScriptHash = null)
    {
        if (string.IsNullOrWhiteSpace(name))
        {
            throw new ArgumentException("Field name cannot be null or whitespace.", nameof(name));
        }

        if (maxLength is < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(maxLength), "Max length must be non-negative.");
        }

        Name = name.Trim();
        Type = type;
        DefaultValue = defaultValue;
        ReadOnly = readOnly;
        MaxLength = maxLength;
        FormatCategory = string.IsNullOrWhiteSpace(formatCategory) ? null : formatCategory.Trim();
        XfaScriptHash = string.IsNullOrWhiteSpace(xfaScriptHash) ? null : xfaScriptHash.Trim();
        _options = new ReadOnlyCollection<FormFieldOption>((options ?? Array.Empty<FormFieldOption>()).ToList());
    }

    /// <summary>
    /// 字段名称。
    /// </summary>
    public string Name { get; }

    /// <summary>
    /// 字段类型。
    /// </summary>
    public FormFieldType Type { get; }

    /// <summary>
    /// 默认值。
    /// </summary>
    public string? DefaultValue { get; }

    /// <summary>
    /// 是否只读。
    /// </summary>
    public bool ReadOnly { get; }

    /// <summary>
    /// 最大长度（仅对文本类字段生效）。
    /// </summary>
    public int? MaxLength { get; }

    /// <summary>
    /// 数值/日期等格式分类。
    /// </summary>
    public string? FormatCategory { get; }

    /// <summary>
    /// 选项集合（单选/多选/下拉列表）。
    /// </summary>
    public IReadOnlyList<FormFieldOption> Options => _options;

    /// <summary>
    /// 关联 XFA 脚本的 SHA256 哈希（若存在）。
    /// </summary>
    public string? XfaScriptHash { get; }

    /// <summary>
    /// 是否具有可选项。
    /// </summary>
    public bool HasOptions => _options.Count > 0;

    /// <summary>
    /// 判断指定值是否为有效选项。
    /// </summary>
    public bool IsValidOption(string value)
    {
        if (!HasOptions)
        {
            return true;
        }

        return _options.Any(opt => string.Equals(opt.Value, value, StringComparison.Ordinal));
    }

    /// <inheritdoc />
    public override string ToString()
    {
        return $"FormField[{Name}, {Type}, readOnly={ReadOnly}]";
    }
}

/// <summary>
/// 表单字段类型。
/// </summary>
public enum FormFieldType
{
    /// <summary>
    /// 文本。
    /// </summary>
    Text,

    /// <summary>
    /// 多行文本。
    /// </summary>
    MultilineText,

    /// <summary>
    /// 复选框。
    /// </summary>
    Checkbox,

    /// <summary>
    /// 单选按钮。
    /// </summary>
    Radio,

    /// <summary>
    /// 下拉列表。
    /// </summary>
    Combo,

    /// <summary>
    /// 列表框。
    /// </summary>
    List,

    /// <summary>
    /// 按钮。
    /// </summary>
    Button,

    /// <summary>
    /// 签名域。
    /// </summary>
    Signature,

    /// <summary>
    /// 日期。
    /// </summary>
    Date,

    /// <summary>
    /// 数值。
    /// </summary>
    Numeric
}

/// <summary>
/// 可选项描述。
/// </summary>
public sealed class FormFieldOption
{
    /// <summary>
    /// 初始化 <see cref="FormFieldOption"/> 实例。
    /// </summary>
    public FormFieldOption(string value, string? label = null)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            throw new ArgumentException("Option value cannot be null or whitespace.", nameof(value));
        }

        Value = value.Trim();
        Label = string.IsNullOrWhiteSpace(label) ? null : label.Trim();
    }

    /// <summary>
    /// 选项值。
    /// </summary>
    public string Value { get; }

    /// <summary>
    /// 选项显示文本。
    /// </summary>
    public string? Label { get; }

    /// <inheritdoc />
    public override string ToString()
    {
        return Label is null ? Value : $"{Label} ({Value})";
    }
}
