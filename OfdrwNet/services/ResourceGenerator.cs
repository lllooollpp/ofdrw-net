using System.Xml.Linq;
using Microsoft.Extensions.Logging;
using OfdrwNet.Models;
using OfdrwNet.Utils;
using OfdrwNet.Font;
using RawImage = OfdrwNet.Image.RawImage;
using OfdrwNet.Image;

namespace OfdrwNet.Services;

/// <summary>
/// 负责 PublicRes.xml 与 DocumentRes.xml 的生成。
/// </summary>
internal sealed class ResourceGenerator
{
    private readonly ILogger? _logger;
    private readonly XNamespace _ofdNs = "http://www.ofdspec.org/2016";

    private readonly OfdImagePreprocessor _preprocessor;

    public ResourceGenerator(ILogger? logger, OfdImagePreprocessor? preprocessor = null)
    {
        _logger = logger;
        _preprocessor = preprocessor ?? new OfdImagePreprocessor();
    }

    public async Task<ResourceGenerationResult> GenerateAsync(
        string docDir,
        IDictionary<string, OfdFont> fontMap,
        IDictionary<string,string> externalFontFiles,
        IList<RawImage> rawImages)
    {
        _logger?.LogInformation("[ResourceGenerator] 开始生成资源文件 DocDir={DocDir}", docDir);
        var resDir = Path.Combine(docDir, "Res");
        if (!Directory.Exists(resDir)) Directory.CreateDirectory(resDir);
        var publicResRel = "PublicRes.xml";
        var documentResRel = "DocumentRes.xml";

        var nsDecl = new XAttribute(XNamespace.Xmlns + "ofd", _ofdNs.NamespaceName);
        var publicRes = new XElement(_ofdNs + "Res", nsDecl, new XAttribute("BaseLoc", "Res"));
        var fontsElement = new XElement(_ofdNs + "Fonts");
        foreach (var font in fontMap.Values)
        {
            XElement fontEl;
            if (externalFontFiles.TryGetValue(font.FontName, out var fontFile))
            {
                var destFontFile = Path.Combine(resDir, Path.GetFileName(fontFile));
                File.Copy(fontFile, destFontFile, true);
                fontEl = new XElement(_ofdNs + "Font",
                    new XAttribute("ID", font.ID),
                    new XAttribute("FamilyName", font.FamilyName),
                    new XAttribute("FontName", font.FontName),
                    new XAttribute("Serif", "true"),
                    new XElement(_ofdNs + "FontFile", Path.GetFileName(fontFile))
                );
            }
            else
            {
                fontEl = new XElement(_ofdNs + "Font",
                    new XAttribute("ID", font.ID),
                    new XAttribute("FamilyName", font.FamilyName),
                    new XAttribute("FontName", font.FontName)
                );
            }
            fontsElement.Add(fontEl);
        }
        publicRes.Add(fontsElement);
        await FileUtil.WriteTextFileUtf8LfAsync(Path.Combine(docDir, publicResRel), "<?xml version=\"1.0\" encoding=\"utf-8\"?>\n" + publicRes.ToString(SaveOptions.None));
        _logger?.LogInformation("[ResourceGenerator] PublicRes.xml 生成完成 FontCount={FontCount}", fontMap.Count);

        var documentRes = new XElement(_ofdNs + "Res", nsDecl, new XAttribute("BaseLoc", "Res"));
        var multiMedias = new XElement(_ofdNs + "MultiMedias");

        if (rawImages.Any())
        {
            // 分配 ResourceID
            int nextId = (fontMap.Values.Any() ? fontMap.Values.Max(f => f.ID) : 0) + 1;
            foreach (var first in rawImages.Where(r => r.IsFirstResource)) if (first.ResourceID == 0) first.ResourceID = nextId++;
            var map = rawImages.Where(r => r.IsFirstResource).ToDictionary(r => r.Hash, r => r.ResourceID);
            foreach (var dup in rawImages.Where(r => !r.IsFirstResource)) if (map.TryGetValue(dup.Hash, out var rid)) dup.ResourceID = rid; else if (dup.ResourceID == 0) dup.ResourceID = nextId++;

            foreach (var img in rawImages.Where(r => r.IsFirstResource).OrderBy(i => i.ResourceID))
            {
                // 预处理（可能转换格式、平铺Alpha）
                var processed = _preprocessor.Process(img.Data);
                var fmt = (string.IsNullOrWhiteSpace(img.Format) ? processed.FormatExt : img.Format!.TrimStart('.')) ?? processed.FormatExt;
                // 如果预处理改变了格式则以预处理为准
                fmt = processed.FormatExt;
                var fmtUpper = fmt.ToUpperInvariant();
                if (fmtUpper == "JPEG") fmtUpper = "JPG";
                var fileName = $"Image_{img.ResourceID}.{fmtUpper}";
                var dest = Path.Combine(resDir, fileName);
                if (processed.Data.Length > 0)
                {
                    await File.WriteAllBytesAsync(dest, processed.Data);
                    _logger?.LogDebug("[ResourceGenerator] 写入图像资源 Path={Path} ResourceID={Rid} Format={Fmt} Preprocessed={Pre}", dest, img.ResourceID, fmtUpper, processed.FormatExt);
                }
                multiMedias.Add(new XElement(_ofdNs + "MultiMedia",
                    new XAttribute("ID", img.ResourceID),
                    new XAttribute("Type", "Image"),
                    new XAttribute("Format", fmtUpper),
                    new XElement(_ofdNs + "MediaFile", fileName)));
            }
        }
        documentRes.Add(multiMedias);
        await FileUtil.WriteTextFileUtf8LfAsync(Path.Combine(docDir, documentResRel), "<?xml version=\"1.0\" encoding=\"utf-8\"?>\n" + documentRes.ToString(SaveOptions.None));
        _logger?.LogInformation("[ResourceGenerator] DocumentRes.xml 生成完成 ImageResCount={Cnt}", multiMedias.Elements().Count());

        var maxFont = fontMap.Values.Any() ? fontMap.Values.Max(f => f.ID) : 0;
        var maxImg = rawImages.Any() ? rawImages.Max(i => i.ResourceID) : 0;
        int maxUnit = Math.Max(maxFont, maxImg);

        return new ResourceGenerationResult
        {
            PublicResRelativePath = publicResRel,
            DocumentResRelativePath = documentResRel,
            MaxUnitIdAfterResources = maxUnit
        };
    }
}
