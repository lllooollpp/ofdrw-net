using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.Extensions.Logging;
using OfdrwNet.Converter.Domain;

namespace OfdrwNet.Converter.Recognition;

/// <summary>
/// 默认复合对象回退策略实现。
/// </summary>
/// <remarks>
/// 集成表格识别器和公式识别器的回退机制:
/// - 表格: 调用 ITableRecognizer.FallbackToStaticDrawing 转换为路径对象
/// - 公式: 提取 LaTeX 或 FallbackText 字段作为纯文本
///
/// 回退决策:
/// - 检查 CompositeResult.Confidence 是否低于阈值
/// - 表格阈值: ConverterOptions.TableThreshold (默认 0.8)
/// - 公式阈值: ConverterOptions.FormulaThreshold (默认 0.8)
/// </remarks>
public sealed class CompositeFallbackPolicy : ICompositeFallbackPolicy
{
    private readonly ITableRecognizer? _tableRecognizer;
    private readonly ILogger<CompositeFallbackPolicy> _logger;

    /// <summary>
    /// 初始化 CompositeFallbackPolicy 实例。
    /// </summary>
    /// <param name="tableRecognizer">表格识别器(用于表格回退)</param>
    /// <param name="logger">日志记录器</param>
    public CompositeFallbackPolicy(
        ITableRecognizer? tableRecognizer,
        ILogger<CompositeFallbackPolicy> logger)
    {
        _tableRecognizer = tableRecognizer;
        _logger = logger;
    }

    /// <inheritdoc/>
    public List<PageObject> ApplyFallback(CompositeResult composite, ConverterOptions options)
    {
        if (composite == null)
        {
            throw new ArgumentNullException(nameof(composite));
        }

        if (options == null)
        {
            throw new ArgumentNullException(nameof(options));
        }

        var startTime = DateTime.UtcNow;

        try
        {
            var result = composite.Type switch
            {
                CompositeType.Table => ApplyTableFallback(composite),
                CompositeType.Formula => ApplyFormulaFallback(composite),
                _ => new List<PageObject>()
            };

            var elapsed = (DateTime.UtcNow - startTime).TotalMilliseconds;
            _logger.LogDebug(
                "Fallback applied for {Type} composite (Confidence={Confidence:F2}) in {Elapsed:F1}ms, produced {Count} objects",
                composite.Type, composite.Confidence, elapsed, result.Count);

            if (elapsed > 50)
            {
                _logger.LogWarning(
                    "Fallback conversion exceeded 50ms threshold: {Type} took {Elapsed:F1}ms",
                    composite.Type, elapsed);
            }

            return result;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Fallback failed for {Type} composite", composite.Type);
            return new List<PageObject>();
        }
    }

    /// <inheritdoc/>
    public bool ShouldFallback(CompositeResult composite, ConverterOptions options)
    {
        if (composite == null || options == null)
        {
            return false;
        }

        var threshold = composite.Type switch
        {
            CompositeType.Table => options.TableThreshold,
            CompositeType.Formula => options.FormulaThreshold,
            _ => 1.0f
        };

        return composite.Confidence < threshold;
    }

    /// <inheritdoc/>
    public string ExtractText(CompositeResult composite)
    {
        if (composite == null)
        {
            return string.Empty;
        }

        return composite.Type switch
        {
            CompositeType.Table => ExtractTableText(composite),
            CompositeType.Formula => ExtractFormulaText(composite),
            _ => string.Empty
        };
    }

    /// <summary>
    /// 应用表格回退策略。
    /// </summary>
    /// <param name="composite">表格复合对象</param>
    /// <returns>静态绘制的路径对象列表</returns>
    private List<PageObject> ApplyTableFallback(CompositeResult composite)
    {
        if (_tableRecognizer == null)
        {
            _logger.LogWarning("TableRecognizer not available, cannot apply table fallback");
            return CreatePlaceholderPathObject(composite);
        }

        if (composite.BoundingBoxes == null || !composite.BoundingBoxes.Any())
        {
            _logger.LogWarning("Table composite has no bounding boxes, using placeholder");
            return CreatePlaceholderPathObject(composite);
        }

        try
        {
            // 构造表格识别结果以调用回退方法
            var tableResult = new TableRecognitionResult
            {
                Bounds = composite.BoundingBoxes.First(),
                RowCount = composite.Cells?.Count ?? 0,
                ColumnCount = composite.Cells?.FirstOrDefault()?.Count ?? 0,
                Cells = BuildTableCells(composite),
                Confidence = composite.Confidence,
                IsFallback = true
            };

            var pathObjects = _tableRecognizer.FallbackToStaticDrawing(tableResult);
            _logger.LogInformation(
                "Table fallback converted to {Count} path objects (Confidence={Confidence:F2})",
                pathObjects.Count, composite.Confidence);

            return pathObjects;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Table fallback conversion failed, using placeholder");
            return CreatePlaceholderPathObject(composite);
        }
    }

    /// <summary>
    /// 应用公式回退策略。
    /// </summary>
    /// <param name="composite">公式复合对象</param>
    /// <returns>纯文本对象列表</returns>
    private List<PageObject> ApplyFormulaFallback(CompositeResult composite)
    {
        var text = ExtractFormulaText(composite);

        if (string.IsNullOrWhiteSpace(text))
        {
            _logger.LogWarning("Formula composite has no extractable text");
            return new List<PageObject>();
        }

        var bounds = composite.BoundingBoxes?.FirstOrDefault() ?? new BoundingBox
        {
            X = 0,
            Y = 0,
            Width = 100,
            Height = 20
        };

        var textObject = new TextObject
        {
            Bounds = bounds,
            Content = text,
            FontName = "Arial",
            FontSize = 12.0,
            ZOrder = 0
        };

        _logger.LogInformation(
            "Formula fallback converted to text: \"{Text}\" (Confidence={Confidence:F2})",
            text.Length > 50 ? text.Substring(0, 47) + "..." : text,
            composite.Confidence);

        return new List<PageObject> { textObject };
    }

    /// <summary>
    /// 提取表格文本内容。
    /// </summary>
    /// <param name="composite">表格复合对象</param>
    /// <returns>CSV格式的文本内容</returns>
    private string ExtractTableText(CompositeResult composite)
    {
        if (composite.Cells == null || !composite.Cells.Any())
        {
            return string.Empty;
        }

        var sb = new StringBuilder();
        foreach (var row in composite.Cells)
        {
            if (row != null && row.Any())
            {
                sb.AppendLine(string.Join(", ", row));
            }
        }

        return sb.ToString();
    }

    /// <summary>
    /// 提取公式文本内容。
    /// </summary>
    /// <param name="composite">公式复合对象</param>
    /// <returns>纯文本内容(移除LaTeX标记)</returns>
    private string ExtractFormulaText(CompositeResult composite)
    {
        // 优先使用 LaTeX 字段
        if (!string.IsNullOrWhiteSpace(composite.LaTeX))
        {
            return StripLatexMarkup(composite.LaTeX);
        }

        return string.Empty;
    }

    /// <summary>
    /// 移除 LaTeX 标记,保留文本内容。
    /// </summary>
    /// <param name="latex">LaTeX 字符串</param>
    /// <returns>纯文本</returns>
    private string StripLatexMarkup(string latex)
    {
        if (string.IsNullOrWhiteSpace(latex))
        {
            return string.Empty;
        }

        // 移除常见 LaTeX 标记
        var result = latex
            .Replace("$", "")                          // 移除数学模式标记
            .Replace("\\frac{", "(")                   // 分数转括号
            .Replace("}{", " / ")
            .Replace("^{", "^(")                       // 上标转括号
            .Replace("_{", "_(")                       // 下标转括号
            .Replace("\\int", "∫")                     // 积分符号
            .Replace("\\sum", "∑")                     // 求和符号
            .Replace("\\prod", "∏")                    // 乘积符号
            .Replace("\\alpha", "α")                   // 希腊字母
            .Replace("\\beta", "β")
            .Replace("\\gamma", "γ")
            .Replace("\\delta", "δ")
            .Replace("\\pi", "π")
            .Replace("\\theta", "θ")
            .Replace("\\lambda", "λ")
            .Replace("\\mu", "μ")
            .Replace("\\sigma", "σ")
            .Replace("\\Sigma", "Σ")
            .Replace("\\infty", "∞")                   // 无穷符号
            .Replace("\\leq", "≤")                     // 比较符号
            .Replace("\\geq", "≥")
            .Replace("\\neq", "≠")
            .Replace("\\approx", "≈")
            .Replace("\\pm", "±")                      // 正负号
            .Replace("\\times", "×")                   // 乘号
            .Replace("\\div", "÷")                     // 除号
            .Replace("}", ")")                         // 剩余右花括号转括号
            .Replace("{", "(")                         // 剩余左花括号转括号
            .Replace("\\", "");                        // 移除剩余反斜杠

        return result.Trim();
    }

    /// <summary>
    /// 构造表格单元格列表。
    /// </summary>
    /// <param name="composite">表格复合对象</param>
    /// <returns>单元格列表</returns>
    private List<TableCell> BuildTableCells(CompositeResult composite)
    {
        var cells = new List<TableCell>();

        if (composite.Cells == null || composite.BoundingBoxes == null)
        {
            return cells;
        }

        var rowCount = composite.Cells.Count;
        var colCount = composite.Cells.FirstOrDefault()?.Count ?? 0;

        var cellWidth = composite.BoundingBoxes.First().Width / Math.Max(1, colCount);
        var cellHeight = composite.BoundingBoxes.First().Height / Math.Max(1, rowCount);

        for (int row = 0; row < rowCount; row++)
        {
            var rowData = composite.Cells.ElementAtOrDefault(row);
            if (rowData == null) continue;

            for (int col = 0; col < colCount; col++)
            {
                var content = rowData.ElementAtOrDefault(col) ?? string.Empty;

                cells.Add(new TableCell
                {
                    RowIndex = row,
                    ColumnIndex = col,
                    Bounds = new BoundingBox
                    {
                        X = composite.BoundingBoxes.First().X + col * cellWidth,
                        Y = composite.BoundingBoxes.First().Y + row * cellHeight,
                        Width = cellWidth,
                        Height = cellHeight
                    },
                    Content = content,
                    IsMerged = false,
                    ColSpan = 1,
                    RowSpan = 1
                });
            }
        }

        return cells;
    }

    /// <summary>
    /// 创建占位路径对象(当回退失败时使用)。
    /// </summary>
    /// <param name="composite">复合对象</param>
    /// <returns>占位路径对象列表</returns>
    private List<PageObject> CreatePlaceholderPathObject(CompositeResult composite)
    {
        if (composite.BoundingBoxes == null || !composite.BoundingBoxes.Any())
        {
            return new List<PageObject>();
        }

        var bounds = composite.BoundingBoxes.First();

        // 创建一个矩形占位符
        var pathObject = new PathObject
        {
            Bounds = bounds,
            PathData = $"M {bounds.X},{bounds.Y} L {bounds.X + bounds.Width},{bounds.Y} " +
                      $"L {bounds.X + bounds.Width},{bounds.Y + bounds.Height} " +
                      $"L {bounds.X},{bounds.Y + bounds.Height} Z",
            IsStraightLine = false,
            Direction = null,
            ZOrder = 0
        };

        return new List<PageObject> { pathObject };
    }
}

