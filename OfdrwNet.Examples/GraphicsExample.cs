using OfdrwNet.Graphics;
using System.Drawing;

namespace OfdrwNet.Examples;

/// <summary>
/// 图形绘制示例
/// 展示如何使用OfdrwNet.Graphics模块进行OFD图形绘制
/// </summary>
public static class GraphicsExample
{
    /// <summary>
    /// 运行图形绘制示例
    /// </summary>
    /// <param name="outputPath">输出文件路径</param>
    public static async Task RunAsync(string outputPath)
    {
        Console.WriteLine("开始图形绘制示例...");

        using var document = new OFDGraphicsDocument(outputPath);

        // 创建第一页：基本图形绘制
        var page1 = document.NewPage(210, 297); // A4尺寸
        await DrawBasicShapes(page1);

        // 创建第二页：使用Canvas高级绘制
        var page2 = document.NewPage(210, 297);
        await DrawWithCanvas(page2);

        // 创建第三页：复杂图形和文本
        var page3 = document.NewPage(210, 297);
        await DrawComplexGraphics(page3);

        // 保存文档
        await document.SaveAsync();
        Console.WriteLine($"图形绘制示例完成！文件保存到: {outputPath}");
    }

    /// <summary>
    /// 绘制基本图形
    /// </summary>
    /// <param name="page">页面对象</param>
    private static Task DrawBasicShapes(OFDPageGraphics page)
    {
        Console.WriteLine("绘制基本图形...");

        // 设置绘制样式
        page.SetStrokeColor("#FF0000"); // 红色
        page.SetLineWidth(0.5);

        // 绘制矩形
        page.DrawRect(20, 20, 60, 40);

        // 绘制圆形
        page.SetStrokeColor("#00FF00"); // 绿色
        page.DrawCircle(140, 40, 20);

        // 绘制直线
        page.SetStrokeColor("#0000FF"); // 蓝色
        page.SetLineWidth(1.0);
        page.DrawLine(20, 80, 180, 80);
        page.DrawLine(20, 90, 180, 120);

        // 填充图形
        page.SetFillColor("#FFFF00"); // 黄色
        page.FillRect(20, 140, 50, 30);

        page.SetFillColor("#FF00FF"); // 紫色
        page.FillCircle(140, 155, 15);

        // 绘制文本
        page.SetFillColor("#000000"); // 黑色
        page.DrawText("基本图形绘制示例", 20, 200, 4.0);
        page.DrawText("矩形、圆形、直线和填充", 20, 210, 3.0);

        return Task.CompletedTask;
    }

    /// <summary>
    /// 使用Canvas进行高级绘制
    /// </summary>
    /// <param name="page">页面对象</param>
    private static Task DrawWithCanvas(OFDPageGraphics page)
    {
        Console.WriteLine("使用Canvas进行高级绘制...");

        using var canvas = new Canvas(page);

        // 创建各种绘制元素
        var line = canvas.DrawLine(new PointF(20, 20), new PointF(180, 50));
        line.StrokeColor = "#FF6B35";
        line.LineWidth = 2.0;

        var rect = canvas.DrawRectangle(new RectangleF(20, 60, 80, 50));
        rect.StrokeColor = "#004E89";
        rect.FillColor = "#A8E6CF";
        rect.Filled = true;
        rect.LineWidth = 1.5;

        var circle = canvas.DrawCircle(new PointF(150, 85), 25);
        circle.StrokeColor = "#C70039";
        circle.FillColor = "#FFD23F";
        circle.Filled = true;
        circle.LineWidth = 1.0;

        // 添加文本
        var text1 = canvas.DrawText("Canvas高级绘制", new PointF(20, 130), 4.5);
        text1.FillColor = "#2E8B57";

        var text2 = canvas.DrawText("支持样式设置和元素管理", new PointF(20, 140), 3.0);
        text2.FillColor = "#4682B4";

        // 绘制网格
        DrawGrid(canvas, 20, 160, 160, 80, 8, 4);

        // 渲染所有元素
        canvas.Render();

        return Task.CompletedTask;
    }

    /// <summary>
    /// 绘制复杂图形和文本
    /// </summary>
    /// <param name="page">页面对象</param>
    private static Task DrawComplexGraphics(OFDPageGraphics page)
    {
        Console.WriteLine("绘制复杂图形和文本...");

        using var canvas = new Canvas(page);

        // 绘制标题
        var title = canvas.DrawText("复杂图形绘制演示", new PointF(60, 20), 5.0);
        title.FillColor = "#2C3E50";

        // 绘制饼图
        DrawPieChart(canvas, new PointF(105, 80), 40, new[]
        {
            ("#FF6B6B", 30.0),  // 红色 30%
            ("#4ECDC4", 25.0),  // 青色 25%
            ("#45B7D1", 20.0),  // 蓝色 20%
            ("#FFA07A", 15.0),  // 橙色 15%
            ("#98D8C8", 10.0)   // 绿色 10%
        });

        // 绘制柱状图
        DrawBarChart(canvas, 20, 150, 160, 60, new[]
        {
            ("A", 30.0), ("B", 50.0), ("C", 25.0), ("D", 40.0), ("E", 35.0)
        });

        // 添加图例
        DrawLegend(canvas, 20, 220, new[]
        {
            ("#FF6B6B", "数据A (30%)"),
            ("#4ECDC4", "数据B (25%)"),
            ("#45B7D1", "数据C (20%)"),
            ("#FFA07A", "数据D (15%)"),
            ("#98D8C8", "数据E (10%)")
        });

        canvas.Render();
        return Task.CompletedTask;
    }

    /// <summary>
    /// 绘制网格
    /// </summary>
    /// <param name="canvas">画布</param>
    /// <param name="x">起始X坐标</param>
    /// <param name="y">起始Y坐标</param>
    /// <param name="width">网格宽度</param>
    /// <param name="height">网格高度</param>
    /// <param name="cols">列数</param>
    /// <param name="rows">行数</param>
    private static void DrawGrid(Canvas canvas, double x, double y, double width, double height, int cols, int rows)
    {
        var cellWidth = width / cols;
        var cellHeight = height / rows;

        // 绘制垂直线
        for (int i = 0; i <= cols; i++)
        {
            var line = canvas.DrawLine(
                new PointF((float)(x + i * cellWidth), (float)y),
                new PointF((float)(x + i * cellWidth), (float)(y + height))
            );
            line.StrokeColor = "#CCCCCC";
            line.LineWidth = 0.3;
        }

        // 绘制水平线
        for (int i = 0; i <= rows; i++)
        {
            var line = canvas.DrawLine(
                new PointF((float)x, (float)(y + i * cellHeight)),
                new PointF((float)(x + width), (float)(y + i * cellHeight))
            );
            line.StrokeColor = "#CCCCCC";
            line.LineWidth = 0.3;
        }
    }

    /// <summary>
    /// 绘制饼图
    /// </summary>
    /// <param name="canvas">画布</param>
    /// <param name="center">中心点</param>
    /// <param name="radius">半径</param>
    /// <param name="data">数据（颜色，百分比）</param>
    private static void DrawPieChart(Canvas canvas, PointF center, double radius, (string Color, double Percentage)[] data)
    {
        // 简化版饼图，使用圆形表示不同扇区
        var totalPercentage = data.Sum(d => d.Percentage);
        var angle = 0.0;

        foreach (var (color, percentage) in data)
        {
            // 计算扇区角度
            var sectorAngle = (percentage / totalPercentage) * 360.0;
            
            // 计算扇区中心位置
            var sectorCenter = new PointF(
                (float)(center.X + Math.Cos((angle + sectorAngle / 2) * Math.PI / 180) * radius * 0.3),
                (float)(center.Y + Math.Sin((angle + sectorAngle / 2) * Math.PI / 180) * radius * 0.3)
            );

            // 绘制扇区（使用圆形近似）
            var sector = canvas.DrawCircle(sectorCenter, radius * 0.8);
            sector.FillColor = color;
            sector.StrokeColor = "#FFFFFF";
            sector.Filled = true;
            sector.LineWidth = 1.0;

            angle += sectorAngle;
        }
    }

    /// <summary>
    /// 绘制柱状图
    /// </summary>
    /// <param name="canvas">画布</param>
    /// <param name="x">起始X坐标</param>
    /// <param name="y">起始Y坐标</param>
    /// <param name="width">图表宽度</param>
    /// <param name="height">图表高度</param>
    /// <param name="data">数据（标签，值）</param>
    private static void DrawBarChart(Canvas canvas, double x, double y, double width, double height, (string Label, double Value)[] data)
    {
        var maxValue = data.Max(d => d.Value);
        var barWidth = width / data.Length * 0.8;
        var spacing = width / data.Length * 0.2;

        for (int i = 0; i < data.Length; i++)
        {
            var (label, value) = data[i];
            var barHeight = (value / maxValue) * height;
            var barX = x + i * (barWidth + spacing) + spacing / 2;
            var barY = y + height - barHeight;

            // 绘制柱子
            var bar = canvas.DrawRectangle(new RectangleF((float)barX, (float)barY, (float)barWidth, (float)barHeight));
            bar.FillColor = GetBarColor(i);
            bar.StrokeColor = "#333333";
            bar.Filled = true;
            bar.LineWidth = 0.5;

            // 绘制标签
            var labelText = canvas.DrawText(label, new PointF((float)(barX + barWidth / 2 - 2), (float)(y + height + 5)), 2.5);
            labelText.FillColor = "#333333";

            // 绘制数值
            var valueText = canvas.DrawText(value.ToString(), new PointF((float)(barX + barWidth / 2 - 5), (float)(barY - 3)), 2.0);
            valueText.FillColor = "#666666";
        }
    }

    /// <summary>
    /// 绘制图例
    /// </summary>
    /// <param name="canvas">画布</param>
    /// <param name="x">起始X坐标</param>
    /// <param name="y">起始Y坐标</param>
    /// <param name="items">图例项（颜色，标签）</param>
    private static void DrawLegend(Canvas canvas, double x, double y, (string Color, string Label)[] items)
    {
        for (int i = 0; i < items.Length; i++)
        {
            var (color, label) = items[i];
            var itemY = y + i * 8;

            // 绘制颜色方块
            var colorBox = canvas.DrawRectangle(new RectangleF((float)x, (float)itemY, 4, 3));
            colorBox.FillColor = color;
            colorBox.Filled = true;

            // 绘制标签
            var labelText = canvas.DrawText(label, new PointF((float)(x + 6), (float)(itemY + 1)), 2.0);
            labelText.FillColor = "#333333";
        }
    }

    /// <summary>
    /// 获取柱状图颜色
    /// </summary>
    /// <param name="index">索引</param>
    /// <returns>颜色字符串</returns>
    private static string GetBarColor(int index)
    {
        var colors = new[]
        {
            "#3498DB", "#E74C3C", "#2ECC71", "#F39C12", "#9B59B6",
            "#1ABC9C", "#E67E22", "#34495E", "#E91E63", "#00BCD4"
        };
        return colors[index % colors.Length];
    }
}