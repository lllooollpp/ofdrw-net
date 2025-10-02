using System.Collections.Generic;

namespace OfdrwNet.Converter.Domain;

/// <summary>
/// 表单字段描述
/// </summary>
public sealed class FormField
{
    /// <summary>
    /// 字段名称
    /// </summary>
    public required string Name { get; init; }

    /// <summary>
    /// 字段类型
    /// </summary>
    public required FormFieldType Type { get; init; }

    /// <summary>
    /// 默认值
    /// </summary>
    public string? DefaultValue { get; init; }

    /// <summary>
    /// 是否只读
    /// </summary>
    public bool ReadOnly { get; init; }

    /// <summary>
    /// 最大长度（用于文本字段）
    /// </summary>
    public int? MaxLength { get; init; }

    /// <summary>
    /// 格式类别（日期、电话、邮箱等）
    /// </summary>
    public string? FormatCategory { get; init; }

    /// <summary>
    /// 选项列表（用于下拉框、单选框等）
    /// </summary>
    public IList<string>? Options { get; init; }

    /// <summary>
    /// XFA 脚本哈希（如果包含脚本）
    /// </summary>
    public string? XfaScriptHash { get; init; }

    /// <summary>
    /// 字段边界框
    /// </summary>
    public BoundingBox? BoundingBox { get; init; }

    /// <summary>
    /// 是否必填
    /// </summary>
    public bool Required { get; init; }
}

/// <summary>
/// 表单字段类型
/// </summary>
public enum FormFieldType
{
    /// <summary>
    /// 文本框
    /// </summary>
    Text,

    /// <summary>
    /// 复选框
    /// </summary>
    CheckBox,

    /// <summary>
    /// 单选框
    /// </summary>
    RadioButton,

    /// <summary>
    /// 下拉框
    /// </summary>
    ComboBox,

    /// <summary>
    /// 列表框
    /// </summary>
    ListBox,

    /// <summary>
    /// 按钮
    /// </summary>
    Button,

    /// <summary>
    /// 签名字段
    /// </summary>
    Signature
}
