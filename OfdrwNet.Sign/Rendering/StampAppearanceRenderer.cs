using Microsoft.Extensions.Logging;
using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using System.IO;

namespace OfdrwNet.Sign.Rendering;

/// <summary>
/// 签章外观渲染器。
/// </summary>
/// <remarks>
/// 生成 OFD 签章的可视化外观流。
///
/// 功能:
/// - 渲染圆形/椭圆形印章
/// - 文字环绕排列
/// - 中心图章/文字
/// - 生成 PNG/JPEG 外观图像
///
/// 布局:
/// - 外圈:公司名称环绕
/// - 中心:签署人/日期/编号
/// - 红色印章风格(可配置)
///
/// 使用场景:
/// - OFD 数字签名可视化
/// - 签章预览生成
/// - 企业印章模板
/// </remarks>
public sealed class StampAppearanceRenderer
{
    private readonly ILogger<StampAppearanceRenderer> _logger;

    // 默认样式常量
    private const int _defaultWidth = 200;
    private const int _defaultHeight = 200;
    private const int _defaultBorderWidth = 3;
    private const string _defaultColor = "#FF0000"; // 红色

    /// <summary>
    /// 初始化 StampAppearanceRenderer 实例。
    /// </summary>
    public StampAppearanceRenderer(ILogger<StampAppearanceRenderer> logger)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <summary>
    /// 渲染签章外观。
    /// </summary>
    /// <param name="config">签章配置</param>
    /// <returns>PNG 图像字节流</returns>
    public byte[] RenderStamp(StampConfig config)
    {
        if (config == null)
        {
            throw new ArgumentNullException(nameof(config));
        }

        try
        {
            _logger.LogInformation("Rendering stamp appearance (Size: {Width}x{Height})", config.Width, config.Height);

            using var bitmap = new Bitmap(config.Width, config.Height);
            using var graphics = Graphics.FromImage(bitmap);

            // 设置高质量渲染
            graphics.SmoothingMode = SmoothingMode.AntiAlias;
            graphics.TextRenderingHint = System.Drawing.Text.TextRenderingHint.AntiAlias;

            // 透明背景
            graphics.Clear(Color.Transparent);

            // 绘制印章边框
            DrawBorder(graphics, config);

            // 绘制环绕文字
            if (!string.IsNullOrWhiteSpace(config.CompanyName))
            {
                DrawCircularText(graphics, config);
            }

            // 绘制中心文字
            if (!string.IsNullOrWhiteSpace(config.CenterText))
            {
                DrawCenterText(graphics, config);
            }

            // 绘制五角星(可选)
            if (config.DrawStar)
            {
                DrawStar(graphics, config);
            }

            // 保存为 PNG
            using var stream = new MemoryStream();
            bitmap.Save(stream, ImageFormat.Png);
            var imageData = stream.ToArray();

            _logger.LogInformation("Stamp appearance rendered successfully (Size: {Size} bytes)", imageData.Length);
            return imageData;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to render stamp appearance");
            throw;
        }
    }

    /// <summary>
    /// 绘制印章边框。
    /// </summary>
    private void DrawBorder(Graphics graphics, StampConfig config)
    {
        var color = ColorTranslator.FromHtml(config.Color);
        using var pen = new Pen(color, config.BorderWidth);

        var rect = new Rectangle(
            config.BorderWidth,
            config.BorderWidth,
            config.Width - config.BorderWidth * 2,
            config.Height - config.BorderWidth * 2);

        if (config.IsEllipse)
        {
            graphics.DrawEllipse(pen, rect);
        }
        else
        {
            // 圆形(宽高相等时)
            var size = Math.Min(config.Width, config.Height) - config.BorderWidth * 2;
            var offsetX = (config.Width - size) / 2;
            var offsetY = (config.Height - size) / 2;
            graphics.DrawEllipse(pen, offsetX, offsetY, size, size);
        }
    }

    /// <summary>
    /// 绘制环绕文字。
    /// </summary>
    private void DrawCircularText(Graphics graphics, StampConfig config)
    {
        var text = config.CompanyName!;
        var centerX = config.Width / 2f;
        var centerY = config.Height / 2f;
        var radius = Math.Min(config.Width, config.Height) / 2f - config.BorderWidth * 2 - 10;

        var color = ColorTranslator.FromHtml(config.Color);
        using var brush = new SolidBrush(color);
        using var font = new Font(config.FontFamily, config.FontSize, FontStyle.Bold);

        var charCount = text.Length;
        var angleStep = 180f / (charCount + 1); // 上半圆分布
        var startAngle = 180f; // 从左侧开始

        for (int i = 0; i < charCount; i++)
        {
            var angle = startAngle + angleStep * (i + 1);
            var radian = angle * Math.PI / 180f;

            var x = centerX + (float)(radius * Math.Cos(radian));
            var y = centerY + (float)(radius * Math.Sin(radian));

            // 字符旋转角度(垂直于半径)
            var charAngle = angle + 90f;

            graphics.TranslateTransform(x, y);
            graphics.RotateTransform(charAngle);
            graphics.DrawString(text[i].ToString(), font, brush, 0, 0, new StringFormat
            {
                Alignment = StringAlignment.Center,
                LineAlignment = StringAlignment.Center
            });
            graphics.ResetTransform();
        }

        _logger.LogDebug("Drew circular text: '{Text}'", text);
    }

    /// <summary>
    /// 绘制中心文字。
    /// </summary>
    private void DrawCenterText(Graphics graphics, StampConfig config)
    {
        var text = config.CenterText!;
        var centerX = config.Width / 2f;
        var centerY = config.Height / 2f;

        var color = ColorTranslator.FromHtml(config.Color);
        using var brush = new SolidBrush(color);
        using var font = new Font(config.FontFamily, config.CenterFontSize, FontStyle.Regular);

        var format = new StringFormat
        {
            Alignment = StringAlignment.Center,
            LineAlignment = StringAlignment.Center
        };

        graphics.DrawString(text, font, brush, centerX, centerY, format);
        _logger.LogDebug("Drew center text: '{Text}'", text);
    }

    /// <summary>
    /// 绘制五角星。
    /// </summary>
    private void DrawStar(Graphics graphics, StampConfig config)
    {
        var centerX = config.Width / 2f;
        var centerY = config.Height / 2f - 20; // 稍微上移
        var outerRadius = 15f;
        var innerRadius = 6f;

        var color = ColorTranslator.FromHtml(config.Color);
        using var brush = new SolidBrush(color);

        var points = new PointF[10];
        for (int i = 0; i < 10; i++)
        {
            var radius = i % 2 == 0 ? outerRadius : innerRadius;
            var angle = (i * 36 - 90) * Math.PI / 180f; // 从顶部开始

            points[i] = new PointF(
                centerX + (float)(radius * Math.Cos(angle)),
                centerY + (float)(radius * Math.Sin(angle)));
        }

        graphics.FillPolygon(brush, points);
        _logger.LogDebug("Drew star at center");
    }

    /// <summary>
    /// 生成默认企业印章。
    /// </summary>
    /// <param name="companyName">公司名称</param>
    /// <param name="signerName">签署人姓名</param>
    /// <returns>PNG 图像字节流</returns>
    public byte[] RenderDefaultStamp(string companyName, string signerName)
    {
        var config = new StampConfig
        {
            Width = _defaultWidth,
            Height = _defaultHeight,
            BorderWidth = _defaultBorderWidth,
            Color = _defaultColor,
            CompanyName = companyName,
            CenterText = signerName,
            FontFamily = "SimHei", // 黑体
            FontSize = 14,
            CenterFontSize = 18,
            DrawStar = true,
            IsEllipse = false
        };

        return RenderStamp(config);
    }
}

/// <summary>
/// 签章配置。
/// </summary>
public sealed class StampConfig
{
    /// <summary>
    /// 印章宽度(像素)。
    /// </summary>
    public int Width { get; set; } = 200;

    /// <summary>
    /// 印章高度(像素)。
    /// </summary>
    public int Height { get; set; } = 200;

    /// <summary>
    /// 边框宽度(像素)。
    /// </summary>
    public int BorderWidth { get; set; } = 3;

    /// <summary>
    /// 印章颜色(HTML 格式,如 "#FF0000")。
    /// </summary>
    public string Color { get; set; } = "#FF0000";

    /// <summary>
    /// 公司名称(环绕文字)。
    /// </summary>
    public string? CompanyName { get; set; }

    /// <summary>
    /// 中心文字(签署人/日期等)。
    /// </summary>
    public string? CenterText { get; set; }

    /// <summary>
    /// 字体名称。
    /// </summary>
    public string FontFamily { get; set; } = "SimHei";

    /// <summary>
    /// 环绕文字字号。
    /// </summary>
    public int FontSize { get; set; } = 14;

    /// <summary>
    /// 中心文字字号。
    /// </summary>
    public int CenterFontSize { get; set; } = 18;

    /// <summary>
    /// 是否绘制五角星。
    /// </summary>
    public bool DrawStar { get; set; } = true;

    /// <summary>
    /// 是否为椭圆形(false=圆形)。
    /// </summary>
    public bool IsEllipse { get; set; } = false;
}
