using System;
using System.Collections.Generic;
using System.IO;
using iText.Kernel.Pdf;
using SkiaSharp;
using SixLabors.ImageSharp.Formats.Png;
using SixLabors.ImageSharp.PixelFormats;

namespace OfdrwNet.Image.Decoding;

/// <summary>
/// 通用 PDF 图像解码辅助工具。
/// 保留与旧转换器一致的回退策略，便于在高阶处理管线中复用与测试。
/// </summary>
internal static class ImageDecodeHelper
{
    private static readonly byte[] _transparent1x1Png =
    {
        0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A,
        0x00, 0x00, 0x00, 0x0D, 0x49, 0x48, 0x44, 0x52,
        0x00, 0x00, 0x00, 0x01, 0x00, 0x00, 0x00, 0x01,
        0x08, 0x06, 0x00, 0x00, 0x00, 0x1F, 0x15, 0xC4,
        0x89, 0x00, 0x00, 0x00, 0x0A, 0x49, 0x44, 0x41,
        0x54, 0x78, 0x9C, 0x63, 0x00, 0x01, 0x00, 0x00,
        0x05, 0x00, 0x01, 0x0D, 0x0A, 0x2D, 0xB4, 0x00,
        0x00, 0x00, 0x00, 0x49, 0x45, 0x4E, 0x44, 0xAE,
        0x42, 0x60, 0x82
    };

    public static byte[] GetTransparentPng() => _transparent1x1Png;

    public static List<string> ExtractFilters(PdfStream pdfStream)
    {
        var filters = new List<string>();
        try
        {
            var filterObj = pdfStream.Get(PdfName.Filter);
            switch (filterObj)
            {
                case PdfName name:
                    filters.Add(name.GetValue());
                    break;
                case PdfArray array:
                    foreach (var item in array)
                    {
                        if (item is PdfName nested)
                            filters.Add(nested.GetValue());
                    }
                    break;
            }
        }
        catch
        {
            // 忽略过滤器读取异常，返回已收集的值。
        }

        return filters;
    }

    public static bool TryImageSharp(byte[] raw, out byte[] png)
    {
        try
        {
            using var image = SixLabors.ImageSharp.Image.Load(raw);
            using var ms = new MemoryStream();
            image.Save(ms, new PngEncoder());
            png = ms.ToArray();
            return true;
        }
        catch
        {
            png = Array.Empty<byte>();
            return false;
        }
    }

    public static bool TrySkia(byte[] raw, out byte[] png)
    {
        try
        {
            using var bitmap = SKBitmap.Decode(raw);
            if (bitmap == null)
            {
                png = Array.Empty<byte>();
                return false;
            }

            using var image = SKImage.FromBitmap(bitmap);
            using var data = image.Encode(SKEncodedImageFormat.Png, 100);
            png = data.ToArray();
            return true;
        }
        catch
        {
            png = Array.Empty<byte>();
            return false;
        }
    }

    /// <summary>
    /// 启发式像素重建（Gray/RGB/RGBA/CMYK）。
    /// </summary>
    public static bool TryHeuristicRebuild(PdfStream pdfStream, byte[] raw, out byte[] png)
    {
        try
        {
            int width = pdfStream.GetAsNumber(PdfName.Width)?.IntValue() ?? 0;
            int height = pdfStream.GetAsNumber(PdfName.Height)?.IntValue() ?? 0;
            int bitsPerComponent = pdfStream.GetAsNumber(PdfName.BitsPerComponent)?.IntValue() ?? 8;
            if (bitsPerComponent != 8 || width <= 0 || height <= 0)
            {
                png = Array.Empty<byte>();
                return false;
            }

            int pixels = width * height;
            using var image = new SixLabors.ImageSharp.Image<Rgba32>(width, height);

            if (raw.Length == pixels)
            {
                for (int y = 0; y < height; y++)
                {
                    for (int x = 0; x < width; x++)
                    {
                        byte gray = raw[y * width + x];
                        image[x, y] = new Rgba32(gray, gray, gray, 255);
                    }
                }
            }
            else if (raw.Length == pixels * 3)
            {
                int index = 0;
                for (int y = 0; y < height; y++)
                {
                    for (int x = 0; x < width; x++)
                    {
                        byte r = raw[index++];
                        byte g = raw[index++];
                        byte b = raw[index++];
                        image[x, y] = new Rgba32(r, g, b, 255);
                    }
                }
            }
            else if (raw.Length == pixels * 4)
            {
                int sampleSize = Math.Min(50, pixels);
                int alphaLike = 0;
                for (int i = 0; i < sampleSize; i++)
                {
                    byte alpha = raw[i * 4 + 3];
                    if (alpha == 0 || alpha == 255)
                        alphaLike++;
                }

                bool treatsAsRgba = alphaLike > sampleSize * 0.9;
                int index = 0;

                if (treatsAsRgba)
                {
                    for (int y = 0; y < height; y++)
                    {
                        for (int x = 0; x < width; x++)
                        {
                            byte r = raw[index++];
                            byte g = raw[index++];
                            byte b = raw[index++];
                            byte a = raw[index++];
                            image[x, y] = new Rgba32(r, g, b, a);
                        }
                    }
                }
                else
                {
                    for (int y = 0; y < height; y++)
                    {
                        for (int x = 0; x < width; x++)
                        {
                            byte cByte = raw[index++];
                            byte mByte = raw[index++];
                            byte yByte = raw[index++];
                            byte kByte = raw[index++];

                            double c = cByte / 255.0;
                            double m = mByte / 255.0;
                            double yComponent = yByte / 255.0;
                            double k = kByte / 255.0;

                            byte r = (byte)Math.Clamp(255 * (1 - c) * (1 - k), 0, 255);
                            byte g = (byte)Math.Clamp(255 * (1 - m) * (1 - k), 0, 255);
                            byte b = (byte)Math.Clamp(255 * (1 - yComponent) * (1 - k), 0, 255);

                            image[x, y] = new Rgba32(r, g, b, 255);
                        }
                    }
                }
            }
            else
            {
                png = Array.Empty<byte>();
                return false;
            }

            using var ms = new MemoryStream();
            image.Save(ms, new PngEncoder());
            png = ms.ToArray();
            return true;
        }
        catch
        {
            png = Array.Empty<byte>();
            return false;
        }
    }
}
