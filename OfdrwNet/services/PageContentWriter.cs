using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using OfdrwNet.Core;
using OfdrwNet.Core.BasicStructure.PageObj;
using OfdrwNet.Core.BasicStructure.PageObj.Layer;
using OfdrwNet.Core.BasicStructure.PageObj.Layer.Block;
using OfdrwNet.Text;
using OfdrwNet.Core.BasicType;
using OfdrwNet.Core.Graph.PathObj;
using OfdrwNet.Core.Graph;
using OfdrwNet.Core.Image;
using OfdrwNet.Core.PageDescription.Color;
using OfdrwNet.Core.Text;
using OfdrwNet.Models;
using OfdrwNet.Utils;
using OfdrwNet.Font;
using RawImage = OfdrwNet.Image.RawImage;
using OfdrwNet.Image;

namespace OfdrwNet.Services;

internal sealed class PageContentWriter
{
    private readonly ILogger? _logger;
    private readonly OfdImageProcessor _imageProcessor;
    private readonly OfdTextObjectBuilder _textObjectBuilder;

    public PageContentWriter(ILogger? logger)
    {
        _logger = logger;
        _imageProcessor = new OfdImageProcessor(logger);
        _textObjectBuilder = new OfdTextObjectBuilder();
    }

    public async Task WritePageAsync(
        string pagesDir,
        int pageNumber,
        int pageIndex,
        List<object> items,
        IEnumerable<RawImage> allImages,
        IEnumerable<RawPath> allPaths,
        IDictionary<string, OfdFont> fontMap,
        string imageOrderingStrategy,
        Func<int> nextId,
        double pageWidth,
        double pageHeight)
    {
        // 转换为 OfdRawImage 集合，通过属性复制
        var ofdImages = allImages.Select(img => new OfdRawImage
        {
            Format = img.Format,
            X = img.X,
            Y = img.Y,
            Width = img.Width,
            Height = img.Height,
            Data = img.Data,
            Page = img.Page,
            ResourceID = img.ResourceID,
            Hash = img.Hash,
            IsFirstResource = img.IsFirstResource,
            CTM = img.CTM,
            Sequence = img.Sequence,
            Z = img.Z,
            Alpha = img.Alpha,
            AltText = img.AltText
        });

        var imagesOnPageList = _imageProcessor.OrderImages(ofdImages, pageNumber, imageOrderingStrategy);
        if (imagesOnPageList.Count > 0)
        {
            _logger?.LogDebug("[PageContentWriter] Page={Page} ImageCount={Count} RIDs={RIDs}", pageNumber, imagesOnPageList.Count, string.Join(',', imagesOnPageList.Select(i => i.ResourceID)));
        }
        else
        {
            _logger?.LogDebug("[PageContentWriter] Page={Page} ImageCount=0", pageNumber);
        }

        var imageObjects = _imageProcessor.BuildImageObjects(imagesOnPageList, nextId);

        var pathItems = items.OfType<RawPath>().Where(p => p.Page == pageNumber).ToList();
        if (pathItems.Count > 0)
        {
            _logger?.LogDebug("[PageContentWriter] Page={Page} PathCount={Cnt}", pageNumber, pathItems.Count);
        }

        // 处理字形运行（文本）
        var textRuns = items.OfType<RawGlyphRun>()
            .Where(r => r.Page == pageNumber && !string.IsNullOrWhiteSpace(r.Text))
            .ToList();
        if (textRuns.Count > 0)
        {
            _logger?.LogDebug("[PageContentWriter] Page={Page} TextRunCount={Cnt}", pageNumber, textRuns.Count);
        }

        var layer = new CtLayer()
            .SetType(LayerType.Body);
        layer.SetObjId(new StId(nextId()));

        foreach (var imageObject in imageObjects)
        {
            layer.AddPageObject(imageObject);
        }

        foreach (var pathItem in pathItems)
        {
            var pathObject = CreatePathObject(pathItem, nextId);
            layer.AddPageObject(pathObject);
        }

        // 使用统一的文本对象构建器
        foreach (var run in textRuns)
        {
            try
            {
                // 转换为 OfdRawGlyphRun
                var ofdGlyphRun = OfdRawGlyphRun.FromRawGlyphRun(run);

                // 转换字体映射格式
                var fontMapForBuilder = fontMap.ToDictionary(kv => kv.Key, kv => (object)kv.Value);

                // 使用统一的文本对象构建器
                var textObjectElement = _textObjectBuilder.CreateTextObject(ofdGlyphRun, fontMapForBuilder, nextId);

                if (textObjectElement != null)
                {
                    // 将 XElement 转换为 TextObject
                    var textObject = ConvertXElementToTextObject(textObjectElement, nextId);
                    if (textObject != null)
                    {
                        layer.AddPageObject(textObject);
                    }
                }
            }
            catch (Exception ex)
            {
                _logger?.LogWarning(ex, "[PageContentWriter] 创建文本对象失败: {Text}", run.Text);
            }
        }

        var content = new Content().AddLayer(layer);

        var page = new Page();
        var areaElement = OfdElement.GetInstance("Area");
        areaElement.AddOfdEntity("PhysicalBox", new StBox(0, 0, pageWidth, pageHeight).ToString());
        page.Set(areaElement);
        page.SetContent(content);

    var pageDir = Path.Combine(pagesDir, $"Page_{Math.Max(pageIndex, 0)}");
        Directory.CreateDirectory(pageDir);
        var pageXml = "<?xml version=\"1.0\" encoding=\"UTF-8\"?>\n" + page.ToXml();
        await FileUtil.WriteTextFileUtf8LfAsync(Path.Combine(pageDir, "Content.xml"), pageXml);
    }

    private PathObject CreatePathObject(RawPath pathItem, Func<int> nextId)
    {
        var lineWidthValue = pathItem.LineWidth ?? 0.0;
        if (lineWidthValue > 0 && lineWidthValue < 0.1)
        {
            lineWidthValue = 0.1;
        }

        double x = pathItem.X;
        double y = pathItem.Y;
        double width = pathItem.Width;
        double height = pathItem.Height;

        if (lineWidthValue > 0)
        {
            var minExtent = Math.Max(lineWidthValue, 0.1) * 1.2;

            if (height < minExtent)
            {
                var delta = minExtent - height;
                y -= delta / 2.0;
                height = minExtent;
            }

            if (width < minExtent && width < height)
            {
                var delta = minExtent - width;
                x -= delta / 2.0;
                width = minExtent;
            }
        }

        var pathObject = new PathObject(new StRefId(nextId()))
            .SetBoundary(x, y, Math.Max(width, 0.1), Math.Max(height, 0.1));

        if (pathItem.Stroke.HasValue)
        {
            pathObject.SetStroke(pathItem.Stroke);
        }

        if (pathItem.Fill.HasValue)
        {
            pathObject.SetFill(pathItem.Fill);
        }

        if (lineWidthValue > 0)
        {
            pathObject.SetLineWidth(lineWidthValue);
        }

        var strokeColor = TryParseColor(pathItem.StrokeColor, true) ?? CreateBlackStrokeColor();
        pathObject.SetStrokeColor(strokeColor);

        var fillColor = TryParseColor(pathItem.FillColor, false);
        if (fillColor != null)
        {
            pathObject.SetFillColor(fillColor);
        }

        if (pathItem.DashPattern is { Length: > 0 })
        {
            var anyPositive = false;
            foreach (var segment in pathItem.DashPattern)
            {
                if (segment > 0.001)
                {
                    anyPositive = true;
                    break;
                }
            }

            if (anyPositive)
            {
                pathObject.SetLineDash(pathItem.DashPattern);
            }
        }

        var ctm = CreateCtm(pathItem.CTM);
        if (ctm != null)
        {
            pathObject.SetCTM(ctm);
        }

        if (!string.IsNullOrWhiteSpace(pathItem.PathData))
        {
            var abbreviated = new AbbreviatedData();
            abbreviated.SetText(pathItem.PathData);
            pathObject.SetAbbreviatedData(abbreviated);
        }

        return pathObject;
    }

    /// <summary>
    /// 将 XElement 转换为 TextObject
    /// </summary>
    /// <param name="textElement">文本对象的 XElement</param>
    /// <param name="nextId">ID 生成器</param>
    /// <returns>TextObject 实例</returns>
    private TextObject? ConvertXElementToTextObject(System.Xml.Linq.XElement textElement, Func<int> nextId)
    {
        try
        {
            // 解析基本属性
            var id = textElement.Attribute("ID")?.Value;
            var fontRef = textElement.Attribute("Font")?.Value;
            var size = float.TryParse(textElement.Attribute("Size")?.Value, out var sizeVal) ? sizeVal : 12f;
            var boundary = textElement.Attribute("Boundary")?.Value;
            var stroke = bool.TryParse(textElement.Attribute("Stroke")?.Value, out var strokeVal) && strokeVal;
            var fill = !bool.TryParse(textElement.Attribute("Fill")?.Value, out var fillVal) || fillVal;

            if (string.IsNullOrEmpty(fontRef) || string.IsNullOrEmpty(boundary))
                return null;

            // 解析边界框
            var boundaryParts = boundary.Split(' ', StringSplitOptions.RemoveEmptyEntries);
            if (boundaryParts.Length < 4)
                return null;

            var x = double.TryParse(boundaryParts[0], out var xVal) ? xVal : 0;
            var y = double.TryParse(boundaryParts[1], out var yVal) ? yVal : 0;
            var width = double.TryParse(boundaryParts[2], out var widthVal) ? widthVal : 0;
            var height = double.TryParse(boundaryParts[3], out var heightVal) ? heightVal : 0;

            // 创建 TextObject
            var textObject = new TextObject(new StRefId(int.TryParse(id, out var idVal) ? idVal : nextId()));
            textObject.SetFont(new StRefId(int.TryParse(fontRef, out var fontRefVal) ? fontRefVal : 1));
            textObject.SetSize(size);
            textObject.SetBoundary(x, y, width, height);
            textObject.SetStroke(stroke);
            textObject.SetFill(fill);

            // 设置填充颜色
            if (fill)
            {
                textObject.SetFillColor(CreateBlackFillColor());
            }

            // 解析 CTM（如果有）
            var ctmAttr = textElement.Attribute("CTM")?.Value;
            if (!string.IsNullOrEmpty(ctmAttr))
            {
                var ctmParts = ctmAttr.Split(' ', StringSplitOptions.RemoveEmptyEntries);
                if (ctmParts.Length >= 6)
                {
                    var ctmValues = new double[6];
                    for (int i = 0; i < 6; i++)
                    {
                        ctmValues[i] = double.TryParse(ctmParts[i], out var val) ? val : 0;
                    }
                    var ctm = CreateCtm(ctmValues);
                    if (ctm != null)
                    {
                        textObject.SetCtm(ctm);
                    }
                }
            }

            // 解析 TextCode 元素
            var textCodeElements = textElement.Elements("TextCode");
            foreach (var tcElement in textCodeElements)
            {
                var tcX = double.TryParse(tcElement.Attribute("X")?.Value, out var tcXVal) ? tcXVal : 0;
                var tcY = double.TryParse(tcElement.Attribute("Y")?.Value, out var tcYVal) ? tcYVal : 0;
                var tcText = tcElement.Value ?? string.Empty;

                if (!string.IsNullOrEmpty(tcText))
                {
                    var textCode = new TextCode()
                        .SetCoordinate(tcX, tcY)
                        .SetContent(tcText);

                    // 解析 DeltaX（如果有）
                    var deltaXAttr = tcElement.Attribute("DeltaX")?.Value;
                    if (!string.IsNullOrEmpty(deltaXAttr))
                    {
                        var deltaXParts = deltaXAttr.Split(' ', StringSplitOptions.RemoveEmptyEntries);
                        var deltaXValues = new List<double>();
                        foreach (var part in deltaXParts)
                        {
                            if (double.TryParse(part, out var val))
                                deltaXValues.Add(val);
                        }
                        if (deltaXValues.Count > 0)
                        {
                            textCode.SetDeltaX(new StArray(deltaXValues.ToArray()));
                        }
                    }

                    // 解析 DeltaY（如果有）
                    var deltaYAttr = tcElement.Attribute("DeltaY")?.Value;
                    if (!string.IsNullOrEmpty(deltaYAttr))
                    {
                        var deltaYParts = deltaYAttr.Split(' ', StringSplitOptions.RemoveEmptyEntries);
                        var deltaYValues = new List<double>();
                        foreach (var part in deltaYParts)
                        {
                            if (double.TryParse(part, out var val))
                                deltaYValues.Add(val);
                        }
                        if (deltaYValues.Count > 0)
                        {
                            textCode.SetDeltaY(new StArray(deltaYValues.ToArray()));
                        }
                    }

                    textObject.AddTextCode(textCode);
                }
            }

            return textObject;
        }
        catch (Exception ex)
        {
            _logger?.LogWarning(ex, "[PageContentWriter] 转换文本对象失败");
            return null;
        }
    }

    private static StArray? CreateCtm(double[]? ctm)
    {
        var normalized = CtmUtil.Normalize(ctm);
        return normalized != null ? new StArray(normalized) : null;
    }

    private static CtColor CreateBlackStrokeColor() => CreateRgbColor(0, 0, 0, isStroke: true);

    private static CtColor CreateBlackFillColor() => CreateRgbColor(0, 0, 0, isStroke: false);

    private static CtColor CreateRgbColor(int r, int g, int b, bool isStroke)
    {
        CtColor color = isStroke ? new StrokeColor() : new FillColor();
        color.SetValue(new StArray(r, g, b));
        color.SetAlpha(255);
        color.AddAttribute("ColorSpace", "RGB");
        return color;
    }

    private static CtColor? TryParseColor(string? value, bool isStroke)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return null;
        }

        var parts = value.Split(new[] { ' ', ',', ';' }, StringSplitOptions.RemoveEmptyEntries);
        if (parts.Length != 3)
        {
            return null;
        }

        if (!int.TryParse(parts[0], NumberStyles.Integer, CultureInfo.InvariantCulture, out var r) ||
            !int.TryParse(parts[1], NumberStyles.Integer, CultureInfo.InvariantCulture, out var g) ||
            !int.TryParse(parts[2], NumberStyles.Integer, CultureInfo.InvariantCulture, out var b))
        {
            return null;
        }

        r = Math.Clamp(r, 0, 255);
        g = Math.Clamp(g, 0, 255);
        b = Math.Clamp(b, 0, 255);

        return CreateRgbColor(r, g, b, isStroke);
    }
}
