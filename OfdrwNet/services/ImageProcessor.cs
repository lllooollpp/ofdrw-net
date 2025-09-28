using Microsoft.Extensions.Logging;
using OfdrwNet.Core.BasicType;
using OfdrwNet.Core.Image;
using OfdrwNet.Models;
using OfdrwNet.Utils;

namespace OfdrwNet.Services;

internal sealed class ImageProcessor
{
    private readonly ILogger? _logger;
    public ImageProcessor(ILogger? logger){ _logger = logger; }

    public List<RawImage> OrderImages(IEnumerable<RawImage> all, int page, string strategy)
    {
        var imagesQuery = all.Where(i => i.Page == page);
        List<RawImage> list;
        switch(strategy?.ToUpperInvariant()){
            case "YASCENDING":
                list = imagesQuery.OrderBy(i=>i.Z).ThenBy(i=>i.Y).ThenBy(i=>i.Sequence).ToList(); break;
            case "YDESCENDING":
                list = imagesQuery.OrderBy(i=>i.Z).ThenByDescending(i=>i.Y).ThenBy(i=>i.Sequence).ToList(); break;
            case "SEQUENCEOLDTOP":
                list = imagesQuery.OrderBy(i=>i.Z).ThenByDescending(i=>i.Sequence).ToList(); break;
            case "SEQUENCE":
            default:
                list = imagesQuery.OrderBy(i=>i.Z).ThenBy(i=>i.Sequence).ToList(); break;
        }
        if(imagesQuery.Any()){
            _logger?.LogDebug("[ImageProcessor] Page={Page} Strategy={Strategy} Order={Order}", page, strategy, string.Join("|", list.Select(i=>$"S{i.Sequence}-Z{i.Z}")));
        }
        return list;
    }

    public IReadOnlyList<ImageObject> BuildImageObjects(IEnumerable<RawImage> images, Func<int> nextId)
    {
        var results = new List<ImageObject>();
        foreach(var img in images)
        {
            if(img.ResourceID == 0)
            {
                _logger?.LogWarning("[ImageProcessor] Page={Page} Image Hash={Hash} ResourceID=0 (未分配)", img.Page, img.Hash.Length > 12 ? img.Hash[..12] : img.Hash);
            }

            var imageObject = new ImageObject()
                .SetID(new StId(nextId()))
                .SetResourceID(new StRefId(img.ResourceID))
                .SetBoundary(new StBox(img.X, img.Y, img.Width, img.Height));

            var normalizedCtm = CtmUtil.Normalize(img.CTM);
            if(normalizedCtm != null)
            {
                imageObject.SetCTM(new StArray(normalizedCtm));
            }

            if(img.Alpha is < 255 and >= 0)
            {
                imageObject.SetAlpha(img.Alpha);
            }

            if(!string.IsNullOrWhiteSpace(img.AltText))
            {
                imageObject.SetAltText(img.AltText);
            }

            results.Add(imageObject);
        }

        return results;
    }
}
