using System.Collections.Generic;

namespace OfdrwNet.Converter.Domain;

/// <summary>
/// 复合对象识别结果（表格或公式）
/// </summary>
public sealed class CompositeResult
{
    /// <summary>
    /// 复合对象类型
    /// </summary>
    public required CompositeType Type { get; init; }

    /// <summary>
    /// 识别置信度 (0.0-1.0)
    /// </summary>
    public required float Confidence { get; init; }

    /// <summary>
    /// 表格单元格数据（仅当 Type=Table 时有效）
    /// </summary>
    public IList<IList<string>>? Cells { get; init; }

    /// <summary>
    /// LaTeX 公式（仅当 Type=Formula 时有效）
    /// </summary>
    public string? LaTeX { get; init; }

    /// <summary>
    /// 边界框集合（每行或每个符号的边界）
    /// </summary>
    public IList<BoundingBox>? BoundingBoxes { get; init; }

    /// <summary>
    /// 原始对象索引（在 PDF 中的对象编号）
    /// </summary>
    public IList<int>? SourceObjectIndices { get; init; }

    /// <summary>
    /// 验证置信度是否达到阈值
    /// </summary>
    public bool MeetsThreshold(float threshold)
    {
        return Confidence >= threshold;
    }
}

/// <summary>
/// 复合对象类型
/// </summary>
public enum CompositeType
{
    /// <summary>
    /// 表格
    /// </summary>
    Table,

    /// <summary>
    /// 公式
    /// </summary>
    Formula
}
