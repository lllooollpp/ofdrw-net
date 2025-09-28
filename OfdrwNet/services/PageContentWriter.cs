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
using OfdrwNet.Core.BasicType;
using OfdrwNet.Core.Graph.PathObj;
using OfdrwNet.Core.Image;
using OfdrwNet.Core.PageDescription.Color;
using OfdrwNet.Core.Text;
using OfdrwNet.Models;
using OfdrwNet.Utils;

namespace OfdrwNet.Services;

internal sealed class PageContentWriter
{
    private readonly ILogger? _logger;
    private readonly ImageProcessor _imageProcessor;
    public PageContentWriter(ILogger? logger)
    {
        _logger = logger;
        _imageProcessor = new ImageProcessor(logger);
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
        var imagesOnPageList = _imageProcessor.OrderImages(allImages, pageNumber, imageOrderingStrategy);
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

        foreach (var run in textRuns)
        {
            var textObject = CreateTextObject(run, fontMap, nextId);
            if (textObject != null)
            {
                layer.AddPageObject(textObject);
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
        var pathObject = new PathObject(new StRefId(nextId()))
            .SetBoundary(pathItem.X, pathItem.Y, pathItem.Width, pathItem.Height);

        if (pathItem.Stroke.HasValue)
        {
            pathObject.SetStroke(pathItem.Stroke);
        }

        if (pathItem.Fill.HasValue)
        {
            pathObject.SetFill(pathItem.Fill);
        }

        if (pathItem.LineWidth.HasValue)
        {
            pathObject.SetLineWidth(pathItem.LineWidth.Value);
        }

        var strokeColor = TryParseColor(pathItem.StrokeColor) ?? CreateBlackColor();
        pathObject.SetStrokeColor(strokeColor);

        var fillColor = TryParseColor(pathItem.FillColor);
        if (fillColor != null)
        {
            pathObject.SetFillColor(fillColor);
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

    private TextObject? CreateTextObject(RawGlyphRun run, IDictionary<string, OfdFont> fontMap, Func<int> nextId)
    {
        if (string.IsNullOrEmpty(run.Text))
        {
            return null;
        }

        if (!fontMap.TryGetValue(run.FontName, out var font))
        {
            font = fontMap.Values.FirstOrDefault();
        }

        if (font == null)
        {
            _logger?.LogWarning("[PageContentWriter] 无可用字体，跳过文本：{FontName}", run.FontName);
            return null;
        }

        var textLen = run.Text.Length;
        var width = run.Width > 0
            ? run.Width
            : (run.DeltaX?.Sum() ?? (textLen * run.FontSizeMm * 0.6));

        const double fontAscentRatio = 0.85;
        const double lineHeightFactor = 1.2;
        var ascentHeight = run.FontSizeMm * fontAscentRatio;
        var totalTextHeight = run.Height > 0 ? run.Height : run.FontSizeMm * lineHeightFactor;

        var textObject = new TextObject(new StRefId(nextId()));
        textObject.SetFont(new StRefId(font.ID));
        textObject.SetSize(run.FontSizeMm);
        textObject.SetBoundary(run.X, run.Y, width, totalTextHeight);
        textObject.SetStroke(false);
        textObject.SetFill(true);
        textObject.SetFillColor(CreateBlackColor());

        var ctm = CreateCtm(run.CTM);
        if (ctm != null)
        {
            textObject.SetCtm(ctm);
        }

        var textCode = new TextCode()
            .SetCoordinate(0, ascentHeight)
            .SetContent(run.Text);

        if (run.DeltaX is { Length: > 0 })
        {
            textCode.SetDeltaX(new StArray(run.DeltaX));
        }

        if (run.DeltaY is { Length: > 0 })
        {
            textCode.SetDeltaY(new StArray(run.DeltaY));
        }

        textObject.AddTextCode(textCode);

        if (run.Glyphs is { Length: > 0 })
        {
            var glyphArray = new StArray(run.Glyphs);
            var cgTransform = new CtCgTransform()
                .SetCodePosition(0)
                .SetCodeCount(run.Glyphs.Length)
                .SetGlyphCount(run.Glyphs.Length)
                .SetGlyphs(glyphArray);
            textObject.AddCgTransform(cgTransform);
        }

        return textObject;
    }

    private static StArray? CreateCtm(double[]? ctm)
    {
        var normalized = CtmUtil.Normalize(ctm);
        return normalized != null ? new StArray(normalized) : null;
    }

    private static CtColor CreateBlackColor()
    {
        return CtColor.Rgb(0, 0, 0);
    }

    private static CtColor? TryParseColor(string? value)
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

        if (int.TryParse(parts[0], NumberStyles.Integer, CultureInfo.InvariantCulture, out var r) &&
            int.TryParse(parts[1], NumberStyles.Integer, CultureInfo.InvariantCulture, out var g) &&
            int.TryParse(parts[2], NumberStyles.Integer, CultureInfo.InvariantCulture, out var b))
        {
            r = Math.Clamp(r, 0, 255);
            g = Math.Clamp(g, 0, 255);
            b = Math.Clamp(b, 0, 255);
            return CtColor.Rgb(r, g, b);
        }

        return null;
    }
}
