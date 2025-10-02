namespace OfdrwNet.Abstractions.Forms;

/// <summary>
/// 表单字段映射器接口。
/// </summary>
/// <remarks>
/// 负责将 PDF AcroForm 字段映射到 OFD 表单字段结构。
/// 支持的字段类型：
/// - Text (文本框)
/// - CheckBox (复选框)
/// - RadioButton (单选框)
/// - ComboBox (下拉框)
/// - ListBox (列表框)
/// - Button (按钮)
/// - Signature (签名字段)
///
/// 映射策略：
/// - 保留字段名称、默认值、只读状态
/// - 转换字段边界框坐标
/// - 提取选项列表（下拉框/单选框）
/// - 检测 XFA 脚本并生成哈希
/// </remarks>
public interface IFormFieldMapper
{
    /// <summary>
    /// 从 PDF 对象映射表单字段。
    /// </summary>
    /// <param name="pdfField">PDF 字段对象</param>
    /// <returns>映射后的 OFD 表单字段，如果无法映射则返回 null</returns>
    /// <remarks>
    /// pdfField 应为 iText7 的 PdfFormField 或类似结构。
    /// 实现应处理所有常见字段类型。
    /// </remarks>
    object? MapField(object pdfField);

    /// <summary>
    /// 批量映射表单字段集合。
    /// </summary>
    /// <param name="pdfFields">PDF 字段集合</param>
    /// <returns>映射后的 OFD 字段列表</returns>
    System.Collections.Generic.IList<object> MapFields(System.Collections.Generic.IEnumerable<object> pdfFields);

    /// <summary>
    /// 检测字段是否包含 JavaScript 计算/验证脚本。
    /// </summary>
    /// <param name="pdfField">PDF 字段对象</param>
    /// <returns>如果包含脚本返回 true</returns>
    bool HasJavaScript(object pdfField);

    /// <summary>
    /// 提取字段的 JavaScript 脚本内容。
    /// </summary>
    /// <param name="pdfField">PDF 字段对象</param>
    /// <returns>脚本内容，如果不存在返回 null</returns>
    string? ExtractJavaScript(object pdfField);
}

