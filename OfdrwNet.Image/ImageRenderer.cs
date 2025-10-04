using OfdrwNet.Core;
using SkiaSharp;
using System.Xml.Linq;

namespace OfdrwNet.Image;

/// <summary>
/// 图像渲染器实现，负责将OFD内容渲染到Skia画布上
/// </summary>
public class ImageRenderer : IImageRenderer
{
    /// <summary>
    /// 渲染页面内容到画布
    /// </summary>
    /// <param name="canvas">Skia画布</param>
    /// <param name="pageInfo">页面信息</param>
    /// <param name="cancellationToken">取消令牌</param>
    public async Task RenderPageContentAsync(SKCanvas canvas, dynamic pageInfo, CancellationToken cancellationToken = default)
    {
        // 获取页面的所有图层
        var layers = pageInfo.GetAllLayers();

        foreach (var layer in layers)
        {
            cancellationToken.ThrowIfCancellationRequested();
            await RenderLayerAsync(canvas, layer);
        }
    }

    /// <summary>
    /// 渲染单个图层
    /// </summary>
    /// <param name="canvas">Skia画布</param>
    /// <param name="layer">图层元素</param>
    public async Task RenderLayerAsync(SKCanvas canvas, XElement layer)
    {
        // 获取图层中的所有对象
        var textObjects = layer.Elements("TextObject");
        var imageObjects = layer.Elements("ImageObject");
        var pathObjects = layer.Elements("PathObject");

        // 渲染文本对象
        foreach (var textObj in textObjects)
        {
            await RenderTextObjectAsync(canvas, textObj);
        }

        // 渲染图像对象
        foreach (var imageObj in imageObjects)
        {
            await RenderImageObjectAsync(canvas, imageObj);
        }

        // 渲染路径对象
        foreach (var pathObj in pathObjects)
        {
            await RenderPathObjectAsync(canvas, pathObj);
        }
    }

    /// <summary>
    /// 渲染文本对象
    /// </summary>
    /// <param name="canvas">Skia画布</param>
    /// <param name="textObject">文本对象元素</param>
    public async Task RenderTextObjectAsync(SKCanvas canvas, XElement textObject)
    {
        // 解析文本属性
        var boundary = ParseBoundary(textObject.Attribute("Boundary")?.Value);
        var fontSize = float.Parse(textObject.Attribute("Size")?.Value ?? "12");

        // 获取文本内容
        var textCodeElements = textObject.Elements("TextCode");
        foreach (var textCode in textCodeElements)
        {
            var x = float.Parse(textCode.Attribute("X")?.Value ?? "0");
            var y = float.Parse(textCode.Attribute("Y")?.Value ?? "0");
            var text = textCode.Value;

            if (!string.IsNullOrEmpty(text))
            {
                using var paint = new SKPaint
                {
                    Color = SKColors.Black,
                    TextSize = fontSize,
                    IsAntialias = true,
                    Typeface = SKTypeface.Default
                };

                canvas.DrawText(text, x, y, paint);
            }
        }

        await Task.CompletedTask;
    }

    /// <summary>
    /// 渲染图像对象
    /// </summary>
    /// <param name="canvas">Skia画布</param>
    /// <param name="imageObject">图像对象元素</param>
    public async Task RenderImageObjectAsync(SKCanvas canvas, XElement imageObject)
    {
        // 这里需要从OFD容器中加载图像资源并绘制
        // 简化实现，实际需要通过ResourceLocator加载图像
        await Task.CompletedTask;
    }

    /// <summary>
    /// 渲染路径对象
    /// </summary>
    /// <param name="canvas">Skia画布</param>
    /// <param name="pathObject">路径对象元素</param>
    public async Task RenderPathObjectAsync(SKCanvas canvas, XElement pathObject)
    {
        // 解析路径数据并绘制
        var pathData = pathObject.Element("AbbreviatedData")?.Value;
        if (!string.IsNullOrEmpty(pathData))
        {
            using var path = SKPath.ParseSvgPathData(pathData);
            using var paint = new SKPaint
            {
                Color = SKColors.Black,
                Style = SKPaintStyle.Stroke,
                StrokeWidth = 1,
                IsAntialias = true
            };

            canvas.DrawPath(path, paint);
        }

        await Task.CompletedTask;
    }

    /// <summary>
    /// 解析边界框
    /// </summary>
    /// <param name="boundaryStr">边界框字符串</param>
    /// <returns>边界框</returns>
    private SKRect ParseBoundary(string? boundaryStr)
    {
        if (string.IsNullOrEmpty(boundaryStr))
            return SKRect.Empty;

        var parts = boundaryStr.Split(' ', StringSplitOptions.RemoveEmptyEntries);
        if (parts.Length >= 4)
        {
            var x = float.Parse(parts[0]);
            var y = float.Parse(parts[1]);
            var width = float.Parse(parts[2]);
            var height = float.Parse(parts[3]);
            return new SKRect(x, y, x + width, y + height);
        }

        return SKRect.Empty;
    }
}
