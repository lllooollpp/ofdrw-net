using System.Collections.Generic;
using OfdrwNet.Converter.Domain;

namespace OfdrwNet.Converter.Recognition;

/// <summary>
/// 复合对象回退策略接口。
/// </summary>
/// <remarks>
/// 负责将低置信度的复合对象(表格/公式)转换为更简单的表示形式,
/// 以确保在识别失败时仍能保持视觉一致性。
///
/// 回退策略:
/// - 表格: 转换为静态绘制的路径对象(保持网格线和单元格边界)
/// - 公式: 提取纯文本内容(移除LaTeX标记)
///
/// 性能要求:
/// - 回退转换应快速完成(< 50ms per object)
/// - 保证视觉一致性(回退后与原始内容视觉相同)
/// - 不应丢失任何文本内容
/// </remarks>
public interface ICompositeFallbackPolicy
{
    /// <summary>
    /// 应用回退策略到低置信度的复合对象。
    /// </summary>
    /// <param name="composite">复合对象结果</param>
    /// <param name="options">转换选项</param>
    /// <returns>回退后的页面对象列表</returns>
    /// <remarks>
    /// 对于表格: 返回表示网格线和单元格边界的PathObject列表
    /// 对于公式: 返回包含纯文本的TextObject列表
    /// </remarks>
    List<PageObject> ApplyFallback(CompositeResult composite, ConverterOptions options);

    /// <summary>
    /// 判断复合对象是否需要回退。
    /// </summary>
    /// <param name="composite">复合对象结果</param>
    /// <param name="options">转换选项</param>
    /// <returns>如果置信度低于阈值则返回true</returns>
    bool ShouldFallback(CompositeResult composite, ConverterOptions options);

    /// <summary>
    /// 提取复合对象的文本内容(用于公式回退)。
    /// </summary>
    /// <param name="composite">复合对象结果</param>
    /// <returns>提取的文本内容</returns>
    string ExtractText(CompositeResult composite);
}

