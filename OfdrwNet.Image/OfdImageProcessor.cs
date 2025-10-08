using Microsoft.Extensions.Logging;
using OfdrwNet.Core.BasicType;
using OfdrwNet.Core.Image;

namespace OfdrwNet.Image;

/// <summary>
/// OFD 图像处理器，用于图像排序和对象构建
/// </summary>
public sealed class OfdImageProcessor
{
    private readonly ILogger? _logger;

    public OfdImageProcessor(ILogger? logger = null)
    {
        _logger = logger;
    }

    /// <summary>
    /// 根据策略排序图像
    /// </summary>
    /// <param name="images">图像列表</param>
    /// <param name="page">页码</param>
    /// <param name="strategy">排序策略</param>
    /// <returns>排序后的图像列表</returns>
    public List<OfdRawImage> OrderImages(IEnumerable<OfdRawImage> images, int page, string strategy)
    {
        var imagesQuery = images.Where(i => i.Page == page);
        List<OfdRawImage> list;
        switch(strategy?.ToUpperInvariant()){
            case "YASCENDING":
                list = imagesQuery.OrderBy(i=>i.Z).ThenBy(i=>i.Y).ThenBy(i=>i.Sequence).ToList();
                break;
            case "YDESCENDING":
                list = imagesQuery.OrderBy(i=>i.Z).ThenByDescending(i=>i.Y).ThenBy(i=>i.Sequence).ToList();
                break;
            case "SEQUENCEOLDTOP":
                list = imagesQuery.OrderBy(i=>i.Z).ThenByDescending(i=>i.Sequence).ToList();
                break;
            case "SEQUENCE":
            default:
                list = imagesQuery.OrderBy(i=>i.Z).ThenBy(i=>i.Sequence).ToList();
                break;
        }
        if(imagesQuery.Any()){
            _logger?.LogDebug("[OfdImageProcessor] Page={Page} Strategy={Strategy} Order={Order}", page, strategy, string.Join("|", list.Select(i=>$"S{i.Sequence}-Z{i.Z}")));
        }
        return list;
    }

    /// <summary>
    /// 构建图像对象列表
    /// </summary>
    /// <param name="images">图像列表</param>
    /// <param name="nextId">获取下一个ID的函数</param>
    /// <returns>图像对象列表</returns>
    public IReadOnlyList<ImageObject> BuildImageObjects(IEnumerable<OfdRawImage> images, Func<int> nextId)
    {
        var results = new List<ImageObject>();
        foreach(var img in images)
        {
            if(img.ResourceID == 0)
            {
                _logger?.LogWarning("[OfdImageProcessor] Page={Page} Image Hash={Hash} ResourceID=0 (未分配)", img.Page, img.Hash.Length > 12 ? img.Hash[..12] : img.Hash);
            }

            var imageObject = new ImageObject()
                .SetID(new StId(nextId()))
                .SetResourceID(new StRefId(img.ResourceID))
                .SetBoundary(new StBox(img.X, img.Y, img.Width, img.Height));

            // 设置变换矩阵
            if(img.CTM != null && img.CTM.Length >= 6)
            {
                var normalizedCtm = NormalizeCtm(img.CTM);
                if(normalizedCtm != null)
                {
                    imageObject.SetCTM(new StArray(normalizedCtm));
                }
            }

            // 设置透明度
            if(img.Alpha is < 255 and >= 0)
            {
                imageObject.SetAlpha(img.Alpha);
            }

            // 设置替代文本
            if(!string.IsNullOrWhiteSpace(img.AltText))
            {
                imageObject.SetAltText(img.AltText);
            }

            results.Add(imageObject);
        }

        return results;
    }

    /// <summary>
    /// 规范化变换矩阵
    /// </summary>
    /// <param name="ctm">原始变换矩阵</param>
    /// <returns>规范化后的变换矩阵</returns>
    private static double[]? NormalizeCtm(double[] ctm)
    {
        if (ctm == null || ctm.Length < 6) return null;

        // 检查是否为单位矩阵
        if (Math.Abs(ctm[0] - 1.0) < 1e-6 && Math.Abs(ctm[1]) < 1e-6 &&
            Math.Abs(ctm[2]) < 1e-6 && Math.Abs(ctm[3] - 1.0) < 1e-6 &&
            Math.Abs(ctm[4]) < 1e-6 && Math.Abs(ctm[5]) < 1e-6)
        {
            return null; // 单位矩阵，不需要设置
        }

        return ctm;
    }
}
