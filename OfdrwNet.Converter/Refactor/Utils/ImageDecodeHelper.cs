using System;
using System.Collections.Generic;
using System.IO;
using SkiaSharp;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Formats.Png;

namespace OfdrwNet.Converter.Refactor.Utils;

/// <summary>
/// 通用 PDF 图像解码回退助手
/// 抽取自原 ImageExtractor 中的 JPEG/JPX/启发式/透明占位逻辑，方便复用与测试。
/// </summary>
internal static class ImageDecodeHelper
{
    private static readonly byte[] Transparent1x1Png = new byte[]{0x89,0x50,0x4E,0x47,0x0D,0x0A,0x1A,0x0A,0x00,0x00,0x00,0x0D,0x49,0x48,0x44,0x52,0x00,0x00,0x00,0x01,0x00,0x00,0x00,0x01,0x08,0x06,0x00,0x00,0x00,0x1F,0x15,0xC4,0x89,0x00,0x00,0x00,0x0A,0x49,0x44,0x41,0x54,0x78,0x9C,0x63,0x00,0x01,0x00,0x00,0x05,0x00,0x01,0x0D,0x0A,0x2D,0xB4,0x00,0x00,0x00,0x00,0x49,0x45,0x4E,0x44,0xAE,0x42,0x60,0x82};

    public static byte[] GetTransparentPng() => Transparent1x1Png;

    public static List<string> ExtractFilters(iText.Kernel.Pdf.PdfStream pdfStream)
    {
        var result = new List<string>();
        try
        {
            var filterObj = pdfStream.Get(iText.Kernel.Pdf.PdfName.Filter);
            if (filterObj is iText.Kernel.Pdf.PdfName fn) result.Add(fn.GetValue());
            else if (filterObj is iText.Kernel.Pdf.PdfArray arr)
                foreach (var f in arr) if (f is iText.Kernel.Pdf.PdfName fn2) result.Add(fn2.GetValue());
        }
        catch { }
        return result;
    }

    public static bool TryImageSharp(byte[] raw, out byte[] png)
    { try { using var img = Image.Load(raw); using var ms = new MemoryStream(); img.Save(ms, new PngEncoder()); png = ms.ToArray(); return true; } catch { png = Array.Empty<byte>(); return false; } }
    public static bool TrySkia(byte[] raw, out byte[] png)
    { try { using var sk = SKBitmap.Decode(raw); if (sk == null) { png = Array.Empty<byte>(); return false; } using var img = SKImage.FromBitmap(sk); using var data = img.Encode(SKEncodedImageFormat.Png, 100); png = data.ToArray(); return true; } catch { png = Array.Empty<byte>(); return false; } }

    /// <summary>
    /// 启发式像素重建（Gray/RGB/RGBA/CMYK）
    /// </summary>
    public static bool TryHeuristicRebuild(iText.Kernel.Pdf.PdfStream pdfStream, byte[] raw, out byte[] png)
    {
        try
        {
            int w = pdfStream.GetAsNumber(iText.Kernel.Pdf.PdfName.Width)?.IntValue() ?? 0;
            int h = pdfStream.GetAsNumber(iText.Kernel.Pdf.PdfName.Height)?.IntValue() ?? 0;
            int bpc = pdfStream.GetAsNumber(iText.Kernel.Pdf.PdfName.BitsPerComponent)?.IntValue() ?? 8;
            if (bpc != 8 || w <= 0 || h <= 0) { png = Array.Empty<byte>(); return false; }
            int pixels = w * h;
            using var img = new SixLabors.ImageSharp.Image<SixLabors.ImageSharp.PixelFormats.Rgba32>(w, h);
            if (raw.Length == pixels) // Gray
            {
                for (int y = 0; y < h; y++) for (int x = 0; x < w; x++) { byte g = raw[y * w + x]; img[x, y] = new SixLabors.ImageSharp.PixelFormats.Rgba32(g, g, g, 255); }
            }
            else if (raw.Length == pixels * 3) // RGB
            {
                int idx = 0; for (int y = 0; y < h; y++) for (int x = 0; x < w; x++) { byte r = raw[idx++]; byte g = raw[idx++]; byte b = raw[idx++]; img[x, y] = new SixLabors.ImageSharp.PixelFormats.Rgba32(r, g, b, 255); }
            }
            else if (raw.Length == pixels * 4) // RGBA or CMYK
            {
                int sample = Math.Min(50, pixels); int alphaLike = 0; for (int s = 0; s < sample; s++) { byte a = raw[s * 4 + 3]; if (a == 0 || a == 255) alphaLike++; }
                bool rgba = alphaLike > sample * 0.9;
                int idx = 0;
                if (rgba)
                {
                    for (int y = 0; y < h; y++) for (int x = 0; x < w; x++) { byte r = raw[idx++]; byte g = raw[idx++]; byte b = raw[idx++]; byte a = raw[idx++]; img[x, y] = new SixLabors.ImageSharp.PixelFormats.Rgba32(r, g, b, a); }
                }
                else // CMYK -> RGB
                {
                    for (int y = 0; y < h; y++) for (int x = 0; x < w; x++) { byte Cb = raw[idx++]; byte Mb = raw[idx++]; byte Yb = raw[idx++]; byte Kb = raw[idx++]; double C = Cb / 255.0; double M = Mb / 255.0; double Y = Yb / 255.0; double K = Kb / 255.0; byte r = (byte)Math.Clamp(255 * (1 - C) * (1 - K), 0, 255); byte g = (byte)Math.Clamp(255 * (1 - M) * (1 - K), 0, 255); byte b = (byte)Math.Clamp(255 * (1 - Y) * (1 - K), 0, 255); img[x, y] = new SixLabors.ImageSharp.PixelFormats.Rgba32(r, g, b, 255); }
                }
            }
            else { png = Array.Empty<byte>(); return false; }
            using var ms = new MemoryStream(); img.Save(ms, new PngEncoder()); png = ms.ToArray(); return true;
        }
        catch { png = Array.Empty<byte>(); return false; }
    }
}
