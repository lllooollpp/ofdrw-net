using OfdrwNet.Core.Diagnostics;
using OfdrwNet.Core.Resources;

namespace OfdrwNet.Font.Embedding;

/// <summary>
/// Font embedding service with GB18030 support
/// </summary>
public class FontEmbeddingService
{
    private readonly IStructuredLogger? _logger;

    public FontEmbeddingService(IStructuredLogger? logger = null)
    {
        _logger = logger;
    }

    /// <summary>
    /// Embed font with GB18030 completeness check
    /// </summary>
    public FontResource EmbedFont(string fontPath, string fontName)
    {
        _logger?.LogInfo(LogEvents.ResourceEmbedded, new
        {
            action = "embed_font",
            font = fontName,
            path = fontPath
        });

        // Placeholder: actual font subsetting & GB18030 check in resource phase
        return new FontResource(
            fontName,
            fileRef: fontPath,
            isEmbedded: true,
            supportsGb18030: CheckGB18030Support(fontPath));
    }

    private bool CheckGB18030Support(string fontPath)
    {
        // Placeholder: actual GB18030 glyph coverage check
        return fontPath.Contains("SimSun", StringComparison.OrdinalIgnoreCase) ||
               fontPath.Contains("SimHei", StringComparison.OrdinalIgnoreCase);
    }
}
