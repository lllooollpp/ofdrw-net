using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using OfdrwNet.Converter.Domain;

namespace OfdrwNet.Converter.Recognition;

/// <summary>
/// 表格识别契约接口。
/// </summary>
/// <remarks>
/// 实现此接口以提供表格识别能力,支持:
/// - 从页面对象识别表格结构
/// - 提取单元格边界与内容
/// - 置信度评估与IOU计算
/// - 低置信度回退为静态绘制
///
/// 性能要求 (DR-1~DR-6):
/// - 召回率 ≥ 92% (检测到的真实表格占比)
/// - 精度 ≥ 90% (检测结果中真实表格占比)
/// - IOU ≥ 0.85 (边界框重叠度)
/// - 单页处理时间 < 500ms
/// </remarks>
public interface ITableRecognizer
{
    /// <summary>
    /// 识别页面中的表格。
    /// </summary>
    /// <param name="pageObjects">页面对象列表 (Text/Path/Image)</param>
    /// <param name="threshold">置信度阈值 (0.0-1.0),低于此值触发回退</param>
    /// <param name="ct">取消令牌</param>
    /// <returns>识别到的表格列表</returns>
    Task<List<TableRecognitionResult>> RecognizeTablesAsync(
        List<PageObject> pageObjects,
        float threshold = 0.8f,
        CancellationToken ct = default);

    /// <summary>
    /// 将低置信度表格转为静态绘制。
    /// </summary>
    /// <param name="table">表格识别结果</param>
    /// <returns>等价的Path对象列表 (保持视觉一致)</returns>
    List<PageObject> FallbackToStaticDrawing(TableRecognitionResult table);

    /// <summary>
    /// 计算两个边界框的交并比 (IOU)。
    /// </summary>
    double EstimateIou(BoundingBox a, BoundingBox b);

    /// <summary>
    /// 估算网格规整度 (辅助置信度计算)。
    /// </summary>
    double EstimateGridRegularity(List<TableCell> cells);
}

/// <summary>
/// 表格识别结果。
/// </summary>
public sealed class TableRecognitionResult
{
    /// <summary>
    /// 表格边界框。
    /// </summary>
    public required BoundingBox Bounds { get; init; }

    /// <summary>
    /// 行数。
    /// </summary>
    public required int RowCount { get; init; }

    /// <summary>
    /// 列数。
    /// </summary>
    public required int ColumnCount { get; init; }

    /// <summary>
    /// 单元格列表。
    /// </summary>
    public required List<TableCell> Cells { get; init; }

    /// <summary>
    /// 识别置信度 (0.0-1.0)。
    /// </summary>
    public required float Confidence { get; init; }

    /// <summary>
    /// 是否需要回退到静态绘制。
    /// </summary>
    public required bool IsFallback { get; init; }
}

/// <summary>
/// 表格单元格。
/// </summary>
public sealed class TableCell
{
    /// <summary>
    /// 行索引 (0-based)。
    /// </summary>
    public required int RowIndex { get; init; }

    /// <summary>
    /// 列索引 (0-based)。
    /// </summary>
    public required int ColumnIndex { get; init; }

    /// <summary>
    /// 单元格边界框。
    /// </summary>
    public required BoundingBox Bounds { get; init; }

    /// <summary>
    /// 单元格内容（文本）。
    /// </summary>
    public string Content { get; set; } = string.Empty;

    /// <summary>
    /// 是否为合并单元格。
    /// </summary>
    public bool IsMerged { get; init; }

    /// <summary>
    /// 跨列数 (仅当 IsMerged=true 时有效)。
    /// </summary>
    public int ColSpan { get; init; } = 1;

    /// <summary>
    /// 跨行数 (仅当 IsMerged=true 时有效)。
    /// </summary>
    public int RowSpan { get; init; } = 1;
}

/// <summary>
/// 页面对象基类。
/// </summary>
public abstract class PageObject
{
    /// <summary>
    /// 对象类型。
    /// </summary>
    public abstract PageObjectType Type { get; }

    /// <summary>
    /// 边界框。
    /// </summary>
    public required BoundingBox Bounds { get; set; }

    /// <summary>
    /// Z-order (绘制顺序)。
    /// </summary>
    public int ZOrder { get; set; }
}

/// <summary>
/// 页面对象类型。
/// </summary>
public enum PageObjectType
{
    /// <summary>
    /// 文本对象。
    /// </summary>
    Text = 0,

    /// <summary>
    /// 路径对象。
    /// </summary>
    Path = 1,

    /// <summary>
    /// 图像对象。
    /// </summary>
    Image = 2
}

/// <summary>
/// 文本对象。
/// </summary>
public sealed class TextObject : PageObject
{
    /// <inheritdoc/>
    public override PageObjectType Type => PageObjectType.Text;

    /// <summary>
    /// 文本内容。
    /// </summary>
    public string? Content { get; set; }

    /// <summary>
    /// 字体名称。
    /// </summary>
    public string? FontName { get; set; }

    /// <summary>
    /// 字号 (pt)。
    /// </summary>
    public double FontSize { get; set; }
}

/// <summary>
/// 路径对象。
/// </summary>
public sealed class PathObject : PageObject
{
    /// <inheritdoc/>
    public override PageObjectType Type => PageObjectType.Path;

    /// <summary>
    /// 路径数据 (SVG格式)。
    /// </summary>
    public string? PathData { get; set; }

    /// <summary>
    /// 是否为直线。
    /// </summary>
    public bool IsStraightLine { get; set; }

    /// <summary>
    /// 线方向 (若为直线)。
    /// </summary>
    public LineDirection? Direction { get; set; }
}

/// <summary>
/// 图像对象。
/// </summary>
public sealed class ImageObject : PageObject
{
    /// <inheritdoc/>
    public override PageObjectType Type => PageObjectType.Image;

    /// <summary>
    /// 图像资源ID。
    /// </summary>
    public string? ResourceId { get; set; }

    /// <summary>
    /// 图像宽度 (像素)。
    /// </summary>
    public int Width { get; set; }

    /// <summary>
    /// 图像高度 (像素)。
    /// </summary>
    public int Height { get; set; }
}

/// <summary>
/// 线方向枚举。
/// </summary>
public enum LineDirection
{
    /// <summary>
    /// 水平线。
    /// </summary>
    Horizontal = 0,

    /// <summary>
    /// 垂直线。
    /// </summary>
    Vertical = 1,

    /// <summary>
    /// 对角线。
    /// </summary>
    Diagonal = 2
}
