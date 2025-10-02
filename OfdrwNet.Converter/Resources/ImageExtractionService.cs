using System;
using System.Drawing;
using OfdrwNet.Core.Diagnostics;
using OfdrwNet.Core.Resources;

namespace OfdrwNet.Converter.Resources;

/// <summary>
/// Image extraction service with DPI limiting
/// </summary>
public class ImageExtractionService
{
    private readonly IStructuredLogger? _logger;
    private readonly int _maxDpi;

    public ImageExtractionService(int maxDpi = 300, IStructuredLogger? logger = null)
    {
        _maxDpi = maxDpi;
        _logger = logger;
    }

    /// <summary>
    /// Extract image metadata with DPI check
    /// </summary>
    /// <param name="imageData">Raw image bytes</param>
    /// <param name="originalDpi">Original DPI value (X/Y)</param>
    /// <param name="pixelSize">Known pixel dimensions, optional</param>
    /// <param name="colorSpace">Detected color space, defaults to unknown</param>
    /// <param name="fileRef">Optional file reference inside package</param>
    /// <returns>Image resource metadata</returns>
    /// <exception cref="ArgumentNullException">When <paramref name="imageData"/> is null</exception>
    /// <exception cref="ArgumentOutOfRangeException">When <paramref name="originalDpi"/> is not positive</exception>
    public ImageResource ExtractImage(byte[] imageData, int originalDpi, Size? pixelSize = null, string colorSpace = "unknown", string? fileRef = null)
    {
        ArgumentNullException.ThrowIfNull(imageData);

        if (originalDpi <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(originalDpi));
        }

        var format = DetectFormat(imageData);
        var normalizedColorSpace = string.IsNullOrWhiteSpace(colorSpace) ? "unknown" : colorSpace;

        var resource = new ImageResource(
            id: $"img_{Guid.NewGuid():N}",
            format: format,
            pixelSize: pixelSize ?? Size.Empty,
            dpiX: originalDpi,
            dpiY: originalDpi,
            colorSpace: normalizedColorSpace,
            fileRef: fileRef,
            lengthBytes: imageData.LongLength);

        var wasDownsampled = false;

        if (originalDpi > _maxDpi)
        {
            _logger?.LogWarn(LogEvents.ResourceEmbedded, new
            {
                action = "dpi_exceeded",
                originalDpi,
                maxDpi = _maxDpi
            });

            // Placeholder: actual downsampling
            resource = resource.WithDpi(_maxDpi, _maxDpi);
            wasDownsampled = true;
        }

        _logger?.LogInfo(LogEvents.ResourceEmbedded, new
        {
            action = "image_extracted",
            resource.Id,
            resource.Format,
            dpiX = resource.DpiX,
            dpiY = resource.DpiY,
            resource.ColorSpace,
            bytes = resource.LengthBytes,
            downsampled = wasDownsampled
        });

        return resource;
    }

    private string DetectFormat(byte[] data)
    {
        if (data.Length < 4) return "unknown";

        // JPEG
        if (data[0] == 0xFF && data[1] == 0xD8) return "jpeg";
        // PNG
        if (data[0] == 0x89 && data[1] == 0x50 && data[2] == 0x4E && data[3] == 0x47) return "png";
        // JBIG2
        if (data[0] == 0x97 && data[1] == 0x4A && data[2] == 0x42 && data[3] == 0x32) return "jbig2";

        return "unknown";
    }
}
