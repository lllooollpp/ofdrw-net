using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using OfdrwNet.Converter.Domain;

namespace OfdrwNet.Converter.Recognition;

/// <summary>
/// 基于规则的表格识别器实现,使用KD-tree聚类和IOU指标。
/// </summary>
/// <remarks>
/// 识别算法:
/// 1. 提取Path对象中的直线段(水平/垂直)
/// 2. 使用KD-Tree空间索引聚类相邻线段
/// 3. 检测网格结构(行列交叉点)
/// 4. 提取单元格边界框
/// 5. 将Text对象分配到对应单元格
/// 6. 计算置信度(网格规整度 × 单元格填充率)
///
/// 性能目标 (DR-1~DR-6):
/// - 召回率 ≥ 92%
/// - 精度 ≥ 90%
/// - IOU ≥ 0.85
/// - 处理时间 < 500ms/页
/// </remarks>
public sealed class RuleBasedTableRecognizer : ITableRecognizer
{
    private readonly ILogger<RuleBasedTableRecognizer> _logger;

    // 配置参数
    private const double LineTolerancePx = 3.0;  // 线段对齐容差
    private const double MinTableWidth = 30.0;    // 最小表格宽度
    private const double MinTableHeight = 30.0;   // 最小表格高度
    private const int MinRows = 2;                // 最小行数
    private const int MinColumns = 2;             // 最小列数
    private const double CellFillRateThreshold = 0.3;  // 单元格填充率阈值

    /// <summary>
    /// 创建表格识别器实例。
    /// </summary>
    public RuleBasedTableRecognizer(ILogger<RuleBasedTableRecognizer> logger)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <inheritdoc/>
    public async Task<List<TableRecognitionResult>> RecognizeTablesAsync(
        List<PageObject> pageObjects,
        float threshold = 0.8f,
        CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(pageObjects);

        if (threshold < 0.0f || threshold > 1.0f)
        {
            throw new ArgumentOutOfRangeException(nameof(threshold), "Threshold must be between 0.0 and 1.0");
        }

        _logger.LogInformation("Starting table recognition with threshold {Threshold} on {Count} objects",
            threshold, pageObjects.Count);

        var startTime = DateTime.UtcNow;

        // 步骤1: 提取直线段
        var lines = ExtractStraightLines(pageObjects);
        _logger.LogDebug("Extracted {Count} straight lines", lines.Count);

        // 步骤2: 分离水平线和垂直线
        var horizontalLines = lines.Where(l => l.Direction == LineDirection.Horizontal).ToList();
        var verticalLines = lines.Where(l => l.Direction == LineDirection.Vertical).ToList();

        _logger.LogDebug("H-lines: {HCount}, V-lines: {VCount}",
            horizontalLines.Count, verticalLines.Count);

        // 步骤3: 使用KD-Tree聚类相邻线段
        var hClusters = ClusterLines(horizontalLines, isHorizontal: true);
        var vClusters = ClusterLines(verticalLines, isHorizontal: false);

        _logger.LogDebug("Clustered into {HClusters} H-clusters, {VClusters} V-clusters",
            hClusters.Count, vClusters.Count);

        // 步骤4: 检测网格结构
        var tables = DetectTableGrids(hClusters, vClusters);
        _logger.LogDebug("Detected {Count} potential tables", tables.Count);

        // 步骤5: 提取单元格并分配文本内容
        var textObjects = pageObjects.OfType<TextObject>().ToList();
        var results = new List<TableRecognitionResult>();

        foreach (var table in tables)
        {
            ct.ThrowIfCancellationRequested();

            var cells = ExtractCells(table);
            AssignTextToCells(cells, textObjects);

            // 步骤6: 计算置信度
            var gridRegularity = EstimateGridRegularity(cells);
            var fillRate = CalculateCellFillRate(cells);
            var confidence = (float)(gridRegularity * 0.6 + fillRate * 0.4);

            var result = new TableRecognitionResult
            {
                Bounds = table.Bounds,
                RowCount = table.Rows,
                ColumnCount = table.Columns,
                Cells = cells,
                Confidence = confidence,
                IsFallback = confidence < threshold
            };

            results.Add(result);

            _logger.LogDebug(
                "Table at ({X},{Y}) {W}×{H}: {Rows}×{Cols}, confidence={Conf:F3}, fallback={Fallback}",
                table.Bounds.X, table.Bounds.Y, table.Bounds.Width, table.Bounds.Height,
                table.Rows, table.Columns, confidence, result.IsFallback);
        }

        var elapsed = DateTime.UtcNow - startTime;
        _logger.LogInformation(
            "Table recognition completed: {Count} tables found in {Time}ms",
            results.Count, elapsed.TotalMilliseconds);

        // 性能检查 (DR-6: < 500ms)
        if (elapsed.TotalMilliseconds > 500)
        {
            _logger.LogWarning(
                "Table recognition exceeded 500ms threshold: {Time}ms",
                elapsed.TotalMilliseconds);
        }

        return await Task.FromResult(results);
    }

    /// <inheritdoc/>
    public List<PageObject> FallbackToStaticDrawing(TableRecognitionResult table)
    {
        ArgumentNullException.ThrowIfNull(table);

        _logger.LogInformation(
            "Converting table to static drawing: {Rows}×{Cols} at ({X},{Y})",
            table.RowCount, table.ColumnCount, table.Bounds.X, table.Bounds.Y);

        var objects = new List<PageObject>();

        // 转换边框为Path对象
        foreach (var cell in table.Cells)
        {
            // 上边框
            objects.Add(new PathObject
            {
                Bounds = new BoundingBox
                {
                    X = cell.Bounds.X,
                    Y = cell.Bounds.Y,
                    Width = cell.Bounds.Width,
                    Height = 1
                },
                PathData = $"M {cell.Bounds.X},{cell.Bounds.Y} L {cell.Bounds.X + cell.Bounds.Width},{cell.Bounds.Y}",
                IsStraightLine = true,
                Direction = LineDirection.Horizontal
            });

            // 左边框
            objects.Add(new PathObject
            {
                Bounds = new BoundingBox
                {
                    X = cell.Bounds.X,
                    Y = cell.Bounds.Y,
                    Width = 1,
                    Height = cell.Bounds.Height
                },
                PathData = $"M {cell.Bounds.X},{cell.Bounds.Y} L {cell.Bounds.X},{cell.Bounds.Y + cell.Bounds.Height}",
                IsStraightLine = true,
                Direction = LineDirection.Vertical
            });

            // 单元格内容
            if (!string.IsNullOrWhiteSpace(cell.Content))
            {
                objects.Add(new TextObject
                {
                    Bounds = new BoundingBox
                    {
                        X = cell.Bounds.X + 2,
                        Y = cell.Bounds.Y + 2,
                        Width = cell.Bounds.Width - 4,
                        Height = cell.Bounds.Height - 4
                    },
                    Content = cell.Content,
                    FontName = "SimSun",
                    FontSize = 12
                });
            }
        }

        // 添加右边框和下边框 (闭合表格)
        var rightX = table.Bounds.X + table.Bounds.Width;
        var bottomY = table.Bounds.Y + table.Bounds.Height;

        objects.Add(new PathObject
        {
            Bounds = new BoundingBox
            {
                X = rightX,
                Y = table.Bounds.Y,
                Width = 1,
                Height = table.Bounds.Height
            },
            PathData = $"M {rightX},{table.Bounds.Y} L {rightX},{bottomY}",
            IsStraightLine = true,
            Direction = LineDirection.Vertical
        });

        objects.Add(new PathObject
        {
            Bounds = new BoundingBox
            {
                X = table.Bounds.X,
                Y = bottomY,
                Width = table.Bounds.Width,
                Height = 1
            },
            PathData = $"M {table.Bounds.X},{bottomY} L {rightX},{bottomY}",
            IsStraightLine = true,
            Direction = LineDirection.Horizontal
        });

        _logger.LogDebug("Generated {Count} static drawing objects", objects.Count);

        return objects;
    }

    /// <inheritdoc/>
    public double EstimateIou(BoundingBox a, BoundingBox b)
    {
        ArgumentNullException.ThrowIfNull(a);
        ArgumentNullException.ThrowIfNull(b);

        return a.ComputeIoU(b);
    }

    /// <inheritdoc/>
    public double EstimateGridRegularity(List<TableCell> cells)
    {
        if (cells == null || cells.Count < 2)
        {
            return 0.0;
        }

        // 计算行高一致性
        var rowGroups = cells.GroupBy(c => c.RowIndex).ToList();
        var rowHeights = rowGroups.Select(g => g.First().Bounds.Height).ToList();
        var rowHeightStdDev = ComputeStandardDeviation(rowHeights);
        var rowHeightMean = rowHeights.Average();
        var rowConsistency = rowHeightMean > 0
            ? 1.0 - Math.Min(1.0, rowHeightStdDev / rowHeightMean)
            : 0.0;

        // 计算列宽一致性
        var colGroups = cells.GroupBy(c => c.ColumnIndex).ToList();
        var colWidths = colGroups.Select(g => g.First().Bounds.Width).ToList();
        var colWidthStdDev = ComputeStandardDeviation(colWidths);
        var colWidthMean = colWidths.Average();
        var colConsistency = colWidthMean > 0
            ? 1.0 - Math.Min(1.0, colWidthStdDev / colWidthMean)
            : 0.0;

        // 计算对齐度 (检查相邻单元格边缘偏差)
        var alignmentScore = ComputeAlignmentScore(cells);

        // 综合规整度 = 行一致性×0.3 + 列一致性×0.3 + 对齐度×0.4
        var regularity = rowConsistency * 0.3 + colConsistency * 0.3 + alignmentScore * 0.4;

        _logger.LogDebug(
            "Grid regularity: row={Row:F3}, col={Col:F3}, align={Align:F3}, total={Total:F3}",
            rowConsistency, colConsistency, alignmentScore, regularity);

        return regularity;
    }

    #region Private Helper Methods

    /// <summary>
    /// 提取直线段（水平或垂直）。
    /// </summary>
    private List<LineSegment> ExtractStraightLines(List<PageObject> pageObjects)
    {
        var lines = new List<LineSegment>();

        foreach (var obj in pageObjects.OfType<PathObject>())
        {
            if (!obj.IsStraightLine || obj.Direction == null)
            {
                continue;
            }

            lines.Add(new LineSegment
            {
                Start = new Point(obj.Bounds.X, obj.Bounds.Y),
                End = obj.Direction == LineDirection.Horizontal
                    ? new Point(obj.Bounds.X + obj.Bounds.Width, obj.Bounds.Y)
                    : new Point(obj.Bounds.X, obj.Bounds.Y + obj.Bounds.Height),
                Direction = obj.Direction.Value,
                Bounds = obj.Bounds
            });
        }

        return lines;
    }

    /// <summary>
    /// 使用KD-Tree聚类相邻线段。
    /// </summary>
    /// <param name="lines">线段列表</param>
    /// <param name="isHorizontal">是否为水平线</param>
    /// <returns>聚类后的线段集合</returns>
    private List<List<LineSegment>> ClusterLines(List<LineSegment> lines, bool isHorizontal)
    {
        if (lines.Count == 0)
        {
            return new List<List<LineSegment>>();
        }

        // 简化KD-Tree实现: 按坐标排序后聚类
        // 水平线按Y坐标聚类, 垂直线按X坐标聚类
        var sorted = isHorizontal
            ? lines.OrderBy(l => l.Start.Y).ToList()
            : lines.OrderBy(l => l.Start.X).ToList();

        var clusters = new List<List<LineSegment>>();
        var currentCluster = new List<LineSegment> { sorted[0] };

        for (int i = 1; i < sorted.Count; i++)
        {
            var prev = sorted[i - 1];
            var curr = sorted[i];

            var distance = isHorizontal
                ? Math.Abs(curr.Start.Y - prev.Start.Y)
                : Math.Abs(curr.Start.X - prev.Start.X);

            if (distance <= LineTolerancePx)
            {
                currentCluster.Add(curr);
            }
            else
            {
                clusters.Add(currentCluster);
                currentCluster = new List<LineSegment> { curr };
            }
        }

        clusters.Add(currentCluster);
        return clusters;
    }

    /// <summary>
    /// 检测网格结构（行列交叉）。
    /// </summary>
    private List<TableGrid> DetectTableGrids(
        List<List<LineSegment>> hClusters,
        List<List<LineSegment>> vClusters)
    {
        var tables = new List<TableGrid>();

        // 对每个水平线簇和垂直线簇的组合，尝试构建表格
        foreach (var hCluster in hClusters)
        {
            foreach (var vCluster in vClusters)
            {
                // 找到重叠区域
                var hLines = hCluster.OrderBy(l => l.Start.Y).ToList();
                var vLines = vCluster.OrderBy(l => l.Start.X).ToList();

                if (hLines.Count < MinRows + 1 || vLines.Count < MinColumns + 1)
                {
                    continue;
                }

                var tableX = vLines.First().Start.X;
                var tableY = hLines.First().Start.Y;
                var tableWidth = vLines.Last().Start.X - tableX;
                var tableHeight = hLines.Last().Start.Y - tableY;

                if (tableWidth < MinTableWidth || tableHeight < MinTableHeight)
                {
                    continue;
                }

                tables.Add(new TableGrid
                {
                    Bounds = new BoundingBox
                    {
                        X = tableX,
                        Y = tableY,
                        Width = tableWidth,
                        Height = tableHeight
                    },
                    Rows = hLines.Count - 1,
                    Columns = vLines.Count - 1,
                    HorizontalLines = hLines,
                    VerticalLines = vLines
                });
            }
        }

        return tables;
    }

    /// <summary>
    /// 提取单元格边界框。
    /// </summary>
    private List<TableCell> ExtractCells(TableGrid table)
    {
        var cells = new List<TableCell>();

        for (int row = 0; row < table.Rows; row++)
        {
            for (int col = 0; col < table.Columns; col++)
            {
                var topY = table.HorizontalLines[row].Start.Y;
                var bottomY = table.HorizontalLines[row + 1].Start.Y;
                var leftX = table.VerticalLines[col].Start.X;
                var rightX = table.VerticalLines[col + 1].Start.X;

                cells.Add(new TableCell
                {
                    RowIndex = row,
                    ColumnIndex = col,
                    Bounds = new BoundingBox
                    {
                        X = leftX,
                        Y = topY,
                        Width = rightX - leftX,
                        Height = bottomY - topY
                    },
                    Content = string.Empty
                });
            }
        }

        return cells;
    }

    /// <summary>
    /// 将文本对象分配到对应单元格。
    /// </summary>
    private void AssignTextToCells(List<TableCell> cells, List<TextObject> textObjects)
    {
        foreach (var text in textObjects)
        {
            // 找到包含此文本的单元格 (基于中心点)
            var textCenterX = text.Bounds.X + text.Bounds.Width / 2;
            var textCenterY = text.Bounds.Y + text.Bounds.Height / 2;

            var containingCell = cells.FirstOrDefault(c =>
                textCenterX >= c.Bounds.X &&
                textCenterX <= c.Bounds.X + c.Bounds.Width &&
                textCenterY >= c.Bounds.Y &&
                textCenterY <= c.Bounds.Y + c.Bounds.Height);

            if (containingCell != null)
            {
                if (!string.IsNullOrWhiteSpace(containingCell.Content))
                {
                    containingCell.Content += " ";
                }
                containingCell.Content += text.Content;
            }
        }
    }

    /// <summary>
    /// 计算单元格填充率。
    /// </summary>
    private double CalculateCellFillRate(List<TableCell> cells)
    {
        if (cells.Count == 0)
        {
            return 0.0;
        }

        var filledCells = cells.Count(c => !string.IsNullOrWhiteSpace(c.Content));
        return (double)filledCells / cells.Count;
    }

    /// <summary>
    /// 计算标准差。
    /// </summary>
    private double ComputeStandardDeviation(List<double> values)
    {
        if (values.Count < 2)
        {
            return 0.0;
        }

        var mean = values.Average();
        var variance = values.Sum(v => Math.Pow(v - mean, 2)) / values.Count;
        return Math.Sqrt(variance);
    }

    /// <summary>
    /// 计算对齐度分数。
    /// </summary>
    private double ComputeAlignmentScore(List<TableCell> cells)
    {
        if (cells.Count < 2)
        {
            return 1.0;
        }

        var totalDeviations = 0.0;
        var comparisons = 0;

        // 检查同行相邻单元格的上下边缘对齐
        var rowGroups = cells.GroupBy(c => c.RowIndex).ToList();
        foreach (var row in rowGroups)
        {
            var rowCells = row.OrderBy(c => c.ColumnIndex).ToList();
            for (int i = 1; i < rowCells.Count; i++)
            {
                var deviation = Math.Abs(rowCells[i].Bounds.Y - rowCells[i - 1].Bounds.Y);
                totalDeviations += deviation;
                comparisons++;
            }
        }

        // 检查同列相邻单元格的左右边缘对齐
        var colGroups = cells.GroupBy(c => c.ColumnIndex).ToList();
        foreach (var col in colGroups)
        {
            var colCells = col.OrderBy(c => c.RowIndex).ToList();
            for (int i = 1; i < colCells.Count; i++)
            {
                var deviation = Math.Abs(colCells[i].Bounds.X - colCells[i - 1].Bounds.X);
                totalDeviations += deviation;
                comparisons++;
            }
        }

        if (comparisons == 0)
        {
            return 1.0;
        }

        var avgDeviation = totalDeviations / comparisons;
        // 偏差 < 2px 为完美对齐 (score=1.0), 偏差 > 10px 为严重不对齐 (score=0.0)
        return Math.Max(0.0, 1.0 - avgDeviation / 10.0);
    }

    #endregion

    #region Helper Classes

    /// <summary>
    /// 线段表示。
    /// </summary>
    private sealed class LineSegment
    {
        public required Point Start { get; init; }
        public required Point End { get; init; }
        public required LineDirection Direction { get; init; }
        public required BoundingBox Bounds { get; init; }
    }

    /// <summary>
    /// 点表示。
    /// </summary>
    private sealed class Point
    {
        public Point(double x, double y)
        {
            X = x;
            Y = y;
        }

        public double X { get; }
        public double Y { get; }
    }

    /// <summary>
    /// 表格网格表示。
    /// </summary>
    private sealed class TableGrid
    {
        public required BoundingBox Bounds { get; init; }
        public required int Rows { get; init; }
        public required int Columns { get; init; }
        public required List<LineSegment> HorizontalLines { get; init; }
        public required List<LineSegment> VerticalLines { get; init; }
    }

    #endregion
}
