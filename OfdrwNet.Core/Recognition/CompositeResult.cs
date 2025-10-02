using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;

namespace OfdrwNet.Core.Recognition;

/// <summary>
/// 复合对象识别结果（表格 / 公式等）。
/// </summary>
public sealed class CompositeResult
{
    private readonly IReadOnlyList<TableCell> _cells;
    private readonly IReadOnlyList<BoundingBox> _boundingBoxes;

    /// <summary>
    /// 创建识别结果。
    /// </summary>
    /// <param name="type">识别类型。</param>
    /// <param name="confidence">置信度 [0,1]。</param>
    /// <param name="cells">识别出的表格单元格集合。</param>
    /// <param name="latex">LaTeX 表达式（公式识别）。</param>
    /// <param name="boundingBoxes">识别区域包围盒。</param>
    public CompositeResult(
        CompositeResultType type,
        double confidence,
        IEnumerable<TableCell>? cells = null,
        string? latex = null,
        IEnumerable<BoundingBox>? boundingBoxes = null)
    {
        if (confidence is < 0 or > 1)
        {
            throw new ArgumentOutOfRangeException(nameof(confidence), confidence, "Confidence must be within [0,1].");
        }

        Type = type;
        Confidence = confidence;
        LaTeX = latex;
        _cells = new ReadOnlyCollection<TableCell>((cells ?? Array.Empty<TableCell>()).ToList());
        _boundingBoxes = new ReadOnlyCollection<BoundingBox>((boundingBoxes ?? Array.Empty<BoundingBox>()).ToList());
    }

    /// <summary>
    /// 识别类型。
    /// </summary>
    public CompositeResultType Type { get; }

    /// <summary>
    /// 置信度。
    /// </summary>
    public double Confidence { get; }

    /// <summary>
    /// 表格单元格集合（若类型为表格）。
    /// </summary>
    public IReadOnlyList<TableCell> Cells => _cells;

    /// <summary>
    /// 公式 LaTeX 表达式（若类型为公式）。
    /// </summary>
    public string? LaTeX { get; }

    /// <summary>
    /// 包围盒集合。
    /// </summary>
    public IReadOnlyList<BoundingBox> BoundingBoxes => _boundingBoxes;

    /// <summary>
    /// 是否低于指定阈值，需要回退。
    /// </summary>
    public bool RequiresFallback(double threshold)
    {
        if (threshold is < 0 or > 1)
        {
            throw new ArgumentOutOfRangeException(nameof(threshold), threshold, "Threshold must be within [0,1].");
        }

        return Confidence < threshold;
    }

    /// <inheritdoc />
    public override string ToString()
    {
        return Type switch
        {
            CompositeResultType.Table => $"CompositeResult[Table, cells={_cells.Count}, conf={Confidence:F2}]",
            CompositeResultType.Formula => $"CompositeResult[Formula, conf={Confidence:F2}, latexLength={LaTeX?.Length ?? 0}]",
            _ => $"CompositeResult[{Type}, conf={Confidence:F2}]"
        };
    }
}

/// <summary>
/// 复合对象类型。
/// </summary>
public enum CompositeResultType
{
    /// <summary>
    /// 未知类型。
    /// </summary>
    Unknown = 0,

    /// <summary>
    /// 表格识别结果。
    /// </summary>
    Table,

    /// <summary>
    /// 公式识别结果。
    /// </summary>
    Formula
}

/// <summary>
/// 识别出的表格单元格。
/// </summary>
public sealed class TableCell
{
    /// <summary>
    /// 创建表格单元格描述。
    /// </summary>
    public TableCell(int rowIndex, int columnIndex, BoundingBox bounds, int rowSpan = 1, int columnSpan = 1, string? text = null)
    {
        if (rowIndex < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(rowIndex));
        }

        if (columnIndex < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(columnIndex));
        }

        if (rowSpan <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(rowSpan));
        }

        if (columnSpan <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(columnSpan));
        }

        RowIndex = rowIndex;
        ColumnIndex = columnIndex;
        Bounds = bounds;
        RowSpan = rowSpan;
        ColumnSpan = columnSpan;
        Text = text;
    }

    /// <summary>
    /// 单元格行索引。
    /// </summary>
    public int RowIndex { get; }

    /// <summary>
    /// 单元格列索引。
    /// </summary>
    public int ColumnIndex { get; }

    /// <summary>
    /// 单元格包围盒。
    /// </summary>
    public BoundingBox Bounds { get; }

    /// <summary>
    /// 行跨度。
    /// </summary>
    public int RowSpan { get; }

    /// <summary>
    /// 列跨度。
    /// </summary>
    public int ColumnSpan { get; }

    /// <summary>
    /// 识别出的文本内容。
    /// </summary>
    public string? Text { get; }
}

/// <summary>
/// 二维包围盒。
/// </summary>
public readonly struct BoundingBox
{
    /// <summary>
    /// 创建包围盒。
    /// </summary>
    public BoundingBox(double x, double y, double width, double height)
    {
        if (width < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(width));
        }

        if (height < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(height));
        }

        X = x;
        Y = y;
        Width = width;
        Height = height;
    }

    /// <summary>
    /// 左上角 X 坐标。
    /// </summary>
    public double X { get; }

    /// <summary>
    /// 左上角 Y 坐标。
    /// </summary>
    public double Y { get; }

    /// <summary>
    /// 宽度。
    /// </summary>
    public double Width { get; }

    /// <summary>
    /// 高度。
    /// </summary>
    public double Height { get; }
}
