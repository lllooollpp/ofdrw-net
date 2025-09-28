using System;
using System.IO;
using System.Linq;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Formats;
using SixLabors.ImageSharp.Formats.Png;
using SixLabors.ImageSharp.Formats.Tiff;
using SixLabors.ImageSharp.PixelFormats;
using SixLabors.ImageSharp.Processing;

namespace OfdrwNet.Services;

/// <summary>
/// 对输入的原始图像字节进行预处理：
/// 1. 识别格式（TIFF/JPEG/PNG/GIF/BMP 等）
/// 2. 对不受支持或需要统一的格式（如 TIFF、多帧、带Alpha的非PNG）转换为 PNG
/// 3. 选项：去除 Alpha（平铺背景）
/// </summary>
internal class ImagePreprocessor
{
    private readonly bool _flattenAlpha;
    private readonly string _flattenColorHex;

    /// <summary>
    ///
    /// </summary>
    /// <param name="flattenAlpha">是否将含Alpha通道的图片转成不含透明的PNG（背景色填充）</param>
    /// <param name="flattenColorHex">填充背景色（HEX #RRGGBB）</param>
    public ImagePreprocessor(bool flattenAlpha = false, string flattenColorHex = "#FFFFFF")
    {
        _flattenAlpha = flattenAlpha;
        _flattenColorHex = flattenColorHex;
    }

    /// <summary>
    /// 预处理结果
    /// </summary>
    public readonly record struct Result(byte[] Data, string FormatExt /* 不含点，如 png */);

    /// <summary>
    /// 处理一张图片
    /// </summary>
    /// <param name="rawBytes">原始字节</param>
    /// <returns>转换后的字节与格式扩展名</returns>
    public Result Process(byte[] rawBytes)
    {
        try
        {
            using var msLoad = new MemoryStream(rawBytes, writable: false);
            IImageFormat? detectedFormat = Image.DetectFormat(msLoad);
            msLoad.Position = 0;
            using var image = Image.Load(msLoad); // 原格式加载（保持所有帧）
            var needConvert = false;
            var targetExt = detectedFormat?.FileExtensions.FirstOrDefault() ?? "dat";

            // TIFF、多帧、含 alpha 而非 png 时进行转换
            if (string.Equals(detectedFormat?.Name, "TIFF", StringComparison.OrdinalIgnoreCase))
            {
                needConvert = true;
                targetExt = "png";
            }
            else if (image.Frames?.Count > 1)
            {
                needConvert = true;
                targetExt = "png"; // 只取第一帧
            }
            else if (_flattenAlpha && HasAlpha(image) && !string.Equals(detectedFormat?.Name, "PNG", StringComparison.OrdinalIgnoreCase))
            {
                needConvert = true;
                targetExt = "png";
            }

            if (!needConvert && !_flattenAlpha)
            {
                // 保留原扩展（若识别出 tiff 则返回 tiff）
                return new Result(rawBytes, targetExt);
            }

            // 如需去Alpha, 平铺背景
            if ((_flattenAlpha && HasAlpha(image)) || needConvert)
            {
                // 统一流程：转成 RGBA，再选择是否平铺 Alpha，最终写 PNG
                using var rgba = image.CloneAs<Rgba32>();
                if (_flattenAlpha && HasAlpha(rgba))
                {
                    var bg = ParseColor(_flattenColorHex);
                    // 遍历像素使用 CopyPixelDataTo
                    var pixelData = new Rgba32[rgba.Width * rgba.Height];
                    rgba.CopyPixelDataTo(pixelData);
                    for (int i = 0; i < pixelData.Length; i++)
                    {
                        var px = pixelData[i];
                        if (px.A < 255)
                        {
                            float a = px.A / 255f;
                            px.R = (byte)(px.R * a + bg.R * (1 - a));
                            px.G = (byte)(px.G * a + bg.G * (1 - a));
                            px.B = (byte)(px.B * a + bg.B * (1 - a));
                            px.A = 255;
                            pixelData[i] = px;
                        }
                    }
                    // 写回
                    rgba.ProcessPixelRows(accessor =>
                    {
                        int idx = 0;
                        for (int y = 0; y < accessor.Height; y++)
                        {
                            var rowSpan = accessor.GetRowSpan(y);
                            for (int x = 0; x < rowSpan.Length; x++)
                            {
                                rowSpan[x] = pixelData[idx++];
                            }
                        }
                    });
                }
                return EncodePng(rgba);
            }

            return new Result(rawBytes, targetExt);
        }
        catch
        {
            // 解析失败，返回原始
            return new Result(rawBytes, "dat");
        }
    }

    private static bool HasAlpha(Image image)
    {
        // 简单检测：尝试转换为 Rgba32 后扫描是否存在 A<255
        try
        {
            using var rgba = image.CloneAs<Rgba32>();
            bool has = false;
            rgba.ProcessPixelRows(accessor =>
            {
                for (int y = 0; y < accessor.Height && !has; y++)
                {
                    var row = accessor.GetRowSpan(y);
                    for (int x = 0; x < row.Length; x++)
                    {
                        if (row[x].A < 255) { has = true; break; }
                    }
                }
            });
            return has;
        }
        catch
        {
            return false;
        }
    }

    private static Rgba32 ParseColor(string hex)
    {
        if (hex.StartsWith('#')) hex = hex[1..];
        if (hex.Length == 6)
        {
            var r = Convert.ToByte(hex.Substring(0, 2), 16);
            var g = Convert.ToByte(hex.Substring(2, 2), 16);
            var b = Convert.ToByte(hex.Substring(4, 2), 16);
            return new Rgba32(r, g, b, 255);
        }
        return new Rgba32(255, 255, 255, 255);
    }

    private static Result EncodePng(Image<Rgba32> img)
    {
        using var ms = new MemoryStream();
        var encoder = new PngEncoder
        {
            ColorType = PngColorType.Rgb,
            BitDepth = PngBitDepth.Bit8,
            CompressionLevel = PngCompressionLevel.BestSpeed
        };
        img.Save(ms, encoder);
        return new Result(ms.ToArray(), "png");
    }
}
